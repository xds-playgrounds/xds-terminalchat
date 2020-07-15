using System;
using System.Threading.Tasks;
using Terminal.Gui;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;

namespace XDS.Messaging.TerminalChat.Services
{
    public class ConsoleDispatcher : IDispatcher
    {
        public void Run(Action action)
        {
            if (action != null)
                Application.MainLoop?.Invoke(action);
        }

        public async Task RunAsync(Func<Task> action)
        {
            Application.MainLoop?.Invoke(async () =>
            {
                if (action != null) await action().ConfigureAwait(false);
            });
            await Task.CompletedTask;
        }
    }
}
