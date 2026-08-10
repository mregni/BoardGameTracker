using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Configuration.Interfaces;
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
    private readonly IEnvironmentProvider _environmentProvider;

    public ManualController(IManualService manualService, IEnvironmentProvider environmentProvider)
    {
        _manualService = manualService;
        _environmentProvider = environmentProvider;
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

    [HttpPost]
    [Route("{id:int}/reindex")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    public async Task<IActionResult> ReindexManual(int id)
    {
        if (!_environmentProvider.RagEnabled)
        {
            return NotFound();
        }

        await _manualService.RequeueManualForIndexing(id);
        return NoContent();
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
    [Route("{id:int}/page/{page:int}/image")]
    public async Task<IActionResult> GetManualPageImage(int id, int page, CancellationToken cancellationToken)
    {
        if (!_environmentProvider.RagEnabled)
        {
            return NotFound();
        }

        var image = await _manualService.GetManualPageImage(id, page, cancellationToken);
        if (image == null)
        {
            return NotFound();
        }

        return File(image.Stream, image.ContentType);
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
