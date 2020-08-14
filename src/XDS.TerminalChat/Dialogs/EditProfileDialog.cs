using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    class EditProfileDialog : ConsoleDialogBase
    {
        readonly ProfileViewModel profileViewModel;

        TextField textFieldName;
        Label textFieldId;
        Label labelError;

        public EditProfileDialog()
        {
            this.profileViewModel = App.ServiceProvider.Get<ProfileViewModel>();
        }

        internal void ShowModal()
        {
            var labelMessage = new Label("Edit the name of your profile.")
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
                Y = Pos.Bottom(textFieldName) + 1
            };

            this.textFieldId = new Label(this.profileViewModel.ChatId)
            {
                X = Pos.Right(labelId) + 1,
                Y = Pos.Top(labelId),
                Width = 50,
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

            var dialog = new Dialog("Add Contact", 0, 0)
            {
                { labelMessage, labelName, this.textFieldName, labelId, this.textFieldId, this.labelError, buttonSave, buttonCancel }
            };
            dialog.ColorScheme = Application.Top.ColorScheme;
            dialog.Ready = () =>
            {
                this.textFieldName.SetFocus();
            };
            Application.Run(dialog);
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
