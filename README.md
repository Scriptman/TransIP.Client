# TransIP.Client
> TransIP API v6 client for C#

[![.NET 6.0 LTS](https://img.shields.io/badge/-.NET%206.0-blueviolet)](https://learn.microsoft.com/en-us/dotnet/api/?view=net-6.0)
[![GitHub issues](https://img.shields.io/github/issues/Scriptman/TransIP.Client.svg)](https://github.com/Scriptman/TransIP.Client/issues)
[![license](https://img.shields.io/github/license/Scriptman/TransIP.Client.svg)](https://github.com/Scriptman/TransIP.Client/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/TransIP.Client.svg)](https://www.nuget.org/packages/TransIP.Client/)

> [!WARNING]  
> This repository is still in development. I am not responsible for any errors in this code. Use at your own risk.

## Get all owned domains
```
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