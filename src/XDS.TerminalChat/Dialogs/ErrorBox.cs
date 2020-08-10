using System;
using NStack;
using Terminal.Gui;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    public static class ErrorBox
    {
        public static void ShowException(Exception e)
        {
            MessageBox.ErrorQuery(Strings.Error, e.Message, new ustring[] {Strings.Ok});
        }

        public static void Show(string errorText)
        {
            MessageBox.ErrorQuery(Strings.Error, errorText, new ustring[] { Strings.Ok });
        }
    }
}
