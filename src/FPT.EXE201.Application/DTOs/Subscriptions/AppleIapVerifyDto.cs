namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// DTO nhan tu Flutter app sau khi StoreKit 2 purchase thanh cong.
/// SignedTransactionInfo la JWS token do Apple cap, backend verify bang Apple JWKS.
/// </summary>
public record AppleIapVerifyDto(string SignedTransactionInfo);
