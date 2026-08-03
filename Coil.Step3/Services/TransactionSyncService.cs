using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Models.Response;
using MySqlX.XDevAPI;

namespace Coil.Step3.Services
{
    public class TransactionSyncService : BackgroundService,ISyncService<TransactionModel>,IStep4CheckService
    {
        private readonly ILogger<TransactionSyncService> _logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly IServiceProvider serviceProvider;
        public TransactionSyncService(ILogger<TransactionSyncService> logger, IHttpClientFactory httpClientFactory,IConfiguration config,IServiceProvider serviceProvider)
        {
            _logger = logger;
            configuration = config;
            this.httpClientFactory = httpClientFactory;
            this.serviceProvider = serviceProvider;
        }
        public async Task<PIDSGStep4CheckResponse> Step4Check(string binname)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var rollingDoorService = scope.ServiceProvider.GetRequiredService<RollingDoorService>();
                var employeeService = scope.ServiceProvider.GetRequiredService<EmployeeService>();
                var transactionService = scope.ServiceProvider.GetRequiredService<TransactionService>();    
                var client = httpClientFactory.CreateClient("CoilApi");
                var res = await client.GetFromJsonAsync<PIDSGStep4CheckResponse>($"api/pid/step4/{binname}");
                if (res is null || res.result.Any())
                {
                    throw new Exception("Step 4 check failed for bin: " + binname);
                }
                var item = res.result.First();
                item.dt = DateTime.Parse(item.dt.Replace("T", " ").Replace("Z", "")).ToString("yyyy-MM-dd HH:mm:ss");

                var lastRollingDoor = await rollingDoorService.GetLastRollingdoorByName(binname, DateTime.Parse(item.dt));
                if (lastRollingDoor is null)
                {
                    throw new Exception("No rolling door found for bin: " + binname);
                }
                await rollingDoorService.UpdateRollingDoorLastEmpty(item.frombin_name, DateTime.Parse(item.dt));
                var employee = await employeeService.GetEmployee(item.badgeno);
                item.badgeno = employee?.badgeId ?? -1;
                var rollingDoor = await rollingDoorService.GetRollingDoor(item.frombin_name);
                var step4 = await SendSalesPidsg(client,new Step4SalesRequest
                {
                    Badgeno = item.badgeno == -1 ? "SYSTEM" : item.badgeno.ToString(),
                    Frombin = lastRollingDoor.name,
                    Tobin = lastRollingDoor.name,
                    LoginDate = item.dt
                });
                step4.EnsureSuccessStatusCode();
                await transactionService.CreateTransaction(new TransactionModel()
                {
                    badgeId = item.badgeno,
                    idWaste = lastRollingDoor.type_waste,
                    neto = item.discharge_weight,
                    recordDate = item.dt,
                    loginDate = item.dt,
                    binId = lastRollingDoor.id,
                    binName = lastRollingDoor.name,
                    status = "SALES",
                    isSuccess = true,
                });
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }
        Task<HttpResponseMessage> SendSalesPidsg(HttpClient client,Step4SalesRequest salesData) => client.PostAsync("api/pid/step4", new StringContent(System.Text.Json.JsonSerializer.Serialize(new
        {
            badgeno = salesData.Badgeno,
            frombin_name = salesData.Frombin,
            tobin_name = salesData.Tobin,
            loginDate = salesData.LoginDate,
            weight = 0,
            activity = "SALES",
            postby = "Local Step 3",
            postDate = salesData.LoginDate,
            binname = "",
            step2value = ""
        }), System.Text.Encoding.UTF8, "application/json"));
        Task<HttpResponseMessage> SendToPIDSG(TransactionModel transaction)
        {
            string[] racks = configuration.GetSection("PIDSG:Rack_Container").Get<string[]>() ?? Array.Empty<string>();
            bool fromRack = racks.Contains(transaction.containerName);
            var client = httpClientFactory.CreateClient("CoilApi");
            return client.PostAsJsonAsync("/api/pid/activityLogbyPcAll", new 
            {
                stationname= "STEP 3 COLLECTION",
                badgeno = transaction.badgeId,
                frombin = transaction.containerName,
                weight = transaction.neto + transaction.Container!.weightbin,
                activity = "Dispose",
                postby = "Local Step 3",
                tobin =  transaction.binName,
                postDate = transaction.recordDate,
                loginDate = transaction.loginDate,
                binname = fromRack ? transaction.containerName : "",
                step2value = fromRack ? transaction.Container!.step2value.ToString("0.00") : ""
            });
        }
        public async Task<IEnumerable<TransactionModel>> SyncTData()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var transactionService = scope.ServiceProvider.GetRequiredService<TransactionService>();
                IEnumerable<TransactionModel> transactions = await transactionService.GetReadyOrPendingTransaction();
                if (transactions is null || !transactions.Any())
                    return [];
                foreach (TransactionModel transaction in transactions)
                {
                    try
                    {
                        var log = await SendToPIDSG(transaction);
                        log.EnsureSuccessStatusCode();
                        transaction.status = "Done";
                        transaction.isSuccess = true;
                        await transactionService.UpdateTransactionStatus(transaction);
                        await transactionService.SetContainerStep2Value(transaction.containerName!, 0);
                    }
                    catch (Exception ex)
                    {
                        transaction.status = "PENDING|PIDSG|1";
                        transaction.isSuccess = false;
                        await transactionService.UpdateTransactionStatus(transaction);
                        _logger.LogError($"Error syncing transaction {transaction.Id}: {ex.Message}");
                    }
                }
                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SyncTData: {ex.Message}");
                throw;
            }   
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
                    _logger.LogError($"Error fetching transactions: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

    }
}
