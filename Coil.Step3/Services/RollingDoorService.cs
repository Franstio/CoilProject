using Coil.Step3.Models.Database;
using Coil.Step3.Models.ModbusPLC;
using Coil.Step3.Models.Response;
using Dapper;
using System.Data;


namespace Coil.Step3.Services
{
    public class RollingDoorService : IDisposable
    {
        public enum RollingDoorOption
        {
            CLOSE=21,
            OPEN=20
        }
        public enum RollingDoorLampOption
        {
            RED=30, 
            GREEN=31,
        }

        private readonly ModbusPLCService modbusPLCService;
        private readonly IDbConnection dbConnection;

        public RollingDoorService(ModbusPLCService modbusPLCService
            ,IDbConnection dbConnection)
        {
            this.modbusPLCService = modbusPLCService;
            this.dbConnection = dbConnection; 
        }

        public async Task<IEnumerable< RollingDoorModel>> GetRollingDoor(int rollingDoorId)
        {

            string query = "Select id,name,weight,type_waste,max_weight,clientId,last_empty from bin where id=@rollingDoorId";
            return await dbConnection.QueryAsync<RollingDoorModel>(query, new { rollingDoorId });
        }
        public async Task<IEnumerable<RollingDoorModel>> GetRollingDoor()
        {

            string query = "Select id,name,weight,type_waste,max_weight,clientId,last_empty from bin";
            return await dbConnection.QueryAsync<RollingDoorModel>(query);
        }
        public async Task<IEnumerable<RollingDoorDisplayResponse>> GetRollingDoorDisplay()
        {

            string query = "Select row_number() over (order by id) as no, name,weight, max_weight as capacity from bin";
            return await dbConnection.QueryAsync<RollingDoorDisplayResponse>(query);
        }

        public async Task<IEnumerable<RollingDoorModel>> GetRollingDoor(string name)
        {
            string query = "Select id,name,weight,type_waste,max_weight,clientId,last_empty from bin where name=@name";
            return await dbConnection.QueryAsync<RollingDoorModel>(query, new { name });
        }
        public async Task<RollingDoorModel?> GetLastRollingdoorByName(string name,DateTime? last_empty =null)
        {
            string query = "Select id,name,weight,type_waste,max_weight,clientId,last_empty from bin where name=@name and (last_empty < @last_empty or last_empty is null)";
            return await dbConnection.QueryFirstOrDefaultAsync<RollingDoorModel>(query, new { name, last_empty = last_empty?.ToString("yyyy-MM-dd HH:mm:ss") });
        }
        public async Task<IEnumerable<RollingDoorModel>> GetRollingDoorByWaste(int wasteId)
        {

            string query = "Select id,name,weight,type_waste,max_weight,clientId,last_empty from bin where type_waste=@wasteId";
            return await dbConnection.QueryAsync<RollingDoorModel>(query, new { wasteId });
        }
        public async Task UpdateRollingDoorBinWeight(int binId,double newweight)
        {

            string query = "update bin set weight=@newweight where id=@binId";
            await dbConnection.ExecuteAsync(query, new { newweight,binId });
        }
        public async Task UpdateRollingDoorLastEmpty(string name, DateTime lastEmpty)
        {

            string query = "update bin set last_empty=@lastEmpty where name=@name";
            await dbConnection.ExecuteAsync(query, new { lastEmpty = lastEmpty.ToString("yyyy-MM-dd HH:mm:ss"), name });
        }   
        public async Task<IEnumerable<RollingDoorModel>> GetRollingDoorWithContainerStation()
        {

            return await dbConnection.QueryAsync<RollingDoorModel>(" select b.id,b.name,b.weight,b.type_waste,b.max_weight,b.clientId,b.last_empty ,c.station from bin b left join container c on b.name=c.name");
        }
        public async Task UpdateRollingDoorMaxWeight(int binId, double newMaxWeight)
        {

            string query = "update bin set max_weight=@newMaxWeight where id=@binId";
            await dbConnection.ExecuteAsync(query, new { newMaxWeight, binId });
        }   
        public async Task SetDoor(byte RollingDoorId, RollingDoorOption option)
        {

            var req = new ModbusPLCRequest()
            {
                ClientId = RollingDoorId,
                Address = (ushort)option,
                Value = 1
            };
            await modbusPLCService.Write(req);
        }

        public async Task SetLamp(byte RollingDoorId, RollingDoorLampOption lampOption,bool isAlive)
        {
            var req = new ModbusPLCRequest()
            {
                ClientId = RollingDoorId,
                Address = (ushort)lampOption,
                Value = (ushort)(isAlive ? 1 : 0)
            };
            await modbusPLCService.Write(req);
        }

        public void Dispose()
        {
            dbConnection.Close();
            dbConnection.Dispose();
        }
    }
}
