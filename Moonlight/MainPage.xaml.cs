﻿using Moonlight.Exception;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Zeroconf;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Moonlight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CryptoProvider CryptoProvider { get; set; }
        public MainViewModel ViewModel { get; private set; }

        public MainPage()
        {
            ViewModel = new MainViewModel();
            this.InitializeComponent();
        }


        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;

            CryptoProvider = new CryptoProvider();
            await CryptoProvider.Initialize();

            if(ViewModel.StreamDevices.Count == 0)
            {
                await GetItemsAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async Task GetItemsAsync()
        {
            StreamDevicesGridView.ItemsSource = ViewModel.StreamDevices;
            ViewModel.IsSearching = true;
            (await NvStreamDevice.DiscoverStreamDevices(CryptoProvider)).ForEach(ViewModel.StreamDevices.Add);
            ViewModel.IsSearching = false;
        }

        private async void StreamDevicesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NvStreamDevice streamDevice = e.ClickedItem as NvStreamDevice;
            if(streamDevice.Paired == NvServerInfo.NvPairStatus.Paired)
            {
                Frame.Navigate(typeof(ApplicationsPage), streamDevice);
            }
            else
            {
                try
                {
                    await streamDevice.Pair();
                    await streamDevice.QueryDataInsecure();
                    if (streamDevice.Paired == NvServerInfo.NvPairStatus.Paired)
                    {
                        Frame.Navigate(typeof(ApplicationsPage), streamDevice);
                    }
                }
                catch (PairingException ex)
                {
                    ContentDialog dialog = new ContentDialog()
                    {
                        Title = "Error",
                        Content = $"Failed to pair with {streamDevice.ServerInfo.HostName}\nReason: {ex.Message}",
                        CloseButtonText = "Ok"
                    };
                    await dialog.ShowAsync();
                }
            }
        }
    }

}
