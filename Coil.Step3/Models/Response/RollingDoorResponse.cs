namespace Coil.Step3.Models.Response
{
    public class RollingDoorResponse
    {
        public string msg { get; set; } = string.Empty;
        public byte clientId { get; set; }
        public ushort address { get; set;  }
        public ushort value { get; set;  }
    }
}
