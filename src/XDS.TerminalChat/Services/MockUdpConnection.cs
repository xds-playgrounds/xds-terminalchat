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

        public Task<bool> ConnectAsync(string remoteDnsHost, int remotePort, Func<byte[], Transport, Task<string>> receiver = null)
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<IEnvelope>> SendRequestAsync(byte[] request)
        {
            throw new NotImplementedException();
        }
    }
}
