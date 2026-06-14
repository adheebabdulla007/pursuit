using FluentValidation;
using Pursuit.Application.DTOs;

namespace Pursuit.Application.Validators;

public class CreateJobDtoValidator : AbstractValidator<CreateJobDto>
{
    public CreateJobDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must be 300 characters or less.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000).WithMessage("Description must be 5000 characters or less.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must be 200 characters or less.");

        RuleFor(x => x.SalaryMin)
            .GreaterThanOrEqualTo(0).WithMessage("SalaryMin cannot be negative.");

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(0).WithMessage("SalaryMax cannot be negative.")
            .GreaterThan(x => x.SalaryMin).WithMessage("SalaryMax must be greater than SalaryMin.");

        RuleFor(x => x.JobType)
            .IsInEnum().WithMessage("JobType must be a valid job type.");
    }
}