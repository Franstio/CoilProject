namespace Coil.Step3.Models.Response
{
    public class PIDSGEmployeeResponse
    {
        public IEnumerable<IEnumerable<PIDSGEmployee>> result { get; set; } = [];
        public class PIDSGEmployee
        {
            public int OUT { get; set; }
            public string badgeno { get; set; } = string.Empty;
            public string employeename { get; set; } = string.Empty;
        }   
    }
}
