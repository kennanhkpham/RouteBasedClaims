using System.Security.Cryptography;
using System.Text;

namespace ClientsService.Services;

public static class PolicyNumberKey
{
    public static string Create(string policyNumber)
    {
        var normalized = policyNumber.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }
}
