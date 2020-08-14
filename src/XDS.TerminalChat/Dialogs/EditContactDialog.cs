using System;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    class EditContactDialog : ConsoleDialogBase
    {
        readonly ContactsViewModel contactsViewModel;

        TextField textFieldName;
        Label textFieldId;
        Label labelError;

        public EditContactDialog()
        {
            this.contactsViewModel = App.ServiceProvider.Get<ContactsViewModel>();
        }

        internal void ShowModal()
        {
            var labelMessage = new Label("Edit the name of your contact.")
            {
                X = Style.XLeftMargin,
                Y = Style.YTopMargin

            };

            var labelName = new Label("Name:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(labelMessage) + 1
            };

            this.textFieldName = new TextField(this.contactsViewModel.ContactToEdit.Name)
            {
                X = Pos.Right(labelName) + 3,
                Y = Pos.Top(labelName),
                Width = 50,
                CanFocus = true
            };
            this.textFieldName.TextChanged = _ => ValidateNameAsync(false);

            var labelId = new Label("XDS ID:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(textFieldName) + 1
            };

            this.textFieldId = new Label(this.contactsViewModel.ContactToEdit.ChatId)
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
            if (!string.IsNullOrEmpty(this.contactsViewModel.ContactToEdit.Name))
                this.textFieldName.CursorPosition = this.contactsViewModel.ContactToEdit.Name.Length;
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

        async void ValidateIdAsync(bool saveIfValid)
        {
            this.contactsViewModel.AddedContactId = this.textFieldId.Text.ToString();
            this.contactsViewModel.NewName = this.textFieldName.Text.ToString();

            // call this to populate contactsViewModel.CurrentError
            bool isValid = this.contactsViewModel.CanExecuteSaveAddedContactCommand();

            this.labelError.Text = this.contactsViewModel.CurrentError;
            this.labelError.SetNeedsDisplay(this.labelError.Bounds);
            this.labelError.Redraw(this.labelError.Bounds);

            if (!isValid)
            {
                return;
            }
            if (!saveIfValid)
                return;

            try
            {
                await this.contactsViewModel.ExecuteSaveAddedContactCommand();
                Application.RequestStop();
            }
            catch (Exception e)
            {
                ErrorBox.ShowException(e);
            }
        }


        async void ValidateNameAsync(bool saveIfValid)
        {
            this.contactsViewModel.NewName = this.textFieldName.Text.ToString();

            // call this to populate contactsViewModel.RenameError
            bool isValid = this.contactsViewModel.CanExecuteRenameContactCommand();

            this.labelError.Text = this.contactsViewModel.RenameError;

            if (!isValid)
            {
                this.labelError.SetNeedsDisplay(this.labelError.Bounds);
                return;
            }
            if (!saveIfValid)
                return;

            try
            {
                await this.contactsViewModel.ExecuteRenameContactCommand();
                Application.RequestStop();
            }
            catch (Exception e)
            {
                ErrorBox.ShowException(e);
            }
        }
    }
}
