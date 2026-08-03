using Coil.Step3.Models.Database;
using Coil.Step3.Models.Requests;
using Coil.Step3.Models.Response;
using Coil.Step3.Services;
using MySqlX.XDevAPI;

namespace Coil.Step3.Interfaces
{
    public interface IStep4CheckService
    {
        Task<PIDSGStep4CheckResponse> Step4Check(string binname);
        
    }
}
