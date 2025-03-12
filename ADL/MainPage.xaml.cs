namespace ADL;

using System.Diagnostics;
using ADL.PopUp;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json;
using Windows.ApplicationModel.DataTransfer;

public sealed partial class MainPage : Page
{
    public static bool AppLoaded = false;
    protected string DomainUser;
    protected string Password;
    public AdObject HoveredItem;
    private string PathFilter = null;

    public MainPage() => this.InitializeComponent();

    #region Data mangagement and filtering
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        AdManager.GetDomains();
        // Await the asynchronous method call
        AdManager.OnReady += (s, e) =>
        {
            if (Dispatcher != null)
            {
                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ProgRing.Visibility = Visibility.Collapsed;
                    MainGrid.Visibility = Visibility.Visible;
                    AppLoaded = true;
                    Refresh.IsEnabled = true;
                    LoadFavNodeChildren();
                });
            }
            else
            {
                Debug.WriteLine("Dispatcher is null.");
                AppLoaded = true;
                Refresh.IsEnabled = true;
            }
        };
        AdManager.OuReady += AdManager_OuReady;

        await Authentificate();
        LoadData();
    }

    private void AdManager_OuReady(object? sender, EventArgs e)
    {
        if (Dispatcher != null)
        {
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                NavigationTree.ItemsSource = AdManager.AdOus;

            });
        }
        else
        {
            Debug.WriteLine("Dispatcher is null.");
        }
    }

    private async Task Authentificate()
    {
        bool Success = false;
        var Severity = InfoBarSeverity.Informational;
        while (Success != true)
        {
            var auth = new Authentification();

            if (!string.IsNullOrEmpty(DomainUser))
                auth = new Authentification(Severity, DomainUser, Password);

            auth.XamlRoot = this.XamlRoot;
            await auth.ShowAsync();
            if (auth.Severity == InfoBarSeverity.Success)
            {
                Success = true;
            }
            DomainUser = auth.UserTitle;
            Password = auth.Message;
            Severity = auth.Severity;
        }
    }
    private void LoadData()
    {
        AppLoaded = false;
        Refresh.IsEnabled = false;
        ProgRing.Visibility = Visibility.Visible;
        MainGrid.Visibility = Visibility.Collapsed;
        new Thread(() => AdManager.GetAllDataAsync(DomainUser, Password)).Start();
    }
    private void Search(bool ShowWarning = false)
    {
        List<AdObject> FinalFilter = new();
        List<AdObject> ByFilter = new();

        switch (SearchBy.SelectedIndex)
        {
            case 0:
                ByFilter = AdManager.AdObjects.Where(obj => obj.Name.ToLower().Contains(SearchBox.Text.ToLower())).ToList();
                break;
            case 1:
                ByFilter = AdManager.AdObjects.Where(obj => obj.Path.ToLower().Contains(SearchBox.Text.ToLower())).ToList();
                break;
            case 2:
                ByFilter = AdManager.AdObjects.Where(obj => obj.FullPath.ToLower().Contains(SearchBox.Text.ToLower())).ToList();
                break;
        }

        FinalFilter = ByFilter;

        if (PathFilter != null)
            FinalFilter = FinalFilter.Where(obj => obj.FullPath.ToLower().Contains(PathFilter?.ToLower() ?? "N/A")).ToList();

        if (FinalFilter.Count < 600)
        {
            WarningMessage.IsOpen = false;
            switch (TypeFilter.SelectedIndex)
            {
                case 1:
                    FinalFilter = FinalFilter.Where(obj => obj.Type == "Device").ToList();
                    break;
                case 2:
                    FinalFilter = FinalFilter.Where(obj => obj.Type == "User").ToList();
                    break;
                case 3:
                    FinalFilter = FinalFilter.Where(obj => obj.Type == "Group").ToList();
                    break;
            }

            switch (StatusFilter.SelectedIndex)
            {
                case 1:
                    FinalFilter = FinalFilter.Where(obj => obj.CanBeEnable == true && obj.IsEnable == true).ToList();
                    break;
                case 2:
                    FinalFilter = FinalFilter.Where(obj => obj.CanBeEnable == true && obj.IsEnable == false).ToList();
                    break;
            }
            if (DomainFilter.SelectedIndex > 0)
            {
                FinalFilter = FinalFilter.Where(obj => obj.Path.ToLower().Contains(DomainFilter.SelectedValue?.ToString()?.ToLower() ?? "na")).ToList();
            }

            if (SortOrderBt.IsChecked && SortOrderBt.Tag != null && !string.IsNullOrEmpty(SortOrderBt.Tag?.ToString()))
            {
                switch (SortOrderBt.Tag.ToString())
                {
                    case "A":
                        FinalFilter = FinalFilter.OrderBy(obj => obj.Name).ToList();
                        break;
                    case "D":
                        FinalFilter = FinalFilter.OrderByDescending(obj => obj.Name).ToList();
                        break;
                }
            }

            DeviceListView.ItemsSource = FinalFilter;
        }
        else if(ShowWarning)
        {
            WarningMessage.IsOpen = true;
            WarningMessage.Message = "Too many results, please refine your search";
            WarningMessage.Severity = InfoBarSeverity.Warning;
            WarningMessage.Title = "Search result warning";
        }

    }
    #endregion

    #region Filter UI click events
    private void SearchBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppLoaded)
            Search();
    }

    private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppLoaded)
            Search();
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppLoaded)
            Search();
    }

    private void DomainFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppLoaded)
            Search();
    }

    private void DescendBt_Click(object sender, RoutedEventArgs e)
    {
        SortOrderBt.IsChecked = false;
        SortOrderBt.Tag = "D";
        SortOrderBt.IsChecked = true;
    }

    private void AscendBt_Click(object sender, RoutedEventArgs e)
    {
        SortOrderBt.IsChecked = false;
        SortOrderBt.Tag = "A";
        SortOrderBt.IsChecked = true;
    }

    private void SortOrderBt_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
    {
        if (AppLoaded)
        {
            if (SortOrderBt.IsChecked == false)
            {
                SortOrderBt.Content = new FontIcon { Glyph = "\uE8CB" };
            }
            else
            {
                switch (SortOrderBt.Tag?.ToString())
                {
                    case "A":
                        SortOrderBt.Content = new FontIcon { Glyph = "\uE74A" };
                        break;
                    case "D":
                        SortOrderBt.Content = new FontIcon { Glyph = "\uE74B" };
                        break;
                }
            }

            Search();
        }
    }
    #endregion

    #region Other ui events
    private void TextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (SearchBox.Text.Length > 0)
        {
            SearchBt.IsEnabled = true;
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Search(SearchBox.Text.Length < 3);
            }
        }
        else
        {
            SearchBt.IsEnabled = false;
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => Search(true);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        StatusFilter.SelectedIndex = 0;
        TypeFilter.SelectedIndex = 0;
        SortOrderBt.IsChecked = false;
        SearchBy.SelectedIndex = 0;
        DomainFilter.SelectedIndex = 0;
        LoadData();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        if (grid != null)
        {
            var dataContext = grid.DataContext as AdObject;
            HoveredItem = dataContext;
        }
    }

    private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem Item)
        {
            switch (Item.Name)
            {
                case "CopyName":
                    if (HoveredItem != null)
                    {
                        DataPackage dataPackage = new();
                        dataPackage.SetText(HoveredItem.Name);
                        Clipboard.SetContent(dataPackage);
                    }
                    break;
                case "CopyPath":
                    if (HoveredItem != null)
                    {
                        DataPackage dataPackage = new();
                        dataPackage.SetText(HoveredItem.Path);
                        Clipboard.SetContent(dataPackage);
                    }
                    break;
                case "CopyFullPath":
                    if (HoveredItem != null)
                    {
                        DataPackage dataPackage = new();
                        dataPackage.SetText(HoveredItem.FullPath);
                        Clipboard.SetContent(dataPackage);
                    }
                    break;
                case "EnableDisable":
                    if (HoveredItem != null)
                    {
                        if (HoveredItem.CanBeEnable)
                        {

                            if (HoveredItem.IsEnable)
                            {
                                var Confirmation = new ActionConfirmation($"Do you want to disable {HoveredItem.Name}", "\uECC9");
                                Confirmation.XamlRoot = this.XamlRoot;
                                if (await Confirmation.ShowAsync() == ContentDialogResult.Secondary)
                                    AdManager.Disable(DomainUser, Password, HoveredItem);
                            }
                            else
                            {
                                var Confirmation = new ActionConfirmation($"Do you want to enable {HoveredItem.Name}", "\uE930");
                                Confirmation.XamlRoot = this.XamlRoot;
                                if (await Confirmation.ShowAsync() == ContentDialogResult.Secondary)
                                    AdManager.Enable(DomainUser, Password, HoveredItem);
                            }
                        }
                        Search();
                    }
                    break;

                case "Delete":
                    if (HoveredItem != null)
                    {
                        var Confirmation = new ActionConfirmation($"Do you want to delete {HoveredItem.Name}", "\uE74D");
                        Confirmation.XamlRoot = this.XamlRoot;
                        if (await Confirmation.ShowAsync() == ContentDialogResult.Secondary)
                            AdManager.Delete(DomainUser, Password, HoveredItem);
                        Search();
                    }
                    break;
                case "Members":
                    if (HoveredItem != null)
                    {
                        var MembersWin = new MembersWindows(DomainUser,Password,HoveredItem);
                        MembersWin.Activate();
                    }
                    break;
            }
        }

    }

    private void NavToggle_Click(object sender, RoutedEventArgs e)
    {
        SplPane.IsPaneOpen = NavToggle.IsChecked ?? false;
        if (SplPane.IsPaneOpen)
        {
            NavigationTree.Visibility = Visibility.Visible;
        }
        else
        {
            NavigationTree.Visibility = Visibility.Collapsed;
        }
    }

    private void NavigationTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is AdOu OU)
        {
            if (OU.FullPath == "All" || OU.FullPath == "Favorites")
            {
                DomainFilter.IsEnabled = true;
                DomainFilter.SelectedIndex = 0;
                SearchBy.IsEnabled = true;
                SearchBy.SelectedIndex = 0;
                PathFilter = null;
            }
            else
            {
                DomainFilter.IsEnabled = false;
                DomainFilter.SelectedIndex = 0;
                SearchBy.IsEnabled = false;
                SearchBy.SelectedIndex = 0;
                PathFilter = OU.FullPath;
            }
            Search(SearchBox.Text.Length > 2);
        }
    }
    private void Fav_Click(object sender, RoutedEventArgs e)
    {

        if (sender is MenuFlyoutItem TreeItem && TreeItem.DataContext is AdOu FavItem)
        {
            if (FavItem != null)
            {
                var FavNode = AdManager.AdOus.Find(ou => ou.FullPath == "Favorites");
                if (FavItem.TypeColor.ToLower() == "remove")
                {
                    FavNode.Children.Remove(FavNode.Children.Find(ou => ou.FullPath == FavItem.FullPath));
                }
                else
                {
                    var Copy = new AdOu { Name = FavItem.Name, Path = FavItem.Path, Domain = FavItem.Domain, FullPath = FavItem.FullPath, TypeIcon = "\uE734", Type = "Visible",TypeColor = "Remove", Tag = "\uECC9" };
                    AdManager.AdOus.Find(ou => ou.FullPath == "Favorites").Children.Add(Copy);
                }
                SaveFavNodeChildren();
            }
        }
    }
    private void SaveFavNodeChildren()
    {
        var favNode = AdManager.AdOus.Find(ou => ou.FullPath == "Favorites");
        if (favNode != null)
        {
            var json = JsonConvert.SerializeObject(favNode.Children);
            ApplicationData.Current.LocalSettings.Values["FavNodeKey"] = json;
            NavigationTree.ItemsSource = null;
            NavigationTree.ItemsSource = AdManager.AdOus;
        }
    }

    private void LoadFavNodeChildren()
    {
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("FavNodeKey"))
        {
            string json = ApplicationData.Current.LocalSettings.Values["FavNodeKey"] as string ?? "{}";
            var favNodeChildren = JsonConvert.DeserializeObject<List<AdOu>>(json);
            var favNode = AdManager.AdOus.Find(ou => ou.FullPath == "Favorites");
            if (favNode != null && favNodeChildren != null)
            {
                favNode.Children = favNodeChildren;
                NavigationTree.ItemsSource = null;
                NavigationTree.ItemsSource = AdManager.AdOus;
            }
        }
    }
    #endregion
}
