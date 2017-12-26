using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Moonlight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ApplicationsPage : Page
    {
        private NvStreamDevice streamDevice;

        public ApplicationsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            streamDevice = e.Parameter as NvStreamDevice;
            if(streamDevice != null)
            {
                hostBox.Text = streamDevice.ServerInfo.HostName;
            }
            base.OnNavigatedTo(e);
        }

        private async Task Debug(string content)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Debug",
                Content = content,
                CloseButtonText = "Ok"
            };
            await dialog.ShowAsync();
        }
    }

}
