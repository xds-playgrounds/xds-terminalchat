using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XDS.Messaging.SDK.ApplicationBehavior.Services.Interfaces;
using XDS.Messaging.SDK.ApplicationBehavior.Services.PortableImplementations;
using XDS.Messaging.SDK.ApplicationBehavior.Workers;
using XDS.SDK.Messaging.CrossTierTypes.FStore;

namespace XDS.Messaging.TerminalChat.Services
{
    public class Cancellation : ICancellation
    {
        readonly ILogger logger;
        readonly CancellationTokenSource cts;
        readonly DeviceVaultService deviceVaultService;
        readonly FStoreConfig fStoreConfig;
        readonly List<IWorker> workers;

        public Cancellation(ILoggerFactory loggerFactory, DeviceVaultService deviceVaultService, FStoreConfig fStoreConfig)
        {
            this.logger = loggerFactory.CreateLogger<Cancellation>();
            this.deviceVaultService = deviceVaultService;
            this.fStoreConfig = fStoreConfig;
            this.cts = new CancellationTokenSource();
            this.cts.Token.Register(Shutdown);
            this.workers = new List<IWorker>();
            this.workers.Add(new InfoWorker(this.workers, loggerFactory, this.cts.Token));
        }

        public CancellationToken Token { get { return this.cts.Token; } }

        public DirectoryInfo DataDirRoot { get; private set; }

        public async Task<bool> PrepareLaunch(DirectoryInfo dataDirRoot)
        {
            this.DataDirRoot = dataDirRoot;

            var isOnboardingRequired = await this.deviceVaultService.CheckIsOnboardingRequiredAsync();

            if (isOnboardingRequired)
            {
                if (dataDirRoot.Exists)
                {
                    dataDirRoot.Delete(true);
                }

            }

            this.fStoreConfig.Initializer(this.fStoreConfig);

            return isOnboardingRequired;
        }

        public async Task StartWorkers()
        {
            foreach (IWorker worker in this.workers)
            {
                await worker.InitializeAsync();
                if (worker.WorkerTask.Status == TaskStatus.Running)
                {
                    this.logger.LogInformation($"Started {worker.GetType().Name}. Status: {worker.WorkerTask.Status}");
                }
                else
                {
                    this.logger.LogError($"Could not start {worker.GetType().Name}: Status: {worker.WorkerTask.Status} FaultReason: {worker.FaultReason}.");
                }
            }
        }

        public bool IsSelfDestructRequested { get; set; }

        public void Shutdown()
        {
            try
            {
                this.logger.LogInformation($"Cancellation requested.");

                if (!this.cts.IsCancellationRequested)
                    this.cts.Cancel();

                foreach (IWorker worker in this.workers)
                {
                    if(worker is InfoWorker) // we do not shut down this one, because we want to see the info till the end
                        continue;

                    if(worker.WorkerTask == null) // WorkerTask can be null if InitializeAsync has not been completed
                        continue;

                    if (worker.WorkerTask.Status > TaskStatus.Created)
                    {
                        worker.WorkerTask.Wait(500);
                        this.logger.LogInformation($"{worker} {worker.WorkerTask.Status}");
                    }
                  
                }
                this.logger.LogInformation($"All running worker tasks have completed.");


                this.deviceVaultService.ClearMasterRandomKey();

                if (this.IsSelfDestructRequested)
                {
                    this.logger.LogCritical($"Self-destruct initiated...");
                    ExecuteSelfDestruct();
                    this.logger.LogCritical($"Self-destruction complete. All personal information was deleted.");
                }

                this.logger.LogInformation("Shutdown complete.");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                Environment.Exit(1);
            }

        }

        void ExecuteSelfDestruct()
        {
            this.DataDirRoot.Delete(true);
        }

        public void Cancel()
        {
            if (!this.cts.IsCancellationRequested)
                this.cts.Cancel();
        }



        public void RegisterWorker(IWorker worker)
        {
            this.workers.Add(worker);
        }

        sealed class InfoWorker : IWorker
        {
            readonly List<IWorker> workers;
            readonly ILogger logger;
            readonly CancellationToken cancellationToken;
            public InfoWorker(List<IWorker> workers, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
            {
                this.logger = loggerFactory.CreateLogger<InfoWorker>();
                this.cancellationToken = cancellationToken;
                this.workers = workers;
            }

            async Task PrintInfoAsync()
            {
                try
                {
                    while (true)
                    {
                        foreach (IWorker worker in this.workers)
                        {
                            var info = worker.GetInfo();
                            if (info != null)
                                this.logger.LogInformation(info);
                        }

                        await Task.Delay(10000);
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogCritical(e.Message);
                    this.FaultReason = e;
                }
            }

            public Task WorkerTask { get; private set; }

            public Exception FaultReason { get; private set; }

            public string GetInfo()
            {
                var sb = new StringBuilder();
                sb.AppendLine("========= WORKERS =========");
                foreach (IWorker worker in this.workers)
                {
                    sb.AppendLine($"{worker.GetType().Name}: {worker.WorkerTask?.Status} {worker.FaultReason}");
                }

                return sb.ToString();
            }

            public async Task InitializeAsync()
            {
                this.WorkerTask = Task.Run(PrintInfoAsync);
                await Task.CompletedTask;
            }

            public void Pause()
            {
                throw new NotImplementedException();
            }

            public void Resume()
            {
                throw new NotImplementedException();
            }
        }
    }
}
