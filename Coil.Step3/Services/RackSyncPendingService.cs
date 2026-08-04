namespace Coil.Step3.Services
{
    public class RackSyncPendingService : BackgroundService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<RackSyncPendingService> logger;
        public RackSyncPendingService(IHttpClientFactory httpClientFactory,IConfiguration configuration,ILogger<RackSyncPendingService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.logger = logger;   
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                 {
                    using var httpclient = httpClientFactory.CreateClient("Step2Api");
                    string url = this.configuration.GetSection("PIDSG:Rack_URL").Value!;
                    var res = await httpclient.GetAsync(url + "/Pending-List");
                    logger.LogInformation(await res.Content.ReadAsStringAsync());
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                }
                finally
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
