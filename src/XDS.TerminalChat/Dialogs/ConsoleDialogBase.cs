using System;
using Terminal.Gui;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    public class ConsoleDialogBase
    {
        public ConsoleDialogBase()
        {
            this.Dialog = new Dialog();
            this.Dialog.KeyPress += OnDialogKeyPress;
            this.Dialog.ColorScheme = Application.Top.ColorScheme;
        }

        protected Dialog Dialog { get; }

        void OnDialogKeyPress(View.KeyEventEventArgs args)
        {
            if (this.Dialog.MostFocused is TextField textField)
            {
                ClipboardMenu.PerformEdit(args, textField);
            }
            else if (this.Dialog.MostFocused is TextView textView)
            {
                ClipboardMenu.PerformEdit(args, textView);
            }
        }
    }
}
