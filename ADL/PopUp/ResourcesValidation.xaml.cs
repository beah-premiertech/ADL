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

namespace ADL.PopUp
{
	public sealed partial class ResourcesValidation : ContentDialog
	{
        public AdObject Resource;
        public ResourcesValidation(List<AdObject> Source)
		{
			this.InitializeComponent();
            DeviceListView.ItemsSource = Source;
        }

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {}

        private void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list)
            {
                if (list.SelectedItem is AdObject obj)
                {
                    IsSecondaryButtonEnabled = true;
                    Resource = obj;
                }
                else
                {
                    IsSecondaryButtonEnabled = false;
                    Resource = null;
                }
            }
        }
    }
}
