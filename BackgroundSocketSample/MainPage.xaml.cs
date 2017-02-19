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
using Windows.Storage;
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
        string socketId = "MySocket";   //SocketのID、バックグラウンドタスク側と統一しておく必要がある
        string initPort = "8000";       //Socketの初期ポート番号

        public MainPage()
        {
            this.InitializeComponent();
            textPort.Text = initPort;
            textIp.Text = NetworkInformation.GetHostNames().Where(q => q.Type == HostNameType.Ipv4).First().DisplayName;
        }
        

        private async void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //タスクがすでに登録されている場合は解除する
                foreach(var t in BackgroundTaskRegistration.AllTasks)
                {
                    t.Value.Unregister(true);
                }

                //バックグラウンドタスクを登録する
                //NameはなんでもいいけどTaskEntryPointはバックグランドタスクの {名前空間}.{クラス名} にしないとだめ
                var socketTaskBuilder = new BackgroundTaskBuilder();
                socketTaskBuilder.Name = "MySocketBackgroundTask";
                socketTaskBuilder.TaskEntryPoint = "BackgroundSocketComponent.SocketListenTask";
                
                //バックグラウンドタスクでSocketを待ち受けるためのトリガー
                //これのおかげでバックグラウンドタスクがSocketに反応できる
                var trigger = new SocketActivityTrigger();
                socketTaskBuilder.SetTrigger(trigger);
                var task = socketTaskBuilder.Register();

                //ソケットリスナー
                var socketListener = new StreamSocketListener();
                var hostname = NetworkInformation.GetHostNames().Where(q => q.Type == HostNameType.Ipv4).First();
                var port = textPort.Text;
                //バックグラウンドタスクとポート番号を合わせるためにローカル設定に入れておく
                ApplicationData.Current.LocalSettings.Values["SocketPort"] = port;
                //バックグラウンドタスクとソケットIDを合わせるためにローカル設定に入れておく
                ApplicationData.Current.LocalSettings.Values["SocketId"] = socketId;
                //バックグラウンドタスクにソケットリスナーの権限を渡すことを許可
                //第2引数はDoNotWakeにしないとBind時にエラーになる
                socketListener.EnableTransferOwnership(task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);
                //ホスト名とポート番号でバインドする
                await socketListener.BindEndpointAsync(hostname, port);

                //ここから下はSuspendingイベントに入れてもいい
                //ソケットリスナーをバックグランドタスクに渡すためにIOを止める
                await socketListener.CancelIOAsync();
                //バックグランドタスクに権限を渡す
                socketListener.TransferOwnership(socketId);

                var dialog = new MessageDialog("Complete to register backgroundtask and to start listen socket!");
                await dialog.ShowAsync();
            }catch(Exception ex)
            {
                var dialog = new MessageDialog(ex.Message);
                await dialog.ShowAsync();
            }

        }

        private async void buttonUnRegister_Click(object sender, RoutedEventArgs e)
        {
            foreach (var t in BackgroundTaskRegistration.AllTasks)
            {
                t.Value.Unregister(true);
            }
            var dialog = new MessageDialog("Complete unregister background tasks");
            await dialog.ShowAsync();
        }
    }
}
