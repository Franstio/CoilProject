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
        public ScaleSerialService(IConfiguration config,IHubContext<ScalesHub,IScalesHub> scaleChannel,ILogger<ScaleSerialService> logger)
        {
            this.configuration  = config;
            this.logger = logger;
            scaleChannelWriter = scaleChannel;
            BuildSerialPort();
        }

        void BuildSerialPort()
        {
            _port = new SerialPort();
            _port.PortName = configuration.GetSection("ScaleConfig:ScalePort").Value!;
            _port.BaudRate = 9600;
            _port.Parity = Parity.None;
            _port.StopBits = StopBits.One;
            _port.DataBits = 8;
            _port.NewLine = "\n\t";
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
                    string data = _port.ReadLine();
                    logger.LogInformation(data);
                    string[] parsed = data.Split(",");
                    if (parsed.Length < 2)
                        continue;
                    string raw = parsed[parsed.Length - 1];
                    string formatted = raw.Replace("+", "").Replace("Kg", "");
                    formatted = formatted.Substring(1, formatted.Length);
                    logger.LogInformation(formatted);
                    if (double.TryParse(formatted, out var weight))
                        await scaleChannelWriter.Clients.All.SendScaleData(weight.ToString("0.00"));
                }

                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                finally
                {
                    await Task.Delay(50);
                }
            }
        }
    }
}
