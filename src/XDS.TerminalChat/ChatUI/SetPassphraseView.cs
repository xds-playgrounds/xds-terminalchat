using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Infrastructure;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.SDK.Cryptography.Api.Infrastructure;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class SetPassphraseView : ConsoleViewBase
    {
        readonly Window mainWindow;
        readonly IMessageBoxService messageBoxService;
        readonly OnboardingViewModel onboardingViewModel;
        readonly DeviceVaultService deviceVaultService;

        public Action OnFinished;

        public SetPassphraseView(Toplevel topLevel) : base(topLevel)
        {
            this.mainWindow = (Window)topLevel;
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.onboardingViewModel = App.ServiceProvider.Get<OnboardingViewModel>();
            this.deviceVaultService = App.ServiceProvider.Get<DeviceVaultService>();
        }

        public override void Create()
        {
            this.mainWindow.RemoveAll();
            this.mainWindow.Title = $"Set master passphrase";
            this.mainWindow.ColorScheme = Colors.Dialog;

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

            var buttonAcceptPassphrase = new Button("Set passphrase")
            {
                X = Pos.Right(frameViewInfo) + 1,
                Y = Pos.Bottom(textViewPassphrase) + 1,
            };

            buttonAcceptPassphrase.Clicked = async () =>
            {
                if (string.IsNullOrWhiteSpace(textViewPassphrase.Text.ToString()))
                {
                    MessageBox.ErrorQuery("Error", "The passphrase must not be empty", "OK");
                }
                else
                {
                    this.onboardingViewModel.ValidatedMasterPassphrase = textViewPassphrase.Text.ToString();
                    var quality = this.deviceVaultService.GetPassphraseQuality(this.onboardingViewModel
                            .ValidatedMasterPassphrase);
                    var labelPassphraseAccepted =
                        new Label(
                            $"Your passphrase was set! Passphrase Quality: {this.deviceVaultService.GetPassphraseQualityText(quality)}")
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

                    var success = await this.onboardingViewModel.CommitAll(op.Context);
                    if (success == true)
                    {
                        labelProgressText.Text = "Cryptographic operations complete!";

                        this.mainWindow.Add(new Label("That's it! You can now start chatting with your new encrypted identity!")
                        {
                            X = Pos.Right(frameViewInfo) + 1,
                            Y = Pos.Bottom(labelProgressText) + 1,
                            Width = Dim.Fill()
                        });

                        this.mainWindow.Add(new Button("Start chat")
                        {
                            X = Pos.Right(frameViewInfo) + 1,
                            Y = Pos.Bottom(labelProgressText) + 3,
                            Clicked = Stop

                        });
                    }

                }
            };

            this.mainWindow.Add(buttonAcceptPassphrase);

            this.mainWindow.FocusFirst();
            this.mainWindow.SetNeedsDisplay();
        }

        public override void Stop()
        {
            base.Stop();
            this.mainWindow.RemoveAll();
            this.OnFinished();
        }
    }
}
