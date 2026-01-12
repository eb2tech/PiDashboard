namespace DashAgent;

public class PiModel
{
    public int Brightness { get; set; }
    public uint CpuTemp { get; set; }
    public uint CpuUsage { get; set; }
    public bool IsOn { get; set; }
    public uint MemoryUsage { get; set; }
}
