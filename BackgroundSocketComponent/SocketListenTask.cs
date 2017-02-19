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
                    var details = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
                    var socketInformation = details.SocketInformation;
                    
                    ShowToast(socketInformation.SocketKind.ToString()+" "+details.Reason.ToString());

                    switch (details.Reason)
                    {
                        case SocketActivityTriggerReason.ConnectionAccepted:

                            if (socketInformation.SocketKind == SocketActivityKind.StreamSocketListener)
                            {
                                var socket = socketInformation.StreamSocketListener;
                                socket.ConnectionReceived += (s, e) =>
                                {
                                    //ShowToast("Connected Socket");
                                    var socketClient = e.Socket;
                                    socketClient.EnableTransferOwnership(taskInstance.Task.TaskId);
                                    socketClient.TransferOwnership(socketInformation.Id);

                                    socketInformation.StreamSocketListener.Dispose();
                                };
                                await Task.Delay(2000);
                                
                            }
                            

                            break;
                        case SocketActivityTriggerReason.KeepAliveTimerExpired:
                            socketInformation.StreamSocket.TransferOwnership(socketInformation.Id);
                            break;

                        case SocketActivityTriggerReason.SocketActivity:
                            using (var reader = new DataReader(socketInformation.StreamSocket.InputStream))
                            {
                                uint readNum = 1;
                                await reader.LoadAsync(readNum);
                                var data = reader.ReadString(readNum);
                                var result = await Launcher.LaunchUriAsync(new Uri("myhoge://"));
                                ShowToast(result.ToString());
                            }
                            socketInformation.StreamSocket.TransferOwnership(socketInformation.Id);
                            break;
                        case SocketActivityTriggerReason.SocketClosed:
                            //socketInformation.StreamSocket.TransferOwnership(socketInformation.Id);
                            var allSockets = SocketActivityInformation.AllSockets;
                            ShowToast(string.Join(" ",allSockets));
                            foreach(var socket in allSockets)
                            {
                                if (socket.Value.SocketKind == SocketActivityKind.StreamSocket)
                                {
                                    socket.Value.StreamSocket.Dispose();
                                }else
                                {
                                    socket.Value.StreamSocketListener.Dispose();
                                }
                            }

                            var socketListener = new StreamSocketListener();
                            var hostname = NetworkInformation.GetHostNames().Where(q => q.Type == HostNameType.Ipv4).First();
                            var port = "9001";

                            socketListener.EnableTransferOwnership(taskInstance.Task.TaskId, SocketActivityConnectedStandbyAction.DoNotWake);

                            await socketListener.BindEndpointAsync(hostname, port);

                            await socketListener.CancelIOAsync();

                            socketListener.TransferOwnership(socketInformation.Id);
                            
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
