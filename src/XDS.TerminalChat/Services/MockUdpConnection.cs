using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XDS.SDK.Cryptography.NoTLS;
using XDS.SDK.Messaging.CrossTierTypes;

namespace XDS.Messaging.TerminalChat.Services
{
    public class MockUdpConnection : IUdpConnection
    {
        public bool IsConnected => throw new NotImplementedException();

        public async Task<bool> ConnectAsync(string remoteDnsHost, int remotePort, Func<byte[], Transport, Task<string>> receiver = null)
        {
            return true;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<IEnvelope>> SendRequestAsync(byte[] request)
        {
            return new List<IEnvelope>();
        }
    }
}
