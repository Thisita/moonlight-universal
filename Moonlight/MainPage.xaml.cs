using Moonlight.Exception;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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
            (await NvStreamDevice.DiscoverStreamDevices(CryptoProvider)).ToList().ForEach(ViewModel.StreamDevices.Add);
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
