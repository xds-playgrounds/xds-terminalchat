using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.Workers;
using XDS.SDK.Messaging.BlockchainClient;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class MainView : ConsoleViewBase
    {
        readonly Toplevel topLevel;

        readonly IChatClientConfiguration chatClientConfiguration;
        readonly ICancellation cancellation;
        public readonly ColorScheme colorScheme;

        MenuBar menu;
        Window mainWindow;
        StatusBar statusBar;

        ContactsView contactsView;
        ChatView chatView;

        Action onBackPress;

        public bool IsOnboardingRequired { get; internal set; }

        public MainView(Toplevel topLevel) : base(topLevel)
        {
            this.topLevel = topLevel;

            this.colorScheme = Theme.CreateColorScheme();
            this.topLevel.ColorScheme = this.colorScheme;

            this.chatClientConfiguration = App.ServiceProvider.Get<IChatClientConfiguration>();
            this.cancellation = App.ServiceProvider.Get<ICancellation>();
        }

        public override void Create()
        {
            CreateMainWindow();
            CreateMenu();
            CreateStatusBar();
            this.topLevel.Add(this.menu, this.mainWindow, this.statusBar);

            NavigationService.Init(this.mainWindow, this.statusBar, this.menu);

            HotKeys.OnQuitPressed = Quit;
            HotKeys.OnKillPressed = SelfDestruct;
        }

        public void CreateMainWindow()
        {
            this.mainWindow = new Window(Strings.WindowTitle)
            {
                X = 0,
                Y = 1, // leave one row for the menu
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = this.colorScheme
            };
        }

        void CreateMenu()
        {
            this.menu = new MenuBar(new[] {
                new MenuBarItem ("_File", new[] {
                    new MenuItem ("_Log", "", ShowLiveLogging),
                    new MenuItem ("_Quit", "", Quit)
                }),
                new MenuBarItem ("_About", "", () =>  MessageBox.Query($"About {Strings.WindowTitle}", $"Version: {this.chatClientConfiguration.UserAgentName}", Strings.Ok))
            });

            this.menu.Width = Dim.Fill();

            this.menu.ColorScheme = this.colorScheme;
        }

        void CreateStatusBar()
        {
            this.statusBar = new StatusBar { ColorScheme = this.colorScheme };
        }


        public override void Stop()
        {
            base.Stop();
            Application.RequestStop();
        }


        protected override void OnViewReady()
        {
            if (!this.IsOnboardingRequired)
            {
                NavigationService.ShowLockScreen();
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
            setMasterPassphraseView.OnFinished = () =>
            {
                this.IsOnboardingRequired = false;
                OnViewReady();
            };
            setMasterPassphraseView.Create();
        }


        void ShowLiveLogging()
        {
            MessageBox.Query(Strings.WindowTitle, "Please press 'C' to return to the chat application", Strings.Ok);
            Stop();
        }

        void Quit()
        {
            this.cancellation.Cancel();
            Stop();
        }

        void SelfDestruct()
        {
            if (MessageBox.ErrorQuery("Self-destruct", "Are you sure?", "Yes", "Cancel") == 0)
            {
                this.cancellation.IsSelfDestructRequested = true;
                this.cancellation.Cancel();
                Stop();
            }
        }
    }
}
