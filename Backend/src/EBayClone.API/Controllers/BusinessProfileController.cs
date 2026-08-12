using System.Security.Claims;
using EBayClone.API.Models;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.BusinessProfile;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/business-profile")]
[Authorize]
public class BusinessProfileController(IBusinessProfileService businessProfileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<BusinessProfileResponse>>> Get(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await businessProfileService.GetByUserIdAsync(userId, ct);
        return Ok(ApiResponse<BusinessProfileResponse?>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BusinessProfileResponse>>> Submit(
        [FromBody] BusinessProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await businessProfileService.SubmitAsync(userId, request, ct);
        return Ok(ApiResponse<BusinessProfileResponse>.Ok(result, "Business profile submitted for verification"));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<BusinessProfileResponse>>> Update(
        [FromBody] BusinessProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await businessProfileService.UpdateAsync(userId, request, ct);
        return Ok(ApiResponse<BusinessProfileResponse>.Ok(result, "Business profile updated"));
    }

    [HttpPost("submit-for-review")]
    public async Task<ActionResult<ApiResponse<BusinessProfileResponse>>> SubmitForReview(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await businessProfileService.SubmitForReviewAsync(userId, ct);
        return Ok(ApiResponse<BusinessProfileResponse>.Ok(result, "Profile submitted for admin review"));
    }

    [HttpPost("documents")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadDocument(
        [FromForm] DocumentUploadRequest request, CancellationToken ct)
    {
        var file = request.File;
        var documentType = request.DocumentType;

        if (file is null)
            return BadRequest(ApiResponse.Fail("File is required"));

        if (file.Length == 0)
            return BadRequest(ApiResponse.Fail("File is empty"));

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(ApiResponse.Fail("Only PDF, JPEG, and PNG files are allowed"));

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var result = await businessProfileService.UploadDocumentAsync(
            userId, stream, file.FileName, file.ContentType, file.Length, documentType, ct);

        return Ok(ApiResponse<DocumentResponse>.Ok(result, "Document uploaded successfully"));
    }

    [HttpDelete("documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteDocument(Guid documentId, CancellationToken ct)
    {
        var userId = GetUserId();
        await businessProfileService.DeleteDocumentAsync(userId, documentId, ct);
        return Ok(ApiResponse.Ok("Document deleted"));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);
}
