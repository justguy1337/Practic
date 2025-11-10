using System.IO;
using CharityManagement.Api.Models;
using CharityManagement.Api.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CharityManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Administrator)]
public class SystemController : ControllerBase
{
    private readonly DataExportService _dataExportService;

    public SystemController(DataExportService dataExportService)
    {
        _dataExportService = dataExportService;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAsync(CancellationToken cancellationToken)
    {
        var payload = await _dataExportService.CreateExportAsync(cancellationToken);
        var fileName = $"charity-export-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
        return File(payload, "application/zip", fileName);
    }

    [HttpPost("backup")]
    public async Task<IActionResult> CreateBackupAsync(CancellationToken cancellationToken)
    {
        var payload = await _dataExportService.CreateExportAsync(cancellationToken);
        var backupFolder = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupFolder);

        var fileName = $"charity-backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
        var filePath = Path.Combine(backupFolder, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, payload, cancellationToken);

        return Ok(new { File = filePath, CreatedAt = DateTimeOffset.UtcNow });
    }
}
