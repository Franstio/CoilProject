namespace Coil.Step3.Models.Database
{
    public class RollingDoorModel
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public double weight { get;set;  }
        public int type_waste { get; set;  }
        public double max_weight { get; set;  }
        public byte clientId { get; set;  }
        public DateTime last_empty { get; set; }

        public string? Station { get; set; } = null;
    }
}
