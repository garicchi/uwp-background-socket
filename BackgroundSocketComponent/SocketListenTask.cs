using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Notifications;

namespace BackgroundSocketComponent
{
    public sealed class SocketListenTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            if (taskInstance.TriggerDetails is SocketActivityTriggerDetails)
            {
                try
                {
                    //ソケットIDを取得
                    var socketId = ApplicationData.Current.LocalSettings.Values["SocketId"].ToString();

                    var details = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
                    var socketInformation = details.SocketInformation;
                    
                    switch (details.Reason)
                    {
                        //コネクションがアクセプトされたなら(自動でアクセプトされる)
                        case SocketActivityTriggerReason.ConnectionAccepted:
                            //SocketInformationにはStreamSocketに値がある場合とStreamSocketListenerの2パターンがあるのでSocketKindで判別
                            if (socketInformation.SocketKind == SocketActivityKind.StreamSocketListener)
                            {
                                //StreamSocketListenerを取得
                                var socket = socketInformation.StreamSocketListener;
                                //AcceptされたSocketがこのイベントで取得できるがこのイベントが発火するまでプロセスを落としてはいけないので
                                //Task.Delayで2秒ぐらい待つ
                                socket.ConnectionReceived += (s, e) =>
                                {
                                    //アクセプトされたソケットを取得したらソケットリスナー登録時同様、このタスクにソケットを受信できるように設定
                                    var socketClient = e.Socket;
                                    socketClient.EnableTransferOwnership(taskInstance.Task.TaskId);
                                    socketClient.TransferOwnership(socketId);

                                    ShowToast(string.Format("Connect {0}", socketClient.Information.LocalAddress.DisplayName));
                                    //ソケットリスナーは破棄しないと次回リスナー起動時に死ぬ
                                    socketInformation.StreamSocketListener.Dispose();
                                };
                                await Task.Delay(2000);
                                
                            }

                            break;
                        case SocketActivityTriggerReason.KeepAliveTimerExpired:
                            socketInformation.StreamSocket.TransferOwnership(socketId);
                            break;
                        
                        //ソケットにデータが来た時
                        case SocketActivityTriggerReason.SocketActivity:
                            //1バイト読んでトーストで表示
                            using (var reader = new DataReader(socketInformation.StreamSocket.InputStream))
                            {
                                uint readNum = 1;
                                await reader.LoadAsync(readNum);
                                var data = reader.ReadString(readNum);
                                ShowToast(string.Format("DataReceived {0}",data.ToString()));
                            }
                            socketInformation.StreamSocket.TransferOwnership(socketId);
                            break;
                        //ソケットが閉じられたとき
                        case SocketActivityTriggerReason.SocketClosed:
                            //ソケットが閉じられたとき、以下の処理を読んで再度リッスンすればよいが
                            //OSが再起動したとき、SocketClosedが2回呼ばれる(謎)
                            //2回の呼び出しの違いはSocketの数なのでSocketの数で1回目を判定して再度リッスン
                            if (SocketActivityInformation.AllSockets.Count == 0)
                            {
                                var socketListener = new StreamSocketListener();
                                var hostname = NetworkInformation.GetHostNames().Where(q => q.Type == HostNameType.Ipv4).First();
                                var port = ApplicationData.Current.LocalSettings.Values["SocketPort"].ToString();

                                socketListener.EnableTransferOwnership(taskInstance.Task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);

                                await socketListener.BindEndpointAsync(hostname, port);

                                await socketListener.CancelIOAsync();

                                socketListener.TransferOwnership(socketId);
                                ShowToast(string.Format("{0}:{1} restart socket listen",hostname,port));
                            }
                            break;
                        
                    }

                }
                catch (Exception e)
                {
                    ShowToast("error = "+e.Message+" "+e.StackTrace);
                }
            }


            deferral.Complete();
        }

        //デバッグ表示用
        private void ShowToast(string message)
        {
            //トーストテンプレートの取得
            XmlDocument doc = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            //XMLの編集
            doc.GetElementsByTagName("text")[0].InnerText = message;

            ToastNotification notification = new ToastNotification(doc);
            //通知の送信
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }
    }
}
