using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.SDK.ApplicationBehavior.Workers;
using XDS.SDK.Cryptography.Api.Infrastructure;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class MainView : ConsoleViewBase
    {
        readonly Toplevel topLevel;

        readonly IDispatcher dispatcher;
        readonly DeviceVaultService deviceVaultService;
        readonly ProfileViewModel profileViewModel;
        readonly ChatWorker chatWorker;

        MenuBar menu;
        Window mainWindow;
        StatusBar statusBar;

        ContactsView contactsView;
        ChatView chatView;

        Action onBackPress;

        public MainView(Toplevel topLevel) : base(topLevel)
        {
            this.topLevel = topLevel;

            this.dispatcher = App.ServiceProvider.Get<IDispatcher>();
            this.deviceVaultService = App.ServiceProvider.Get<DeviceVaultService>();
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
            this.chatWorker = App.ServiceProvider.Get<ChatWorker>();
        }

        public override void Create()
        {
            CreateMainWindow();
            CreateMenu();
            CreateStatusBar();

            this.topLevel.Add(this.menu, this.mainWindow, this.statusBar);

        }

        public void CreateMainWindow()
        {
            this.mainWindow = new Window(ChatUIViewFactory.WindowTitleLocked)
            {
                X = 0,
                Y = 1, // leave one row for the menu
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
        }

        void CreateMenu()
        {
            this.menu = new MenuBar(new[] {
                new MenuBarItem ("_File", new[] {
                    new MenuItem ("_Quit", "", StopApplication)
                }),
                new MenuBarItem ("_About XDS Chat", "", () =>  MessageBox.Query(50, 7, "XDS Chat", "Version: v. 1.0.0", "Ok"))
            });
        }
        void CreateStatusBar()
        {
            this.statusBar = new StatusBar(new[]
            {
                //new StatusItem(Key.F1, "~F1~ Help", () => { }),
                new StatusItem(Key.ControlQ, "~^Q~ Quit", StopApplication),
                new StatusItem(Key.ControlC, "~^C~ Back", () =>
                {
                    this.onBackPress?.Invoke();
                }),
                new StatusItem(Key.ControlK, "~^K~ Kill Switch", async () =>
                {
                    try
                    {
                        if (MessageBox.ErrorQuery("Self-destruct", "Really...?", "Yes!!!", "Cancel") == 0)
                        {
                            await this.deviceVaultService.DeleteAllData();
                            if (Directory.Exists(FStoreInitializer.FStoreConfig.StoreLocation.ToString()))
                            {
                                Directory.Delete(FStoreInitializer.FStoreConfig.StoreLocation.ToString(),true);
                            }
                            Environment.Exit(666);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.ErrorQuery($"Error in self-destruct sequence", e.Message);
                        Environment.Exit(1);
                    }

                }),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
            });
        }

        void StopApplication()
        {
            Application.RequestStop();
        }

        public override async void Stop()
        {
            base.Stop();
            this.contactsView?.Stop();
            await this.chatWorker.StopRunLoopAndDisconnectAll();
            this.deviceVaultService.ClearMasterRandomKey();
        }

        public void UpdateClockInStatusBar()
        {
            this.dispatcher.Run(() =>
            {
                if (this.IsViewReady)
                {
                    this.statusBar.Items.Last().Title = GetCurrentUtcDateStringInvariant();
                    this.statusBar.SetNeedsDisplay();
                }
            });
        }

        protected override void OnViewReady()
        {
            this.mainWindow.Title = ChatUIViewFactory.WindowTitleLocked;
            this.mainWindow.ColorScheme = Colors.Dialog;

            var isOnboardingRequired = AsyncMethod.RunSync(() => this.deviceVaultService.CheckIfOnboardingRequired());

            if (!isOnboardingRequired)
            {
                var unlockView = ChatUIViewFactory.CreateUnlockView(OnAcceptPassphraseAsync);

                this.mainWindow.Add(unlockView);
                this.mainWindow.FocusFirst();
            }
            else
            {
                var setupView = new SetupView(this.mainWindow);
                setupView.OnFinished = OnSetupViewFinished;
                setupView.Create();
            }


            base.OnViewReady();
        }

        void OnSetupViewFinished()
        {
            var setMasterPassphraseView = new SetPassphraseView(this.mainWindow);
            setMasterPassphraseView.OnFinished = OnViewReady;
            setMasterPassphraseView.Create();
        }

        async Task OnAcceptPassphraseAsync(string passphrase)
        {
            var op = new LongRunningOperation(p => { }, () => { });
            try
            {
                this.deviceVaultService.TryLoadDecryptAndSetMasterRandomKey(passphrase, op.Context).ConfigureAwait(false).GetAwaiter().GetResult();

                await this.profileViewModel.LoadProfile();
                await this.chatWorker.InitAsync();
                this.chatWorker.StartRunning();

                this.mainWindow.Title = $"Hey {this.profileViewModel.Name}, your chat id is {this.profileViewModel.ChatId} and your address is {this.profileViewModel.DefaultAddress} - unlocked";
                this.mainWindow.SetNeedsDisplay();

                DisplayContactsView();

            }
            catch (Exception ex)
            {
                var result = MockLocalization.ReplaceKey(ex.Message);

                if (result != ex.Message)
                {
                    MessageBox.Query(45, 6, "Device Vault", "\r\nIncorrect Passphrase.", "Ok");
                }
                else
                    MessageBox.ErrorQuery(50, 7, "Error", ex.Message, "Ok");
            }
        }

        void DisplayContactsView()
        {
            this.contactsView = new ContactsView(this.mainWindow, OnChatContactSelected);
            this.contactsView.Create();
        }

        void OnChatContactSelected()
        {
            this.chatView = new ChatView(this.mainWindow);
            this.chatView.OnFinished = DisplayContactsView;
            this.chatView.Create();
            this.onBackPress = this.chatView.Stop;
        }

        static string GetCurrentUtcDateStringInvariant()
        {
            return $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC";
        }
    }
}
