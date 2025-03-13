using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace ADL.PopUp
{
	public sealed partial class ResetPassword : ContentDialog
	{
        private AdObject User;
        protected string? UserDomain { get; set; }
        protected string? Password { get; set; }
        public string? ResetStatus { get; set; }
        public ResetPassword(string UserDomain, string Password, AdObject User, string? Message = null)
		{
			this.InitializeComponent();
            this.User = User;
            this.UserDomain = UserDomain;
            this.Password = Password;
            Title = $"Reset password for {User.Name}";

            if (Message != null)
            {
                ErrorInfo.IsOpen = true;
                ErrorInfo.Message = Message;
                ErrorInfo.Severity = InfoBarSeverity.Error;
                ErrorInfo.Title = "Reset password error";
            }
        }

        private void Cancel(ContentDialog sender, ContentDialogButtonClickEventArgs args) => ResetStatus = null;

		private void Reset(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
            if (User != null && Pwd.Password != null && Pwd.Password.Length > 3)
            {
                ResetStatus = AdManager.ResetPassword(UserDomain, Password, User, Pwd.Password, ChangeOnNext.IsChecked ?? false);
            }

        }

        private void Pwd_PasswordChanged(object sender, RoutedEventArgs e) => IsSecondaryButtonEnabled = Pwd.Password.Length > 3;
        
    }
}
