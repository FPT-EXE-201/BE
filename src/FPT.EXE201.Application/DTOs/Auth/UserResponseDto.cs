using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPT.EXE201.Application.DTOs.Auth
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        
        // Profile info
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string PreferredLanguage { get; set; } = "vi";
    }
}
