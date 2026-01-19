using System;

namespace FPT.EXE201.Domain.Entities
{
    public class AuthRefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid Jti { get; set; }
        public byte[] TokenHash { get; set; } = Array.Empty<byte>();
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? RotatedFromId { get; set; }

        // Device & Security info
        public string? DeviceInfo { get; set; } // JSON
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public AuthRefreshToken? RotatedFrom { get; set; }
    }
}
