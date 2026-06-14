using FluentValidation;
using Pursuit.Application.DTOs;

namespace Pursuit.Application.Validators;

public class UpdateApplicationStatusDtoValidator : AbstractValidator<UpdateApplicationStatusDto>
{
    public UpdateApplicationStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid application status.");
    }
}