namespace FPT.EXE201.Application.DTOs.Auth;

public class DeleteAccountRequestDto
{
    public string Password { get; set; } = string.Empty;
    public string ConfirmationPhrase { get; set; } = string.Empty;
}