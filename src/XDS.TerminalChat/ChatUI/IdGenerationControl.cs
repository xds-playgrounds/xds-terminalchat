using System;
using NStack;
using Terminal.Gui;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class IdGenerationControl : FrameView
    {

        internal FrameView InfoFrame { get; private set; }
        internal ProgressBar ActivityProgressBar { get; private set; }
        internal Action OnCaptureEntropyButtonClick = null;
        public Label LabelProgress;
        public Label LabelDone;
        public TextField TextFieldYourId;
        public TextField TextFieldYourAddress;
        public Label LabelRecovery;
        public Button EntropyButton;

        internal IdGenerationControl(ustring title) : base(title)
        {

            this.InfoFrame = new FrameView("Info")
            {
                X = 0,
                Y = 0,
                Height = Dim.Percent(100),
                Width = Dim.Percent(25)
            };

            var labelInfoText = new Label(1, 1, "Please press the 'Add Entropy' button in erratic intervals, to enhance the entropy of your private key. This adds safety when your system's RNG is compromised.");
            labelInfoText.LayoutStyle = LayoutStyle.Computed;
            this.InfoFrame.Add(labelInfoText);


            Add(this.InfoFrame);

            this.EntropyButton = new Button("Add Entropy")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = 0,
                Clicked = CaptureEntropy
            };


            Add(this.EntropyButton);

            this.ActivityProgressBar = new ProgressBar
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.EntropyButton) + 1,
                Width = Dim.Fill(),
                Height = 1,
                Fraction = 0.0f,
                //ColorScheme = Colors.Error
            };
            Add(this.ActivityProgressBar);



            this.LabelProgress = new Label("0 %  ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.ActivityProgressBar),
                Width = Dim.Fill()
            };
            Add(this.LabelProgress);

            this.LabelDone = new Label("")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelProgress) + 1,
                Width = Dim.Fill()
            };
            Add(this.LabelDone);

            var labelYourId = new Label("Your XDS ID:")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelDone) + 2,
            };

            this.TextFieldYourId = new TextField("")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(labelYourId),
                Width = 0,
                ReadOnly = true
            };
            Add(labelYourId, this.TextFieldYourId);

            var labelYourAddress = new Label("Your XDS Address: ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.TextFieldYourId) + 1,
            };

            this.TextFieldYourAddress = new TextField("")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(labelYourAddress),
                Width = 0,
                ReadOnly = true

            };
            Add(labelYourAddress,this.TextFieldYourAddress);

            this.LabelRecovery = new Label("")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.TextFieldYourAddress) + 1,
                Width = Dim.Fill(),

            };
            Add(this.LabelRecovery);


            // Set height to height of controls + spacing + frame
            this.Height = Dim.Fill();
        }

        internal void CaptureEntropy()
        {
            this.OnCaptureEntropyButtonClick?.Invoke();
        }
    }
}
