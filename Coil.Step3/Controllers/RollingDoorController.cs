using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Models.Response;
using Coil.Step3.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI;
using Enum = System.Enum;

namespace Coil.Step3.Controllers
{
    [ApiController]
    [Route("")]
    public class RollingDoorController : ControllerBase
    {


        private readonly ILogger<RollingDoorController> _logger;
        private readonly RollingDoorService _rollingDoorService;
        private readonly TransactionService _transactionService;
        private readonly IStep4CheckService _step4CheckService;
        private readonly ISyncService<RollingDoorModel> _rollingDoorSyncService;
        public RollingDoorController(ILogger<RollingDoorController> logger, RollingDoorService rollingDoorService, TransactionService transactionService, IStep4CheckService step4CheckService, ISyncService<RollingDoorModel> rollingDoorSyncService)
        {
            _logger = logger;
            _rollingDoorService = rollingDoorService;
            _transactionService = transactionService;
            _step4CheckService = step4CheckService;
            _rollingDoorSyncService = rollingDoorSyncService;
        }

        [HttpPost("/rollingdoorUp")]
        public async Task<IActionResult> RollingDoorUp(RollingDoorRequest req)
        {
            var rollingDoor = await _rollingDoorService.GetRollingDoor(req.idRollingDoor);
            if (rollingDoor is null || !rollingDoor.Any())
                return NotFound(new { err = "Rolling Door Not Found" });
            await _rollingDoorService.SetDoor(rollingDoor.First().clientId, RollingDoorService.RollingDoorOption.OPEN);
            return Ok(new { msg = "Rolling Door Dibuka" });
        }
        [HttpPost("/rollingdoorDown")]
        public async Task<IActionResult> RollingDoorDown(RollingDoorRequest req)
        {
            var rollingDoor = await _rollingDoorService.GetRollingDoor(req.idRollingDoor);
            if (rollingDoor is null || !rollingDoor.Any())
                return NotFound(new { err = "Rolling Door Not Found" });
            await _rollingDoorService.SetDoor(rollingDoor.First().clientId, RollingDoorService.RollingDoorOption.CLOSE);
            return Ok(new { msg = "Rolling Door Ditutup" });
        }

        [HttpPost("/activedDoor")]
        public async Task<IActionResult> ActivedDoor(Step4ActivedDoorRequest req)
        {
            var rollingDoor = await _rollingDoorService.GetRollingDoor(req.name);
            if (rollingDoor is null || !rollingDoor.Any())
                return NotFound("Rolling Door Not Found");
            RollingDoorService.RollingDoorOption option = req.doorStatus == 1 ? RollingDoorService.RollingDoorOption.OPEN : RollingDoorService.RollingDoorOption.CLOSE;
            if (option == RollingDoorService.RollingDoorOption.CLOSE)
            {
                await _step4CheckService.Step4Check(req.name);
            }
            await _rollingDoorService.SetDoor(rollingDoor.First().clientId, option);
            return Ok(new RollingDoorResponse { msg = $"Rolling Door {(req.doorStatus == 1 ? "Buka" : "Tutup")}", clientId = rollingDoor.First().clientId, address = (ushort)option, value = 1 }); ;
        }

        [HttpGet("/step4-check/{binname}")]
        public async Task<IActionResult> Step4Check(string binname)
        {
            try
            {
                var res = await _step4CheckService.Step4Check(binname);
                return Ok(new { msg = "Step 4 Check Success", data = res });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                return StatusCode(500, new { msg = "Step 4 Check Failed", error = ex.Message });
            }
        }

        [HttpGet("/switchLamp")]
        public async Task<IActionResult> switchLamp([FromQuery] string id, [FromQuery] string lamp, [FromQuery] string value)
        {
            bool lampRes = Enum.TryParse<RollingDoorService.RollingDoorLampOption>(lamp, out var switchLamp);
            bool idRes = byte.TryParse(id, out var clientId);
            bool isAlive = value == "1";
            if (!lampRes)
                return NotFound("lamp not found");
            if (!idRes)
                return NotFound("Id not found");
            await _rollingDoorService.SetLamp(clientId, switchLamp, isAlive);
            return Ok(new { msg = "OK" });
        }

        [HttpPost("/triggerAvailbleBin")]
        public async Task<IActionResult> triggerAvailbleBin(TriggerBinAvailableRequest req)
        {
            var doors = await _rollingDoorService.GetRollingDoor(req.wasteId);
            if (!doors.Any())
                return NotFound("id waste not found");
            foreach (var door in doors)
            {
                double maxWeight = door.max_weight * 0.95;
                await _rollingDoorService.SetLamp(door.clientId, RollingDoorService.RollingDoorLampOption.RED, door.weight >= maxWeight && req.valueIsOpen);
                await _rollingDoorService.SetLamp(door.clientId, RollingDoorService.RollingDoorLampOption.GREEN, door.weight < maxWeight && req.valueIsOpen);
            }
            return Ok(new { msg = "Success Trigger Bin" });
        }

        [HttpPost("/UpdateBinWeight")]
        public async Task<IActionResult> updateBinWeight(UpdateBinWeightRequest req)
        {
            var doors = await _rollingDoorService.GetRollingDoor(req.binId);
            if (!doors.Any())
            {
                _logger.LogError($"Bin Not Found {req.binId} {req.neto}");
                return NotFound("Bin Not Found");
            }
            //var door = doors.First();
            //door.weight = req.neto + door.weight;
            //await _rollingDoorService.UpdateRollingDoorBinWeight(door.id, door.weight);
            //await _rollingDoorService.SetDoor(door.clientId, RollingDoorService.RollingDoorOption.CLOSE);
            return Ok(new { msg = "ok" });
        }
       

        [HttpGet("/bin-sync")]
        public async Task<IActionResult> binSync()
        {
            try
            {
                var res = await _rollingDoorSyncService.SyncTData();
                return Ok(new { msg = "ok", data = res });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                return StatusCode(500, new { msg = "error", error = ex.Message });
            }
        }

        [HttpGet("/door")]
        public async Task<IActionResult> GetDoorDisplay()
        {
            return Ok(await _rollingDoorService.GetRollingDoorDisplay());
        }
    }
}
