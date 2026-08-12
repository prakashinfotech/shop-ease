using EBayClone.Application.Common;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await categoryService.GetAllAsync(ct);
        return Ok(ApiResponse<IEnumerable<CategoryResponse>>.Ok(result));
    }

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryTreeResponse>>>> GetTree(CancellationToken ct)
    {
        var result = await categoryService.GetTreeAsync(ct);
        return Ok(ApiResponse<IEnumerable<CategoryTreeResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await categoryService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

    [HttpGet("{id:guid}/metadata")]
    public async Task<ActionResult<ApiResponse<CategoryMetadataResponse>>> GetMetadata(Guid id, CancellationToken ct)
    {
        var result = await categoryService.GetMetadataAsync(id, ct);
        return Ok(ApiResponse<CategoryMetadataResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(
        [FromBody] CategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<CategoryResponse>.Ok(result, "Category created"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(
        Guid id, [FromBody] CategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<CategoryResponse>.Ok(result, "Category updated"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(id, ct);
        return Ok(ApiResponse.Ok("Category deleted"));
    }

    [HttpPost("{id:guid}/attributes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryAttributeResponse>>> CreateAttribute(
        Guid id, [FromBody] CategoryAttributeRequest request, CancellationToken ct)
    {
        var result = await categoryService.CreateAttributeAsync(id, request, ct);
        return Ok(ApiResponse<CategoryAttributeResponse>.Ok(result, "Attribute created"));
    }

    [HttpPut("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryAttributeResponse>>> UpdateAttribute(
        Guid id, Guid attributeId, [FromBody] CategoryAttributeRequest request, CancellationToken ct)
    {
        var result = await categoryService.UpdateAttributeAsync(id, attributeId, request, ct);
        return Ok(ApiResponse<CategoryAttributeResponse>.Ok(result, "Attribute updated"));
    }

    [HttpDelete("{id:guid}/attributes/{attributeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteAttribute(Guid id, Guid attributeId, CancellationToken ct)
    {
        await categoryService.DeleteAttributeAsync(id, attributeId, ct);
        return Ok(ApiResponse.Ok("Attribute deleted"));
    }
}
