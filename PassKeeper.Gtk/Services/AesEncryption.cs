using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace PassKeeper.Gtk.Services;

public static class AesEncryption
{
    private const int KeySizeBytes = 32; // 256 bits
    private const int IvSizeBytes = 16;  // AES block size
    private const int SaltSizeBytes = 16;
    private const int Iterations = 100_000;

    public static byte[] Encrypt(string plainText, char[]? password)
    {
        if (plainText is null) throw new ArgumentNullException(nameof(plainText));
        if (password is null) throw new ArgumentException("Password must not be empty.", nameof(password));

        var salt = new byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);

        var key = DeriveKey(password, salt, KeySizeBytes);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[IvSizeBytes];
        RandomNumberGenerator.Fill(iv);
        aes.IV = iv;

        using var ms = new MemoryStream();
        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        var cipherBytes = ms.ToArray();

        var result = new byte[salt.Length + iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, salt.Length + iv.Length, cipherBytes.Length);
        return result;
    }

    // Decrypt(salt+iv+ciphertext, password) -> plaintext
    public static string Decrypt(byte[] combined, char[]? password)
    {
        if (combined is null) throw new ArgumentNullException(nameof(combined));
        if (password is null) throw new ArgumentException("Password must not be empty.", nameof(password));

        var minLength = SaltSizeBytes + IvSizeBytes + 1;
        if (combined.Length < minLength) throw new ArgumentException("Invalid cipher data", nameof(combined));

        var salt = new byte[SaltSizeBytes];
        Buffer.BlockCopy(combined, 0, salt, 0, salt.Length);

        var iv = new byte[IvSizeBytes];
        Buffer.BlockCopy(combined, salt.Length, iv, 0, iv.Length);

        var cipherBytes = new byte[combined.Length - salt.Length - iv.Length];
        Buffer.BlockCopy(combined, salt.Length + iv.Length, cipherBytes, 0, cipherBytes.Length);

        var key = DeriveKey(password, salt, KeySizeBytes);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(cipherBytes);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static byte[] DeriveKey(char[] password, byte[] salt, int keyBytes)
    {
        // aqui a senha é convertida para um string, menos seguro que um char[]
        // TODO: validar nas próximas versões da lib ou se não existe outra forma de derivar a chave 
        
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        return KeyDerivation.Pbkdf2(
            password: Encoding.UTF8.GetString(passwordBytes),
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: keyBytes
        );
    }
}