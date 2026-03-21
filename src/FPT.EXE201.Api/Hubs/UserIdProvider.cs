using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FPT.EXE201.Api.Hubs;

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Try getting ID from the "sub" claim
        var sub = connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(sub)) 
        {
            return sub;
        }

        // Fallback to custom "userId" claim
        var userId = connection.User?.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(userId)) 
        {
            return userId;
        }

        // Last fallback to default NameIdentifier
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
