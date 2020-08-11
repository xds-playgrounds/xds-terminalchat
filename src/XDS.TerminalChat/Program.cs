using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XDS.Messaging.SDK.ApplicationBehavior;
using XDS.Messaging.SDK.ApplicationBehavior.Data;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.AppSupport.NetStandard;
using XDS.Messaging.TerminalChat.Services;
using XDS.SDK.Cryptography.Api.Implementations;
using XDS.SDK.Cryptography.Api.Interfaces;
using XDS.SDK.Cryptography.NetStandard;
using XDS.SDK.Messaging.BlockchainClient;
using XDS.SDK.Messaging.CrossTierTypes;
using XDS.SDK.Messaging.CrossTierTypes.FStore;
using XDS.SDK.Messaging.MessageHostClient;
using XDS.SDK.Messaging.MessageHostClient.Data;

namespace XDS.Messaging.TerminalChat
{
    class Program
    {
        static ILogger _logger;
        static ICancellation _cancellation;

        static async Task<int> Main(string[] args)
        {
            try
            {
                var dataDirRoot = SelectDataDirRoot();

                Configure(dataDirRoot);

                CreateLogger();

                _cancellation = App.ServiceProvider.GetService<ICancellation>();

                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    _cancellation.Cancel();
                };

                App.IsOnboardingRequired = await _cancellation.PrepareLaunch(dataDirRoot);

                App.ShowUI();

                while (!_cancellation.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Please press 'C' to go back to the chat application, 'Ctrl-C' to quit.");
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'c' || key.KeyChar == 'C')
                    {
                        App.ShowUI();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an error running the program: {e}");
                return 1;
            }

            return 0;
        }

        public static DirectoryInfo SelectDataDirRoot()
        {
            const string dir = ".xdschat";
            try
            {
                // Try to put DataDirRoot in the directory of the program. This
                // enables the user to have a portable installation, 
                // or to chat with himself with multiple installations.
                ProcessModule processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    var path = Path.Combine(Path.GetDirectoryName(processModule.FileName), dir);
                    return new DirectoryInfo(path);
                }
            }
            catch { } // if that doesn't work we take the personal folder

            return new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir));
        }

        static void Configure(DirectoryInfo dataDirRoot)
        {
            IServiceCollection serviceCollection = App.CreateServiceCollection();
            AddRequiredServices(serviceCollection, dataDirRoot);
            App.CreateServiceProvider(serviceCollection);
        }

        static void CreateLogger()
        {
            var loggerFactory = App.ServiceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<Program>();
        }

        static void AddRequiredServices(IServiceCollection services, DirectoryInfo dataDirRoot)
        {

            services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });
            services.AddSingleton(GetConfiguration());
            services.AddSingleton<ICancellation, Cancellation>();
            services.AddSingleton<PeerManager>();

            var fStoreConfig = new FStoreConfig
            {
                DefaultStoreName = "FStore",
                StoreLocation = dataDirRoot,
                Initializer = FStoreInitializer.EnsureFStore
            };

            services.AddSingleton(fStoreConfig);
            services.AddSingleton<AppRepository>();
            services.AddSingleton<MessageRelayRecordRepository>();
            services.AddSingleton<PeerRepository>();

            BootstrapperCommon.RegisterServices(services);

            services.AddSingleton<IDispatcher, ConsoleDispatcher>();

            services.AddSingleton<IAssemblyInfoProvider>(new AssemblyInfoProvider { Assembly = typeof(Program).GetTypeInfo().Assembly });
            var crypto = new XDSSecService();
            crypto.Init(new Platform_NetStandard());
            services.AddSingleton<IXDSSecService>(crypto);

            services.AddSingleton<IMessageBoxService>(new MessageBoxService());

            services.AddSingleton<IFileService, FileService>();

            services.AddSingleton<MessageRelayConnectionFactory>();
            services.AddSingleton<ITcpConnection>(x => x.GetRequiredService<MessageRelayConnectionFactory>());
            services.AddSingleton<IMessageRelayAddressReceiver>(x =>
                x.GetRequiredService<MessageRelayConnectionFactory>());

            services.AddSingleton<IUdpConnection, MockUdpConnection>();
            services.AddSingleton<INotificationService, NotificationService>();
        }

        static IChatClientConfiguration GetConfiguration()
        {
            return new ChatClientConfiguration
            {
                UserAgentName = AssemblyVersionUtil.GetProgramDisplayName(typeof(Program).Assembly),
                SeedNodes = new[] { "178.62.62.160", "159.65.148.135", "206.189.33.114", "134.122.89.152", "161.35.156.96", "127.0.0.1" },
            };
        }
    }
}
