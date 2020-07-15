using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Settings;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;

namespace XDS.Messaging.TerminalChat.Services
{
    public class ConsoleSettingsManager : AbstractSettingsManager
	{
		public ConsoleSettingsManager(IDependencyInjection dependencyInjection, ILoggerFactory loggerFactory) : base(dependencyInjection, loggerFactory) { }

		public override string CurrentDirectoryName
		{
			get { return FStoreInitializer.FStoreConfig.StoreLocation.FullName; }
			set
			{
				throw new NotImplementedException();
			}
		}

		public override void FactorySettings()
		{
			this.Logger.LogInformation("Applying factory settings.");

			this.ChatSettings = new ChatSettings
			{
				Hosts = new System.Collections.Generic.List<HostRecord>(new[] { new HostRecord { DnsIp = "52.136.231.134", Port = 55558, Label = "Default", IsSelected = true } }),
				Interval = 1000,
			};
			
			this.CryptographySettings = new CryptographySettings { LogRounds = 10 };
			this.UpdateSettings = new UpdateSettings
			{
				Version = this.Aip.AssemblyVersion,
				SKU = this.Aip.AssemblyProduct,
				Date = DateTime.UtcNow,
				Notify = true
			};
		}

		protected override string ReadSettingsFile()
		{
			var settingsFilename = Path.Combine(this.CurrentDirectoryName, SettingsFilename);
			if (File.Exists(settingsFilename))
				return File.ReadAllText(settingsFilename, Encoding.Unicode);
			return null;
		}

		protected override void WriteSettingsFile(string settingsFile)
		{
			File.WriteAllText(Path.Combine(this.CurrentDirectoryName, SettingsFilename), settingsFile, Encoding.Unicode);
		}
	}
}
