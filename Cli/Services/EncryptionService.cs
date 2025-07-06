using System.Security.Cryptography;
using System.Text;

namespace Tasker.Cli.Services;

public static class EncryptionService
{
    private const byte CURRENT_VERSION = 1;
    
    public static string EncryptToken(string token, DateTime expiresAt)
    {
        try
        {
            var machineId = MachineIdService.GetMachineId();
            
            // Create payload with token and expiration timestamp
            var payload = $"{token}|{expiresAt:O}"; // ISO 8601 format
            
            // Generate random salt and padding for variable structure
            var randomSalt = RandomNumberGenerator.GetBytes(32);
            var paddingLength = RandomNumberGenerator.GetInt32(16, 64); // 16-64 bytes random padding
            var padding = RandomNumberGenerator.GetBytes(paddingLength);
            
            var key = DeriveKey(machineId, randomSalt);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var encryptedBytes = encryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);
            
            // Variable structure: Version + RandomSalt + IV + PaddingLength + Padding + EncryptedData
            var dataToAuthenticate = new List<byte>();
            dataToAuthenticate.Add(CURRENT_VERSION);
            dataToAuthenticate.AddRange(randomSalt);
            dataToAuthenticate.AddRange(aes.IV);
            dataToAuthenticate.Add((byte)paddingLength);
            dataToAuthenticate.AddRange(padding);
            dataToAuthenticate.AddRange(encryptedBytes);
            
            var dataArray = dataToAuthenticate.ToArray();
            
            // Generate HMAC for authentication
            var hmacKey = DeriveHmacKey(machineId, randomSalt);
            using var hmac = new HMACSHA256(hmacKey);
            var hmacBytes = hmac.ComputeHash(dataArray);
            
            // Final result: HMAC + Variable Structure
            var result = new byte[hmacBytes.Length + dataArray.Length];
            Array.Copy(hmacBytes, 0, result, 0, hmacBytes.Length);
            Array.Copy(dataArray, 0, result, hmacBytes.Length, dataArray.Length);
            
            return Convert.ToBase64String(result);
        }
        catch
        {
            // If encryption fails, return empty string (fail securely)
            return string.Empty;
        }
    }

    public static (string token, DateTime expiresAt) DecryptToken(string encryptedToken)
    {
        try
        {
            var machineId = MachineIdService.GetMachineId();
            var encryptedData = Convert.FromBase64String(encryptedToken);
            
            // Check minimum size for new format
            if (encryptedData.Length < 100) // Minimum: HMAC(32) + Version(1) + Salt(32) + IV(16) + PaddingLen(1) + MinPadding(16) + some data
            {
                return (string.Empty, DateTime.MinValue); // Too small to be valid new format
            }
            
            // Extract HMAC (first 32 bytes)
            var receivedHmac = new byte[32];
            Array.Copy(encryptedData, 0, receivedHmac, 0, 32);
            
            // Extract the data that was authenticated (everything after HMAC)
            var authenticatedData = new byte[encryptedData.Length - 32];
            Array.Copy(encryptedData, 32, authenticatedData, 0, authenticatedData.Length);
            
            // Check minimum authenticated data size
            if (authenticatedData.Length < 50) // Version(1) + Salt(32) + IV(16) + PaddingLen(1) = 50 minimum
            {
                return (string.Empty, DateTime.MinValue);
            }
            
            // Extract version (first byte of authenticated data)
            var version = authenticatedData[0];
            if (version != CURRENT_VERSION)
            {
                return (string.Empty, DateTime.MinValue); // Unsupported version - old tokens will be cleared
            }
            
            // Extract random salt (bytes 1-32 of authenticated data)
            var randomSalt = new byte[32];
            Array.Copy(authenticatedData, 1, randomSalt, 0, 32);
            
            // Verify HMAC first
            var hmacKey = DeriveHmacKey(machineId, randomSalt);
            using var hmac = new HMACSHA256(hmacKey);
            var computedHmac = hmac.ComputeHash(authenticatedData);
            
            // Constant-time comparison to prevent timing attacks
            if (!CryptographicOperations.FixedTimeEquals(receivedHmac, computedHmac))
            {
                return (string.Empty, DateTime.MinValue); // HMAC verification failed
            }
            
            // Extract IV (bytes 33-48 of authenticated data)
            var iv = new byte[16];
            Array.Copy(authenticatedData, 33, iv, 0, 16);
            
            // Extract padding length (byte 49)
            var paddingLength = authenticatedData[49];
            
            // Check bounds for padding
            var encryptedStart = 50 + paddingLength;
            if (encryptedStart >= authenticatedData.Length)
            {
                return (string.Empty, DateTime.MinValue); // Invalid padding length
            }
            
            // Skip padding and extract encrypted bytes
            var encryptedBytes = new byte[authenticatedData.Length - encryptedStart];
            Array.Copy(authenticatedData, encryptedStart, encryptedBytes, 0, encryptedBytes.Length);
            
            // Derive key using machine ID + random salt
            var key = DeriveKey(machineId, randomSalt);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            var payload = Encoding.UTF8.GetString(decryptedBytes);
            var parts = payload.Split('|');
            
            if (parts.Length != 2)
            {
                return (string.Empty, DateTime.MinValue); // Invalid payload format
            }
            
            var token = parts[0];
            if (!DateTime.TryParse(parts[1], out var expiresAt))
            {
                return (string.Empty, DateTime.MinValue); // Invalid expiration format
            }
            
            // Check if token has expired (cryptographically enforced)
            if (expiresAt <= DateTime.UtcNow)
            {
                return (string.Empty, DateTime.MinValue); // Token expired
            }
            
            return (token, expiresAt);
        }
        catch
        {
            // If decryption fails, token is invalid or from different machine
            return (string.Empty, DateTime.MinValue);
        }
    }

    private static byte[] DeriveKey(string machineId, byte[] randomSalt)
    {
        // Combine machine ID with random salt
        var combinedInput = Encoding.UTF8.GetBytes(machineId);
        var keyMaterial = new byte[combinedInput.Length + randomSalt.Length];
        
        Array.Copy(combinedInput, 0, keyMaterial, 0, combinedInput.Length);
        Array.Copy(randomSalt, 0, keyMaterial, combinedInput.Length, randomSalt.Length);
        
        // Create separate PBKDF2 salt from machine ID (not the random salt)
        using var sha256 = SHA256.Create();
        var pbkdf2Salt = sha256.ComputeHash(Encoding.UTF8.GetBytes($"PBKDF2Salt:{machineId}"));
        
        using var pbkdf2 = new Rfc2898DeriveBytes(keyMaterial, pbkdf2Salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key for AES
    }

    private static byte[] DeriveHmacKey(string machineId, byte[] randomSalt)
    {
        // Create separate HMAC key derivation (different from encryption key)
        var combinedInput = Encoding.UTF8.GetBytes($"HMAC:{machineId}");
        var keyMaterial = new byte[combinedInput.Length + randomSalt.Length];
        
        Array.Copy(combinedInput, 0, keyMaterial, 0, combinedInput.Length);
        Array.Copy(randomSalt, 0, keyMaterial, combinedInput.Length, randomSalt.Length);
        
        using var sha256 = SHA256.Create();
        var hmacSalt = sha256.ComputeHash(Encoding.UTF8.GetBytes($"HMACSalt:{machineId}"));
        
        using var pbkdf2 = new Rfc2898DeriveBytes(keyMaterial, hmacSalt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key for HMAC
    }

    public static string EncryptConnectionString(string connectionString)
    {
        try
        {
            var machineId = MachineIdService.GetMachineId();
            var randomSalt = RandomNumberGenerator.GetBytes(32);
            
            // Use different context for connection string encryption
            var keyMaterial = Encoding.UTF8.GetBytes($"ConnectionString:{machineId}");
            var combinedKeyMaterial = new byte[keyMaterial.Length + randomSalt.Length];
            Array.Copy(keyMaterial, 0, combinedKeyMaterial, 0, keyMaterial.Length);
            Array.Copy(randomSalt, 0, combinedKeyMaterial, keyMaterial.Length, randomSalt.Length);
            
            using var sha256 = SHA256.Create();
            var pbkdf2Salt = sha256.ComputeHash(Encoding.UTF8.GetBytes($"ConnectionStringSalt:{machineId}"));
            
            using var pbkdf2 = new Rfc2898DeriveBytes(combinedKeyMaterial, pbkdf2Salt, 100000, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var dataBytes = Encoding.UTF8.GetBytes(connectionString);
            var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
            
            // Structure: RandomSalt + IV + EncryptedData
            var result = new byte[randomSalt.Length + aes.IV.Length + encryptedBytes.Length];
            Array.Copy(randomSalt, 0, result, 0, randomSalt.Length);
            Array.Copy(aes.IV, 0, result, randomSalt.Length, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, randomSalt.Length + aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string DecryptConnectionString(string encryptedConnectionString)
    {
        try
        {
            var machineId = MachineIdService.GetMachineId();
            var encryptedData = Convert.FromBase64String(encryptedConnectionString);
            
            if (encryptedData.Length < 48) // 32 salt + 16 IV minimum
            {
                return string.Empty;
            }
            
            // Extract salt (first 32 bytes)
            var randomSalt = new byte[32];
            Array.Copy(encryptedData, 0, randomSalt, 0, 32);
            
            // Extract IV (next 16 bytes)
            var iv = new byte[16];
            Array.Copy(encryptedData, 32, iv, 0, 16);
            
            // Extract encrypted data (rest)
            var encryptedBytes = new byte[encryptedData.Length - 48];
            Array.Copy(encryptedData, 48, encryptedBytes, 0, encryptedBytes.Length);
            
            // Derive key
            var keyMaterial = Encoding.UTF8.GetBytes($"ConnectionString:{machineId}");
            var combinedKeyMaterial = new byte[keyMaterial.Length + randomSalt.Length];
            Array.Copy(keyMaterial, 0, combinedKeyMaterial, 0, keyMaterial.Length);
            Array.Copy(randomSalt, 0, combinedKeyMaterial, keyMaterial.Length, randomSalt.Length);
            
            using var sha256 = SHA256.Create();
            var pbkdf2Salt = sha256.ComputeHash(Encoding.UTF8.GetBytes($"ConnectionStringSalt:{machineId}"));
            
            using var pbkdf2 = new Rfc2898DeriveBytes(combinedKeyMaterial, pbkdf2Salt, 100000, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}