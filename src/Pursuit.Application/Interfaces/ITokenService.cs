using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);

    string GenerateRefreshToken();

    string HashToken(string token);
}