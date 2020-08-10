using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.TerminalChat.ChatUI;
using XDS.Messaging.TerminalChat.Dialogs;

namespace XDS.Messaging.TerminalChat.Services
{
	public class MessageBoxService : IMessageBoxService
	{
		public async Task<RequestResult> Show(string messageBoxText, string title, RequestButton buttons, RequestImage image)
		{
			int choice;

			switch (buttons)
			{
				case RequestButton.OK:
					MessageBox.Query(title, messageBoxText, Strings.Ok);
					return RequestResult.OK;
				case RequestButton.OKCancel:
					choice = MessageBox.Query(title, messageBoxText, Strings.Ok,"Cancel");
					return choice == 0 ? RequestResult.OK : RequestResult.Cancel;
				case RequestButton.YesNoCancel:
					choice = MessageBox.Query(title, messageBoxText, "Yes", "No", "Cancel");
					return choice == 0 ? RequestResult.Yes : choice == 1 ? RequestResult.No : RequestResult.Cancel;
				case RequestButton.YesNo:
					choice = MessageBox.Query(title, messageBoxText, "Yes", "No");
					return choice == 0 ? RequestResult.Yes : RequestResult.No;
				default:
					return RequestResult.None;
			}
		}

		public async Task ShowError(Exception e, [CallerMemberName] string callerMemberName = "")
		{
			ErrorBox.ShowException(e);
            await Task.CompletedTask;
        }

		public async Task ShowError(string error)
		{
            ErrorBox.Show(error);
			await Task.CompletedTask;
		}
	}
}
