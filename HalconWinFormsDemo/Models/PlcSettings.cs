namespace HalconWinFormsDemo.Models
{
    public class PlcSettings
    {
        public string PlcAIp { get; set; } = "192.168.0.10";
        public string PlcBIp { get; set; } = "192.168.0.11";

        // Holding Register, 0-based
        public ushort PlcAAlarmRegister { get; set; } = 0;
        public ushort PlcBAlarmRegister { get; set; } = 0;

        // Modbus slave id,通常 1
        public byte SlaveId { get; set; } = 1;
    }
}
