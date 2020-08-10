using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.SDK.ApplicationBehavior.Workers;
using XDS.Messaging.TerminalChat.Dialogs;
using XDS.SDK.Cryptography.Api.Infrastructure;
using XDS.SDK.Messaging.BlockchainClient;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class LockScreenView : ConsoleViewBase
    {
        readonly Window mainWindow;
        readonly DeviceVaultService deviceVaultService;
        readonly ProfileViewModel profileViewModel;
        readonly ChatWorker chatWorker;
        readonly ICancellation cancellation;
        readonly PeerManager peerManager;

        public LockScreenView(Window mainWindow) : base(mainWindow)
        {
            this.mainWindow = mainWindow;
            this.deviceVaultService = App.ServiceProvider.Get<DeviceVaultService>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
            this.chatWorker = App.ServiceProvider.Get<ChatWorker>();
            this.cancellation = App.ServiceProvider.Get<ICancellation>();
            this.peerManager = App.ServiceProvider.Get<PeerManager>(); // we need to trigger the c'tor in PeerManager so that it registers as worker
        }

        public override void Create()
        {
            if (Console.CapsLock)
                MessageBox.Query("Vault", "CAPS LOCK is set!", Strings.Ok);

            var labelWelcome = new Label("Welcome,") { X = 1, Y = 1 };

            var labelPlease = new Label("please enter your passphrase to unlock the vault.")
            {
                X = Pos.Left(labelWelcome),
                Y = Pos.Bottom(labelWelcome) + 1
            };

            var labelPassphrase = new Label("Passphrase: ")
            {
                X = Pos.Left(labelWelcome),
                Y = Pos.Bottom(labelPlease) + 2
            };


            var textFieldPassphrase = new TextField("")
            {
                Secret = true,
                X = Pos.Right(labelPassphrase) + 1,
                Y = Pos.Top(labelPassphrase),
                Width = Dim.Percent(50f)
            };

            var buttonOk = new Button(Strings.Ok, true)
            {
                X = Pos.Left(textFieldPassphrase),
                Y = Pos.Bottom(textFieldPassphrase) + 1
            };

            buttonOk.Clicked = () => OnAcceptPassphraseAsync(textFieldPassphrase.Text.ToString());
            this.mainWindow.Add(labelWelcome, labelPlease, labelPassphrase, textFieldPassphrase, buttonOk);
            this.mainWindow.SetFocus(textFieldPassphrase);
        }


        async void OnAcceptPassphraseAsync(string passphrase)
        {
            if (string.IsNullOrEmpty(passphrase))
            {
                ErrorBox.Show("The passphrase is required.");
                return;
            }
                

            var op = new LongRunningOperation(p => { }, () => { });
            try
            {
                this.deviceVaultService.TryLoadDecryptAndSetMasterRandomKeyAsync(passphrase, op.Context).ConfigureAwait(false).GetAwaiter().GetResult();
                await this.profileViewModel.LoadProfile();
                await this.cancellation.StartWorkers();

                NavigationService.ShowContactsView();

            }
            catch (Exception ex)
            {
                var result = MockLocalization.ReplaceKey(ex.Message);

                if (result != ex.Message)
                {
                    MessageBox.Query("Device Vault", "\r\nIncorrect Passphrase.", Strings.Ok);
                }
                else
                    ErrorBox.ShowException(ex);
            }
        }
    }
}
