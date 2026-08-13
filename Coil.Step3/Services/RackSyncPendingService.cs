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
                    string[] urls = this.configuration.GetSection("PIDSG:Step2Api_URL").Get<string[]>()!;
                    List<Task<HttpResponseMessage>> tasks = urls.Select(url => httpclient.GetAsync(url + "/Pending-List")).ToList();
                    var results =  await Task.WhenAll(tasks);
                    foreach (var result in results)
                        logger.LogInformation(await result.Content.ReadAsStringAsync());
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
