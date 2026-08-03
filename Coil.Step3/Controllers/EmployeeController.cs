using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Tls;

namespace Coil.Step3.Controllers
{
    [Route("")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ISyncService<EmployeeModel> employeeSyncService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly EmployeeService employeeService;

        public EmployeeController(ILogger<EmployeeController> logger, EmployeeService employeeService, ISyncService<EmployeeModel> employeeSyncService)
        {
            _logger = logger;
            this.employeeService = employeeService;
            this.employeeSyncService = employeeSyncService;
        }

        [HttpGet("/ScanBadgeid")]
        public async Task<IActionResult> ScanBadgeId(ScanBadgeIdRequest req)
        {
            var usr = await employeeService.GetEmployee(req.badgeId);
            if (usr is null)
            {
                string errMsg = "Badge ID is not found";
                _logger.LogError(errMsg);
                return Ok(new { error = errMsg });
            }
            return Ok(new { user = usr });
        }
        [HttpGet("/employee-sync")]
        public async Task<IActionResult> EmployeeSync()
        {
            try
            {
                var result = await employeeSyncService.SyncTData();
                return Ok(new { msg = "Employee Sync Success", data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                return StatusCode(500, new { error = "Employee Sync Failed", details = ex.Message });
            }
        }


    }
}
