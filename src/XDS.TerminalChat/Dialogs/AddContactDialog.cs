using System;
using System.Collections.Generic;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;
using XDS.Messaging.TerminalChat.ChatUI;

namespace XDS.Messaging.TerminalChat.Dialogs
{
    class AddContactDialog : ConsoleDialogBase
    {
        readonly ContactsViewModel contactsViewModel;

        TextField textFieldName;
        TextField textFieldId;
        Label labelError;

        public AddContactDialog()
        {
            this.contactsViewModel = App.ServiceProvider.Get<ContactsViewModel>();
        }

        internal void ShowModal()
        {
            this.Dialog.Title = "Add Contact";

            var labelMessage = new Label("Please enter a name and the XDS ID of your contact.")
            {
                X = Style.XLeftMargin,
                Y = Style.YTopMargin

            };

            var labelName = new Label("Name:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(labelMessage) + 1
            };

            textFieldName = new TextField("")
            {
                X = Pos.Right(labelName) + 3,
                Y = Pos.Top(labelName),
                Width = 50,
                CanFocus = true
            };


            var labelId = new Label("XDS ID:")
            {
                X = Style.XLeftMargin,
                Y = Pos.Bottom(textFieldName) + 1
            };

            this.textFieldId = new TextField("xds1")
            {
                X = Pos.Right(labelId) + 1,
                Y = Pos.Top(labelId),
                Width = 50,
                CanFocus = true
            };

            this.labelError = new Label("                                                   ")
            {
                X = Pos.Left(this.textFieldId),
                Y = Pos.Bottom(this.textFieldId) + 1,
                Width = Dim.Fill()
            };

            textFieldName.CursorPosition = 0;
            textFieldName.PositionCursor();

            var buttonSave = new Button("Save")
            {
                X = Pos.Left(this.textFieldId),
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = () => ValidateIdAsync(true)
            };

            var buttonCancel = new Button("Cancel")
            {
                X = Pos.Right(buttonSave) + 2,
                Y = Pos.Bottom(this.labelError) + 2,
                Clicked = Application.RequestStop
            };

            this.textFieldId.TextChanged = _ => ValidateIdAsync(false);

            this.Dialog.Add(labelMessage, labelName, this.textFieldName, labelId, this.textFieldId, this.labelError, buttonSave, buttonCancel);

            this.Dialog.Ready = () =>
            {
                this.textFieldName.SetFocus();
            };

            Application.Run(this.Dialog);
        }

        async void ValidateIdAsync(bool saveIfValid)
        {
            this.contactsViewModel.AddedContactId = this.textFieldId.Text.ToString();
            this.contactsViewModel.NewName = this.textFieldName.Text.ToString();

            // call this to populate contactsViewModel.CurrentError
            bool isValid = this.contactsViewModel.CanExecuteSaveAddedContactCommand();

            this.labelError.Text = this.contactsViewModel.CurrentError;

            if (!isValid)
            {
                this.labelError.SetNeedsDisplay(this.labelError.Bounds);
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
    }
}
