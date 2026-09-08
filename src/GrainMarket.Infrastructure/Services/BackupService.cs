using System.Diagnostics;
using GrainMarket.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GrainMarket.Infrastructure.Services;

/// <summary>
/// Setup &gt; Backup: shells out to `pg_dump` (must be on PATH, matching the server's Postgres
/// version) and writes a timestamped .backup file locally. The password is passed via the
/// PGPASSWORD environment variable, never on the command line, so it never shows up in a process
/// listing or gets logged.
/// </summary>
public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;

    public BackupService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        var fileName = $"grainmarket-{DateTime.Now:yyyyMMdd-HHmmss}.backup";
        var filePath = Path.Combine(backupDir, fileName);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_dump",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add(builder.Host ?? "localhost");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(builder.Port.ToString());
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(builder.Username ?? "postgres");
        startInfo.ArgumentList.Add("-F");
        startInfo.ArgumentList.Add("c");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(filePath);
        startInfo.ArgumentList.Add(builder.Database ?? "grainmarket");
        startInfo.Environment["PGPASSWORD"] = builder.Password ?? string.Empty;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start pg_dump. Is PostgreSQL client tools installed and on PATH?");

        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pg_dump failed with exit code {process.ExitCode}: {stderr}");
        }

        return filePath;
    }
}
