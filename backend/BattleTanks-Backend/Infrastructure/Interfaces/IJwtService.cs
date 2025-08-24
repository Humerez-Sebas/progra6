using Domain.Entities;
using System.Security.Claims;

namespace Infrastructure.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    DateTime GetTokenExpiration(string token);
    bool ValidateToken(string token);
}