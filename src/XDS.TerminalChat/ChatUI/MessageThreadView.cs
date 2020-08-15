using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Chat;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.Dialogs;
using XDS.SDK.Cryptography.Api.DataTypes;
using XDS.SDK.Cryptography.Api.Interfaces;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class MessageThreadView : IChatView
    {
        readonly IDispatcher dispatcher;

        readonly string ownName;
        readonly string contactName;

        ListView ListView { get; set; }
        List<Message> messages;
        readonly List<string> textLines = new List<string>();

        public MessageThreadView(string ownName, string contactName)
        {
            this.ownName = ownName;
            this.contactName = contactName;
            this.dispatcher = App.ServiceProvider.Get<IDispatcher>();
        }

        public async Task OnMessageAddedAsync(Message message)
        {
            this.messages.Add(message);
            UpdateUI();
            await Task.CompletedTask;
        }

        public async Task OnMessageDecrypted(Message message)
        {
            Tuple<Message, int> messageInThread = FindMessageToUpdate(message);
            if (messageInThread != null)
            {
                this.messages[messageInThread.Item2] = message;
                UpdateUI();
            }
            await Task.CompletedTask;
        }

        public async Task OnMessageEncrypted(Message message)
        {
            Tuple<Message, int> messageInThread = FindMessageToUpdate(message);
            if (messageInThread != null)
            {
                this.messages[messageInThread.Item2] = message;
                UpdateUI();
            }
            await Task.CompletedTask;
        }

        public async Task OnThreadLoaded(IReadOnlyCollection<Message> messages)
        {
            this.messages = messages.ToList();
            UpdateUI();
            await Task.CompletedTask;
        }

        void UpdateUI()
        {
            this.textLines.Clear();

            foreach (Message message in this.messages)
            {
                if (message.Side == MessageSide.Me)
                    this.textLines.Add($"{this.ownName}: {message.ThreadText} ({message.SendMessageState})");
                else
                    this.textLines.Add($"{this.contactName}: {message.ThreadText}");
            }

            this.dispatcher.Run(() =>
            {
                this.ListView.SetSource(this.textLines);
                if (this.textLines.Count > 0)
                    this.ListView.SelectedItem = this.textLines.Count - 1;
            });
        }

        Tuple<Message, int> FindMessageToUpdate(Message message)
        {
            var messageToUpdate = this.messages.SingleOrDefault(x => x.Id == message.Id);
            if (messageToUpdate == null)
                return null;
            return new Tuple<Message, int>(messageToUpdate, this.messages.IndexOf(messageToUpdate));
        }

        internal void SetListView(ListView listViewMessages)
        {
            this.ListView = listViewMessages;
            this.ListView.OpenSelectedItem += OpenMessage;
        }

        void OpenMessage(ListViewItemEventArgs args)
        {
            try
            {
                var message = this.messages[args.Item];
                SaveFileToDownloads(message);
            }
            catch (Exception e)
            {
                ErrorBox.ShowException(e);
            }
        }
        void SaveFileToDownloads(Message message)
        {

            var fileName = message.ThreadText.Split(',').First().Trim();
            var appDir = App.ServiceProvider.Get<ICancellation>().DataDirRoot.Parent;
            var exportDir = Path.Combine(appDir.FullName, "temp");
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            var xdsSec = App.ServiceProvider.Get<IXDSSecService>();



            var decryptionkey = new KeyMaterial64(xdsSec.DefaultDecrypt(message.EncryptedE2EEncryptionKey, xdsSec.SymmetricKeyRepository.GetMasterRandomKey()));
            var plaintextBytes = xdsSec.DefaultDecrypt(message.ImageCipher, decryptionkey);

            File.WriteAllBytes(Path.Combine(exportDir, fileName), plaintextBytes);


            MessageBox.Query("File decrypted",
                $"The file was decrypted and saved as {Path.Combine(exportDir, fileName)}. We'll try to delete it when you quit the app!", Strings.Ok);

        }



        public void UpdateSendMessageStateFromBackgroundThread(Message message)
        {
            Tuple<Message, int> messageInThread = FindMessageToUpdate(message);
            if (messageInThread != null)
            {
                this.messages[messageInThread.Item2].SendMessageState = message.SendMessageState;
                UpdateUI();
            }
        }
    }
}

