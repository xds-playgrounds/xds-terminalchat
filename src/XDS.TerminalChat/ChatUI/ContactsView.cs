using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.SDK.Messaging.CrossTierTypes.Photon;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class ContactsView : ConsoleViewBase
    {
        readonly ContactListManager contactListManager;
        readonly ContactsViewModel contactsViewModel;
        readonly IMessageBoxService messageBoxService;
        readonly ProfileViewModel profileViewModel;
        readonly PhotonWalletManager photonWalletManager;

        readonly Action switchToChatWithCurrentContact;

        Label contactsCountLabel;
        Label walletLabel;

        string defaultAddress;

        readonly Window window;

        public ContactsView(Window mainWindow, Action switchToChatWithCurrentContact) : base(mainWindow)
        {
            this.window = mainWindow;
            this.switchToChatWithCurrentContact = switchToChatWithCurrentContact;

            this.contactListManager = App.ServiceProvider.Get<ContactListManager>();
            this.contactsViewModel = App.ServiceProvider.Get<ContactsViewModel>();
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.photonWalletManager = App.ServiceProvider.Get<PhotonWalletManager>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
        }

        public override async void Create()
        {
            try
            {
                await InitializeModelAsync();
            }
            catch (Exception e)
            {
                await this.messageBoxService.ShowError(e);
            }

            while (!this.contactListManager._isInitialized) // hack, race condition
            {
                Thread.Sleep(100);

            }
            this.window.RemoveAll();

            this.contactsCountLabel = new Label($"You have {this.contactListManager.Conversations.Count} conversations with {this.contactListManager.Contacts.Count} contacts.");
           


            List<string> items = new List<string>();
            foreach (var contact in this.contactListManager.Contacts)
            {
                var contactId = contact.StaticPublicKey != null ? contact.ChatId : contact.UnverfiedId; // TODO Identity.UnverifiedId also in Android App
                items.Add(contactId);
            }



            var listView = new ListView(items)
            {
                X = 0,
                Y = Pos.Bottom(this.contactsCountLabel) + 1,
                Height = Dim.Fill(2),
                Width = 30
            };

            listView.OpenSelectedItem += OnListViewSelected; //(ListViewItemEventArgs e) => lbListView.Text = items[listview.SelectedItem];
            this.window.Add(this.contactsCountLabel,  listView);


            var buttonAddContact = new Button("Add Contact")
            {
                X = 0,
                Y = Pos.Bottom(listView) + 1

            };
            buttonAddContact.Clicked = OnButtonAddContactClicked;
            this.window.Add(buttonAddContact);
            buttonAddContact.FocusFirst();
            this.window.SetNeedsDisplay();
        }

        async Task InitializeModelAsync()
        {
            await this.contactListManager.InitFromStore();
            await this.contactListManager.UpdateContacts(); // this would insert the contact twice in the conversations list, investigate



        }

        void OnListViewSelected(ListViewItemEventArgs e)
        {
            var key = (string)e.Value;

            var contactForChat = this.contactListManager.Contacts.SingleOrDefault(x => x.StaticPublicKey != null && x.ChatId == key);
            if (contactForChat != null)
            {
                this.contactListManager.CurrentContact = contactForChat;
                this.switchToChatWithCurrentContact();
            }
            else
            {
                MessageBox.Query("Contact without public key.", $"Contact '{key}' is not yet valid for sending messages, because its public key has not yet been retrieved from the network.", "Ok");
            }
        }


        void OnButtonAddContactClicked()
        {
            var buttonSave = new Button("Save", true);
            var buttonCancel = new Button("Cancel") { Clicked = Application.RequestStop };
            var buttons = new[] { buttonSave, buttonCancel };

            var dialog = new Dialog("Add Contact", 0, 0, buttons);

            dialog.Add(ChatUIViewFactory.CreateAddContactView(buttonSave, OnSaveAddedContactClicked));
            dialog.Ready = () => OnDialogReady(dialog);
            Application.Run(dialog);
        }

        static void OnDialogReady(Dialog dialog)
        {
            dialog.ColorScheme = Colors.Dialog;
            dialog.Subviews[0].FocusLast();
        }

        async Task OnSaveAddedContactClicked(string addedContactChatId)
        {


            await this.contactsViewModel.ExecuteSaveAddedContactCommand();

            Application.RequestStop();
            Create();
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}
