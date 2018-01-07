using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Moonlight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ApplicationsPage : Page
    {
        public ApplicationsViewModel ViewModel { get; private set; }

        public ApplicationsPage()
        {
            ViewModel = new ApplicationsViewModel();
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.StreamDevice = e.Parameter as NvStreamDevice;

            if(ViewModel.Applications.Count == 0)
            {
                await GetItemsAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async Task GetItemsAsync()
        {
            ApplicationsGridView.ItemsSource = ViewModel.Applications;
            ViewModel.IsSearching = true;
            (await ViewModel.StreamDevice.GetApplications()).ForEach(ViewModel.Applications.Add);
            ViewModel.IsSearching = false;
        }

        private async void ApplicationsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NvApplication application = e.ClickedItem as NvApplication;
            NvGameSession gameSession = await ViewModel.StreamDevice.LaunchApplication(application);
            Frame.Navigate(typeof(StreamPage), gameSession);
        }
    }

}
