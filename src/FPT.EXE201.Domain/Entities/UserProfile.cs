using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities
{
    public class UserProfile : BaseEntity
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string PreferredLang { get; set; } = "vi";

        public User User { get; set; } = default!;
        public Language? PreferredLanguage { get; set; }
    }
}
