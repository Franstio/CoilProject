using Coil.Step3.Models.ModbusPLC;
using Coil.Step3.Models.Settings;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Options;
using NModbus;
using NModbus.Serial;
using Polly;
using Polly.Registry;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading.Channels;
    
namespace Coil.Step3.Services
{
    public class ModbusPLCService 
    {
        private readonly ILogger<ModbusPLCService> _logger;
        private readonly SemaphoreSlim semaphore  = new SemaphoreSlim(1, 1);
        private readonly ModbusConfig modbusConfig = null!;
        private IModbusMaster? modbusMaster = null;
        private SerialPort? serialPort = null;
        public ModbusPLCService(
            ILogger<ModbusPLCService> logger, 
            IOptions<ModbusConfig> modbusConfig)
        {
            _logger = logger;
            this.modbusConfig = modbusConfig.Value;
        }
        

        public void Open()
        {
            if (serialPort is not null)
                serialPort.Close();
            serialPort = new SerialPort(modbusConfig.ModbusPort, 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
            serialPort.Open();

            var factory = new ModbusFactory();
            modbusMaster = factory.CreateRtuMaster(serialPort);
        }
        public void Close()
        {
            modbusMaster?.Dispose();
            serialPort?.Close();
            serialPort = null;
        }
        public async Task Write(ModbusPLCRequest req)
        {
            try
            {
                await semaphore.WaitAsync();
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(3500));
                await WriteWork(req);
            }
            catch (TaskCanceledException taskcancelledEx)
            {
                _logger.LogError(taskcancelledEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }
        public async Task<ushort[]> Read(ModbusPLCRequest req)
        {
            try
            {
                await semaphore.WaitAsync();
                var cts = new CancellationTokenSource();
                var a = await ReadWork(req);
                return a;
            }
            catch (TaskCanceledException taskcancelledEx)
            {
                _logger.LogError(taskcancelledEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " | " + ex.StackTrace);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }
        private Task<ushort[]> ReadWork(ModbusPLCRequest request)
        {
            Open();
            if (serialPort is null || !serialPort.IsOpen)
                throw new ConnectionAbortedException("Serial Port not opened");
            if (modbusMaster is null)
                throw new ArgumentNullException(nameof(modbusMaster));
            return modbusMaster.ReadHoldingRegistersAsync(request.ClientId, request.Address,request.Value);
        }

        private Task WriteWork(ModbusPLCRequest request )
        {
            Open();
            
            if (serialPort is null || !serialPort.IsOpen)
                throw new ConnectionAbortedException("Serial Port not opened");
            if (modbusMaster is null)
                throw new ArgumentNullException(nameof(modbusMaster));
            return modbusMaster.WriteSingleRegisterAsync(request.ClientId, request.Address, request.Value);
        }
    }
}
