using ErrorOr;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Interfaces.Services;

public interface IDataStore
{
    void ChangeDbPassword(string newPassword);
    IEnumerable<ItemView> GetAll();
    IEnumerable<ItemView> Get(string? filter, bool filterDeleted);
    ItemView? GetById(Guid id);
    Guid Add(ItemView itemView);
    void Update(ItemView itemView);
    void SoftDelete(Guid id);
    void HardDeleteOlds();
    long Count();
    object GetDbConfiguration(string key, object? defaultValue = null);
    ErrorOr<string> GetPassword(Guid id);
    void Dispose();
    string FullDbPath { get; }
}