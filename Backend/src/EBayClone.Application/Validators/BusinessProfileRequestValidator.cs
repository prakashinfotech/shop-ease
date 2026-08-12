using EBayClone.Application.DTOs.BusinessProfile;
using FluentValidation;

namespace EBayClone.Application.Validators;

public class BusinessProfileRequestValidator : AbstractValidator<BusinessProfileRequest>
{
    public BusinessProfileRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MinimumLength(2).WithMessage("Company name must be at least 2 characters")
            .MaximumLength(200);

        // India GST: 2-digit state + 5-char PAN + 4-digit + 1 alpha + 1 alphanum + Z + 1 alphanum
        RuleFor(x => x.GstNumber)
            .NotEmpty().WithMessage("GST number is required")
            .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .WithMessage("Invalid GST number format (e.g. 27AAPFU0939F1ZV)");

        // India PAN: 5 letters + 4 digits + 1 letter
        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN number is required")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
            .WithMessage("Invalid PAN number format (e.g. AAPFU0939F)");

        RuleFor(x => x.BusinessAddress)
            .MaximumLength(500).When(x => x.BusinessAddress is not null);

        RuleFor(x => x.BusinessPhone)
            .Matches(@"^\+?[0-9\s\-]{7,20}$").When(x => x.BusinessPhone is not null)
            .WithMessage("Invalid phone number format");

        RuleFor(x => x.BusinessEmail)
            .EmailAddress().When(x => x.BusinessEmail is not null)
            .WithMessage("Invalid business email format");
    }
}
