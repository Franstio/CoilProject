using Coil.Step3.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.IO.Ports;
using System.Threading.Channels;

namespace Coil.Step3.Services
{
    public class ScaleSerialService : BackgroundService
    {
        private SerialPort _port = null!;
        private readonly IConfiguration configuration;
        private readonly IHubContext<ScalesHub,IScalesHub> scaleChannelWriter;
        private readonly ILogger<ScaleSerialService> logger;
        private int interval = 100;
        public ScaleSerialService(IConfiguration config,IHubContext<ScalesHub,IScalesHub> scaleChannel,ILogger<ScaleSerialService> logger)
        {
            this.configuration  = config;
            this.logger = logger;
            scaleChannelWriter = scaleChannel;
            BuildSerialPort();
            interval = configuration.GetSection("ScaleConfig:Interval").Get<int>();
        }

        void BuildSerialPort()
        {
            _port = new SerialPort();
            _port.PortName = configuration.GetSection("ScaleConfig:ScalePort").Value!;
            _port.BaudRate = 9600;
            _port.Parity = Parity.None;
            _port.StopBits = StopBits.One;
            _port.DataBits = 8;
            _port.NewLine = "\r";
        }




        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            while (!stoppingToken.IsCancellationRequested)
            {

                try
                {
                    if (!_port.IsOpen)
                        _port.Open();
                    string? data = _port.ReadExisting().Split("\r").FirstOrDefault();
                    if (data is null)
                        continue;
                    logger.LogInformation(data);
                    string[] parsed = data.Split(",");
                    string raw = parsed[parsed.Length - 1];
                    string formatted = raw.Replace("Kg", "").Replace("+","").Replace("=","");
                    formatted = formatted.Substring(1, formatted.Length-1);
                    logger.LogInformation(formatted);
                    await _port.BaseStream.FlushAsync();
                    if (double.TryParse(formatted, out var weight))
                        await scaleChannelWriter.Clients.All.SendScaleData( new Weight50Kg(weight.ToString("0.00")));
                }

                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                finally
                {
                    await Task.Delay(interval,stoppingToken);
                }
            }
        }
    }
}
