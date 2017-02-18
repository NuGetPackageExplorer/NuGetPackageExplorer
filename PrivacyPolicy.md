NuGet Package Explorer respects user's privacy and it does not collect any personal information from user without explicitly asking for user's consent.

**OS information and IP address**

When NuGet Package Explorer makes web requests to http://nuget.org to retrieve NuGet packages data, it includes user's machine's OS information in the User-Agent header. The author of NuGet Package Explorer does not have access to this information, but the website http://nuget.org does and it logs this information.
The website will also log user's IP address. Again, the author of NuGet Package Explorer does not have access to this data.

**3rd-party package source**

When user specifies a different package source than the default source at http://nuget.org, he/she will be subjected to the privacy policy of that website. NuGet Package Explorer does not send any such data to its author.
