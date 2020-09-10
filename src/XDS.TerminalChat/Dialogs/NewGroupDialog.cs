using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    class NewGroupDialog : ConsoleDialogBase
    {
        readonly ProfileViewModel profileViewModel;

        TextField textFieldName;
        TextField textFieldId;
        Label labelError;

        public NewGroupDialog()
        {
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
        }

        internal void ShowModal()
        {
            var labelMessage = new Label("Enter the name for the group.")
            {
                X = Style.XLeftMargin,
                Y = Style.YTopMargin

            };

            var labelName = new Label("Your Name:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(labelMessage) + 1
            };

            this.textFieldName = new TextField(this.profileViewModel.Name)
            {
                X = Pos.Right(labelName) + 3,
                Y = Pos.Top(labelName),
                Width = 50,
                CanFocus = true
            };
            this.textFieldName.TextChanged = _ => ValidateNameAsync(false);

            var labelId = new Label("Your XDS ID:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(this.textFieldName) + 1
            };

            this.textFieldId = new TextField(this.profileViewModel.ChatId)
            {
                X = Pos.Right(labelId) + 1,
                Y = Pos.Top(labelId),
                Width = 50,
                ReadOnly = true
            };

            this.labelError = new Label("                                       ")
            {
                X = Pos.Left(this.textFieldId),
                Y = Pos.Bottom(this.textFieldId) + 1,
                Width = Dim.Fill()
            };
            if (!string.IsNullOrEmpty(this.profileViewModel.Name))
                this.textFieldName.CursorPosition = this.profileViewModel.Name.Length;
            this.textFieldName.PositionCursor();

            var buttonSave = new Button("Save")
            {
                X = Pos.Left(this.textFieldId),
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = () => ValidateNameAsync(true)
            };

            var buttonCancel = new Button("Cancel")
            {
                X = Pos.Right(buttonSave) + 2,
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = Application.RequestStop
            };

            this.Dialog.Add(labelMessage, labelName, this.textFieldName, labelId, this.textFieldId, this.labelError, buttonSave, buttonCancel);
            
            this.Dialog.Ready = () =>
            {
                this.textFieldName.SetFocus();
            };

            Application.Run(this.Dialog);
        }

        async void ValidateNameAsync(bool saveIfValid)
        {
            this.profileViewModel.NewName = this.textFieldName.Text.ToString();

            // call this to populate contactsViewModel.RenameError
            bool isValid = this.profileViewModel.CanExecuteRenameCommand();

            this.labelError.Text = this.profileViewModel.RenameError;

            if (!isValid)
            {
                this.labelError.SetNeedsDisplay(this.labelError.Bounds);
                return;
            }
            if (!saveIfValid)
                return;

            try
            {
                await this.profileViewModel.ExecuteRenameCommand();
                Application.RequestStop();
            }
            catch (Exception e)
            {
                ErrorBox.ShowException(e);
            }
        }
    }
}
