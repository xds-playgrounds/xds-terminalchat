using Terminal.Gui;

namespace XDS.Messaging.TerminalChat.ChatUI
{
	public abstract class ConsoleViewBase
	{
		/// <summary>
		/// Indicates that the elements of the view are ready to use.
		/// </summary>
		protected bool IsViewReady;
		
		/// <summary>
		/// Creates a new ConsoleView and hooks it up with the Ready event of the parent.
		/// </summary>
		/// <param name="topLevel"></param>
		protected ConsoleViewBase(Toplevel topLevel)
		{
			topLevel.Ready = OnViewReady;
			topLevel.RemoveAll();
		}

		/// <summary>
		/// Sets IsViewReady to true.
		/// </summary>
		protected virtual void OnViewReady()
		{
            this.IsViewReady = true;
		}

		/// <summary>
		/// The place to create the view elements and connect them with the parent.
		/// </summary>
		public abstract void Create();

		/// <summary>
		/// The place to tear down the view and stop all services that are specific to this view.
		/// Sets IsViewReady to false.
		/// </summary>
		public virtual void Stop()
		{
            this.IsViewReady = false;
		}
	}
}
