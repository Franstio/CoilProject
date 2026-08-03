using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Response;
using System.Text.Json;

namespace Coil.Step3.Services
{
    public class RollingDoorSyncService : BackgroundService,ISyncService<RollingDoorModel> 
    {
        private readonly ILogger<RollingDoorSyncService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IServiceProvider serviceProvider;

        public RollingDoorSyncService( ILogger<RollingDoorSyncService> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.serviceProvider = serviceProvider;
        }
        public async Task<IEnumerable<RollingDoorModel>> SyncTData()
        {
            using var scope = serviceProvider.CreateScope();
            var rollingDoorService = scope.ServiceProvider.GetRequiredService<RollingDoorService>();
            var client = httpClientFactory.CreateClient("CoilApi");
            var res = await rollingDoorService.GetRollingDoorWithContainerStation();
            if (res is null || !res.Any())
                throw new Exception("Rolling Door Sync PIDSG Response is null");
            var rollingDoorNames = res.Select(x => x.name).ToList();
            var httpRes = await client.GetFromJsonAsync<PIDSGBinResponse>($"api/pid/bin-sync?f1={JsonSerializer.Serialize(rollingDoorNames)}");
            if (httpRes is null || httpRes.result is null || !httpRes.result.Any())
                throw new Exception("Rolling Door Sync PIDSG Response is null");
            var binSync = httpRes.result.First();
            foreach (var bin in binSync)
            {
                var rollingDoor = res.FirstOrDefault(x => x.name == bin.name);
                if (rollingDoor is null)
                    throw new Exception($"Rolling Door Sync PIDSG Response is null for {bin.name}");
                rollingDoor.max_weight = bin.capacity;
                await rollingDoorService.UpdateRollingDoorMaxWeight(rollingDoor.id, bin.capacity);
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
