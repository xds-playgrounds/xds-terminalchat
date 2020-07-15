using System;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.ViewModels;

namespace XDS.Messaging.TerminalChat.ChatUI
{
	class ChatUIViewFactory
    {
        public const string WindowTitleLocked = "Anonymous - Vault Locked";

		public static View[] CreateUnlockView(Func<string, Task> onAcceptPassphrase)
		{
			var labelMessage = new Label("Welcome, please enter your passphrase to unlock the vault.") { X = 1, Y = 1 };
			var labelPassphrase = new Label("Passphrase: ")
			{
				X = Pos.Left(labelMessage),
				Y = Pos.Bottom(labelMessage) + 1
			};


			var textFieldPassphrase = new TextField("")
			{
				Secret = true,
				X = Pos.Right(labelPassphrase) + 1,
				Y = Pos.Top(labelPassphrase),
				Width = 50
			};


			var buttonOk = new Button(13, 5, "Ok", true);

			buttonOk.Clicked = () => onAcceptPassphrase(textFieldPassphrase.Text.ToString());
			return new View[] { labelMessage, labelPassphrase, textFieldPassphrase, buttonOk };
		}

		public static View[] CreateAddContactView(Button buttonSave, Func<string, Task> onSaveContactClicked)
		{
			var labelMessage = new Label("Please enter the XDS ID of your contact.") { X = 1, Y = 1 };
			var labelId = new Label("XDS ID: ")
			{
				X = Pos.Left(labelMessage),
				Y = Pos.Bottom(labelMessage) + 1
			};


			var textFieldId = new TextField("")
			{
				X = Pos.Right(labelId) + 1,
				Y = Pos.Top(labelId),
				Width = 50,
				CanFocus = true
			};

			var labelError = new Label("                                ")
			{
				X = Pos.Left(labelMessage),
				Y = Pos.Bottom(textFieldId) + 1
			};

			//labelError.ColorScheme = Colors.Error;

			textFieldId.ColorScheme = Colors.Menu;
			textFieldId.CursorPosition = 0;
			textFieldId.PositionCursor();

			var model = App.ServiceProvider.Get<ContactsViewModel>();

			textFieldId.TextChanged = _ =>
			{
				model.AddedContactId = textFieldId.Text.ToString();
				if (!model.CanExecuteSaveAddedContactCommand())
				{
					labelError.Text = model.CurrentError;
					labelError.SetNeedsDisplay(labelError.Bounds);
					buttonSave.Clicked = () => MessageBox.ErrorQuery("Error", model.CurrentError, "Ok");
				}
				else
				{
					labelError.Text = model.CurrentError;
					buttonSave.Clicked = () => onSaveContactClicked(textFieldId.Text.ToString());
				}
			};


			buttonSave.Clicked = () => onSaveContactClicked(textFieldId.Text.ToString());
			return new View[] { labelMessage, labelId, textFieldId, labelError };
		}

		


	}
}
