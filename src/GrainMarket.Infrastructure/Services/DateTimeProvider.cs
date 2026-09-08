using GrainMarket.Application.Common.Interfaces;

namespace GrainMarket.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
