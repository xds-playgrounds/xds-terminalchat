using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.SDK.Messaging.BlockchainClient;
using XDS.SDK.Messaging.CrossTierTypes;
using XDS.SDK.Messaging.MessageHostClient;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class ChatView : ConsoleViewBase
    {
        readonly ContactListManager contactListManager;
        readonly ProfileViewModel profileViewModel;
        readonly MessagesViewModel messagesViewModel;
        readonly IMessageBoxService messageBoxService;

        readonly MessageThreadView messageThreadView;
        readonly ITcpConnection tcpConnection;

        readonly Window mainWindow;
        readonly List<string> connectionListEntries;

        TextField textFieldMessageText;
        ListView listViewMessages;
        ListView listViewConnections;
        
        bool firstEnterHandled;

        public ChatView(Window mainWindow) : base(mainWindow)
        {
            this.mainWindow = mainWindow;
            this.contactListManager = App.ServiceProvider.Get<ContactListManager>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();

            this.messageThreadView = new MessageThreadView(this.profileViewModel.Name, this.contactListManager.CurrentContact.ChatId);
            this.messagesViewModel = new MessagesViewModel(App.ServiceProvider, this.contactListManager.CurrentContact.Id, this.messageThreadView);
            this.tcpConnection = App.ServiceProvider.Get<ITcpConnection>();
            this.connectionListEntries = new List<string>();
            this.connectionListEntries.Add("Connecting...");
        }

        public override void Create()
        {
          
            //this.mainWindow.Title = $"{this.profileViewModel.Name} ({this.profileViewModel.ChatId}) vs. {this.contactListManager.CurrentContact.Name} ({this.contactListManager.CurrentContact.ChatId})";

            #region chat-view
            var chatViewFrame = new FrameView("Messages")
            {
                X = 0,
                Y = 1,
                Width = Dim.Percent(75),
                Height = Dim.Fill() - 3,
            };

            this.listViewMessages = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            this.messageThreadView.ListView = this.listViewMessages;

            chatViewFrame.Add(this.listViewMessages);
            this.mainWindow.Add(chatViewFrame);

            #endregion

            #region connections
            var frameViewConnections = new FrameView("Connections")
            {
                X = Pos.Right(chatViewFrame),
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Percent(50f)
            };
            this.listViewConnections = new ListView(this.connectionListEntries)
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            this.listViewConnections.AllowsMarking = false;
            frameViewConnections.Add(this.listViewConnections);
            this.mainWindow.Add(frameViewConnections);

            #endregion



            #region online-user-list
            var userListFrame = new FrameView("Notifications")
            {
                X = Pos.Right(chatViewFrame),
                Y = Pos.Bottom(frameViewConnections),
                Width = Dim.Fill(),
                Height = Dim.Percent(50)
            };
            var userList = new ListView(this.contactListManager.Contacts.Where(x => x.StaticPublicKey != null && x.ChatId != this.contactListManager.CurrentContact.ChatId).Select(x => x.ChatId).ToList())
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            userList.AllowsMarking = false;
            userListFrame.Add(userList);
            this.mainWindow.Add(userListFrame);
            #endregion

            #region chat-bar
            var chatBar = new FrameView(null)
            {
                X = 0,
                Y = Pos.Bottom(chatViewFrame),
                Width = chatViewFrame.Width,
                Height = 3
            };

            this.textFieldMessageText = new TextField("")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(75),
                Height = 1,
                CanFocus = true
            };

            var sendButton = new Button("Send", true)
            {
                X = Pos.Right(this.textFieldMessageText) + 1,
                Y = 0,
                Height = 1
            };

            sendButton.Clicked = () =>
            {
                // work around a bug, where there contact selection triggers sending an empty message
                if (!this.firstEnterHandled)
                {
                    this.firstEnterHandled = true;
                    return;
                }
                _ = SendTextMessageAsync();

            };

            chatBar.Add(this.textFieldMessageText);
            chatBar.Add(sendButton);
            this.mainWindow.Add(chatBar);
            AsyncMethod.RunSync(this.messagesViewModel.InitializeThread);
            chatBar.SetFocus(this.textFieldMessageText);
            OnViewReady();
            #endregion
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            Task.Run(async () =>
            {
                while (this.IsViewReady)
                {
                    try
                    {
                        var connections = ((MessageRelayConnectionFactory)this.tcpConnection).GetCurrentConnections();

                        // MainLoop will be null when the user exits the UI
                        Application.MainLoop?.Invoke(async () =>
                        {
                            var cl = new List<string>();

                            if (connections.Length == 0)
                            {

                                cl.Add("Connecting...");
                            }
                            else
                            {
                                foreach (var connection in connections)
                                {
                                    string info = $"{connection.MessageRelayRecord.IpAddress}";
                                    if (connection.ConnectionState == ConnectedPeer.State.Connected)
                                    {
                                        info+= $" Bytes in: {connection.BytesReceived}, out: {connection.BytesSent}";
                                    }
                                    else
                                    {
                                        info +=
                                            $"{connection.MessageRelayRecord.IpAddress}, State: {connection.ConnectionState}";
                                    }
                                    cl.Add(info);
                                }
                            }

                            this.connectionListEntries.Clear();
                            this.connectionListEntries.AddRange(cl);
                            if (this.IsViewReady)
                                await this.listViewConnections.SetSourceAsync(this.connectionListEntries);
                        });

                        await Task.Delay(5000);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                }
            });
        }

     

        async Task SendTextMessageAsync()
        {
            var message = this.textFieldMessageText.Text.ToString().Trim();

            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    await this.messagesViewModel.SendMessage(MessageType.Text, message);
                    this.textFieldMessageText.Text = "";
                }
                catch (Exception e)
                {
                    await this.messageBoxService.ShowError(e);
                }

                //Application.MainLoop.Invoke(() =>
                //{
                //	//_messages.Add($"{_profileViewModel.Name}: {_textFieldMessageText.Text}");
                //	//_listViewMessages.SetSource(_messages);
                //	_textFieldMessageText.Text = "";
                //});

            }
        }


    }
}
