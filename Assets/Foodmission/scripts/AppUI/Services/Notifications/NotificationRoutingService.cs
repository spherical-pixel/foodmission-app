using System;
using System.Collections.Generic;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Handles deep linking and screen navigation when notifications are clicked/opened.
    /// </summary>
    public class NotificationRoutingService : IDisposable
    {
        private readonly INotificationService _notificationService;
        private Action<string, Argument[]> _navigationHandler;
        private Action _openNotificationsDrawerHandler;

        public NotificationRoutingService(INotificationService notificationService)
        {
            _notificationService = notificationService;
            if (_notificationService != null)
            {
                _notificationService.OnNotificationOpened += HandleNotificationOpened;
            }
        }

        public void SetNavigationHandler(Action<string, Argument[]> navigationHandler)
        {
            _navigationHandler = navigationHandler;
        }

        public void SetNotificationsDrawerHandler(Action openNotificationsDrawerHandler)
        {
            _openNotificationsDrawerHandler = openNotificationsDrawerHandler;
        }

        public void HandleNotificationOpened(NotificationPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            Debug.Log($"[NotificationRoutingService] Handling notification click: Action='{payload.Action}', TargetId='{payload.TargetId}'");

            var argsList = new List<Argument>();
            if (!string.IsNullOrEmpty(payload.TargetId))
            {
                argsList.Add(new Argument("id", payload.TargetId));
                argsList.Add(new Argument("targetId", payload.TargetId));
            }
            var args = argsList.ToArray();

            string navAction = ResolveNavigationAction(payload.Action);

            if (!string.IsNullOrEmpty(navAction))
            {
                _navigationHandler?.Invoke(navAction, args);
            }
            else if (payload.Action == "open_notifications_drawer" || payload.Action == "notifications")
            {
                _openNotificationsDrawerHandler?.Invoke();
            }
            else
            {
                // Fallback to Home if unknown action
                _navigationHandler?.Invoke(Actions.go_to_home, args);
            }
        }

        public static string ResolveNavigationAction(string rawAction)
        {
            if (string.IsNullOrEmpty(rawAction)) return null;

            switch (rawAction.ToLowerInvariant())
            {
                case "go_to_pantry":
                case "pantry":
                    return Actions.go_to_pantry;

                case "go_to_meallog":
                case "go_to_meal_log":
                case "meallog":
                case "meal_log":
                    return Actions.go_to_meallog;

                case "go_to_groups":
                case "groups":
                    return Actions.go_to_groups;

                case "go_to_group_detail":
                case "group_detail":
                    return Actions.groups_to_detail;

                case "go_to_foodwaste":
                case "foodwaste":
                    return Actions.go_to_foodwaste;

                case "go_to_recipes":
                case "recipes":
                    return Actions.go_to_recipes;

                case "open_quiz":
                case "quiz":
                    return Actions.open_quiz;

                case "go_to_settings":
                case "settings":
                    return Actions.go_to_settings;

                case "go_to_home":
                case "home":
                    return Actions.go_to_home;

                default:
                    return null;
            }
        }

        public void Dispose()
        {
            if (_notificationService != null)
            {
                _notificationService.OnNotificationOpened -= HandleNotificationOpened;
            }
            _navigationHandler = null;
            _openNotificationsDrawerHandler = null;
        }
    }
}
