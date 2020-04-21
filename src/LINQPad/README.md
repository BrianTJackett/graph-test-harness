# Microsoft Graph test harness - LINQPad

## Supported use cases

- Microsoft Graph C# SDK for confidential client (application) client secret authentication
- Microsoft Graph C# SDK for confidential client (application) certificate authentication
- Microsoft Graph C# SDK for public client (delegated) device code authentication

Please see "[coming soon](./README.md#coming-soon)" section for future planned support.

## Prerequisites
  
- [LINQPad v6](https://www.linqpad.net/LINQPad6.aspx) installed on development machine.  (**Note:** Sample was built and tested using LINQPad v6.7.5 (x64).  May work with other versions but those have not been tested.)
- [.Net Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (**Note:** Sample built with .Net Core SDK 3.1.201.  May work with other versions but those have not been tested.

Set the following "passwords" in LINQPad (File -> Password Manager):

- *clientId*
- *tenantId*
- *redirectUri*

The following "passwords" also needed depending on authentication mode:

- Client secret authentication
  - *clientSecret*
- Certificate authentication
  - *certificateThumbprint*
- Device code authentication
  - *clientIdPublic*

## Coming soon

- ~~Public client (delegated) authentication~~
- ~~Certificate authentication~~
- Revised naming scheme for "passwords" since public / device code was added after the fact

## Research topics
