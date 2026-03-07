using System.Net;
using System.Security.Claims;

using Api;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Api.Tests;

public sealed class MsdlProxyTests
{
    [Fact]
    public async Task RunRejectsAbsoluteUrls()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=https://evil.invalid/a.pdb/abc/a.pdb"));
        var proxy = CreateProxy(_ => throw new InvalidOperationException("should not call upstream"));

        var response = await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunRejectsSchemalessUrls()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=//evil.invalid/a.pdb/abc/a.pdb"));
        var proxy = CreateProxy(_ => throw new InvalidOperationException("should not call upstream"));

        var response = await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunRejectsTraversalAndMismatchedFileNames()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=file.pdb/%2e%2e/other.pdb"));
        var proxy = CreateProxy(_ => throw new InvalidOperationException("should not call upstream"));

        var response = await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunRejectsMalformedKeyShape()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=file.pdb/only-two-segments"));
        var proxy = CreateProxy(_ => throw new InvalidOperationException("should not call upstream"));

        var response = await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunReturnsBadGatewayWithoutEchoingUpstreamErrorBody()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=file.pdb/ABCDEF1234567890/file.pdb"));
        using var upstream = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("secret upstream body")
        };
        var proxy = CreateProxy(_ => upstream);

        var response = (TestHttpResponseData)await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.DoesNotContain("secret upstream body", response.ReadBody(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunReturnsRequestEntityTooLargeWhenContentLengthExceedsLimit()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=file.pdb/ABCDEF1234567890/file.pdb"));
        using var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("small")
        };
        upstream.Content.Headers.ContentLength = 1024L * 1024L * 1024L;
        var proxy = CreateProxy(_ => upstream);

        var response = await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task RunReturnsPayloadForValidSymbolKey()
    {
        using var context = new TestFunctionContext();
        var request = new TestHttpRequestData(context, new Uri("https://example.invalid/api/MsdlProxy?symbolkey=file.pdb/ABCDEF1234567890/file.pdb"));
        var payload = new ByteArrayContent([1, 2, 3, 4]);
        using var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = payload
        };
        var proxy = CreateProxy(message =>
        {
            Assert.Equal("https://msdl.microsoft.com/download/symbols/file.pdb/ABCDEF1234567890/file.pdb", message.RequestUri!.AbsoluteUri);
            return upstream;
        });

        var response = (TestHttpResponseData)await proxy.Run(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, response.ReadBytes());
    }

    private static MsdlProxy CreateProxy(Func<HttpRequestMessage, HttpResponseMessage> send)
    {
        return new MsdlProxy(NullLogger<MsdlProxy>.Instance, new TestHttpClientFactory(send));
    }

    private sealed class TestHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> send) : IHttpClientFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler for the test.")]
        public HttpClient CreateClient(string name = "")
        {
            var handler = new DelegatingHandlerStub(send);
            return new HttpClient(handler, disposeHandler: true);
        }
    }

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(send(request));
        }
    }

    private sealed class TestFunctionContext : FunctionContext, IDisposable
    {
        private readonly IServiceProvider _instanceServices = new ServiceCollection().BuildServiceProvider();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public override string InvocationId => Guid.NewGuid().ToString("n");

        public override string FunctionId => "MsdlProxy";

        public override TraceContext TraceContext => throw new NotImplementedException();

        public override BindingContext BindingContext => throw new NotImplementedException();

        public override RetryContext RetryContext => throw new NotImplementedException();

        public override IServiceProvider InstanceServices { get => _instanceServices; set => throw new NotImplementedException(); }

        public override FunctionDefinition FunctionDefinition => throw new NotImplementedException();

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features => throw new NotImplementedException();

        public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            (_instanceServices as IDisposable)?.Dispose();
        }
    }

    private sealed class TestHttpRequestData(FunctionContext functionContext, Uri url) : HttpRequestData(functionContext)
    {
        public override Stream Body { get; } = new MemoryStream();

        public override HttpHeadersCollection Headers { get; } = [];

        public override IReadOnlyCollection<IHttpCookie> Cookies => [];

        public override Uri Url { get; } = url;

        public override IEnumerable<ClaimsIdentity> Identities => [];

        public override string Method => "GET";

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }
    }

    private sealed class TestHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext)
    {
        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; } = [];

        public override Stream Body { get; set; } = new MemoryStream();

        public override HttpCookies Cookies { get; } = new TestHttpCookies();

        public string ReadBody()
        {
            Body.Position = 0;
            using var reader = new StreamReader(Body, leaveOpen: true);
            return reader.ReadToEnd();
        }

        public byte[] ReadBytes()
        {
            Body.Position = 0;
            using var stream = new MemoryStream();
            Body.CopyTo(stream);
            return stream.ToArray();
        }
    }

    private sealed class TestHttpCookies : HttpCookies
    {
        private readonly List<IHttpCookie> _cookies = [];

        public override void Append(string name, string value)
        {
            _cookies.Add(new TestHttpCookie(name, value));
        }

        public override void Append(IHttpCookie cookie)
        {
            _cookies.Add(cookie);
        }

        public override IHttpCookie CreateNew()
        {
            return new TestHttpCookie(string.Empty, string.Empty);
        }
    }

    private sealed class TestHttpCookie(string name, string value) : IHttpCookie
    {
        public string Name => name;
        public string Value => value;
        public string? Domain => null;
        public string? Path => null;
        public DateTimeOffset? Expires => null;
        public bool? HttpOnly => null;
        public bool? Secure => null;
        public SameSite SameSite => SameSite.None;
        public double? MaxAge => null;
    }
}
