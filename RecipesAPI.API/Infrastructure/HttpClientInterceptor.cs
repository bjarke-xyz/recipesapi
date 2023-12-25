
namespace RecipesAPI.API.Infrastructure;

public class HttpClientInterceptor : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // System.Console.WriteLine("yo" + request.RequestUri);
        return base.SendAsync(request, cancellationToken);
    }
}