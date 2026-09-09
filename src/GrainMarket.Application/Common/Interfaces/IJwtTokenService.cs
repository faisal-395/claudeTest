using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);

    /// <summary>How long an access token issued by GenerateToken stays valid for.</summary>
    TimeSpan AccessTokenLifetime { get; }

    /// <summary>How long a refresh token issued alongside it stays valid for.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
