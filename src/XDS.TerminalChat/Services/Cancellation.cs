using System.Threading;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;

namespace XDS.Messaging.TerminalChat.Services
{
    public class Cancellation : ICancellation
    {
        public Cancellation()
        {
            this.ApplicationStopping = new CancellationTokenSource();
        }

        public CancellationTokenSource ApplicationStopping { get; }

        public bool CanExit { get; set; }
    }
}
