using System.Security.Cryptography;
using System.Text;

namespace Tasker.Domain.Services;

public static class DomainEncryptionService
{
    public static string Encrypt(string plainText, Guid userId)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var key = DeriveUserKey(userId);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch
        {
            return plainText;
        }
    }

    public static string Decrypt(string encryptedText, Guid userId)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            var encryptedData = Convert.FromBase64String(encryptedText);

            if (encryptedData.Length < 16)
                return encryptedText;

            var key = DeriveUserKey(userId);
            using var aes = Aes.Create();
            aes.Key = key;

            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            var encryptedBytes = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, encryptedBytes, 0, encryptedBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return encryptedText;
        }
    }

    private static byte[] DeriveUserKey(Guid userId)
    {
        var userIdBytes = userId.ToByteArray();
        var keyMaterial = Encoding.UTF8.GetBytes($"TaskerUser:{userId}");

        var combinedKeyMaterial = new byte[keyMaterial.Length + userIdBytes.Length];
        Array.Copy(keyMaterial, 0, combinedKeyMaterial, 0, keyMaterial.Length);
        Array.Copy(userIdBytes, 0, combinedKeyMaterial, keyMaterial.Length, userIdBytes.Length);

        using var sha256 = SHA256.Create();
        var salt = sha256.ComputeHash(Encoding.UTF8.GetBytes($"TaskerUserSalt:{userId}"));

        using var pbkdf2 = new Rfc2898DeriveBytes(combinedKeyMaterial, salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}
