using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
