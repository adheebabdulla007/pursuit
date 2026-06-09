using Pursuit.Domain.Entities;

namespace Pursuit.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}