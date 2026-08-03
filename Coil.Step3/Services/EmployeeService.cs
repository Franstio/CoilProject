using Coil.Step3.Models.Database;
using Dapper;
using System.Data;

namespace Coil.Step3.Services
{
    public class EmployeeService : IDisposable
    {
        private readonly IDbConnection dbConnection;
        private readonly ILogger<EmployeeService> logger;

        public EmployeeService(IDbConnection dbConnection, ILogger<EmployeeService> logger)
        {
            this.dbConnection = dbConnection;
            this.logger = logger;
        }

        public async Task CreateEmployee(EmployeeModel model)
        {
            try
            {
                string query = "insert into employee(badgeId,username,active) values(@BadgeId,@Username,@Active)";
                await dbConnection.ExecuteAsync(query, model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }

        }
        public async Task UpdateEmployee(EmployeeModel model)
        {
            try
            {
                string query = "update employee set username=@Username,active=@Active where badgeId=@BadgeId";
                await dbConnection.ExecuteAsync(query, model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }
        public async Task DeactivateEmployee(IEnumerable<int> badgeIds)
        {
            try
            {
                string query = "update employee set active=0 where badgeId in @badgeIds";
                await dbConnection.ExecuteAsync(query, new { badgeIds });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }
        public async Task<EmployeeModel?> GetEmployee(int badgeId)
        {
            try
            {
                string query = "SELECT badgeId,username,active from employee where badgeId=@badgeId";
                return await dbConnection.QueryFirstOrDefaultAsync<EmployeeModel>(query, new { badgeId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }
        public async Task<EmployeeModel?> GetEmployee(string username)
        {
            try
            {
                string query = "SELECT badgeId,username,active from employee where username=@username";
                return await dbConnection.QueryFirstOrDefaultAsync<EmployeeModel>(query, new { username });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }

        public void Dispose()
        {
            dbConnection.Close();
            dbConnection.Dispose();
        }
    }
}
