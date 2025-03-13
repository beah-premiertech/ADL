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
using System.DirectoryServices.AccountManagement;
using System.Diagnostics;

namespace ADL
{
    public sealed partial class Authentification : ContentDialog
    {
        private ApplicationDataContainer? LocalSettings;
        private const string UserDomainKey = "UserDomain";
        public InfoBarSeverity Severity { get; set; }
        public string? UserTitle { get; set; }
        public string? Message { get; set; }

        public Authentification(InfoBarSeverity s = InfoBarSeverity.Informational, string? t = null, string? m = null)
        {
            this.InitializeComponent();
            Domain.ItemsSource = AdManager.NtDomains;
            try
            {
                if (ApplicationData.Current != null && ApplicationData.Current.LocalSettings != null && ApplicationData.Current.LocalSettings.Values != null)
                {
                    LocalSettings = ApplicationData.Current.LocalSettings;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle the exception or log it as needed
                Debug.WriteLine($"Error accessing ApplicationData.Current: {ex.Message}");
            }

            Severity = s;
            UserTitle = t;
            Message = m;

            LoadUserDomain();

            if (!string.IsNullOrWhiteSpace(UserTitle))
            {
                var Split = UserTitle.Split('\\');
                // Select item
                User.Text = Split.LastOrDefault();

                if (!string.IsNullOrWhiteSpace(Message))
                {
                    Status.Title = UserTitle;
                    Status.Message = Message;
                    Status.Severity = Severity;
                }
            }
        }

        private void TryConnect(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (Domain.SelectedValue is AdNtDomain NTdomain)
            {
                if (ValidateCredentials(NTdomain.Domain, User.Text, Pwd.Password))
                {
                    UserTitle = $@"{NTdomain.NTDomain}\{User.Text}";
                    Message = Pwd.Password;
                    Severity = InfoBarSeverity.Success;
                    SaveUserDomain(UserTitle);
                }
                else
                {
                    UserTitle = $@"{NTdomain.NTDomain}\{User.Text}";
                    Message = $"Password is not valid";
                    Severity = InfoBarSeverity.Error;
                }
            }
        }

        private void User_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConnectButton();
        }

        private void Pwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            EnableConnectButton();
        }

        public void EnableConnectButton()
        {
            if (User.Text.Length > 1 && Pwd.Password.Length > 1)
            {
                this.IsSecondaryButtonEnabled = true;
            }
            else
            {
                this.IsSecondaryButtonEnabled = false;
            }
        }

        static bool ValidateCredentials(string domain, string username, string password)
        {
            using (var context = new PrincipalContext(ContextType.Domain, domain))
            {
                return context.ValidateCredentials(username, password);
            }
        }

        private void SaveUserDomain(string userDomain)
        {
            if (LocalSettings != null)
            {
                LocalSettings.Values[UserDomainKey] = userDomain;
                LocalSettings.Values[$"{UserDomainKey}Index"] = Domain.SelectedIndex;
            }
        }

        private void LoadUserDomain()
        {
            try
            {
                if (LocalSettings != null && LocalSettings.Values.ContainsKey(UserDomainKey) && LocalSettings.Values.ContainsKey($"{UserDomainKey}Index"))
                {
                    UserTitle = LocalSettings.Values[UserDomainKey] as string;
                    var Index = LocalSettings.Values[$"{UserDomainKey}Index"] as string;
                    if(int.TryParse(Index, out var DomainIndex))
                    { 
                        Domain.SelectedIndex = DomainIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Debug.WriteLine($"Error accessing local settings: {ex.Message}");
            }
        }
    }
}
