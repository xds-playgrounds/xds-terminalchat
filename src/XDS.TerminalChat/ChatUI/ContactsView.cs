using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Chat;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.Dialogs;
using XDS.SDK.Cryptography;
using XDS.SDK.Messaging.CrossTierTypes;
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

        ListView contactsListView;
        Label contactsCountLabel;
        Label walletLabel;

        string defaultAddress;

        readonly Window window;

        public ContactsView(Window mainWindow) : base(mainWindow)
        {
            this.window = mainWindow;

            this.contactListManager = App.ServiceProvider.Get<ContactListManager>();

            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.photonWalletManager = App.ServiceProvider.Get<PhotonWalletManager>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
            this.contactsViewModel = App.ServiceProvider.Get<ContactsViewModel>();
        }

        public override async void Create()
        {
            try
            {
                UnSubscribe();
                await InitializeModelAsync();

                while (!this.contactListManager._isInitialized) // hack, race condition
                {
                    Thread.Sleep(100);

                }
                this.window.RemoveAll();

                var maxContactNameLength = 0;
                if (this.contactListManager.Contacts.Any())
                    maxContactNameLength = this.contactListManager.Contacts.Max(x => x.Name.Length);

                var maxNameLength = Math.Max(this.profileViewModel.Name.Length, maxContactNameLength);

                var profile = $"{this.profileViewModel.Name.PadRight(maxNameLength)} | {this.profileViewModel.ChatId.PadRight(14)} | Public Key: {this.profileViewModel.PublicKey.ToHexString()}";

                var labelYourProfile = new Label("Your Profile:")
                {
                    X = Style.XLeftMargin,
                    Y = Style.YTopMargin,
                    Width = Dim.Fill()
                };

                var labelProfile = new Label(profile)
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(labelYourProfile) + 1,
                    Width = Dim.Fill()
                };

                var count = this.contactListManager.Contacts.Count;

                var contactsText = "You have added no contacts yet. You can already receive messages with your XDS ID.";
                if (count == 1)
                    contactsText = "You have 1 contact.";
                if (count > 1)
                    contactsText = $"You have {count} contacts:";


                this.contactsCountLabel = new Label(contactsText)
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(labelProfile) + 2,
                    Width = Dim.Fill(),
                };



                List<string> items = CreateListViewItems(maxNameLength, this.contactListManager.Contacts);

                this.contactsListView = new ListView(items)
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(this.contactsCountLabel) + 1,
                    Height = Dim.Fill(3),
                    Width = Dim.Fill()
                };

                this.contactsListView.OpenSelectedItem += OnListViewSelected; //(ListViewItemEventArgs e) => lbListView.Text = items[listview.SelectedItem];
                this.window.Add(labelYourProfile, labelProfile, this.contactsCountLabel, this.contactsListView);

                this.contactsListView.SelectedItemChanged += OnListViewSelectedChanged;

                var buttonAddContact = new Button("Add Contact", true)
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(this.contactsListView) + 1,
                    Clicked = () =>
                    {
                        UnSubscribe();
                        var addContactDialog = new AddContactDialog();
                        addContactDialog.ShowModal();
                        Create(); // refresh to load added contact
                    }
                };
                this.window.Add(buttonAddContact);

                if (count == 0)
                {
                    while (!buttonAddContact.HasFocus)
                    {
                        this.window.FocusNext();
                    }
                }
                else
                {
                    var buttonEditContact = new Button("Edit Contact")
                    {
                        X = Pos.Right(buttonAddContact) + 3,
                        Y = Pos.Bottom(this.contactsListView) + 1,
                        Clicked = () =>
                        {
                            UnSubscribe();
                            var editContactDialog = new EditContactDialog();
                            editContactDialog.ShowModal();
                            Create(); // refresh to load added contact
                        }
                    };
                    this.window.Add(buttonEditContact);

                    var buttonDeleteContact = new Button("Delete Contact")
                    {
                        X = Pos.Right(buttonEditContact) + 3,
                        Y = Pos.Bottom(this.contactsListView) + 1,
                        Clicked = () =>
                        {
                            if (MessageBox.Query("Delete Contact", $"Delete {this.contactsViewModel.ContactToEdit.Name} ({this.contactsViewModel.ContactToEdit.ChatId})?", "YES", "NO") == 0)
                            {
                                UnSubscribe();
                                this.contactsViewModel.ExecuteDeleteCommand();
                                Create(); // refresh to load added contact
                            }
                            else
                            {
                                return;
                            }

                        }
                    };
                    this.window.Add(buttonDeleteContact);
                    //this.contactsListView.SetFocus();
                }

                var buttonEditProfile = new Button("Edit Profile")
                {
                    X = Pos.Percent(85),
                    Y = Pos.Bottom(this.contactsListView) + 1,
                    Clicked = () =>
                    {
                        UnSubscribe();
                        var addContactDialog = new EditProfileDialog();
                        addContactDialog.ShowModal();
                        Create(); // refresh to load added contact
                    }
                };
                this.window.Add(buttonEditProfile);

                Subscribe();
            }
            catch (Exception e)
            {
                ErrorBox.Show(e.ToString());
            }
           
        }

        async Task InitializeModelAsync()
        {
            await this.contactListManager.InitFromStore();
            await this.contactListManager.UpdateContacts(); // this would insert the contact twice in the conversations list, investigate
        }

        void OnListViewSelected(ListViewItemEventArgs e)
        {
            var key = (string)e.Value;
            var chatId = key.Split("|")[1].Trim();
            var contactForChat = this.contactListManager.Contacts.SingleOrDefault(x => x.StaticPublicKey != null && x.ChatId == chatId);
            if (contactForChat != null)
            {
                UnSubscribe();
                this.contactListManager.CurrentContact = contactForChat;
                NavigationService.ShowChatView();
            }
            else
            {
                MessageBox.Query("Contact without public key.", $"Contact '{chatId}' is not yet valid for sending messages, because its public key has not yet been retrieved from the network.", Strings.Ok);
            }
        }

        void UnSubscribe()
        {
            this.contactListManager.Contacts.CollectionChanged -= Contacts_CollectionChanged;
        }

        void Subscribe()
        {
            this.contactListManager.Contacts.CollectionChanged += Contacts_CollectionChanged;  
        }

        void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.MainLoop?.Invoke(Create);
        }

       static List<string> CreateListViewItems(int maxNameLength, IList<Contact> contacts)
        {
            List<string> items = new List<string>();
            foreach (var contact in contacts)
            {
                var name = contact.Name.PadRight(maxNameLength);
                var contactId = contact.StaticPublicKey != null ? contact.ChatId : contact.UnverfiedId; // TODO Identity.UnverifiedId also in Android App
                string description;
                if (contact.ContactState == ContactState.Valid)
                    description = $"Public Key: {contact.StaticPublicKey.ToHexString()}";
                else if (contact.ContactState == ContactState.Added)
                    description = "Searching the XDS network for this ID's Public Key - Please wait!";
                else
                    description = $"Contact State: {contact.ContactState} - something is wrong here...";

                items.Add($"{name} | {contactId.PadRight(14)} | {description}");
            }

            return items;
        }

        void OnListViewSelectedChanged(ListViewItemEventArgs e)
        {
            var key = (string)e.Value;

            if(key == default)
                return;

            var chatId = key.Split("|")[1].Trim();
            Contact contactToEdit = this.contactListManager.Contacts.SingleOrDefault(x => x.ChatId == chatId);
            this.contactsViewModel.ContactToEdit = contactToEdit;

            // for deleting
            this.contactsViewModel.SelectedContacts.Clear();
            this.contactsViewModel.SelectedContacts.Add(contactToEdit);
        }
    }
}
