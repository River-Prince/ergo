# ergo

A simple HTTP proxy library for .NET framework. 

Currently designed for "outbound" connections only (i.e. something on your machine / server talking through the proxy to the outside world).

Supports HTTPS and CONNECT.

## Getting Started

### Prerequisites

Requires .NET framework 4.6.1 or later.

### Installing

Simply download ergo.dll and add it as a reference in your C# project.

## Usage

To create a HTTP proxy:

```
var proxy = new HttpProxy();
var ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.1"), port);

proxy.ListenerAddresses.Add(ipEndPoint);
```

To start the proxy:

```
proxy.Start();
```

To stop the proxy:

```
proxy.Stop();
```
