using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XDS.Messaging.SDK.ApplicationBehavior;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Chat;
using XDS.Messaging.SDK.ApplicationBehavior.Models.Serialization;
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
        static bool _isEscapePressed;
        static PeerManager _peerManager;

        static void Main(string[] args)
        {
            InitServices();

            CreateLogger();

            _logger.LogInformation("Press 'ESC' to shut down, 'c' to show the chat ui.");

            InitStorage();

            StartSearchingForMessageNodes();

            try
            {
                

                while (!_isEscapePressed)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        _isEscapePressed = true;
                    else
                    {
                        if (key.KeyChar == 'c')
                        {
                            App.ShowUI();
                        }
                    }
                }

                _logger.LogInformation("ESC was pressed, cancelling...");
                _cancellation.ApplicationStopping.Cancel();
                _peerManager.WaitForShutdown().Wait();

                _logger.LogInformation("Press any key to exit.");
                Console.ReadKey();

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        static void StartSearchingForMessageNodes()
        {
            _peerManager = App.ServiceProvider.GetService<PeerManager>();
            _peerManager.AddSeedNodesIfMissing().Wait();

            Task.Run(_peerManager.Start);

            Task.Run(() =>
            {
                while (!_cancellation.ApplicationStopping.IsCancellationRequested)
                {
                    try
                    {
                        Task.Delay(5000, _cancellation.ApplicationStopping.Token).Wait();
                        _peerManager.PrintStatus();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Status task stopped: {e.Message}");
                        break;
                    }
                }
            });
        }

        static void InitServices()
        {
            IServiceCollection serviceCollection = App.CreateServiceCollection();
            AddRequiredServices(serviceCollection);
            App.CreateServiceProvider(serviceCollection);
        }

        static void CreateLogger()
        {
            var loggerFactory = App.ServiceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<Program>();
            _cancellation = App.ServiceProvider.GetService<ICancellation>();
        }

        static void InitStorage()
        {
            FStoreInitializer.InitFStore();
        }

        static void AddRequiredServices(IServiceCollection services)
        {

            services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });
            services.AddSingleton(GetConfiguration());
            services.AddSingleton<ICancellation,Cancellation>();
            services.AddSingleton<PeerManager>();

            var fStoreConfig = FStoreInitializer.FStoreConfig;
            services.AddSingleton<IAsyncRepository<Profile>>(new FStoreRepository<Profile>(new FStoreMono(fStoreConfig), RepositorySerializer.Serialize, RepositorySerializer.Deserialize<Profile>));
            services.AddSingleton<IAsyncRepository<Identity>>(new FStoreRepository<Identity>(new FStoreMono(fStoreConfig), RepositorySerializer.Serialize, RepositorySerializer.Deserialize<Identity>));
            services.AddSingleton<IAsyncRepository<Message>>(new FStoreRepository<Message>(new FStoreMono(fStoreConfig), RepositorySerializer.Serialize, RepositorySerializer.Deserialize<Message>));
            services.AddSingleton<MessageRelayRecordRepository>(new MessageRelayRecordRepository(fStoreConfig));

            BootstrapperCommon.RegisterServices(services);

            services.AddSingleton<IDispatcher, ConsoleDispatcher>();

            services.AddSingleton<IAssemblyInfoProvider>(new AssemblyInfoProvider { Assembly = typeof(Program).GetTypeInfo().Assembly });
            var crypto = new XDSSecService();
            crypto.Init(new Platform_NetStandard());
            services.AddSingleton<IXDSSecService>(crypto);

            services.AddSingleton<AbstractSettingsManager,ConsoleSettingsManager>();
            services.AddSingleton<IMessageBoxService>(new MessageBoxService());

            services.AddSingleton<IFileService,FileService>();
            services.AddSingleton<ITcpConnection,MessageRelayConnectionFactory>();
            services.AddSingleton<IUdpConnection,MockUdpConnection>();
            services.AddSingleton<INotificationService,NotificationService>();
        }

        static IChatClientConfiguration GetConfiguration()
        {
            return new ChatClientConfiguration
            {
                UserAgentName = AssemblyVersionUtil.GetProgramDisplayName(typeof(Program).Assembly),
                SeedNodes = new[] { "178.62.62.160", "159.65.148.135", "206.189.33.114", "134.122.89.152", "161.35.156.96" },
                RepositoryConfiguration = FStoreInitializer.FStoreConfig
            };
        }
    }
}
