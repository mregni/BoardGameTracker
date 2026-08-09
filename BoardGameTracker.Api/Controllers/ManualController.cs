using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Manuals.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/manual")]
[Authorize]
public class ManualController : ControllerBase
{
    private const long MaxUploadBytes = 1024L * 1024 * 1024;

    private readonly IManualService _manualService;

    public ManualController(IManualService manualService)
    {
        _manualService = manualService;
    }

    [HttpGet]
    [Route("game/{gameId:int}")]
    public async Task<IActionResult> GetManualsForGame(int gameId)
    {
        var manuals = await _manualService.GetManualsForGame(gameId);
        return Ok(manuals.ToListDto());
    }

    [HttpPost]
    [Route("game/{gameId:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> UploadManuals(int gameId, [FromForm] UploadManualsCommand command)
    {
        var manuals = await _manualService.UploadManuals(gameId, command.Files);
        return Ok(manuals.ToListDto());
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> DeleteManual(int id)
    {
        await _manualService.DeleteManual(id);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:int}/download")]
    public async Task<IActionResult> DownloadManual(int id)
    {
        var download = await _manualService.GetManualForDownload(id);
        return File(download.Stream, download.ContentType, download.FileName);
    }

    [HttpGet]
    [Route("gamenight/{linkId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetManualsForGameNight(Guid linkId)
    {
        var manuals = await _manualService.GetManualsForGameNight(linkId);
        return Ok(manuals);
    }

    [HttpGet]
    [Route("gamenight/{linkId:guid}/manual/{manualId:int}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadGameNightManual(Guid linkId, int manualId)
    {
        var download = await _manualService.GetManualForGameNightDownload(linkId, manualId);
        return File(download.Stream, download.ContentType, download.FileName);
    }
}
