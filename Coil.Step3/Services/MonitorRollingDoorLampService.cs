namespace Coil.Step3.Services
{
    public class MonitorRollingDoorLampService : BackgroundService
    {

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<MonitorRollingDoorLampService> logger;

        public MonitorRollingDoorLampService(ILogger<MonitorRollingDoorLampService> logger,IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var rollingDoorService = scope.ServiceProvider.GetRequiredService<RollingDoorService>();
                    var doors = await rollingDoorService.GetRollingDoor();
                    foreach (var door in doors)
                    {
                        double maxWeight = door.max_weight * 0.95;
                        await rollingDoorService.SetLamp(door.clientId, RollingDoorService.RollingDoorLampOption.RED, door.weight >= maxWeight);
                    }
                }
                catch(Exception e)
                {
                    logger.LogError(e.Message);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }

    }
}
