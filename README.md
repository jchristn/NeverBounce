<img src="https://github.com/jchristn/NeverBounce/raw/main/Assets/icon.png" width="150" height="150">

# NeverBounce

NeverBounce is a simple library for validating email addresses using the [NeverBounce](https://neverbounce.com/) API.

 [![NuGet Version](https://img.shields.io/nuget/v/NeverBounce.svg?style=flat)](https://www.nuget.org/packages/NeverBounce/) [![NuGet](https://img.shields.io/nuget/dt/NeverBounce.svg)](https://www.nuget.org/packages/NeverBounce) 

## Usage
```csharp
using NeverBounce;
NeverBounceClient client = new NeverBounceClient("[your API key]");
EmailValidationResult result;

result = client.Verify("[some email address]");
result = await client.VerifyAsync("[some email address]");

if (result.Valid)
{
  // success
}
else
{
  // failure
}
```

The ```EmailValidationResult``` object, when shown as JSON, appears as:
```
{
  "Time": {
    "Start": "2022-05-22T05:18:36.0598952Z",
    "End": "2022-05-22T05:18:36.9516956Z",
    "TotalMs": 891.8
  },
  "Valid": true,
  "Flags": {
    "HasDns": true,
    "HasDnsMx": true,
    "IsFreeService": true,
    "SmtpConnectable": true,
    "AllFlags": [
      "free_email_host",
      "has_dns",
      "has_dns_mx",
      "smtp_connectable",
      "has_dns"
    ]
  },
  "Raw": {
    "status": "success",
    "result": "valid",
    "flags": [
      "free_email_host",
      "has_dns",
      "has_dns_mx",
      "smtp_connectable",
      "has_dns"
    ],
    "suggested_correction": "",
    "execution_time": 166
  }
}
```
The following are the main components:

- ```Time``` indicates the start and end time, and the total amount of time in milliseconds taken to get a response
- ```Valid``` is a ```boolean``` indicating whether or not the address should be considered valid
- ```Flags``` indicates the flags returned by the service
- ```Raw``` is the raw response received from the server

## Need More Capabilities?

The library is designed to be really light with not much configuration.  If you have an enhancement, please feel free to either 1) file an issue, 2) submit a PR, or 3) simply clone and use the code as you see fit (MIT license).

## Special Thanks

Thanks to the authors that provided the free logo found here: https://icon-library.com/icon/check-email-icon-13.html
