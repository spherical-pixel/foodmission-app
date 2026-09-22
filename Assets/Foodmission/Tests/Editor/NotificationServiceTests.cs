using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private TestStoreService _storeService;
        private TestLocalStorageService _localStorageService;
        private NotificationService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _localStorageService = new TestLocalStorageService();
            _service = new NotificationService(_storeService, _localStorageService);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void NotificationChannels_DefinesExpectedChannels()
        {
            Assert.AreEqual("foodmission_pantry", NotificationChannels.PantryExpiryId);
            Assert.AreEqual("foodmission_reminders", NotificationChannels.DailyRemindersId);
            Assert.AreEqual("foodmission_gamification", NotificationChannels.GamificationId);

            Assert.IsNotEmpty(NotificationChannels.PantryExpiryName);
            Assert.IsNotEmpty(NotificationChannels.DailyRemindersName);
            Assert.IsNotEmpty(NotificationChannels.GamificationName);
        }

        [Test]
        public void DevicePushRegistration_SerializationAndCopy_WorkCorrectly()
        {
            var reg = new DevicePushRegistration
            {
                token = "fcm_test_token_123",
                platform = "android",
                deviceModel = "Pixel 8",
                appVersion = "1.0.0",
                registeredAt = "2026-08-20T12:00:00Z",
                notificationsEnabled = true
            };

            var copy = reg.Copy();
            Assert.AreEqual(reg.token, copy.token);
            Assert.AreEqual(reg.platform, copy.platform);
            Assert.AreEqual(reg.deviceModel, copy.deviceModel);
            Assert.AreEqual(reg.appVersion, copy.appVersion);
            Assert.AreEqual(reg.registeredAt, copy.registeredAt);
            Assert.AreEqual(reg.notificationsEnabled, copy.notificationsEnabled);

            string json = JsonConvert.SerializeObject(reg);
            var deserialized = JsonConvert.DeserializeObject<DevicePushRegistration>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(reg.token, deserialized.token);
            Assert.AreEqual(reg.platform, deserialized.platform);
        }

        [Test]
        public void AppState_SetDevicePushRegistrationReducer_UpdatesState()
        {
            var reg = new DevicePushRegistration
            {
                token = "token_xyz",
                platform = "ios",
                notificationsEnabled = true
            };

            _storeService.store.Dispatch(AppActions.setDevicePushRegistration.Invoke(reg));

            var state = _storeService.GetAppState();
            Assert.IsNotNull(state.devicePushRegistration);
            Assert.AreEqual("token_xyz", state.devicePushRegistration.token);
            Assert.AreEqual("ios", state.devicePushRegistration.platform);
            Assert.IsTrue(state.devicePushRegistration.notificationsEnabled);
        }

        [Test]
        public void NotificationRoutingService_ResolveNavigationAction_MapsKnownActions()
        {
            Assert.AreEqual(Actions.go_to_pantry, NotificationRoutingService.ResolveNavigationAction("go_to_pantry"));
            Assert.AreEqual(Actions.go_to_pantry, NotificationRoutingService.ResolveNavigationAction("pantry"));
            Assert.AreEqual(Actions.go_to_meallog, NotificationRoutingService.ResolveNavigationAction("go_to_meallog"));
            Assert.AreEqual(Actions.go_to_meallog, NotificationRoutingService.ResolveNavigationAction("meal_log"));
            Assert.AreEqual(Actions.go_to_groups, NotificationRoutingService.ResolveNavigationAction("go_to_groups"));
            Assert.AreEqual(Actions.groups_to_detail, NotificationRoutingService.ResolveNavigationAction("group_detail"));
            Assert.AreEqual(Actions.go_to_foodwaste, NotificationRoutingService.ResolveNavigationAction("foodwaste"));
            Assert.AreEqual(Actions.go_to_recipes, NotificationRoutingService.ResolveNavigationAction("recipes"));
            Assert.AreEqual(Actions.open_quiz, NotificationRoutingService.ResolveNavigationAction("quiz"));
            Assert.AreEqual(Actions.go_to_settings, NotificationRoutingService.ResolveNavigationAction("settings"));
            Assert.AreEqual(Actions.go_to_home, NotificationRoutingService.ResolveNavigationAction("home"));
            Assert.IsNull(NotificationRoutingService.ResolveNavigationAction("unregistered_action"));
            Assert.IsNull(NotificationRoutingService.ResolveNavigationAction(null));
        }

        [Test]
        public void NotificationRoutingService_HandleNotificationOpened_InvokesNavigationHandlerWithArguments()
        {
            var mockNotificationService = new Mock<INotificationService>();
            var router = new NotificationRoutingService(mockNotificationService.Object);

            string navigatedAction = null;
            Unity.AppUI.Navigation.Argument[] navigatedArgs = null;

            router.SetNavigationHandler((action, args) =>
            {
                navigatedAction = action;
                navigatedArgs = args;
            });

            var payload = new NotificationPayload
            {
                Action = "go_to_pantry",
                TargetId = "item_456"
            };

            router.HandleNotificationOpened(payload);

            Assert.AreEqual(Actions.go_to_pantry, navigatedAction);
            Assert.IsNotNull(navigatedArgs);
            Assert.IsTrue(System.Array.Exists(navigatedArgs, a => a.name == "targetId" && (string)a.value == "item_456"));

            router.Dispose();
        }

        [Test]
        public void NotificationRoutingService_HandleNotificationOpened_OpensDrawerOnDrawerAction()
        {
            var mockNotificationService = new Mock<INotificationService>();
            var router = new NotificationRoutingService(mockNotificationService.Object);

            bool drawerOpened = false;
            router.SetNotificationsDrawerHandler(() => drawerOpened = true);

            var payload = new NotificationPayload
            {
                Action = "open_notifications_drawer"
            };

            router.HandleNotificationOpened(payload);

            Assert.IsTrue(drawerOpened);
            router.Dispose();
        }

        [Test]
        public async Task NotificationService_InitializeAsync_RestoresOrCreatesPushRegistration()
        {
            await _service.InitializeAsync();

            var reg = _service.GetDevicePushRegistration();
            Assert.IsNotNull(reg);
            Assert.IsNotEmpty(reg.platform);

            var state = _storeService.GetAppState();
            Assert.IsNotNull(state.devicePushRegistration);
            Assert.AreEqual(reg.platform, state.devicePushRegistration.platform);
        }

        [Test]
        public void NotificationService_SetNotificationsEnabled_UpdatesStoreAndStorage()
        {
            _service.SetNotificationsEnabled(true);
            Assert.IsTrue(_service.AreNotificationsEnabled());
            Assert.IsTrue(_storeService.GetAppState().pushNotificationsEnabled);

            var saved = _localStorageService.GetValue<DevicePushRegistration>("device_push_registration");
            Assert.IsNotNull(saved);
            Assert.IsTrue(saved.notificationsEnabled);

            _service.SetNotificationsEnabled(false);
            Assert.IsFalse(_service.AreNotificationsEnabled());
            Assert.IsFalse(_storeService.GetAppState().pushNotificationsEnabled);
        }

        [Test]
        public void NotificationService_SetNotificationsEnabled_False_ClearsToken()
        {
            _service.SetNotificationsEnabled(true);
            var reg = _service.GetDevicePushRegistration();
            reg.token = "test_fcm_token_123";
            _localStorageService.SetValue("device_push_registration", reg);

            _service.SetNotificationsEnabled(false);

            var updated = _service.GetDevicePushRegistration();
            Assert.IsEmpty(updated.token);
            Assert.IsFalse(updated.notificationsEnabled);
        }

        [Test]
        public async Task NotificationService_PromptFlow_WorksCorrectly()
        {
            // 1. Unauthenticated -> ShouldPrompt is false
            Assert.IsFalse(_service.ShouldPromptForNotifications());

            // 2. Authenticated -> ShouldPrompt is true
            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(new AppActions.LoginPayload("user_123", "User", "user@test.com", "fake_token", "Bearer", 3600, "refresh_token")));
            Assert.IsTrue(_service.ShouldPromptForNotifications());

            // 3. User declines
            _service.DeclineNotifications();
            Assert.IsFalse(_service.ShouldPromptForNotifications());
            Assert.IsFalse(_service.AreNotificationsEnabled());

            // 4. Reset prompt state and test Accept
            _localStorageService.SetValue("notification_permission_prompted", false);
            Assert.IsTrue(_service.ShouldPromptForNotifications());

            await _service.AcceptNotificationsAsync();
            Assert.IsFalse(_service.ShouldPromptForNotifications());
            Assert.IsTrue(_service.AreNotificationsEnabled());
        }

        [Test]
        public void NotificationService_RescheduleAllNotifications_DoesNotThrowAndReschedules()
        {
            _service.SetNotificationsEnabled(true);

            var pantryWrapper = new PantryItemArrayWrapper
            {
                items = new PantryItem[]
                {
                    new PantryItem
                    {
                        id = "pantry_item_1",
                        expiryDate = DateTime.Now.AddDays(5).ToString("o"),
                        foodProduct = new FoodProduct { id = "food_1", name = "Yogurt" }
                    }
                }
            };
            _localStorageService.SetValue("pantry_cache", pantryWrapper);

            Assert.DoesNotThrow(() =>
            {
                _service.RescheduleAllNotifications(TimeSpan.FromHours(10));
            });
        }

        [Test]
        public void NotificationService_SyncPantryReminders_WhenNotificationsDisabled_ClearsRegistry()
        {
            _service.SetNotificationsEnabled(false);

            var items = new List<PantryItemView>
            {
                new PantryItemView
                {
                    Item = new PantryItem { id = "item_1", expiryDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd") },
                    DisplayName = "Leche"
                }
            };

            _service.SyncPantryReminders(items);

            var registry = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.IsTrue(registry == null || registry.records.Count == 0);
        }

        [Test]
        public void NotificationService_SyncPantryReminders_SchedulesNewItem_AndAvoidsReschedulingWhenUnchanged()
        {
            _service.SetNotificationsEnabled(true);

            var futureDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd");
            var items = new List<PantryItemView>
            {
                new PantryItemView
                {
                    Item = new PantryItem { id = "item_1", expiryDate = futureDate },
                    DisplayName = "Yogurt"
                }
            };

            // First sync: schedules the item
            _service.SyncPantryReminders(items);

            var registry = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.IsNotNull(registry);
            Assert.AreEqual(1, registry.records.Count);
            Assert.AreEqual("item_1", registry.records[0].itemId);
            Assert.AreEqual("Yogurt", registry.records[0].itemName);

            // Second sync with exact same item: should not duplicate or change count
            _service.SyncPantryReminders(items);

            var registryAfter = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(1, registryAfter.records.Count);
        }

        [Test]
        public void NotificationService_SyncPantryReminders_CancelsRemovedItem()
        {
            _service.SetNotificationsEnabled(true);

            var futureDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd");
            var initialItems = new List<PantryItemView>
            {
                new PantryItemView
                {
                    Item = new PantryItem { id = "item_1", expiryDate = futureDate },
                    DisplayName = "Manzanas"
                },
                new PantryItemView
                {
                    Item = new PantryItem { id = "item_2", expiryDate = futureDate },
                    DisplayName = "Plátanos"
                }
            };

            _service.SyncPantryReminders(initialItems);

            var registry = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(2, registry.records.Count);

            // Now item_2 is removed (consumed or deleted)
            var updatedItems = new List<PantryItemView>
            {
                initialItems[0] // Only item_1 remains
            };

            _service.SyncPantryReminders(updatedItems);

            var registryAfter = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(1, registryAfter.records.Count);
            Assert.AreEqual("item_1", registryAfter.records[0].itemId);
        }

        [Test]
        public void NotificationService_SyncPantryReminders_ReschedulesWhenExpiryDateModified()
        {
            _service.SetNotificationsEnabled(true);

            var initialDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd");
            var item = new PantryItemView
            {
                Item = new PantryItem { id = "item_mod", expiryDate = initialDate },
                DisplayName = "Queso"
            };

            _service.SyncPantryReminders(new[] { item });

            var registry = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(1, registry.records.Count);
            Assert.AreEqual(initialDate, registry.records[0].expiryDate);

            // Modify expiry date
            var newDate = DateTime.Now.AddDays(10).ToString("yyyy-MM-dd");
            item.Item.expiryDate = newDate;

            _service.SyncPantryReminders(new[] { item });

            var registryAfter = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(1, registryAfter.records.Count);
            Assert.AreEqual(newDate, registryAfter.records[0].expiryDate);
        }

        [Test]
        public void NotificationService_CancelPantryReminder_RemovesFromRegistry()
        {
            _service.SetNotificationsEnabled(true);

            var futureDate = DateTime.Now.AddDays(4);
            _service.SchedulePantryExpiryReminder("item_cancel", "Pan", futureDate);

            var registry = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.IsNotNull(registry);
            Assert.AreEqual(1, registry.records.Count);

            _service.CancelPantryReminder("item_cancel");

            var registryAfter = _localStorageService.GetValue<ScheduledPantryRemindersRegistry>("scheduled_pantry_reminders");
            Assert.AreEqual(0, registryAfter.records.Count);
        }

        [Test]
        public void NotificationService_SetNotificationsEnabled_SetsPromptedFlag()
        {
            _service.SetNotificationsEnabled(true);
            Assert.IsTrue(_localStorageService.GetValue<bool>("notification_permission_prompted", false));

            _service.SetNotificationsEnabled(false);
            Assert.IsTrue(_localStorageService.GetValue<bool>("notification_permission_prompted", false));
        }
    }
}
