namespace ReverseProxy.Models;

public class AppTitleData
{
  public int Listener { get; set; } = 0;
  public int Total { get; set; } = 0;
  public int Live { get; set; } = 0;
  public int Blocked { get; set; } = 0;

  public string Load {
    get {
      return Live switch {
        < 100 => "Low",
        < 500 => "Medium",
        < 1000 => "High",
        < 5000 => "Very High",
        < 10000 => "Chaotic",
        _ => "ON FIRE!!!!"
      };
    }
  }

  public int Error { get; set; } = 0;
  public int Killed { get; set; } = 0;
  public ulong BytesForwarded { get; set; } = 0;
  public ulong BytesResponded { get; set; } = 0;
}