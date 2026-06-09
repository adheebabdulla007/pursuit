using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Pursuit.Application.Interfaces;

namespace Pursuit.Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const KeyDerivationPrf Algorithm = KeyDerivationPrf.HMACSHA256;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: Algorithm,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        byte[] combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    public bool Verify(string password, string hash)
    {
        byte[] combined = Convert.FromBase64String(hash);

        byte[] salt = new byte[SaltSize];
        byte[] expectedHash = new byte[HashSize];

        Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(combined, SaltSize, expectedHash, 0, HashSize);

        byte[] actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: Algorithm,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}