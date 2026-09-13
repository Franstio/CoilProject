using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Models.Response;
using Dapper;
using System.Data;
using static Coil.Step3.Models.Requests.TransactionRequest;

namespace Coil.Step3.Services
{
    public class TransactionService : IDisposable
    {
        private readonly IDbConnection dbConnection;
        private readonly ILogger<TransactionService> logger;

        public  TransactionService(IDbConnection dbConnection,
            ILogger<TransactionService> logger)
        {
            this.dbConnection = dbConnection;
            this.logger = logger;
        }
        
        public async Task UpdateStep2Value(string containerName,Step2ValueRequest req)
        {
            try
            {
                var container = await dbConnection.QueryFirstOrDefaultAsync<ContainerModel>("Select containerid,name,station,weightbin,idWaste,step2value from container where name=@name", new { name = containerName });
                if (container is null)
                    throw new ArgumentNullException("Container Not Found");
                container.step2value = req.value + container.step2value;
                await dbConnection.ExecuteAsync("Update container set step2value=@step2value where name=@name", new { step2value = container.step2value, name = container.name });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }

        public async Task<ContainerModel?> GetContainer(int containerId)
        {
            string query = "select containerid,c.name,station,weightbin,step2value,idWaste,w.id,w.name from container c inner join waste w on c.idWaste = w.id where containerid=@containerId";
            var data = await dbConnection.QueryAsync<ContainerModel, WasteModel, ContainerModel>(query, (c, w) =>
            {
                c.Waste = w;
                return c;
            }, new { containerId = containerId }, splitOn: "id");
            return data.FirstOrDefault();
        }
        public async Task<ContainerModel?> GetContainer(string containerId)
        {
            string query = "select containerId,c.name,station,weightbin,step2value,idWaste,w.id,w.name from container c inner join waste w on c.idWaste = w.id where c.name=@containerId";
            var data = await dbConnection.QueryAsync<ContainerModel, WasteModel, ContainerModel>(query, (c, w) =>
            {
                c.Waste = w;
                return c;
            }, new { containerId = containerId }, splitOn: "id");
            return data.FirstOrDefault();
        }
        public async Task<IEnumerable<ContainerModel>> GetContainers()
        {
            string query = "select containerid,c.name,station,weightbin,step2value,idWaste,w.id,w.name from container c inner join waste w on c.idWaste = w.id";
            var data = await dbConnection.QueryAsync<ContainerModel, WasteModel, ContainerModel>(query, (c, w) =>
            {
                c.Waste = w;
                return c;
            }, splitOn: "id");
            return data;

        }
        public async Task<IEnumerable<TransactionModel>> GetReadyOrPendingTransaction()
        {
            string query = "Select t.id,t.badgeId,t.status,t.isSuccess,t.containerName,t.binName,t.neto,t.recordDate,t.loginDate,c.containerid,c.weightbin,c.step2value from transaction t left join container c on t.idContainer=c.containerId where (t.status like '%PENDING%' or t.status='READY') and  t.id not in (select id_transaction from log_record);";
            var data = dbConnection.QueryAsync<TransactionModel, ContainerModel, TransactionModel>(query, (t, c) =>
            {
                t.Container = c;
                return t;
            }, splitOn: "containerId");
            return await data;

        }
        public async Task UpdateTransactionStatus(TransactionModel transaction)
        {
            string query = "update transaction set status=@status,isSuccess=@isSuccess where id=@id";
            await dbConnection.ExecuteAsync(query, new { status = transaction.status, isSuccess = transaction.isSuccess, id = transaction.Id });
        }
        public async Task SetContainerStep2Value(string name, double step2value)
        {
            string query = "update container set step2value=@step2value where name=@name";
            await dbConnection.ExecuteAsync(query, new { step2value, name });

        }
        public async Task SetContainerWeightBin(string name, double weightbin)
        {
            string query = "update container set weightbin=@weightbin where name=@name";
            await dbConnection.ExecuteAsync(query, new { weightbin, name });

        }    
        

        public async Task<bool> CheckTransaction(int idContainer)
        {

            string query = "select cast(recorddate as datetime) from transaction where idContainer=@idContainer and cast(recordDate as datetime) >= now() - interval 1 minute order by id desc limit 0,1";
            var data = await dbConnection.QueryFirstOrDefaultAsync<DateTime?>(query, new { idContainer });
            return data != null;
        }
        public async Task<TransactionModel> CreateTransaction(TransactionModel transactionModel)
        {
            using var tr = dbConnection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string query = @"INSERT INTO Transaction (
            badgeId, idContainer, idWaste, recordDate, neto,
            containerName, binName, binId, status, isSuccess, loginDate
        ) VALUES (
            @badgeId, @idContainer, @idWaste, @recordDate, @neto,
            @containerName, @binName, @binId, @status, @isSuccess, @loginDate
        );
        SELECT LAST_INSERT_ID();";
                transactionModel.Id = await tr.Connection!.ExecuteScalarAsync<int>(query,transactionModel);
                tr.Commit();
                return transactionModel;
            }
            catch (Exception ex)
            {
                tr.Rollback();
                logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
        }
        public async Task<TransactionModel> SaveTransaksi(TransactionRequestItem req)
        {
            var dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (await CheckTransaction(req.idContainer))
            {
                 throw new Exception("Transaction already exists in the last 1 minute.");
            }
            TransactionModel transactionModel = new TransactionModel()
            {
                badgeId = req.badgeId,
                binId = req.binId,
                binName = req.binName,
                containerName = req.containerName,
                idContainer = req.idContainer,
                idWaste = req.idWaste,  
                isSuccess = false,
                loginDate = req.loginDate,
                neto = req.neto,
                recordDate = dt,
                status = "READY",
            };
            return await CreateTransaction(transactionModel);

        }

        public void Dispose()
        {
            try

            {
                dbConnection.Close();
                dbConnection.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
