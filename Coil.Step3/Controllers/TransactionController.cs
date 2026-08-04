using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Services;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Coil.Step3.Controllers
{
    [Route("")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly RollingDoorService rollingDoorService;
        private readonly ILogger<TransactionController> logger;
        private readonly TransactionService transactionService;
        private readonly ISyncService<TransactionModel> transactionSyncService;
        private readonly ISyncService<ContainerModel> containerSyncService;
        private readonly ISyncService<RollingDoorModel> rollingDoorSyncService;
        private readonly ISyncService<EmployeeModel> employeeSyncService;
        public TransactionController(RollingDoorService rollingDoorService, ILogger<TransactionController> logger, TransactionService transactionService, ISyncService<TransactionModel> transactionSyncService, ISyncService<ContainerModel> containerSyncService, ISyncService<RollingDoorModel> rollingDoorSyncService, ISyncService<EmployeeModel> employeeSyncService)
        {
            this.rollingDoorService = rollingDoorService;
            this.logger = logger;
            this.transactionService = transactionService;
            this.transactionSyncService = transactionSyncService;
            this.containerSyncService = containerSyncService;
            this.rollingDoorSyncService = rollingDoorSyncService;
            this.employeeSyncService = employeeSyncService;
        }

        [HttpPut("/step2value/{containerName}")]
        [HttpPost("/step2value/{containerName}")]
        public async Task<IActionResult> PostStep2Value(string containerName, Step2ValueRequest req)
        {
            await transactionService.UpdateStep2Value(containerName, req);
            return Ok(new { msg = "ok" });
        }

        [HttpPost("/ScanContainer")]
        public async Task<IActionResult> ScanContainer(ScanContainerRequest req)
        {
            var container = await transactionService.GetContainer(req.containerId);
            if (container is null)
            {
                logger.LogError($"Container is not found (id: {req.containerId})");
             }
            return Ok(new { container });
        }
        [HttpGet("/ping")]
        public IActionResult Ping()
        {
            return Ok(new { msg = "ok" });
        }
        [HttpGet("/clean")]
        public IActionResult Clean()
        {
            return Ok(new { msg = "ok" });
        }
        [HttpGet("/start-work")]
        public IActionResult StartWork()
        {
            return Ok(new { msg = "ok" });
        }

        [HttpPost("/SaveTransaksi")]
        public async Task<IActionResult> SaveTransaksi(IEnumerable<TransactionRequest> req)
        {
            List<TransactionModel> items = [];
            foreach (var data in req)
            {
                try
                {
                    items.Add(await transactionService.SaveTransaksi(data));
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message + " | " + e.StackTrace);
                }
            }
            return Ok(new { state = items });
        }

        [HttpPost("/CheckBinCapacity")]
        public async Task<IActionResult> CheckBinCapacity(CheckBinAvailableRequest req)
        {
            var doors = await rollingDoorService.GetRollingDoorByWaste(req.type_waste);

            if (!doors.Any())
            {
                string message = $"Bin Not found with waste id: {req.type_waste}";
                logger.LogError(message);
                return Ok(new { success = false, message });
            }
            var availableDoors = doors.Where(door => (door.weight + req.neto) <= door.max_weight).OrderByDescending(x => x.weight);
            if (!availableDoors.Any())
            {
                string message = $"No Bin that can hold {req.neto}";
                logger.LogError(message);
                return Ok(new { success = false, message });
            }
            var door = availableDoors.First();
            await rollingDoorService.SetDoor(door.clientId, RollingDoorService.RollingDoorOption.OPEN);
            return Ok(new { success = false, bin = door });
        }
        [HttpGet("/SyncPending")]
        public async Task<IActionResult> SyncPending()
        {
            var syncRes = await transactionSyncService.SyncTData();
            return Ok(new { msg = "Sync Pending Transaction", data = syncRes });
        }
        [HttpGet("/container-sync")]
        public async Task<IActionResult> ContainerSync()
        {
            var syncRes = await containerSyncService.SyncTData();
            return Ok(new { msg = "Container Sync", data = syncRes });
        }
        [HttpGet("/sync-all")]
        public async Task<IActionResult> SyncAll()
        {
            var syncRes = await transactionSyncService.SyncTData();
            var containerRes = await containerSyncService.SyncTData();
            var rollingDoorRes = await rollingDoorSyncService.SyncTData();
            var employeeRes = await employeeSyncService.SyncTData();
            return Ok(new { msg = "Sync All", data = new { transactions = syncRes, containers = containerRes, rollingDoors = rollingDoorRes, employees = employeeRes } });
        }
    }
}
