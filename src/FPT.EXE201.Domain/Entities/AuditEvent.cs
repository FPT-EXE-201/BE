using System;

namespace FPT.EXE201.Domain.Entities
{
    public class AuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityTable { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? BeforeJson { get; set; } // JSON
        public string? AfterJson { get; set; } // JSON
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? ActorUser { get; set; }
    }
}
