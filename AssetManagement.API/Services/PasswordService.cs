using System.Security.Cryptography;
using System.Text;
using AssetManagement.API.Interfaces;
using BCrypt.Net;

namespace AssetManagement.API.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

