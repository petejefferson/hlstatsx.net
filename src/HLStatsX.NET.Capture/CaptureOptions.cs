namespace HLStatsX.NET.Capture;

public sealed class CaptureOptions
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 27500;
    public string ForwardHost { get; set; } = "127.0.0.1";
    public int ForwardPort { get; set; } = 27501;
    public string LogFile { get; set; } = "capture.log";
}
