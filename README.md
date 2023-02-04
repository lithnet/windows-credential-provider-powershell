![](https://github.com/lithnet/miis-powershell/wiki/images/logo-ex-small.png)

# Lithnet.CredentialProvider.Management PowerShell module
Lithnet.CredentialProvider.Management is a PowerShell module for managing .NET-based Windows Credential Providers.

## Getting started

Install or download the module from the PowerShell Gallery

```powershell
Install-Module Lithnet.CredentialProvider.Management
```

### Register a Credential Provider
This cmdlet registers the credential provider COM component, and registers the credential provider itself.

This cmdlet can be used with .NET Framework and .NET Core credential providers. It cannot be used to register native credential providers.

```powershell
Register-CredentialProvider -File "C:\path\to\credprovider.dll"
```

### Unregister a credential provider
This cmdlet allows you to uninstall a credential provider. The COM registration is removed, and the DLL is removed from the list of registered credential providers for the system. Using the `File` parameter requires a .NET Framework or .NET Core DLL, however, using `Clsid` or `ProgId` can be used with any type of credential provider.

```powershell
# Unregister using the credential provider DLL
Unregister-CredentialProvider -File "C:\path\to\credprovider.dll"

# Unregister using CLSID
Unregister-CredentialProvider -Clsid "00000000-0000-0000-0000-000000000000"

# Unregister using ProgId
Unregister-CredentialProvider -ProgId "MyCredentalProvider"
```

### Disable a credential provider
This cmdlet disables a credential provider, without removing its registration. It will not be shown when invoked by CredUI or LogonUI. Using the `File` parameter requires a .NET Framework or .NET Core DLL, however, using `Clsid` or `ProgId` can be used with any type of credential provider.

```powershell
# Disable using the credential provider DLL
Disable-CredentialProvider -File "C:\path\to\credprovider.dll"

# Disable using CLSID
Disable-CredentialProvider -Clsid "00000000-0000-0000-0000-000000000000"

# Disable using ProgId
Disable-CredentialProvider -ProgId "MyCredentalProvider"
```

### Enable a credential provider
This cmdlet enables a previously disabled credential provider. Using the `File` parameter requires a .NET Framework or .NET Core DLL, however, using `Clsid` or `ProgId` can be used with any type of credential provider.

```powershell
# Enable using the credential provider DLL
Enable-CredentialProvider -File "C:\path\to\credprovider.dll"

# Enable using CLSID
Enable-CredentialProvider -Clsid "00000000-0000-0000-0000-000000000000"

# Enable using ProgId
Enable-CredentialProvider -ProgId "MyCredentalProvider"
```

### Get a list of all credential providers
Gets a list of all credental providers registered on the system, and their registration state
```powershell
Get-CredentialProvider
```

### Get information on a specific credential provider
Gets information on a specific credential provider. Using the `File` parameter requires a .NET Framework or .NET Core DLL, however, using `Clsid` or `ProgId` can be used with any type of credential provider.
```powershell
# Get information from the credential provider DLL
Get-CredentialProvider -File "C:\path\to\credprovider.dll"

# Get using CLSID
Get-CredentialProvider -Clsid "00000000-0000-0000-0000-000000000000"

# Get using ProgId
Get-CredentialProvider -ProgId "MyCredentalProvider"
```

### Test a credential provider
This cmdlet invokes CredUI, which allows you to do basic testing of your credential provider
```powershell
Invoke-CredUI
```
