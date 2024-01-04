NetProxy
========

Netproxy is a simple ipv6/ipv4 UDP & TCP proxy based on .NET 5.0.
Tested on *win10-x64* and *ubuntu.16.20-x64*.

Why? 
====
We needed a simple, crossplatform IPV6 compatible UDP forwarder, and couldn't find a satisfying solution. 
Nginx was obviously a great candidate but building it on Windows with UDP forwarding enabled was quite a pain.

The objective is to be able to expose as an ipv6 endpoint a server located in an ipv4 only server provider.

Limitations
===========
Each remote client is mapped to a port of the local server therefore:
- The original IP of the client is hidden to the server the packets are forwarded to.
- The number of concurrent clients is limited by the number of available ports in the server running the proxy.

Disclaimer
==========
Error management exist, but is minimalist. IPV6 is not supported on the forwarding side.

Usage
=====
- Compile for your platform following instructions at https://www.microsoft.com/net/core
- Rewrite the `config.json` file to fit your need
- Run NetProxy

Configuration
=============
`config.json` contains a map of named forwarding rules, for instance :


     {
      "Login": {
        "LocalIp": "127.0.0.1",
        "LocalPort": 8800,
        "Protocol": "tcp",
        "ForwardIp": "127.0.0.1",
        "ForwardPort": 8888,
        "MaxConnectionLimit" : 99,
        "FilterConnection" : [
          "127.0.0.1",
        ]
      },
      "AntiCheat": {
        "LocalIp": "127.0.0.1",
        "LocalPort": 8780,
        "Protocol": "tcp",
        "ForwardIp": "127.0.0.1",
        "ForwardPort": 8781
      },
      "Game": {
        "LocalIp": "127.0.0.1",
        "LocalPort": 8809,
        "Protocol": "tcp",
        "ForwardIp": "127.0.0.1",
        "ForwardPort": 8889,
        "RequireConnectionToPort" : 8780
      },
      
    }

- *localport* : The local port the forwarder should listen to.
- *localip* : An optional local binding IP the forwarder should listen to. If empty or missing, it will listen to ANY_ADDRESS.
- *protocol* : The protocol to forward. `tcp`,`udp`, or `any`.
- *forwardIp* : The ip the traffic will be forwarded to.
- *forwardPort* : The port the traffic will be forwarded to.
- *MaxConnectionLimit* : Number of connection limit. If 0 or not exists, no limit applied.
- *ConnectionLimitPerIp* : Number of connections allowed from a single Ip Address. If 0 or not exists, no limit applied.
- *RequireConnectionToPort* : In a case where it is required to have a connection established to a proxy port before connecting to another.
- *FilterConnection* : Only allows Ip Addresses in this array. It is recommended to set this config in your Firewall rather than in Proxy Config
