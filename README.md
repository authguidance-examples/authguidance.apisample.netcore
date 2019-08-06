# authguidance.apisample.netcore

### Overview

* An SPA sample using OAuth 2.0 and Open Id Connect, referenced in my blog at https://authguidance.com
* **The goal of this sample is to implement our [API Platform Architecture](https://authguidance.com/2019/03/24/api-platform-design/) in .Net Core**

### Details

* See the [Overview Page](http://authguidance.com/2018/01/05/net-core-code-sample-overview/) for hat the API does and how to run it
* See the [Coding Key Points](http://authguidance.com/2018/01/06/net-core-api-key-coding-points/) for technical implementation details

### Programming Languages

* C# code and .Net Core 2 is used for the API

### Middleware Used

* The [IdentityModel2](https://github.com/IdentityModel/IdentityModel2) library is used for API OAuth operations
* The [Microsoft In Memory Cache](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-2.2) is used to cache API claims in memory
* Kestrel is used to host the API
* Okta is used for the Authorization Server
* OpenSSL is used for SSL certificate handling
