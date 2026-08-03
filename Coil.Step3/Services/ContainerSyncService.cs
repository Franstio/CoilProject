using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Response;
using System.Text.Json;

namespace Coil.Step3.Services
{
    public class ContainerSyncService : BackgroundService,ISyncService<ContainerModel>
    {
        private readonly ILogger<ContainerSyncService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IServiceProvider serviceProvider;
        public ContainerSyncService(IServiceProvider serviceProvider,ILogger<ContainerSyncService> logger, IHttpClientFactory httpClientFactory)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ContainerModel>> SyncTData()
        {
            using var scope = serviceProvider.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<TransactionService>();
            var client = httpClientFactory.CreateClient("CoilApi");
            var res = await transactionService.GetContainers();
            if (res is null || !res.Any())
                throw new Exception("Container List is null");
            var name = res.Select(x => x.name).ToList();
            var httpRes = await client.GetFromJsonAsync<PIDSGBinResponse>($"api/pid/bin-sync?f1={JsonSerializer.Serialize(name)}");
            if (httpRes is null || httpRes.result is null || !httpRes.result.Any())
                throw new Exception("Container Sync PIDSG Response is null");
            var binSync = httpRes.result.First();
            foreach (var bin in binSync)
            {
                var container = res.FirstOrDefault(x => x.name == bin.name);
                if (container is null)
                    throw new Exception($"Container Sync PIDSG Response is null for {bin.name}");
                container.weightbin = bin.weight;
                await transactionService.SetContainerWeightBin(container.name, container.weightbin);
            }
            return res;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncTData();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message + " | " + ex.StackTrace);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
    }
}
