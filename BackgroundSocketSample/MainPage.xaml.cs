using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace BackgroundSocketSample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        StreamSocketListener socketListener = null;
        string socketId = "MySocket";
        public MainPage()
        {
            this.InitializeComponent();
            App.Current.Suspending += async(s, e) =>
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                if (socketListener != null)
                {
                    /*
                    await socketListener.CancelIOAsync();

                    socketListener.TransferOwnership(socketId);
                    Debug.WriteLine("transfer");
                    */
                }

                deferral.Complete();
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.Parameter=="hoge")
            {
                webView.Navigate(new Uri("http://garicchi.com"));
            }
        }

        private async void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var socketTaskBuilder = new BackgroundTaskBuilder();
                socketTaskBuilder.Name = "MySocketBackgroundTask";
                socketTaskBuilder.TaskEntryPoint = "BackgroundSocketComponent.SocketListenTask";

                var trigger = new SocketActivityTrigger();
                socketTaskBuilder.SetTrigger(trigger);
                var task = socketTaskBuilder.Register();
                socketListener = new StreamSocketListener();
                var hostname = NetworkInformation.GetHostNames().Where(q => q.Type == HostNameType.Ipv4).First();
                var port = "9001";
                Debug.WriteLine(string.Format("listen socket {0}:{1}", hostname.DisplayName, port));

                socketListener.EnableTransferOwnership(task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);

                await socketListener.BindEndpointAsync(hostname, port);


                await socketListener.CancelIOAsync();

                socketListener.TransferOwnership(socketId);

                var dialog = new MessageDialog("タスク登録完了しました");
                await dialog.ShowAsync();
            }catch(Exception ex)
            {
                var dialog = new MessageDialog(ex.Message);
                await dialog.ShowAsync();
            }

        }
    }
}
