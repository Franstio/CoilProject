using Coil.Step3.Models.Database;

namespace Coil.Step3.Interfaces
{
    public interface ISyncService<T> where T : class
    {
        Task<IEnumerable<T>> SyncTData();
    }
}
