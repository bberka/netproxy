
namespace ReverseProxy_NET6.Lib;

public static class BlockIpManager
{
  private static readonly List<string> _blockedIps = new();

  private static DateTime _lastUpdate = DateTime.MinValue;

  private static List<string> BlockedIps {
    get {
      if (_lastUpdate.AddMinutes(1) < DateTime.Now) Load();
      return _blockedIps;
    }
  }

  public static void Add(string ip) {
    if (!_blockedIps.Contains(ip)) _blockedIps.Add(ip);
  }

  public static void Remove(string ip) {
    if (_blockedIps.Contains(ip)) _blockedIps.Remove(ip);
  }

  public static bool IsBlocked(string ip) {
    if (ip.Length < 5) return false;
    return _blockedIps.Contains(ip);
  }

  public static void Clear() {
    _blockedIps.Clear();
  }

  public static void Load() {
    try {
      if (!File.Exists("blocked_ips.txt")) return;
      var blockedIps = File.ReadAllLines("blocked_ips.txt");
      foreach (var ip in blockedIps) {
        if (ip.StartsWith("--")) continue;
        Add(ip);
      }

      _lastUpdate = DateTime.Now;
    }
    catch (Exception ex) {
      Log.Fatal($"An error occurred : {ex}");
      throw;
    }
  }
}