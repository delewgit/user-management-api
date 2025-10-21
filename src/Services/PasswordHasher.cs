using System;
using System.Security.Cryptography;

namespace UserManagementApi.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashed);
}
public class PasswordHasher : IPasswordHasher
{
    // Format: {iterations}.{saltBase64}.{hashBase64}
    private const int Iterations = 10000;
    private const int SaltSize = 16; // 128 bit
    private const int SubkeySize = 32; // 256 bit

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, SubkeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string hashed)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (string.IsNullOrWhiteSpace(hashed)) return false;

        var parts = hashed.Split('.');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expectedSubkey = Convert.FromBase64String(parts[2]);
        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedSubkey.Length);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}