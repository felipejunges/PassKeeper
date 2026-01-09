using LiteDB;
using PassKeeper.Gtk.Interfaces.Services;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Services;

public class DataStore : IDataStore, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Item> _itens;
    private readonly ILiteCollection<ItemPassword> _passwords;
    private bool _disposed;
    
    public string FullDbPath { get; }

    private readonly ISecretStore _secretStore;

    public DataStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;

        var password = new string(_secretStore.GetSecret("SEC"));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A non-empty password is required to encrypt the database.", nameof(password));

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GtkSharpApp", "items.db");
        var dir = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(dir);

        FullDbPath = path;

        var conn = new ConnectionString
        {
            Filename = path,
            Password = password
        };

        _db = new LiteDatabase(conn);

        _itens = _db.GetCollection<Item>("items");
        _passwords = _db.GetCollection<ItemPassword>("passwords");

        _itens.EnsureIndex<string>(x => x.Title);
    }

    public IEnumerable<ItemView> GetAll() => _itens.FindAll().Select(MapToItemView);

    public IEnumerable<ItemView> Get(string? filter)
    {
        var itens = string.IsNullOrWhiteSpace(filter)
            ? _itens.FindAll()
            : _itens.Find(i => i.Title.Contains(filter));

        return itens.Select(MapToItemView);
    }

    public ItemView? GetById(Guid id) => MapToItemView(_itens.FindById(id));

    public Guid Add(ItemView itemView)
    {
        var item = MapToItem(itemView);

        _itens.Insert(item);

        SavePassword(itemView);

        return item.Id;
    }

    public void Update(ItemView itemView)
    {
        var item = MapToItem(itemView);

        _itens.Update(item);

        SavePassword(itemView);
    }

    public void Delete(Guid id)
    {
        _itens.Delete(id);
        _passwords.Delete(id);
    }

    public long Count() => _itens.LongCount();

    private void SavePassword(ItemView itemView)
    {
        if (string.IsNullOrWhiteSpace(itemView.Password))
            return;

        // TODO: transformar AesEncryption em uma interface e receber instância via injeção de dependência para ser testável
        var itemPassword = new ItemPassword
        {
            Id = itemView.Id,
            Password = AesEncryption.Encrypt(itemView.Password, _secretStore.GetSecret("SEC"))
        };

        _passwords.Upsert(itemPassword);
    }

    public string GetPassword(Guid id)
    {
        var item = _passwords.FindById(id);
        if (item?.Password is null)
            return string.Empty;

        // TODO: transformar AesEncryption em uma interface e receber instância via injeção de dependência para ser testável
        return AesEncryption.Decrypt(item.Password, _secretStore.GetSecret("SEC"));
    }

    private ItemView MapToItemView(Item item)
    {
        var itemView = new ItemView
        {
            Id = item.Id,
            Title = item.Title,
            Username = item.Username,
            Email = item.Email,
            OtherInfo = item.OtherInfo
        };

        return itemView;
    }

    private Item MapToItem(ItemView itemView)
    {
        var item = new Item
        {
            Id = itemView.Id,
            Title = itemView.Title,
            Username = itemView.Username,
            Email = itemView.Email,
            OtherInfo = itemView.OtherInfo,
        };

        return item;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _db.Dispose();
        _secretStore.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}