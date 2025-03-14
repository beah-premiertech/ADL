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

namespace ADL;

public sealed partial class BrowseOus : ContentDialog
{
    public AdOu SelectedOu { get; set; }
    public BrowseOus()
    {
        this.InitializeComponent();
        NavigationTree.ItemsSource = AdDataCollections.AdOus;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {}

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {}

    private void NavigationTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        SelectedOu = (AdOu)args.InvokedItem;
        if (SelectedOu.Name != "All" && SelectedOu.Name != "Favorites" && !AdDataCollections.Domains.Contains(SelectedOu.Name))
        {
            IsSecondaryButtonEnabled = true;
        }
        else
        {
            IsSecondaryButtonEnabled = false;
        }
    }
}
