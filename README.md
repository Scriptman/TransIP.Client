# TransIP.Client
> TransIP API v6 client for C#

[![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-purple)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![GitHub issues](https://img.shields.io/github/issues/Scriptman/TransIP.Client.svg)](https://github.com/Scriptman/TransIP.Client/issues)
[![license](https://img.shields.io/github/license/Scriptman/TransIP.Client.svg)](https://github.com/Scriptman/TransIP.Client/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/TransIP.Client.svg)](https://www.nuget.org/packages/TransIP.Client/)

Do you like that i take the effort to make a public client API in C# for the TransIP Rest API?

Buy me a coffee or contribute to this project.

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/scriptman)

> [!WARNING]  
> This repository is still in development. I am not responsible for any errors in this code. Use at your own risk.

## Get all owned domains
```csharp
TransIpApi transIp = new TransIpApi("username", "private_key", ClientMode.ReadWrite); // ClientMode.ReadOnly also available.

try
{
    var domains = await transIp.domainService().GetAllDomainsAsync(AdditionalData.Nameservers | AdditionalData.Contacts);

    foreach (var domain in domains)
    {
        Console.WriteLine(domain.Name);
    }
}
catch ( Exception ex )
{
    Console.WriteLine (ex.Message);
}
```

## Get nameservers
```csharp
TransIpApi transIp = new TransIpApi("username", "private_key", ClientMode.ReadWrite); // ClientMode.ReadOnly also available.

try
{
    var domainService = transIp.domainService("domain.nl");
    var nameservers = await domainService.GetNameserversAsync();

    foreach (Nameserver ns in nameservers)
    {
        Console.WriteLine(ns.Hostname);
    }
}
catch ( Exception ex )
{
    Console.WriteLine (ex.Message);
}
```

## Set nameservers
```csharp
TransIpApi transIp = new TransIpApi("username", "private_key", ClientMode.ReadWrite); // ClientMode.ReadOnly also available.

var nameservers = new List<Nameserver>
{
    new Nameserver { Hostname = "ns0.transip.net" }, // Optional: Ipv4 and Ipv6 properties
    new Nameserver { Hostname = "ns1.transip.nl" },
    new Nameserver { Hostname = "ns2.transip.eu" }
};

try
{
    var domainService = transIp.domainService("domain.nl");
    
    if (await domainService.SetNameserversAsync(nameservers))
    {
        Console.WriteLine("Nameservers successfully updated");
    }
}
catch ( Exception ex )
{
    Console.WriteLine (ex.Message);
}
```