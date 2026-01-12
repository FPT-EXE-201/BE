using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPT.EXE201.Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? PreferredLanguage { get; set; } = "vi";
    }
}
