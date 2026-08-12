using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.EmailTemplates;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/admin/email-templates")]
[Authorize(Roles = "Admin")]
public class EmailTemplatesController(IEmailTemplateService templateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmailTemplateResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EmailTemplateType? type = null,
        CancellationToken ct = default)
    {
        var result = await templateService.GetAllAsync(page, pageSize, type, ct);
        return Ok(ApiResponse<PagedResult<EmailTemplateResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmailTemplateResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await templateService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<EmailTemplateResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmailTemplateResponse>>> Create(
        [FromBody] CreateEmailTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<EmailTemplateResponse>.Ok(result, "Template created"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmailTemplateResponse>>> Update(
        Guid id, [FromBody] UpdateEmailTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<EmailTemplateResponse>.Ok(result, "Template updated"));
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<EmailTemplateResponse>>> Activate(Guid id, CancellationToken ct)
    {
        var result = await templateService.ActivateAsync(id, ct);
        return Ok(ApiResponse<EmailTemplateResponse>.Ok(result, "Template activated"));
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<EmailTemplateResponse>>> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await templateService.DeactivateAsync(id, ct);
        return Ok(ApiResponse<EmailTemplateResponse>.Ok(result, "Template deactivated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await templateService.DeleteAsync(id, ct);
        return Ok(ApiResponse.Ok("Template deleted"));
    }
}
