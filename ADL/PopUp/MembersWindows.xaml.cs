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
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using ADL.Class;

namespace ADL.PopUp;


public sealed partial class MembersWindows : Window
{
    public AdObject Group;
    protected string DomainUser;
    protected string Password;
    public AdBaseObject WorkItem;
    public MembersWindows(string DU,string P, AdObject AdObj)
    {
        this.InitializeComponent();
        Group = AdObj;
        DomainUser = DU;
        Password = P;
        TitleText.Text = $"Members of {Group.Name}";
        DeviceListView.ItemsSource = AdAction.Members(DomainUser, Password, Group);
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        if (grid != null)
        {
            var dataContext = grid.DataContext as AdBaseObject;
            WorkItem = dataContext;
        }
    }
    private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem Item)
        {
            switch (Item.Name)
            {
                case "Remove":
                    if (WorkItem != null)
                    {
                        var Confirmation = new ActionConfirmation($"Do you want to remove {WorkItem.Name} from {Group.Name}", "\uE74D");
                        Confirmation.XamlRoot = this.Content.XamlRoot;
                        if (await Confirmation.ShowAsync() == ContentDialogResult.Secondary)
                        {
                            AdAction.AddRemoveMembers(DomainUser, Password, Group, WorkItem.FullPath, true);
                            DeviceListView.ItemsSource = AdAction.Members(DomainUser, Password, Group);
                        }
                    }
                    break;
    
            }
        }

    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchText.Text.Length > 2)
        {
            CheckName.IsEnabled = true;
        }
        else
        {
            CheckName.IsEnabled = false;
            AddTo.IsEnabled = false;
            WorkItem = null;
        }
    }

    private async void CheckName_Click(object sender, RoutedEventArgs e)
    {
        var CheckRes = new ResourcesValidation(AdDataCollections.AdObjects.Where(obj => obj.Type != "Group" && obj.Name.ToLower().Contains(SearchText.Text.ToLower())).ToList());
        CheckRes.XamlRoot = this.Content.XamlRoot;
        await CheckRes.ShowAsync();
        if (CheckRes.Resource != null)
        {
            SearchText.Text = CheckRes.Resource.Name;
            AddTo.IsEnabled = true;
            WorkItem = CheckRes.Resource;
        }
    }

    private async void AddTo_Click(object sender, RoutedEventArgs e)
    {
        if (WorkItem != null)
        {
            var Confirmation = new ActionConfirmation($"Do you want to add {WorkItem.Name} to {Group.Name}", "\uE74D");
            Confirmation.XamlRoot = this.Content.XamlRoot;
            if (await Confirmation.ShowAsync() == ContentDialogResult.Secondary)
            {
                AdAction.AddRemoveMembers(DomainUser, Password, Group, WorkItem.FullPath);
                DeviceListView.ItemsSource = AdAction.Members(DomainUser, Password, Group);
            }
        }
    }
}
