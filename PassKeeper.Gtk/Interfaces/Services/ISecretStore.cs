namespace PassKeeper.Gtk.Interfaces.Services;

public interface ISecretStore
{
    void SaveSecret(string key, char[] secret);
    char[]? GetSecret(string key);
    void DeleteSecret(string key);
    void Dispose();
}