namespace Coil.Step3.Models.Database
{
    public class TransactionModel
    {
        public int Id { get; set; }
        public int badgeId { get; set; }
        public int? idContainer { get; set; }
        public int idWaste { get; set; }
        public string recordDate { get; set; } = string.Empty;
        public double neto { get; set; }
        public string? containerName { get; set; }
        public string? binName { get; set; }
        public int? binId { get; set; }
        public string? status { get; set; }
        public bool isSuccess { get; set; }
        public string? loginDate { get; set; }

        public ContainerModel? Container { get; set; }  
    }
}
