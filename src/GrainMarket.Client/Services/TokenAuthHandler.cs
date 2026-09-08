using System.Net.Http.Headers;

namespace GrainMarket.Client.Services;

public class TokenAuthHandler : DelegatingHandler
{
    private readonly AuthState _authState;

    public TokenAuthHandler(AuthState authState)
    {
        _authState = authState;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_authState.Token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
