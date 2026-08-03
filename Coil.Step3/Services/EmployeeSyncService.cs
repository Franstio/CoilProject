using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Response;

namespace Coil.Step3.Services
{
    public class EmployeeSyncService : BackgroundService,ISyncService<EmployeeModel>
    {
        private readonly ILogger<EmployeeSyncService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly IServiceProvider serviceProvider;

        public EmployeeSyncService(ILogger<EmployeeSyncService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<EmployeeModel>> SyncTData()
        {
            using var scope = serviceProvider.CreateScope();
            var employeeService = scope.ServiceProvider.GetRequiredService<EmployeeService>();
            var client = httpClientFactory.CreateClient("CoilApi");
            List<int> employeeId = [];
            var res = await client.GetFromJsonAsync<PIDSGEmployeeResponse>($"api/pid/employee-sync?f1={configuration.GetSection("PIDSG:STATION").Value}");
            var syncEmp = res?.result.FirstOrDefault();
            List<EmployeeModel> result = [];
            if (res is null || syncEmp is null)
                throw new Exception("Employee Sync PIDSG Response is null");
            foreach (var emp in syncEmp)
            {
                if (int.TryParse(emp.badgeno, out int badgeId) == false)
                {
                    logger.LogError($"Employee Sync PIDSG Response is invalid. BadgeNo: {emp.badgeno}");
                    continue;
                }
                var check = await employeeService.GetEmployee(badgeId);
                if (check is null)
                {
                    var newEmp = new EmployeeModel()
                    {
                        active = true,
                        badgeId =badgeId,
                        username = emp.employeename
                    };
                    await employeeService.CreateEmployee(newEmp);
                    result.Add(newEmp);
                }
                else
                {
                    check.username = emp.employeename;
                    check.active = true;
                    await employeeService.UpdateEmployee(check);
                    result.Add(check);  
                }
                employeeId.Add(badgeId);
            }
            if (employeeId.Any())
            {
                await employeeService.DeactivateEmployee(employeeId);
            }
            return result;

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
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
