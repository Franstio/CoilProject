using Microsoft.AspNetCore.SignalR;

namespace Coil.Step3.Hubs
{
    public interface IScalesHub
    {
        Task SendScaleData(string weight);
    }
    public class ScalesHub : Hub<IScalesHub>
    {
    }
}
