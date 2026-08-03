namespace Coil.Step3.Models.Response
{
    public class PIDSGStep4CheckResponse
    {
        public IEnumerable<PIDSGStep4CheckResponseItem> result { get; set; } = []; 
        public class PIDSGStep4CheckResponseItem
        {
            public int badgeno { get; set; }
            public string dt { get; set; } = string.Empty;
            public string frombin_name { get; set; } = string.Empty;
            public double discharge_weight { get; set; }
        }
    }
}
