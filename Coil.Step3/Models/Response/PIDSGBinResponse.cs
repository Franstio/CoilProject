namespace Coil.Step3.Models.Response
{
    public class PIDSGBinResponse
    {
        public IEnumerable<IEnumerable<PIDSGBinResponseItem>> result { get; set; } = [];
        public class PIDSGBinResponseItem
        {
            public double capacity { get; set; }
            public double weight { get; set;  }
            public string name { get; set; } = string.Empty;
        }
    }
}
