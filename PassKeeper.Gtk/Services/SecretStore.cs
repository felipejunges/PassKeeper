using Microsoft.AspNetCore.DataProtection;
using PassKeeper.Gtk.Interfaces.Services;
using System.Text;

namespace PassKeeper.Gtk.Services;

public class SecretStore : ISecretStore, IDisposable
{
    private readonly IDataProtector _protector;
    private readonly Dictionary<string, byte[]> _store = new();
    private bool _disposed;

    public SecretStore(string? keyDirectory = null, string purpose = "PassKeeper.Secrets")
    {
        IDataProtectionProvider provider;
        if (!string.IsNullOrEmpty(keyDirectory))
        {
            Directory.CreateDirectory(keyDirectory);
            provider = DataProtectionProvider.Create(new DirectoryInfo(keyDirectory),
                builder => builder.SetApplicationName(purpose));
        }
        else
        {
            // Use per-user local app data as default cross-platform location
            string baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PassKeeper",
                "DataProtection-Keys");
            Directory.CreateDirectory(baseDir);
            provider = DataProtectionProvider.Create(new DirectoryInfo(baseDir),
                builder => builder.SetApplicationName(purpose));
        }

        _protector = provider.CreateProtector(purpose);
    }

    // Save a secret provided as char[]; the input char[] will be cleared.
    public void SaveSecret(string key, char[] secret)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (secret == null) throw new ArgumentNullException(nameof(secret));

        byte[] plain = null!;
        try
        {
            var encoding = Encoding.UTF8;
            plain = new byte[encoding.GetByteCount(secret, 0, secret.Length)];
            encoding.GetBytes(secret, 0, secret.Length, plain, 0);

            var protectedBytes = _protector.Protect(plain);

            lock (_store)
            {
                if (_store.TryGetValue(key, out var old))
                {
                    ClearBytes(old);
                }

                _store[key] = protectedBytes;
            }
        }
        finally
        {
            ClearBytes(plain);
            ClearChars(secret);
        }
    }

    // Retrieve the secret as char[]; caller must ClearChars the returned array when done.
    public char[]? GetSecret(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        byte[]? protectedBytes;
        lock (_store)
        {
            if (!_store.TryGetValue(key, out protectedBytes))
                return null;
        }

        byte[]? plain = null;
        try
        {
            plain = _protector.Unprotect(protectedBytes);
            var encoding = Encoding.UTF8;
            var result = new char[encoding.GetCharCount(plain, 0, plain.Length)];
            encoding.GetChars(plain, 0, plain.Length, result, 0);
            return result;
        }
        finally
        {
            ClearBytes(plain);
        }
    }

    public void DeleteSecret(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        lock (_store)
        {
            if (_store.TryGetValue(key, out var bytes))
            {
                ClearBytes(bytes);
                _store.Remove(key);
            }
        }
    }

    private static void ClearBytes(byte[]? data)
    {
        if (data == null) return;
        Array.Clear(data, 0, data.Length);
    }

    private static void ClearChars(char[]? data)
    {
        if (data == null) return;
        Array.Clear(data, 0, data.Length);
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_store)
        {
            foreach (var kv in _store.Values)
                ClearBytes(kv);
            _store.Clear();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}