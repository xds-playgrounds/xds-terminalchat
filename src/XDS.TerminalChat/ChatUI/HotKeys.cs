using System;
using System.Collections.Generic;
using System.Text;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    static class HotKeys
    {
        /// <summary>
        /// When Ctrl-C is pressed.
        /// </summary>
        public static Action OnBackPress { get; internal set; }

        public static Action OnQuitPressed { get; internal set; }

        public static Action OnKillPressed { get; internal set; }
    }
}
