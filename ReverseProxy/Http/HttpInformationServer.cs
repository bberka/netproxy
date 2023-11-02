using System.Runtime;

namespace ReverseProxy.Http;

public sealed class HttpInformationServer
{
  private const int Port = 80;

  private HttpInformationServer() { }

  public static HttpInformationServer This {
    get {
      _instance ??= new();
      return _instance;
    }
  }

  private static HttpInformationServer? _instance;


  public void StartListening() {
    
    
    
     
    
  }

  public void StopListening() {
    
  }

  public void HandleOnlineCountRequest() {
    
  }

  public void HandleIpListRequest() {
    
  }
  
  
   
}