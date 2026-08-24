using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.AppUI.MVVM;


#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace eu.foodmission.platform
{
    /// <summary>
    /// Unified notification service handling local notifications (com.unity.mobile.notifications)
    /// and remote push notifications (Firebase Cloud Messaging) with local persistence.
    /// </summary>
    public class NotificationService : INotificationService, IDisposable
    {
        private const string STORAGE_KEY_PUSH_REG = "device_push_registration";
        private const string STORAGE_KEY_PROMPTED = "notification_permission_prompted";
        private const string STORAGE_KEY_LAST_TOKEN = "last_fcm_token";

        private readonly IStoreService _storeService;
        private readonly ILocalStorageService _localStorageService;
        private readonly IAuthService _authService;

        private DevicePushRegistration _currentPushRegistration;
        private bool _isInitialized = false;

        public event Action<NotificationPayload> OnNotificationOpened;
        public event Action<NotificationModel> OnNotificationReceived;

        public NotificationService(IStoreService storeService, ILocalStorageService localStorageService, IAuthService authService = null)
        {
            _storeService = storeService;
            _localStorageService = localStorageService;
            _authService = authService;
        }

        private IAuthService GetAuthService()
        {
            return _authService ?? App.current?.services?.GetService<IAuthService>();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            // 1. Restore local push registration data
            EnsurePushRegistrationLoaded();

            // Sync with Redux state
            _storeService.store?.Dispatch(AppActions.setDevicePushRegistration.Invoke(_currentPushRegistration));

            // 2. Setup native notification channels (Android channels without prompting permissions)
            SetupNativeChannels();

            // 3. Only initialize Firebase Messaging if notifications are explicitly enabled AND we already have an active registration
            if (AreNotificationsEnabled() && !string.IsNullOrEmpty(_currentPushRegistration.token))
            {
                await InitializeFirebaseMessagingAsync();
            }

            // 4. Check if the app was launched by tapping a notification
            CheckLaunchNotification();
        }

        public bool ShouldPromptForNotifications()
        {
            AppState state = _storeService.store?.GetState();
            if (state == null || string.IsNullOrEmpty(state.accessToken))
            {
                return false;
            }

            bool alreadyPrompted = _localStorageService.GetValue<bool>(STORAGE_KEY_PROMPTED, false);
            return !alreadyPrompted;
        }

        public async Task<bool> AcceptNotificationsAsync()
        {
            _localStorageService.SetValue(STORAGE_KEY_PROMPTED, true);
            bool granted = await RequestPermissionsAsync();
            SetNotificationsEnabled(granted);
            GetAuthService()?.SyncSettingsAsync();
            return granted;
        }

        public void DeclineNotifications()
        {
            _localStorageService.SetValue(STORAGE_KEY_PROMPTED, true);
            SetNotificationsEnabled(false);
            GetAuthService()?.SyncSettingsAsync();
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            bool granted = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                granted = true;
            }
            else
            {
                var callbacks = new PermissionCallbacks();
                var tcs = new TaskCompletionSource<bool>();

                callbacks.PermissionGranted += _ => tcs.TrySetResult(true);
                callbacks.PermissionDenied += _ => tcs.TrySetResult(false);
                callbacks.PermissionDeniedAndDontAskAgain += _ => tcs.TrySetResult(false);

                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS", callbacks);
                granted = await tcs.Task;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true))
            {
                while (!req.IsFinished)
                {
                    await Task.Yield();
                }
                granted = req.Granted;
            }
#else
            await Task.Yield();
            granted = true;
#endif

            SetNotificationsEnabled(granted);
            return granted;
        }

        public bool AreNotificationsEnabled()
        {
            return _storeService.store?.GetState()?.pushNotificationsEnabled ?? false;
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            _storeService.store?.Dispatch(AppActions.setPushNotifications.Invoke(enabled));

            var reg = EnsurePushRegistrationLoaded();
            reg.notificationsEnabled = enabled;

            if (!enabled)
            {
                // Discard active registration token in Redux and server, and cancel all local scheduled notifications
                reg.token = "";
                reg.registeredAt = "";
                CancelAllNotifications();
            }
            else
            {
                // Restore cached token if available
                if (string.IsNullOrEmpty(reg.token))
                {
                    string cachedToken = _localStorageService.GetValue<string>(STORAGE_KEY_LAST_TOKEN, "");
                    if (!string.IsNullOrEmpty(cachedToken))
                    {
                        reg.token = cachedToken;
                        reg.registeredAt = DateTime.UtcNow.ToString("o");
                    }
                }
                _ = InitializeFirebaseMessagingAsync();
            }

            _localStorageService.SetValue(STORAGE_KEY_PUSH_REG, reg);
            _storeService.store?.Dispatch(AppActions.setDevicePushRegistration.Invoke(reg));
        }

        public void SchedulePantryExpiryReminder(string itemId, string itemName, DateTime expiryDate)
        {
            if (!AreNotificationsEnabled() || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            TimeSpan preferredTime = TimeSpan.FromHours(10);
            string timeStr = _storeService.store?.GetState()?.notificationPreferredTime;
            if (!string.IsNullOrEmpty(timeStr) && TimeSpan.TryParse(timeStr, out var ts))
            {
                preferredTime = ts;
            }

            DateTime now = DateTime.Now;
            DateTime reminderTime = expiryDate.Date.Add(preferredTime).AddDays(-2); // 48h before at preferred hour

            if (reminderTime <= now)
            {
                reminderTime = expiryDate.Date.Add(preferredTime).AddDays(-1); // 24h before at preferred hour
            }

            if (reminderTime <= now)
            {
                return; // Already in the past
            }

            string notifId = $"pantry_{itemId}";
            string title = "Aviso de despensa";
            string body = $"Tu producto '{itemName}' caduca pronto. ¡Aprovecha para consumirlo!";

            CancelNotification(notifId);
            ScheduleNativeNotification(notifId, NotificationChannels.PantryExpiryId, title, body, reminderTime, "go_to_pantry", itemId);
        }

        public void CancelPantryReminder(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            CancelNotification($"pantry_{itemId}");
        }

        public void ScheduleLocalNotification(string id, string title, string body, DateTime deliveryTime, string channelId = null, string action = null, string targetId = null)
        {
            if (!AreNotificationsEnabled())
            {
                return;
            }

            string channel = !string.IsNullOrEmpty(channelId) ? channelId : NotificationChannels.DailyRemindersId;
            ScheduleNativeNotification(id, channel, title, body, deliveryTime, action ?? "", targetId ?? "");
        }

        public void ScheduleLocalNotificationInSeconds(string id, string title, string body, int secondsDelay, string channelId = null, string action = null, string targetId = null)
        {
            DateTime fireTime = DateTime.UtcNow.AddSeconds(Math.Max(1, secondsDelay));
            ScheduleLocalNotification(id, title, body, fireTime, channelId, action, targetId);
        }

        public void ScheduleDailyMealReminder(TimeSpan preferredTime)
        {
            if (!AreNotificationsEnabled())
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime target = now.Date.Add(preferredTime);
            if (target <= now)
            {
                target = target.AddDays(1);
            }

            string notifId = "daily_meal_reminder";
            string title = "¿Qué has comido hoy?";
            string body = "No olvides registrar tus comidas en Foodmission para mantener tus hábitos al día.";

            CancelNotification(notifId);
            ScheduleNativeNotification(notifId, NotificationChannels.DailyRemindersId, title, body, target, "go_to_meal_log", "");
        }

        public void RescheduleAllNotifications(TimeSpan preferredTime)
        {
            if (!AreNotificationsEnabled())
            {
                return;
            }

            // 1. Reschedule daily meal reminder
            ScheduleDailyMealReminder(preferredTime);

            // 2. Reschedule any cached pantry expiry items
            var cachedPantry = _localStorageService.GetValue<PantryItemArrayWrapper>("pantry_cache");
            if (cachedPantry?.items != null)
            {
                foreach (var item in cachedPantry.items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.expiryDate) && DateTime.TryParse(item.expiryDate, out DateTime expDate))
                    {
                        CancelPantryReminder(item.id);
                        string itemName = item.foodProduct?.name ?? item.genericFood?.foodName ?? "Producto";
                        SchedulePantryExpiryReminder(item.id, itemName, expDate);
                    }
                }
            }
        }

        public void CancelNotification(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            int androidId = Math.Abs(notificationId.GetHashCode());
            AndroidNotificationCenter.CancelNotification(androidId);
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveScheduledNotification(notificationId);
            iOSNotificationCenter.RemoveDeliveredNotification(notificationId);
#endif
        }

        public void CancelAllNotifications()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }

        public DevicePushRegistration GetDevicePushRegistration()
        {
            return EnsurePushRegistrationLoaded();
        }

        private DevicePushRegistration EnsurePushRegistrationLoaded()
        {
            if (_currentPushRegistration == null)
            {
                _currentPushRegistration = _localStorageService.GetValue<DevicePushRegistration>(STORAGE_KEY_PUSH_REG);
                if (_currentPushRegistration == null)
                {
                    _currentPushRegistration = new DevicePushRegistration
                    {
                        platform = GetCurrentPlatformName(),
                        deviceModel = SystemInfo.deviceModel,
                        appVersion = Application.version,
                        notificationsEnabled = _storeService.store?.GetState()?.pushNotificationsEnabled ?? false
                    };
                }

                if (string.IsNullOrEmpty(_currentPushRegistration.token) && _currentPushRegistration.notificationsEnabled)
                {
                    string cachedToken = _localStorageService.GetValue<string>(STORAGE_KEY_LAST_TOKEN, "");
                    if (!string.IsNullOrEmpty(cachedToken))
                    {
                        _currentPushRegistration.token = cachedToken;
                        _currentPushRegistration.registeredAt = DateTime.UtcNow.ToString("o");
                    }
                }
            }
            return _currentPushRegistration;
        }

        private void SetupNativeChannels()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var pantryChannel = new AndroidNotificationChannel
            {
                Id = NotificationChannels.PantryExpiryId,
                Name = NotificationChannels.PantryExpiryName,
                Description = NotificationChannels.PantryExpiryDescription,
                Importance = Importance.High,
                CanBypassDnd = false,
                CanShowBadge = true,
                EnableLights = true,
                EnableVibration = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(pantryChannel);

            var remindersChannel = new AndroidNotificationChannel
            {
                Id = NotificationChannels.DailyRemindersId,
                Name = NotificationChannels.DailyRemindersName,
                Description = NotificationChannels.DailyRemindersDescription,
                Importance = Importance.Default,
                CanShowBadge = true,
                EnableLights = true,
                EnableVibration = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(remindersChannel);

            var gamificationChannel = new AndroidNotificationChannel
            {
                Id = NotificationChannels.GamificationId,
                Name = NotificationChannels.GamificationName,
                Description = NotificationChannels.GamificationDescription,
                Importance = Importance.Default,
                CanShowBadge = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(gamificationChannel);
#endif
        }

        private void ScheduleNativeNotification(string id, string channelId, string title, string body, DateTime deliveryTime, string action, string targetId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = deliveryTime,
                SmallIcon = "icon_small",
                LargeIcon = "icon_large",
                IntentData = $"{{\"action\":\"{action}\",\"targetId\":\"{targetId}\",\"id\":\"{id}\"}}"
            };
            int androidId = Math.Abs(id.GetHashCode());
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, androidId);
#elif UNITY_IOS && !UNITY_EDITOR
            var timeSpan = deliveryTime.ToUniversalTime() - DateTime.UtcNow;
            if (timeSpan.TotalSeconds <= 0) timeSpan = TimeSpan.FromSeconds(1);

            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = timeSpan,
                Repeats = false
            };

            var notification = new iOSNotification
            {
                Identifier = id,
                Title = title,
                Body = body,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge),
                CategoryIdentifier = channelId,
                Trigger = trigger,
                Data = $"{{\"action\":\"{action}\",\"targetId\":\"{targetId}\",\"id\":\"{id}\"}}"
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#else
            Debug.Log($"[NotificationService] Scheduled mock notification '{id}' for {deliveryTime:s}: {title} - {body}");
