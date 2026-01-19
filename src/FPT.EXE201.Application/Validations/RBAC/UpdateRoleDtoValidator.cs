using FluentValidation;
using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.Validations.RBAC
{
    public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
    {
        public UpdateRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required")
                .MaximumLength(100).WithMessage("Role name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Description must not exceed 255 characters");
        }
    }
}
