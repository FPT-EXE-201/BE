using System;

namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// DTO for Admin viewing all subscription transactions.
/// </summary>
public class TransactionAdminDto
{
    public Guid Id { get; set; }
    
    /// <summary>PayOS Order Code</summary>
    public string OrderCode { get; set; } = null!;
    
    /// <summary>Payment Transaction ID (from PayOS/Apple)</summary>
    public string? ReferenceCode { get; set; }
    
    public decimal TransferAmount { get; set; }
    
    public decimal OriginalAmount { get; set; }
    
    public DateTime TransactionDate { get; set; }
    
    public string Status { get; set; } = null!;
    
    public string? UserFirstName { get; set; }
    
    public string? UserLastName { get; set; }
    
    public string? UserEmail { get; set; }
    
    public DateTime ProExpiresAt { get; set; }
    
    public string PackageName { get; set; } = null!;
    
    public string PackageType { get; set; } = null!;
}
