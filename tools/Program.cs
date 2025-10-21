// A small JWT generator that reads the secret from the first arg or JWT_SECRET env var.
// Optional second arg is a key id (kid) to include in the token header.
// Usage:
//   dotnet run --project .\tools "your-secret" [kid]
//   OR set env var JWT_SECRET and run: dotnet run --project .\tools
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

if (args.Length == 0 && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET")))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools -- <secret> [kid]");
    Console.Error.WriteLine("Or set environment variable JWT_SECRET and run without args.");
    return 1;
}

var secret = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("JWT_SECRET");
var kid = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable("JWT_KID");

// validate secret length (basic)
if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
{
    Console.Error.WriteLine("Error: secret is missing or too short (use a strong secret, >=16 chars recommended).");
    return 2;
}

var keyBytes = Encoding.UTF8.GetBytes(secret);
var key = new SymmetricSecurityKey(keyBytes);

// attach kid to the key if provided (so header will include it)
if (!string.IsNullOrWhiteSpace(kid))
{
    key.KeyId = kid;
}

var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var handler = new JwtSecurityTokenHandler();
var descriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim("sub", "1")
    }),
    Expires = DateTime.UtcNow.AddHours(1),
    SigningCredentials = creds
};

var token = handler.CreateToken(descriptor);
var tokenString = handler.WriteToken(token);

Console.WriteLine(tokenString);
return 0;