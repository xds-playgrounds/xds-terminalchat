using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;
using TextCopy;
using Clipboard = Terminal.Gui.Clipboard;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    static class ClipboardMenu
    {
        public static MenuBar Menu { get; set; }

        public static void Copy()
        {
            if (Menu.LastFocused is TextField textField && textField.SelectedLength != 0)
            {
                textField.Copy();
                ClipboardService.SetText(Clipboard.Contents.ToString());
            }
            else if (Menu.LastFocused is TextView textView)
            {
                CopyFrom(textView);
            }
        }

        public static void Cut()
        {
            if (Menu.LastFocused is TextField textField && textField.SelectedLength != 0 && !textField.ReadOnly)
            {
                textField.Cut();
                ClipboardService.SetText(Clipboard.Contents.ToString());
            }
            else if (Menu.LastFocused is TextView textView)
            {
                CutFrom(textView);
            }
        }

        public static void Paste()
        {
            Clipboard.Contents = ClipboardService.GetText();

            if (String.IsNullOrWhiteSpace(Clipboard.Contents.ToString()))
                return;

            if (Menu.LastFocused is TextField textField)
            {
                if (!textField.ReadOnly)
                {
                    textField.Paste();
                    return;
                }

            }

            else if (Menu.LastFocused is TextView textView)
            {
                if (!textView.ReadOnly)
                {
                    textView.ProcessKey(new KeyEvent(Key.ControlY, new KeyModifiers()));
                }

            }
        }

        public static void SelectAll()
        {
            if (Menu.LastFocused is TextField textField)
            {
                textField.SelectedStart = 0;
                textField.SelectedLength = textField.Text.Length;
                textField.SelectedText = textField.Text;
                textField.SetNeedsDisplay(textField.Bounds);
            }
            else if (Menu.LastFocused is TextView textView)
            {
                _textViewSelectAll = textView.Text.ToString();
            }
        }

        public static void PasteTo(TextField textField)
        {
            Clipboard.Contents = ClipboardService.GetText();

            if (String.IsNullOrWhiteSpace(Clipboard.Contents.ToString()) && !textField.ReadOnly)
                return;

            textField.Paste();
        }

        public static void PasteTo(TextView textView)
        {
            Clipboard.Contents = ClipboardService.GetText();

            if (String.IsNullOrWhiteSpace(Clipboard.Contents.ToString()) && !textView.ReadOnly)
                return;

            textView.ProcessKey(new KeyEvent(Key.ControlY, new KeyModifiers()));
        }

        public static void CopyFrom(TextField textField)
        {
            if (textField != null && textField.SelectedLength != 0)
            {
                textField.Copy();
                ClipboardService.SetText(Clipboard.Contents.ToString());
            }
        }

        public static void CopyFrom(TextView textView)
        {
            if (textView != null && textView.Text.ToString() == _textViewSelectAll)
            {
                ClipboardService.SetText(_textViewSelectAll);
                _textViewSelectAll = null;
            }
        }

        public static void CutFrom(TextField textField)
        {
            if (textField != null && textField.SelectedLength != 0 && !textField.ReadOnly)
            {
                textField.Cut();
                ClipboardService.SetText(Clipboard.Contents.ToString());
            }
        }

        public static void CutFrom(TextView textView)
        {
            if (textView != null && textView.Text.ToString() == _textViewSelectAll && !textView.ReadOnly)
            {
                ClipboardService.SetText(_textViewSelectAll);
                _textViewSelectAll = null;
                textView.Text = "";
            }
        }

        public static void PerformEdit(View.KeyEventEventArgs args, TextField textField)
        {
            if (textField == null)
                return;

            try
            {
                switch (args.KeyEvent.Key)
                {
                    case Key.ControlV:
                        PasteTo(textField);
                        args.Handled = true; // meaning we'll handle this key _exclusively_
                        break;
                    case Key.ControlC:
                        CopyFrom(textField);
                        args.Handled = true;
                        break;
                    case Key.ControlX:
                        CutFrom(textField);
                        args.Handled = true;
                        break;
                    case Key.ControlA:
                        textField.SelectedStart = 0;
                        textField.SelectedLength = textField.Text.Length;
                        textField.SelectedText = textField.Text;
                        textField.SetNeedsDisplay(textField.Bounds);
                        args.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // we want catch and ignore the error if the clipboard on some platform throws
            }
        }


        static string _textViewSelectAll;

        public static void PerformEdit(View.KeyEventEventArgs args, TextView textView)
        {
            if (textView == null)
                return;

            try
            {
                switch (args.KeyEvent.Key)
                {
                    case Key.ControlV:
                        PasteTo(textView);
                        args.Handled = true; // meaning we'll handle this key _exclusively_
                        break;
                    case Key.ControlC:
                        CopyFrom(textView);
                        args.Handled = true;
                        break;
                    case Key.ControlX:
                        CutFrom(textView);
                        args.Handled = true;
                        break;
                    case Key.ControlA:
                        _textViewSelectAll = textView.Text.ToString();
                        args.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // we want catch and ignore the error if the clipboard on some platform throws
            }
        }
    }
}
