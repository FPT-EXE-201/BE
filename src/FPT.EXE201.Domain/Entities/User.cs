using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Email { get; set; }
        public string? EmailNormalized { get; set; }
        public string? Phone { get; set; }

        // DB: VARBINARY(255)
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public UserStatus Status { get; set; } = UserStatus.Pending;
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public UserProfile? Profile { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
