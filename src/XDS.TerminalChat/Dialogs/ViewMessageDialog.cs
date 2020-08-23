using Terminal.Gui;
using TextCopy;
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

        Label labelTechnicalInfo;

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

            var labelStatus = new Label($"Status: {status}\n\nPlaintext:")
            {
                X = Style.XLeftMargin,
                Y = Style.YTopMargin,
                Height = 3,
                Width = Dim.Fill()
            };

            bool hasLineBreaks = this.message.ThreadText.Contains("\r") || this.message.ThreadText.Contains("\n");

            View labelMessageText;
            if (!hasLineBreaks)
            {
                labelMessageText = new TextField
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(labelStatus) + 1,
                    Width = Dim.Fill(),
                    Height = 9,
                    ReadOnly = true,
                    Text = this.message.ThreadText
                };
            }
            else
            {
                labelMessageText = new TextView
                {
                    X = Style.XLeftMargin,
                    Y = Pos.Bottom(labelStatus) + 1,
                    Width = Dim.Fill(),
                    Height = 9,
                    ReadOnly = true,
                    Text = this.message.ThreadText
                };
            }

          


            var technicalInfo = $"Encrypted Size: {this.message.TextCipher.Length} Bytes, Network Payload Hash: {this.message.NetworkPayloadHash}\n" +
                                $"Encryption: AES 256 Bit CBC Authenticated, Plaintext Compression: Deflate, Forward Secrecy: YES\n" +
                                $"Dynamic Public Key: {this.message.DynamicPublicKey.ToHexString()}\n" +
                                $"Dynamic Public Key ID: {this.message.DynamicPublicKeyId}, Private Key Hint: {this.message.PrivateKeyHint}";

            this.labelTechnicalInfo = new Label(technicalInfo)
            {
                X = Pos.Left(labelStatus),
                Y = Pos.Bottom(labelMessageText) + 1,
                Height = 4,
                Width = Dim.Fill()
            };


            var buttonExport = new Button("Export")
            {
                X = Pos.Left(this.labelTechnicalInfo),
                Y = Pos.Bottom(this.labelTechnicalInfo) + 2,
                Clicked = () =>
                {
                    this.Export = true;
                    Application.RequestStop();

                }
            };

            var buttonCopy = new Button("Copy")
            {
                X = Pos.Right(buttonExport) + 2,
                Y = Pos.Bottom(this.labelTechnicalInfo) + 2,
                Clicked = () =>
                {
                    ClipboardService.SetText(this.message.ThreadText);
                }
            };

            var buttonCancel = new Button("Cancel")
            {
                X = Pos.Right(buttonCopy) + 2,
                Y = Pos.Bottom(this.labelTechnicalInfo) + 2,
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

            this.Dialog.Title = title;

            this.Dialog.Add(labelStatus, labelMessageText, this.labelTechnicalInfo, buttonExport, buttonCopy,
                buttonCancel);
            
           
            Application.Run(this.Dialog);
        }
    }
}
