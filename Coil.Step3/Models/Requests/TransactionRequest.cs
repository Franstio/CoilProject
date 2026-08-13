namespace Coil.Step3.Models.Requests
{
    public class TransactionRequest
    {
        public IEnumerable<TransactionRequestItem> payload { get; set; } = [];
        public class TransactionRequestItem
        {
            public int idContainer { get; set; }
            public int badgeId { get; set; }
            public int idWaste { get; set; }
            public double neto { get; set; }
            public int binId { get; set; }
            public string? containerName { get; set; }
            public string? binName { get; set; }
            public string? status { get; set; }
            public bool isSuccess { get; set; }
            public string? loginDate { get; set; }
        }
    }
}
