using System;
using System.Diagnostics;
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

                    this.onboardingViewModel.OnboardingGenerateIdentity(principal);

                    await Task.Delay(600);
                    idGenerationControl.LabelDone.Text = "Complete. Creating your XDS ID...done.";
                    idGenerationControl.LabelYourId.Text = $"Your XDS ID: {this.onboardingViewModel.ChatId}";
                    idGenerationControl.LabelYourAddress.Text = $"Your XDS Receive Address: {this.onboardingViewModel.DefaultAddress}";
                    idGenerationControl.LabelYourId.ColorScheme = Colors.Error;
                    idGenerationControl.LabelYourAddress.ColorScheme = Colors.Error;

                    idGenerationControl.LabelRecovery.Text = "Recovery Sentence:";
                    var textFieldRecoveryCode = new TextView()
                    {
                        X = idGenerationControl.LabelRecovery.X,
                        Y = idGenerationControl.LabelRecovery.Y + 1,
                        Width = Dim.Fill(),
                        Height = 5
                    };
                    textFieldRecoveryCode.ReadOnly = true;
                    textFieldRecoveryCode.LayoutStyle = LayoutStyle.Computed;
                    textFieldRecoveryCode.Text = principalFactory.MasterSentence;
                    idGenerationControl.Add(textFieldRecoveryCode);


                    var labelYourName =
                        new Label("Please enter a name for yourself - only you will see this!!!")
                        {
                            X = textFieldRecoveryCode.X,
                            Y = textFieldRecoveryCode.Y + 2,
                        };
                    idGenerationControl.Add(labelYourName);

                    var textFieldName = new TextField("Me")
                    {
                        X = labelYourName.X,
                        Y = labelYourName.Y + 2,
                        Width = Dim.Fill()

                    };
                    idGenerationControl.Add(textFieldName);

                    idGenerationControl.Add(new Button("Continue")
                    {
                        X = textFieldName.X,
                        Y = textFieldName.Y + 2,
                        Clicked = () =>
                        {
                            this.onboardingViewModel.Name = string.IsNullOrWhiteSpace(textFieldName.Text.ToString()) ? "Bob" : textFieldName.Text.ToString();
                            this.onboardingViewModel.PictureBytes = Guid.NewGuid().ToByteArray(); // pass the checks for null and all-bytes-zero
                            this.OnFinished();
                        }
                    });

                }
            };


            this.mainWindow.Add(idGenerationControl);


        }
    }
}
