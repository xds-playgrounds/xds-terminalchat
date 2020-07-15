using System;
using NStack;
using Terminal.Gui;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public class IdGenerationControl : FrameView
    {

        const int VerticalSpace = 3;

        internal FrameView InfoFrame { get; private set; }
        internal ProgressBar ActivityProgressBar { get; private set; }
        internal Action OnCaptureEntropyButtonClick = null;
        public Label LabelProgress;
        public Label LabelDone;
        public Label LabelYourId;
        public Label LabelYourAddress;
        public Label LabelRecovery;


        internal IdGenerationControl(ustring title) : base(title)
        {
            
            this.ColorScheme = Colors.Dialog;

            this.InfoFrame = new FrameView("Info")
            {
                X = 0,
                Y = 0,
                Height = Dim.Percent(100),
                Width = Dim.Percent(25)
            };

            var labelInfoText = new Label(1, 1, "Please press the 'Add entropy' button in intervals, to enhance the entropy of your private key. This can add safety if your system has been hacked.");
            labelInfoText.LayoutStyle = LayoutStyle.Computed;
            this.InfoFrame.Add(labelInfoText);
           

            Add(this.InfoFrame);

            var entropyButton = new Button("Add entropy")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = 0,
                Clicked = CaptureEntropy
            };
            

            Add(entropyButton);
            

            this.ActivityProgressBar = new ProgressBar
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(entropyButton) + 1,
                Width = Dim.Fill(),
                Height = 1,
                Fraction = 0.0f,
                ColorScheme = Colors.Error
            };
            Add(this.ActivityProgressBar);

           

            this.LabelProgress = new Label("0 %  ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.ActivityProgressBar),
            };
            Add(this.LabelProgress);

            this.LabelDone = new Label("                                                                                                       ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelProgress) +1,
            };
            Add(this.LabelDone);

            this.LabelYourId = new Label("                                                                                                       ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelDone) + 1,
               
            };
            Add(this.LabelYourId);

            this.LabelYourAddress= new Label("                                                                                                       ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelYourId) + 1,

            };
            Add(this.LabelYourAddress);

            this.LabelRecovery = new Label("                                                                                                       ")
            {
                X = Pos.Right(this.InfoFrame) + 1,
                Y = Pos.Bottom(this.LabelYourAddress) + 1,

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
