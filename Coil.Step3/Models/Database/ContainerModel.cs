namespace Coil.Step3.Models.Database
{
    public class ContainerModel
    {
        public int containerid { get; set; }
        public string name { get; set; } = string.Empty;
        public string station { get; set; } = string.Empty;
        public double weightbin { get; set; } 
        public int idWaste { get; set; } 
        public double step2value { get; set;  }
        public WasteModel? Waste { get; set; }
    }
}
