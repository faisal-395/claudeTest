namespace GrainMarket.Application.Common.Interfaces;

public interface IBackupService
{
    /// <summary>Runs pg_dump against the configured database and returns the path to the timestamped backup file.</summary>
    Task<string> CreateBackupAsync(CancellationToken ct = default);
}
