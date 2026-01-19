using FluentValidation;
using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.Validations.RBAC
{
    public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
    {
        public CreateRoleDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Role code is required")
                .MaximumLength(50).WithMessage("Role code must not exceed 50 characters")
                .Matches("^[A-Z_]+$").WithMessage("Role code must contain only uppercase letters and underscores");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required")
                .MaximumLength(100).WithMessage("Role name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Description must not exceed 255 characters");
        }
    }
}
