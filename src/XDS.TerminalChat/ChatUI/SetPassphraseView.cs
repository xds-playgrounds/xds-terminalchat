using System;
using System.IO;
using System.Text;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.Dialogs;
using XDS.SDK.Cryptography.Api.Infrastructure;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class SetPassphraseView : ConsoleViewBase
    {
        readonly Window mainWindow;
        readonly IMessageBoxService messageBoxService;
        readonly OnboardingViewModel onboardingViewModel;
        readonly DeviceVaultService deviceVaultService;
        readonly ICancellation cancellation;

        public SetPassphraseView(Toplevel topLevel) : base(topLevel)
        {
            this.mainWindow = (Window)topLevel;
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.onboardingViewModel = App.ServiceProvider.Get<OnboardingViewModel>();
            this.deviceVaultService = App.ServiceProvider.Get<DeviceVaultService>();
            this.cancellation = App.ServiceProvider.Get<ICancellation>();
        }

        public override void Create()
        {
            this.mainWindow.RemoveAll();

            var frameViewInfo = new FrameView("Info")
            {
                X = 0,
                Y = 0,
                Height = Dim.Percent(100),
                Width = Dim.Percent(25)
            };
            var labelInfoText = new Label(1, 1, "Now, please choose your device vault passphrase - it will be used to encrypt all your locally stored data.");
            labelInfoText.LayoutStyle = LayoutStyle.Computed;
            frameViewInfo.Add(labelInfoText);
            this.mainWindow.Add(frameViewInfo);

            var labelPassphrase = new Label("Passphrase:")
            {
                X = Pos.Right(frameViewInfo) + 1,
                Y = 1,
            };
            this.mainWindow.Add(labelPassphrase);
            var textViewPassphrase = new TextField("")
            {
                Secret = true,
                X = Pos.Right(frameViewInfo) + 1,
                Y = Pos.Bottom(labelPassphrase),
                Width = Dim.Fill()
            };
            this.mainWindow.Add(textViewPassphrase);

            var buttonAcceptPassphrase = new Button("Set passphrase", true)
            {
                X = Pos.Right(frameViewInfo) + 1,
                Y = Pos.Bottom(textViewPassphrase) + 1,
            };

            buttonAcceptPassphrase.Clicked = async () =>
            {
                if (string.IsNullOrWhiteSpace(textViewPassphrase.Text.ToString()))
                {
                    ErrorBox.Show("The passphrase must not be empty");
                }
                else
                {
                    this.onboardingViewModel.ValidatedMasterPassphrase = textViewPassphrase.Text.ToString();
                    var quality = this.deviceVaultService.GetPassphraseQuality(this.onboardingViewModel
                            .ValidatedMasterPassphrase);
                    var labelPassphraseAccepted =
                        new Label(
                            $"Your passphrase was set! Passphrase Quality: {this.deviceVaultService.GetPassphraseQualityText(quality)}.")
                        {
                            X = Pos.Right(frameViewInfo) + 1,
                            Y = Pos.Bottom(textViewPassphrase) + 1,
                        };
                    this.mainWindow.Remove(buttonAcceptPassphrase);
                    this.mainWindow.Add(labelPassphraseAccepted);
                    var progressBar = new ProgressBar
                    {
                        X = Pos.Right(frameViewInfo) + 1,
                        Y = Pos.Bottom(labelPassphraseAccepted) + 1,
                        Width = Dim.Fill(),
                        Height = 1,
                        Fraction = 0.0f,
                        ColorScheme = Colors.Error
                    };
                    this.mainWindow.Add(progressBar);
                    var labelProgressText =
                        new Label("                                                                            ")
                        {
                            X = Pos.Right(frameViewInfo) + 1,
                            Y = Pos.Bottom(progressBar) + 1,
                            Width = Dim.Fill()
                        };
                    this.mainWindow.Add(labelProgressText);
                    var op = new LongRunningOperation(encryptionProgress =>
                    {
                        progressBar.Fraction = encryptionProgress.Percent / 100f;
                        labelProgressText.Text =
                            $"{encryptionProgress.Percent} % {MockLocalization.ReplaceKey(encryptionProgress.Message)}";

                    }, () => { });

                    try
                    {
                        await this.onboardingViewModel.CommitAllAsync(op.Context);
                        labelProgressText.Text = "Cryptographic operations complete!";

                        this.mainWindow.Add(
                            new Label("That's it! You can now start chatting with your new encrypted identity!")
                            {
                                X = Pos.Right(frameViewInfo) + 1,
                                Y = Pos.Bottom(labelProgressText) + 1,
                                Width = Dim.Fill()
                            });

                        var exportFilePath = Path.Combine(this.cancellation.DataDirRoot.Parent.ToString(), this.onboardingViewModel.ChatId + ".txt");

                        var choice = MessageBox.ErrorQuery("Cleartext Export",
                            $"Export your XDS ID, XDS Address and Recovery Sentence? Path: {exportFilePath}", "YES", "NO");
                        if (choice == 0)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"XDS ID={this.onboardingViewModel.ChatId}");
                            sb.AppendLine($"XDS Address={this.onboardingViewModel.DefaultAddress}");
                            sb.AppendLine($"Recovery Sentence={this.onboardingViewModel.MasterSentence}");
                            File.WriteAllText(exportFilePath,sb.ToString());
                        }

                        var buttonStartChat = new Button("Start chat")
                        {
                            X = Pos.Right(frameViewInfo) + 1,
                            Y = Pos.Bottom(labelProgressText) + 3,
                            Clicked = () =>
                            {

                                App.IsOnboardingRequired = false;
                                NavigationService.ShowLockScreen();
                            }
                        };

                        this.mainWindow.Add(buttonStartChat);
                        buttonStartChat.SetFocus();
                    }
                    catch (Exception e)
                    {
                        ErrorBox.Show($"Could not commit profile: {e}");
                    }
                }
            };

            this.mainWindow.Add(buttonAcceptPassphrase);


            textViewPassphrase.SetFocus();
        }

    }
}
