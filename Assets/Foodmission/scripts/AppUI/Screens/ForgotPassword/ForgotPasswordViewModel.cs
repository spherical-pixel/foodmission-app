using System;
using System.Text.RegularExpressions;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation.Generated;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;


namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Email entered by the user for password reset
        /// </summary>
        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private string _emailHelpTextValue = "";

        [ObservableProperty]
        private HelpTextVariant _emailHelpTextVariant = HelpTextVariant.Default;

        /// <summary>
        /// True if waiting for password reset request to complete
        /// </summary>
        [ObservableProperty]
        private DisplayStyle _isLoading = DisplayStyle.None;

        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        public event System.Action<string> ShowErrorRequest;

        /// <summary>
        /// Event fired when password reset request succeeds
        /// </summary>
        public event System.Action<string> ShowSuccessRequest;

        public ForgotPasswordViewModel(IAuthService authService, IStoreService storeService) : base(storeService)
        {
            _authService = authService;
            IsLoading = DisplayStyle.None;
        }

        /// <summary>
        /// Validates the email field
        /// </summary>
        public bool ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email))
            {
                EmailHelpTextValue = "@UI:ERROR_NO_EMPTY";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            if (!IsValidEmail(Email))
            {
                EmailHelpTextValue = "@UI:ERROR_EMAIL_INVALID";
                EmailHelpTextVariant = HelpTextVariant.Destructive;
                return false;
            }

            EmailHelpTextValue = string.Empty;
            EmailHelpTextVariant = HelpTextVariant.Default;
            return true;
        }

        /// <summary>
        /// Simple email validation
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Called when user clicks the Continue button
        /// Sends password reset request via AuthService
        /// </summary>
        public async void RequestPasswordReset()
        {
            if (!ValidateEmail())
            {
                ShowErrorRequest?.Invoke("@UI:ERROR_FIELDS_VALIDATION");
                return;
            }

            IsLoading = DisplayStyle.Flex;

            try
            {
                var result = await _authService.RequestPasswordResetAsync(Email);

                if (result.success)
                {
                    ShowSuccessRequest?.Invoke("@UI:FORGOT_PASSWORD_EMAIL_SENT");
                    Email = string.Empty;
                }
                else
                {
                    ShowErrorRequest?.Invoke(result.message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Password reset error: {ex.Message}");
                ShowErrorRequest?.Invoke("@UI:FORGOT_PASSWORD_ERROR");
            }
            finally
            {
                IsLoading = DisplayStyle.None;
            }
        }
    }
}
