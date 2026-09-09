using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GrainMarket.Application.Auth;

namespace GrainMarket.Client.Services;

/// <summary>
/// Attaches the current JWT to every outgoing request and keeps the session alive across long idle
/// periods: it proactively swaps the access token for a fresh one shortly before it expires, and —
/// if a request still comes back 401 (clock skew, or the app was asleep well past expiry) — refreshes
/// once and retries. The session is only ever cleared when the server explicitly rejects the refresh
/// token (it's genuinely expired or revoked) — a network hiccup, a timeout, or the server being
/// briefly unreachable (e.g. right after the machine wakes from sleep) leaves the session alone, so
/// the app never logs someone out just because a request didn't make it through once. Only on that
/// explicit rejection does AuthState.SessionExpired fire, sending the UI back to login.
/// </summary>
public class TokenAuthHandler : DelegatingHandler
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    private readonly AuthState _authState;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public TokenAuthHandler(AuthState authState, IHttpClientFactory httpClientFactory)
    {
        _authState = authState;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_authState.IsAuthenticated && _authState.IsExpiringSoon(ExpiryBuffer))
        {
            await RefreshTokenAsync(cancellationToken);
        }

        Attach(request);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && _authState.RefreshToken is not null)
        {
            var refreshed = await RefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                response.Dispose();
                var retryRequest = await CloneAsync(request);
                Attach(retryRequest);
                return await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private void Attach(HttpRequestMessage request)
    {
        if (_authState.Token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.Token);
        }
    }

    private async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (_authState.RefreshToken is null)
        {
            // Nothing to fall back on (never logged in, or the session was already cleared).
            return false;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have already refreshed while this one waited on the lock.
            if (!_authState.IsExpiringSoon(ExpiryBuffer))
            {
                return true;
            }

            var refreshToken = _authState.RefreshToken;
            if (refreshToken is null)
            {
                return false;
            }

            HttpResponseMessage response;
            try
            {
                var client = _httpClientFactory.CreateClient("AuthRefresh");
                response = await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest(refreshToken), cancellationToken);
            }
            catch
            {
                // Couldn't even reach the server (offline, DNS hiccup right after waking from
                // sleep, request timed out) — transient, not a rejection. Leave the session alone;
                // the caller falls back to whatever token it already has and tries again next time.
                return false;
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // The server has explicitly rejected this refresh token — it's genuinely expired,
                // revoked, or the account was deactivated. Only this case ends the session.
                _authState.ClearDueToExpiry();
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Some other failure (e.g. the API is mid-restart) — transient, don't end the session.
                return false;
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (login is null)
            {
                return false;
            }

            _authState.SetToken(login);
            await _authState.PersistTokenAsync();
            return true;
        }
        catch
        {
            // Unexpected failure reading/persisting the refreshed token — transient, don't end the
            // session over it.
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content is not null)
        {
            var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer);
            buffer.Position = 0;
            clone.Content = new StreamContent(buffer);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}
