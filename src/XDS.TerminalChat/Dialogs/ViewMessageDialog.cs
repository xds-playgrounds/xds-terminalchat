using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Chat;
using XDS.Messaging.TerminalChat.ChatUI;
using XDS.SDK.Cryptography;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    class ViewMessageDialog : ConsoleDialogBase
    {
        readonly Message message;
        readonly string ownName;
        readonly string ownId;
        readonly string contactName;
        readonly string contactId;

        Label labelError;

        public ViewMessageDialog(Message message, string ownName, string ownId, string contactName, string contactId)
        {
            this.message = message;
            this.ownName = ownName;
            this.ownId = ownId;
            this.contactName = contactName;
            this.contactId = contactId;
        }

        /// <summary>
        /// If true, the message should be exported as plaintext.
        /// </summary>
        public bool Export { get; internal set; }

        internal void ShowModal()
        {
            var status = "Read (by you)";
            if (this.message.Side == MessageSide.Me)
                status = this.message.SendMessageState.ToString().Replace("Untracable", "Untracked");

            var labelMessage = new Label($"Status: {status}\n\nPlaintext:")
            {
                X = Style.XLeftMargin,
                Y = Style.YTopMargin,
                Height = 3,
                Width = Dim.Fill()
            };

            var labelName = new TextView
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(labelMessage) + 1,
                Width = Dim.Fill(),
                Height = 9
            };

            labelName.ReadOnly = true;
            labelName.Text = this.message.ThreadText;


            var technicalInfo = $"Encrypted Size: {this.message.TextCipher.Length} Bytes, Network Payload Hash: {this.message.NetworkPayloadHash}\n" +
                                $"Encryption: AES 256 Bit CBC Authenticated, Plaintext Compression: Deflate, Forward Secrecy: YES\n" +
                                $"Dynamic Public Key: {this.message.DynamicPublicKey.ToHexString()}\n" +
                                $"Dynamic Public Key ID: {this.message.DynamicPublicKeyId}, Private Key Hint: {this.message.PrivateKeyHint}";

            this.labelError = new Label(technicalInfo)
            {
                X = Pos.Left(labelMessage),
                Y = Pos.Bottom(labelName) + 1,
                Height = 4,
                Width = Dim.Fill()
            };


            var buttonSave = new Button("Export")
            {
                X = Pos.Left(this.labelError),
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = () =>
                {
                    this.Export = true;
                    Application.RequestStop();

                }
            };

            var buttonCancel = new Button("Cancel")
            {
                X = Pos.Right(buttonSave) + 2,
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = Application.RequestStop
            };

            string title;
            if (this.message.Side == MessageSide.You)
            {
               title = $"Message from {this.contactName} ({this.contactId}), sent on {this.message.GetEncrytedDateUtc()} UTC";
            }
            else
            {
                title = $"Message to {this.contactName} ({this.contactId}), sent on {this.message.GetEncrytedDateUtc()} UTC";
            }
            var dialog = new Dialog(title, 0, 0)
            {
                { labelMessage, labelName, this.labelError, buttonSave, buttonCancel }
            };
            dialog.ColorScheme = Application.Top.ColorScheme;
            dialog.Ready = () =>
            {
                //this.textFieldName.SetFocus();
            };
            Application.Run(dialog);
        }

       
    }
}
