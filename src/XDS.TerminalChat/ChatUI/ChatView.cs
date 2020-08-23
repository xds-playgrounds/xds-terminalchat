using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using TextCopy;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.Dialogs;
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

        readonly IClipboard clipboard;

        readonly Window mainWindow;
        readonly List<string> connectionListEntries;

        TextView textFieldMessageText;
        ListView listViewMessages;
        ListView listViewConnections;

        bool firstEnterHandled;

        public ChatView(Window mainWindow) : base(mainWindow)
        {
            this.mainWindow = mainWindow;
            this.contactListManager = App.ServiceProvider.Get<ContactListManager>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.clipboard = App.ServiceProvider.Get<IClipboard>();

            this.messageThreadView = new MessageThreadView(this.profileViewModel.Name, this.profileViewModel.ChatId, this.contactListManager.CurrentContact.Name, this.contactListManager.CurrentContact.ChatId);
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
                Height = Dim.Fill() - 5,
            };

            this.listViewMessages = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };

            this.messageThreadView.SetListView(this.listViewMessages);

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
                Height = 5
            };
           
            this.textFieldMessageText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(85),
                Height = 3,
                CanFocus = true,
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
            this.textFieldMessageText.SetFocus();

            #endregion


            HotKeys.OnOpenFile = OnOpenFileAsync;

            OnViewReady();


        }



        async void OnOpenFileAsync()
        {
            long maxSize = 25 * 1024 * 1024;
            var openDialog = new OpenDialog("Open", "Open a file") { AllowsMultipleSelection = false, DirectoryPath = GetOpenFileDialogDirectory() };

            Application.Run(openDialog);

            if (openDialog.Canceled)
                return;

            var fi = new FileInfo(openDialog.FilePath.ToString());
            if (!fi.Exists)
                return;

            var fileName = Path.GetFileName(openDialog.FilePath.ToString());

            if (fi.Length > maxSize)
            {
                ErrorBox.Show($"File {fileName} has a size of {GetBytesReadable(fi.Length)} but allowed are only {GetBytesReadable(maxSize)}.");
                return;
            }


            if (MessageBox.Query("Send File?", $"{fileName}, {GetBytesReadable(fi.Length)}", Strings.Ok,
                Strings.Cancel) == 0)
            {
                var fileBytes = File.ReadAllBytes(openDialog.FilePath.ToString());
                await this.messagesViewModel.SendMessage(MessageType.File,
                    $"{fileName}, {GetBytesReadable(fi.Length)}", fileBytes);
                _openFileDialogDirectory = openDialog.DirectoryPath.ToString();
            }

          
        }

        static string _openFileDialogDirectory;

        string GetOpenFileDialogDirectory()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_openFileDialogDirectory) && !Directory.Exists(_openFileDialogDirectory))
                    _openFileDialogDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                return _openFileDialogDirectory;
            }
            catch (Exception)
            {
                return "";
            }
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        // Credits: https://www.somacon.com/p576.php
        public string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
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
                                        info += $" Bytes in: {connection.BytesReceived}, out: {connection.BytesSent}";
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
