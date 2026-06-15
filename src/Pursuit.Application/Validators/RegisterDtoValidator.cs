using FluentValidation;
using Pursuit.Application.DTOs;
using Pursuit.Domain.Enums;

namespace Pursuit.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must be 100 characters or less.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must be 100 characters or less.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or less.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed)
                          && parsed != UserRole.Admin)
            .WithMessage("Role must be either 'Employer' or 'JobSeeker'.");

        RuleFor(x => x.TenantName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company name is required for employer registration.")
            .MaximumLength(200).WithMessage("Company name must be 200 characters or less.")
            .When(x => Enum.TryParse<UserRole>(x.Role, ignoreCase: true, out var role)
                       && role == UserRole.Employer);
    }
}