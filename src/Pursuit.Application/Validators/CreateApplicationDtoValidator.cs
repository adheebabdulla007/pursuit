using FluentValidation;
using Pursuit.Application.DTOs;

namespace Pursuit.Application.Validators;

public class CreateApplicationDtoValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationDtoValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("JobId is required.");

        RuleFor(x => x.ResumeUrl)
            .NotEmpty().WithMessage("ResumeUrl is required.")
            .MaximumLength(500).WithMessage("ResumeUrl must be 500 characters or less.")
            .Must(BeAValidUrl).WithMessage("ResumeUrl must be a valid absolute URL.");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}