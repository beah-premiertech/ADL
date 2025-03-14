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
using ADL.Class;

namespace ADL.PopUp;


public sealed partial class AddDevice : ContentDialog
	{
    public string Result { get; set; }
    public string NewName { get; set; }
    public string Path { get; set; }
    protected string DomainUser { get; set; }
    protected string Password { get; set; }
    private string Domain { get; set; }
    public AddDevice(string DomainUser, string Password, AdOu SelectedOu)
		{
			this.InitializeComponent();
        this.DomainUser = DomainUser;
        this.Password = Password;
        this.Domain = SelectedOu.Domain;
        DevicePath.Text = SelectedOu.FullPath;
        Title = $"Add device to {SelectedOu.Name}";
    }

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
        if (DeviceName.Text.Length > 2)
        {
            Result = AdAction.AddDevice(DomainUser, Password, Domain, DeviceName.Text, DevicePath.Text);
            Path = DevicePath.Text;
            NewName = DeviceName.Text;
        }
		}

    private void DeviceName_TextChanged(object sender, TextChangedEventArgs e) => IsSecondaryButtonEnabled = DeviceName.Text.Length > 2;

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var MoveWin = new BrowseOus();
        MoveWin.XamlRoot = this.XamlRoot;
        await MoveWin.ShowAsync();
        if (MoveWin.SelectedOu != null)
        {
            DevicePath.Text = MoveWin.SelectedOu.FullPath;
            Title = $"Add device to {MoveWin.SelectedOu.Name}";

        }
    }
}
