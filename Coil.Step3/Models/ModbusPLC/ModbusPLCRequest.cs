namespace Coil.Step3.Models.ModbusPLC
{
    public class ModbusPLCRequest
    {
        public byte ClientId { get; set; } = 1;
        public ushort Address { get; set; } = 0;
        public ushort Value { get; set; } = 0;
    }
}
