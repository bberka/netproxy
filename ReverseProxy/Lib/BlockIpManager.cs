﻿namespace ReverseProxy.Lib;

public static class BlockIpManager
{
  private static readonly int BLOCK_LIST_RELOAD_INTERVAL = 5;
  private static readonly List<string> _blockedIps = new();

  private static DateTime _lastUpdate = DateTime.MinValue;

  private static List<string> BlockedIps {
    get {
      Reload();
      return _blockedIps;
    }
  }

  private static void Add(string ip) {
    lock (_blockedIps) {
      if (!_blockedIps.Contains(ip)) _blockedIps.Add(ip);
    }
  }

  private static void Remove(string ip) {
    lock (_blockedIps) {
      if (_blockedIps.Contains(ip)) {
        _blockedIps.Remove(ip);
        WriteAll();
      }
    }
  }

  public static bool IsBlocked(string ip) {
    if (ip.Length < 5) return false;
    if (ip.StartsWith("::ffff:")) ip = ip[7..];
    return _blockedIps.Contains(ip);
  }

  private static void Clear() {
    lock (_blockedIps) {
      _blockedIps.Clear();
    }
  }

  private static List<string> GetLines() {
    if (!File.Exists("blocked_ips.txt")) return new List<string>();
    var blockedIps = File.ReadAllLines("blocked_ips.txt").ToList();
    blockedIps.RemoveAll(x => x.StartsWith("--"));
    return blockedIps;
  }

  private static void WriteAll() {
    lock (_blockedIps) {
      File.WriteAllLines("blocked_ips.txt", _blockedIps);
    }
  }

  private static void Reload() {
    try {
      if (_lastUpdate.AddMinutes(BLOCK_LIST_RELOAD_INTERVAL) > DateTime.Now) return;
      var lines = GetLines();
      foreach (var ip in lines) {
        if (ip.StartsWith("--")) continue;
        Add(ip);
      }

      _lastUpdate = DateTime.Now;
    }
    catch (Exception ex) {
      Log.Fatal(ex,$"An error occurred");
    }
  }
}