#endif
        }

        private async Task InitializeFirebaseMessagingAsync()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            try
            {
                var dependencyStatus = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    Firebase.Messaging.FirebaseMessaging.TokenReceived -= OnFirebaseTokenReceived;
                    Firebase.Messaging.FirebaseMessaging.TokenReceived += OnFirebaseTokenReceived;
                    Firebase.Messaging.FirebaseMessaging.MessageReceived -= OnFirebaseMessageReceived;
                    Firebase.Messaging.FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
                    Debug.Log("[NotificationService] Firebase Messaging initialized successfully.");

                    try
                    {
                        string currentToken = await Firebase.Messaging.FirebaseMessaging.GetTokenAsync();
                        if (!string.IsNullOrEmpty(currentToken))
                        {
                            UpdatePushRegistrationToken(currentToken);
                        }
                    }
                    catch (Exception tokenEx)
                    {
                        Debug.LogWarning($"[NotificationService] GetTokenAsync: {tokenEx.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NotificationService] Could not resolve Firebase dependencies: {dependencyStatus}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationService] Firebase init exception: {ex.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        private void OnFirebaseTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Token)) return;

            Debug.Log($"[NotificationService] Received FCM Registration Token: {e.Token}");
            UpdatePushRegistrationToken(e.Token);
        }

        private void UpdatePushRegistrationToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            _localStorageService.SetValue(STORAGE_KEY_LAST_TOKEN, token);

            var reg = EnsurePushRegistrationLoaded();
            reg.token = token;
            reg.platform = GetCurrentPlatformName();
            reg.deviceModel = SystemInfo.deviceModel;
            reg.appVersion = Application.version;
            reg.registeredAt = DateTime.UtcNow.ToString("o");
            reg.notificationsEnabled = true;

            _localStorageService.SetValue(STORAGE_KEY_PUSH_REG, reg);
            _storeService.store?.Dispatch(AppActions.setDevicePushRegistration.Invoke(reg));

            // Sync settings with server so device registration is saved in user profile settings
            GetAuthService()?.SyncSettingsAsync();
        }

        private void OnFirebaseMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
        {
            if (e?.Message == null) return;

            string title = e.Message.Notification?.Title ?? "";
            string body = e.Message.Notification?.Body ?? "";
            string action = "";
            string targetId = "";

            if (e.Message.Data != null)
            {
                e.Message.Data.TryGetValue("action", out action);
                e.Message.Data.TryGetValue("targetId", out targetId);
            }

            var model = new NotificationModel
            {
                Id = e.Message.MessageId ?? Guid.NewGuid().ToString(),
                Text = !string.IsNullOrEmpty(title) ? $"{title}: {body}" : body,
                Timestamp = "Just now",
                Type = NotificationType.System,
                IsRead = false
            };

            OnNotificationReceived?.Invoke(model);
        }
#endif

        private void CheckLaunchNotification()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var lastNotification = AndroidNotificationCenter.GetLastNotificationIntent();
            if (lastNotification != null && !string.IsNullOrEmpty(lastNotification.Notification.IntentData))
            {
                try
                {
                    var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<NotificationPayload>(lastNotification.Notification.IntentData);
                    if (payload != null)
                    {
                        OnNotificationOpened?.Invoke(payload);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NotificationService] Failed to parse Android launch intent: {ex.Message}");
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            var lastNotification = iOSNotificationCenter.GetLastRespondedNotification();
            if (lastNotification != null && !string.IsNullOrEmpty(lastNotification.Data))
            {
                try
                {
                    var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<NotificationPayload>(lastNotification.Data);
                    if (payload != null)
                    {
                        OnNotificationOpened?.Invoke(payload);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NotificationService] Failed to parse iOS launch intent: {ex.Message}");
                }
            }
#endif
        }

        private static string GetCurrentPlatformName()
        {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#else
            return "editor";
#endif
        }

        public void Dispose()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            try
            {
                Firebase.Messaging.FirebaseMessaging.TokenReceived -= OnFirebaseTokenReceived;
                Firebase.Messaging.FirebaseMessaging.MessageReceived -= OnFirebaseMessageReceived;
            }
            catch { }
#endif
        }
    }
}
