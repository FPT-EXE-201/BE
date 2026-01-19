using FluentValidation;
using FPT.EXE201.Application.DTOs.RBAC;

namespace FPT.EXE201.Application.Validations.RBAC
{
    public class AssignRoleDtoValidator : AbstractValidator<AssignRoleDto>
    {
        public AssignRoleDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.RoleIds)
                .NotEmpty().WithMessage("At least one role must be specified");
        }
    }
}
