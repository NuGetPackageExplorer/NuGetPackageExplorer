# Information Collected And Transmitted By NuGet Package Explorer

First, a reminder: NuGet Package Explorer is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.

With that out of the way, here's a breakdown of all the information we may collect via Bugsnag and/or Application Insights.

### Application-Level
Includes:
* Exception information
  * Could, in rare cases, contain paths packages on your computer
* Machine name
* Host name
* Version number (e.g. 2.0.x.x)

### Operating System-Level
Includes:
* Architecture (e.g. 32-bit)
* Version (e.g. Windows 10.0.17763.0)
* Build (e.g. 17134.1.amd64fre.rs4_release.180410-1804)
* Available processors/cores (e.g. 8 cores)
* Machine Name (e.g. MyFastPC)
* .NET Core Common Language Runtime version (e.g. 4.0.30319.42000)
* Light/Dark mode configuration (light/dark)
* Right-to-Left configuration (on/off)
* Transparency configuration (on/off)
* Accent color configuration (on/off)
* System Animations configuration (on/off)

## Package Sources

**OS information and IP address**

When NuGet Package Explorer makes web requests to http://nuget.org to retrieve NuGet packages data, it includes user's machine's OS information in the User-Agent header. The author of NuGet Package Explorer does not have access to this information, but the website http://nuget.org does and it logs this information.
The website will also log user's IP address. Again, the author of NuGet Package Explorer does not have access to this data.

**3rd-party package source**

When user specifies a different package source than the default source at http://nuget.org, he/she will be subjected to the privacy policy of that website. NuGet Package Explorer does not send any such data to its author.

## Third-Party Policies

* Bugsnag https://docs.bugsnag.com/legal/privacy-policy/
* Microsoft Store https://docs.microsoft.com/en-us/legal/windows/agreements/store-policies