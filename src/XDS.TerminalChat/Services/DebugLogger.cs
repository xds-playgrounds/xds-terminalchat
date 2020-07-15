using System;
using Microsoft.Extensions.Logging;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;

namespace XDS.Messaging.TerminalChat.Services
{
	public class DebugLogger : ILog
	{
		readonly ILogger logger;

		public DebugLogger(ILoggerFactory loggerFactory)
		{
			this.logger = loggerFactory.CreateLogger<DebugLogger>();
		}

		public void Debug(string info)
		{
			this.logger.LogDebug(info);
		}

		public void Exception(Exception e, string message = null)
		{
			var msg = message == null ? e.Message : $"{message}: {e.Message}";
			this.logger.LogError(msg);
		}
	}
}
