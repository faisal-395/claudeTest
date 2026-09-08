using GrainMarket.Api.Common;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/backup")]
[ModulePermission(ModuleName.Backup)]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [ModulePermission(ModuleName.Backup, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<object>> CreateBackup(CancellationToken ct)
    {
        var path = await _backupService.CreateBackupAsync(ct);
        return Ok(new { path });
    }
}
