using Microsoft.AspNetCore.SignalR;

namespace Coil.Step3.Hubs
{
    public interface IScalesHub
    {
        Task SendScaleData(Weight50Kg weight50Kg);
    }
    public record Weight50Kg(string weight50Kg);
    public class ScalesHub : Hub<IScalesHub>
    {
    }
}
