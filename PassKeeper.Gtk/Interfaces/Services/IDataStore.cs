using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Interfaces.Services;

public interface IDataStore
{
    void ChangeDbPassword(string newPassword);
    IEnumerable<ItemView> GetAll();
    IEnumerable<ItemView> Get(string? filter);
    ItemView? GetById(Guid id);
    Guid Add(ItemView itemView);
    void Update(ItemView itemView);
    void Delete(Guid id);
    long Count();
    string GetPassword(Guid id);
    void Dispose();
    string FullDbPath { get; }
}