using System.ComponentModel.DataAnnotations;
using EBayClone.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EBayClone.API.Models;

public class ImageUploadRequest
{
    public IFormFile? File { get; set; }
}

public class DocumentUploadRequest
{
    public IFormFile? File { get; set; }
    [Required]
    public DocumentType DocumentType { get; set; }
}
