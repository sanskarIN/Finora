using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Finora.Shared;

namespace Finora.IntegrationTests;

internal static class BackupTestCipher
{
    private const int Iterations = 210_000;

    public static byte[] RewriteJson(byte[] encryptedBackup, string password, Action<JsonObject> rewrite)
    {
        ArgumentNullException.ThrowIfNull(encryptedBackup);
        ArgumentNullException.ThrowIfNull(rewrite);

        var plaintext = Decrypt(encryptedBackup, password);
        try
        {
            var root = JsonNode.Parse(plaintext)?.AsObject()
                ?? throw new InvalidDataException("Test backup JSON root is missing.");
            rewrite(root);
            var rewritten = Encoding.UTF8.GetBytes(root.ToJsonString());
            try
            {
                return Encrypt(rewritten, password);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rewritten);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static byte[] Decrypt(byte[] data, string password)
    {
        using var stream = new MemoryStream(data, writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var magicBytes = reader.ReadBytes(AppConstants.BackupMagic.Length);
        if (!magicBytes.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(AppConstants.BackupMagic)))
            throw new InvalidDataException("Test input is not a Finora backup.");

        var salt = reader.ReadBytes(16);
        var nonce = reader.ReadBytes(12);
        var tag = reader.ReadBytes(16);
        var length = reader.ReadInt32();
        if (length < 0 || stream.Length - stream.Position != length)
            throw new InvalidDataException("Test backup length is invalid.");
        var ciphertext = reader.ReadBytes(length);
        var plaintext = new byte[length];
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, Encoding.ASCII.GetBytes(AppConstants.BackupMagic));
            return plaintext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] Encrypt(byte[] plaintext, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.ASCII.GetBytes(AppConstants.BackupMagic));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes(AppConstants.BackupMagic));
        writer.Write(salt);
        writer.Write(nonce);
        writer.Write(tag);
        writer.Write(ciphertext.Length);
        writer.Write(ciphertext);
        writer.Flush();
        return stream.ToArray();
    }
}
