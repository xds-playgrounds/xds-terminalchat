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

        #region app screens

        public static void ShowLockScreen()
        {
            SetMinimumStatusBar();

            var unlockView = new LockScreenView(_mainWindow);
            unlockView.Create();
        }

        internal static void ShowContactsView()
        {
            SetContactsViewStatusBar();

            var contactsView = new ContactsView(_mainWindow);
            contactsView.Create();

            PushHistory(ShowContactsView);
        }

        internal static void ShowChatView()
        {
            SetChatStatusBar();

            var chatView = new ChatView(_mainWindow);
            chatView.Create();

            PushHistory(ShowChatView);
        }

        internal static void ShowWalletView()
        {
            SetWalletStatusBar();

            var walletView = new WalletView(_mainWindow);
            walletView.Create();

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
