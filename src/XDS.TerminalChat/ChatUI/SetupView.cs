using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Microsoft.Extensions.DependencyInjection;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.SDK.Cryptography.Api.Interfaces;
using XDS.SDK.Messaging.BlockchainClient;
using XDS.SDK.Messaging.Principal;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class SetupView : ConsoleViewBase
    {
        readonly Window mainWindow;
        readonly IMessageBoxService messageBoxService;
        readonly Stopwatch stopwatch;
        readonly OnboardingViewModel onboardingViewModel;
        readonly IXDSSecService xdsSecService;
        readonly IChatClientConfiguration chatClientConfiguration;
        readonly ICancellation cancellation;

        public Action OnFinished;

        public SetupView(Toplevel topLevel) : base(topLevel)
        {
            this.mainWindow = (Window)topLevel;
            this.messageBoxService = App.ServiceProvider.Get<IMessageBoxService>();
            this.onboardingViewModel = App.ServiceProvider.Get<OnboardingViewModel>();
            this.stopwatch = new Stopwatch();
            this.xdsSecService = App.ServiceProvider.Get<IXDSSecService>();
            this.chatClientConfiguration = App.ServiceProvider.GetService<IChatClientConfiguration>();
            this.cancellation = App.ServiceProvider.Get<ICancellation>();
        }

        public override async void Create()
        {
            this.mainWindow.RemoveAll();
            this.mainWindow.Title = Strings.WindowTitle;
            var result = await this.messageBoxService.Show(@$"No encrypted profile found! Do you want to create a new XDS ID?

We were searching in:
{this.cancellation.DataDirRoot}
",
                Strings.WindowTitle, RequestButton.YesNo, RequestImage.None);
            if (result == RequestResult.No)
            {
                Application.Top.Running = false;
                return;
            }

            this.stopwatch.Reset();

            // Demo #1 - Use System.Timer (and threading)
            var idGenerationControl = new IdGenerationControl("Generate XDS ID")
            {
                Width = Dim.Percent(100),
            };

            idGenerationControl.OnCaptureEntropyButtonClick = async () =>
            {
                if (!this.stopwatch.IsRunning)
                {
                    RandomCapture.Reset();
                    this.stopwatch.Restart();
                    idGenerationControl.ActivityProgressBar.Fraction = 0f;
                    idGenerationControl.LabelProgress.Text = "0 %";
                }



                for (var i = 0; i < 50; i++)
                {
                    if (RandomCapture.BytesGenerated == RandomCapture.BytesNeeded)
                        break;

                    var data = RandomCapture.CaputureFromPointer(Math.Abs(DateTime.Now.Ticks.GetHashCode() + Guid.NewGuid().GetHashCode()), this.stopwatch.ElapsedMilliseconds);
                    idGenerationControl.ActivityProgressBar.Fraction = (float)data.Percent / 100f;
                    idGenerationControl.LabelProgress.Text = data.Progress;

                }

                if (RandomCapture.BytesGenerated == RandomCapture.BytesNeeded)
                {
                    idGenerationControl.LabelDone.Text = "Complete. Creating your XDS ID...";

                    var entropy96 = this.xdsSecService.CombinePseudoRandomWithRandom(RandomCapture.GeneratedBytes, 3 * 32).Result;

                    var principalFactory = new PrincipalFactory(this.xdsSecService);
                    principalFactory.CreateMnemonics(entropy96);

                    var principal = principalFactory.GetXDSPrincipal();

                    this.onboardingViewModel.OnboardingGenerateIdentity(principal, principalFactory.MasterSentence);

                    await Task.Delay(600);
                    idGenerationControl.LabelDone.Text = "Complete. Creating your XDS ID...done.";
                    idGenerationControl.TextFieldYourId.Text = $"{this.onboardingViewModel.ChatId}";
                    idGenerationControl.TextFieldYourId.Width = Dim.Fill(); // has zero width, make it visible now
                    idGenerationControl.TextFieldYourAddress.Text = $"{this.onboardingViewModel.DefaultAddress}";
                    idGenerationControl.TextFieldYourAddress.Width = Dim.Fill();
                    idGenerationControl.TextFieldYourId.ReadOnly = true;
                    idGenerationControl.TextFieldYourAddress.ReadOnly = true;

                    idGenerationControl.LabelRecovery.Text = "Recovery Sentence:";

                    var textFieldRecoveryCode = new TextField
                    {
                        X = idGenerationControl.LabelRecovery.X,
                        Y = idGenerationControl.LabelRecovery.Y + 1,
                        Width = Dim.Fill(),
                        Height = 5,
                    };

                    textFieldRecoveryCode.ReadOnly = true;
                    textFieldRecoveryCode.LayoutStyle = LayoutStyle.Computed;
                    textFieldRecoveryCode.Text = principalFactory.MasterSentence;
                    idGenerationControl.Add(textFieldRecoveryCode);

                    textFieldRecoveryCode.KeyPress += args =>
                    {
                        if (!textFieldRecoveryCode.HasFocus)
                            return;

                        if (args.KeyEvent.Key == Key.ControlA)
                        {
                            textFieldRecoveryCode.SelectedStart = 0;
                            textFieldRecoveryCode.SelectedLength = textFieldRecoveryCode.Text.Length;
                            textFieldRecoveryCode.SelectedText = principalFactory.MasterSentence;
                            textFieldRecoveryCode.SetNeedsDisplay(textFieldRecoveryCode.Bounds);
                            args.Handled = true;
                        }
                    };


                    var labelYourName =
                        new Label("Your Name (not visible to others):")
                        {
                            X = textFieldRecoveryCode.X,
                            Y = textFieldRecoveryCode.Y + 2,
                        };
                    idGenerationControl.Add(labelYourName);

                    var textFieldName = new TextField("Anonymous")
                    {
                        X = labelYourName.X,
                        Y = labelYourName.Y + 1,
                        Width = Dim.Fill()

                    };
                    idGenerationControl.Add(textFieldName);

                    var buttonContinue = new Button("Continue")
                    {
                        X = textFieldName.X,
                        Y = textFieldName.Y + 3,
                        Clicked = () =>
                        {
                            this.onboardingViewModel.Name = string.IsNullOrWhiteSpace(textFieldName.Text.ToString())
                                ? "Anonymous"
                                : textFieldName.Text.ToString();
                            this.onboardingViewModel.PictureBytes =
                                Guid.NewGuid().ToByteArray(); // pass the checks for null and all-bytes-zero
                            NavigationService.ShowSetPassphraseView();
                        }
                    };
                    idGenerationControl.Add(buttonContinue);
                    buttonContinue.SetFocus();

                    var buttonBackup = new Button("Backup")
                    {
                        X = Pos.Right(buttonContinue) + Style.SpaceBetweenButtons,
                        Y = textFieldName.Y + 3,
                        Clicked = () =>
                        {
                            var exportFilePath = Path.Combine(this.cancellation.GetTempDir(true), this.onboardingViewModel.ChatId + ".txt");

                            var choice = MessageBox.ErrorQuery("Cleartext Export",
                                $"Export your XDS ID, XDS Address and Recovery Sentence? Path: {exportFilePath}", "YES", "NO");
                            if (choice == 0)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("WARNING - THIS FILE CONTAINS SENSITIVE PRIVATE INFORMATION DESCRIBING YOUR XDS ID AND WALLET PRIVATE KEYS.");
                                sb.AppendLine("Using this information, an attacker can decrypt your messages, impersonate and steal your identity and access the XDS coins in your wallet.");
                                sb.AppendLine("It's strongly recommended you create your XDS identity on an air-gapped system and keep this information always offline in a safe place. When you do not need your XDS identity any more, you should destroy this information, so that nobody else can pretend to be you.");
                                sb.AppendLine("====================================");
                                sb.AppendLine($"XDS ID={this.onboardingViewModel.ChatId}");
                                sb.AppendLine($"XDS Address={this.onboardingViewModel.DefaultAddress}");
                                sb.AppendLine($"Recovery Sentence={this.onboardingViewModel.MasterSentence}");
                                File.WriteAllText(exportFilePath, sb.ToString());
                            }

                            if (choice == 0)
                            {
                                MessageBox.ErrorQuery("Success",
                                    $"Recovery sentence saved to: {exportFilePath}\nPlease move this file to a safe place (e.g. USB stick).\nWE WILL DELETE THE TEMP DIRECTORY WHEN YOU QUIT THE APP!", "OK");
                            }
                        }
                    };
                    idGenerationControl.Add(buttonBackup);

                }
            };


            this.mainWindow.Add(idGenerationControl);



            idGenerationControl.EntropyButton.SetFocus();




        }
    }
}
