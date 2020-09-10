using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    static class NavigationService
    {
        static Window _mainWindow;
        static StatusBar _statusBar;
        static MenuBar _menuBar;

        static readonly Stack<Action> History = new Stack<Action>();

        static Action _showSelf;

        static ConsoleViewBase _currentView;

        internal static void Init(Window window, StatusBar statusBar, MenuBar menuBar)
        {
            _mainWindow = window;
            _statusBar = statusBar;
            _menuBar = menuBar;

            HotKeys.OnBackPress = GoBack;
        }

        #region history

        static void GoBack()
        {
        pop:
            if (History.TryPop(out Action showView))
            {
                if (showView == _showSelf)
                    goto pop;
                showView?.Invoke();
            }
        }

        static void PushHistory(Action showSelf)
        {
            if (showSelf != null)
                History.Push(showSelf);
            _showSelf = showSelf;
        }

        #endregion

        #region cleanup

        static void StopCurrentView()
        {
            if(_currentView != null)
                _currentView.Stop();
        }
        #endregion

        #region app screens

        public static void ShowLockScreen()
        {
            StopCurrentView();
            SetMinimumStatusBar();

            _currentView = new LockScreenView(_mainWindow);
            _currentView.Create();
        }

        internal static void ShowContactsView()
        {
            StopCurrentView();
            SetContactsViewStatusBar();

            _currentView = new ContactsView(_mainWindow);
            _currentView.Create();

            PushHistory(ShowContactsView);
        }

        internal static void ShowGroupsView()
        {
            StopCurrentView();
            SetGroupsStatusBar();

            _currentView = new GroupsView(_mainWindow);
            _currentView.Create();

            PushHistory(ShowGroupsView);
        }

        internal static void ShowChatView()
        {
            StopCurrentView();
            SetChatStatusBar();

            _currentView = new ChatView(_mainWindow);
            _currentView.Create();

            PushHistory(ShowChatView);
        }

        internal static void ShowWalletView()
        {
            StopCurrentView();
            SetWalletStatusBar();

            _currentView = new WalletView(_mainWindow);
            _currentView.Create();

            PushHistory(ShowWalletView);
        }

        #endregion

        #region onboarding screens

        public static void ShowSetupView()
        {
            SetMinimumStatusBar();

            var setupView = new SetupView(_mainWindow);
            setupView.Create();
        }

        internal static void ShowSetPassphraseView()
        {
            SetMinimumStatusBar();

            var setPassphraseView = new SetPassphraseView(_mainWindow);
            setPassphraseView.Create();
        }

        #endregion

        #region status bars

        static void SetMinimumStatusBar()
        {
            _statusBar.RemoveAll();
            _statusBar.Items = new[]
            {
                new StatusItem(Key.ControlQ, "~^Q~ Quit", HotKeys.OnQuitPressed),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
            };
        }

        static void SetWalletStatusBar()
        {
            _statusBar.RemoveAll();
            _statusBar.Items = new[]
            {
                new StatusItem(Key.ControlQ, "~^Q~ Quit", HotKeys.OnQuitPressed),
                new StatusItem(Key.ControlK, "~^K~ Kill Switch", () => { HotKeys.OnKillPressed?.Invoke(); }),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
                new StatusItem(Key.ControlB, "~^B~ Back", () => { HotKeys.OnBackPress?.Invoke(); }),
            };
        }

        static void SetGroupsStatusBar()
        {
            _statusBar.RemoveAll();
            _statusBar.Items = new[]
            {
                new StatusItem(Key.ControlQ, "~^Q~ Quit", HotKeys.OnQuitPressed),
                new StatusItem(Key.ControlK, "~^K~ Kill Switch", () => { HotKeys.OnKillPressed?.Invoke(); }),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
                new StatusItem(Key.ControlB, "~^B~ Back", () => { HotKeys.OnBackPress?.Invoke(); }),
            };
        }

        static void SetChatStatusBar()
        {
            _statusBar.RemoveAll();
            _statusBar.Items = new[]
            {
                new StatusItem(Key.ControlQ, "~^Q~ Quit", HotKeys.OnQuitPressed),
                new StatusItem(Key.ControlK, "~^K~ Kill Switch", () => { HotKeys.OnKillPressed?.Invoke(); }),
                new StatusItem(Key.ControlW, "~^W~ Wallet", () => { NavigationService.ShowWalletView(); }),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
                new StatusItem(Key.ControlB, "~^B~ Back", () => { HotKeys.OnBackPress?.Invoke(); }),
                new StatusItem(Key.ControlO, "~^O~ Send File", () => { HotKeys.OnOpenFile?.Invoke(); }),
            };
        }

        static void SetContactsViewStatusBar()
        {
            _statusBar.RemoveAll();
            _statusBar.Items = new[]
            {
                new StatusItem(Key.ControlQ, "~^Q~ Quit", HotKeys.OnQuitPressed),
                new StatusItem(Key.ControlK, "~^K~ Kill Switch", () => { HotKeys.OnKillPressed?.Invoke(); }),
                new StatusItem(Key.ControlW, "~^W~ Wallet", () => { NavigationService.ShowWalletView(); }),
                new StatusItem(Key.ControlG, "~^G~ Groups", () => { NavigationService.ShowGroupsView(); }),
                new StatusItem(Key.Unknown, GetCurrentUtcDateStringInvariant(), () => { }),
            };
        }

        #endregion

        #region clock

        static string GetCurrentUtcDateStringInvariant()
        {
            return $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC";
        }

        public static void UpdateClockInStatusBar()
        {
            Application.MainLoop?.Invoke(() =>
            {
                var clock = _statusBar?.Items.SingleOrDefault(x => x.Title.ToString().Contains("UTC"));

                if (clock != null)
                {
                    clock.Title = GetCurrentUtcDateStringInvariant();
                    _statusBar.SetNeedsDisplay();
                }
            });
        }



        #endregion
    }
}
