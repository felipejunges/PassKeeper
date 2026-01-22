using LiteDB;
using LiteDB.Engine;
using PassKeeper.Gtk.Interfaces.Services;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Services;

public class DataStore : IDataStore, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Item> _itens;
    private readonly ILiteCollection<AppConfiguration> _configuration;
    private bool _disposed;

    public static TimeSpan TimeToHardDelete = TimeSpan.FromDays(30);
    
    public string FullDbPath { get; }

    public DataStore(string? password, bool debug)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A non-empty password is required to encrypt the database.", nameof(password));

        var path = CreateDbPath(debug);

        var dir = Path.GetDirectoryName(path) ?? ".";
        Directory.CreateDirectory(dir);

        FullDbPath = path;

        var conn = new ConnectionString
        {
            Filename = FullDbPath,
            Password = password
        };
        
        try
        {
            _db = new LiteDatabase(conn);
            _db.UserVersion = 1;
            
            _itens = _db.GetCollection<Item>("items");
            _configuration = _db.GetCollection<AppConfiguration>("configuration");
            
            _itens.EnsureIndex<string>(x => x.Title);
        }
        catch (LiteException ex) when (ex.Message.Contains("Invalid password"))
        {
            throw new UnauthorizedAccessException("Invalid database password.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Failed to access database file at '{FullDbPath}'.", ex);
        }
    }

    private static string CreateDbPath(bool debug)
    {
        var paths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PassKeeper", 
            "items.db"
        };
        
        if (debug)
            paths.Insert(2, "debug");
        
        return Path.Combine(paths.ToArray());
    }

    public void ChangeDbPassword(string newPassword)
    {
        try
        {
            _db.Rebuild(new RebuildOptions
            {
                Password = newPassword
            });
        }
        catch (LiteException ex)
        {
            if (!ex.Message.Contains("Invalid password"))
                throw;
        }
        finally
        {
            _db.Dispose();
        }
    }

    public IEnumerable<ItemView> Get(string? filter, bool filterDeleted)
    {
        var itens = _itens.Find(i => 
            (
                string.IsNullOrEmpty(filter)
                || i.Title.Contains(filter)
                || (i.Group != null && i.Group.Contains(filter)))
            && (filterDeleted || i.SoftDeletedIn == null));

        return itens.Select(MapToItemView);
    }

    public ItemView? GetById(Guid id)
    {
        var item = _itens.FindById(id);

        return item == null ? null : MapToItemViewWithPassword(item);
    }

    public Guid Add(ItemView itemView)
    {
        var item = MapToItem(itemView);

        _itens.Insert(item);

        return item.Id;
    }

    public void Update(ItemView itemView)
    {
        var item = MapToItem(itemView);

        _itens.Update(item);
    }

    public void SoftDelete(Guid id)
    {
        var item = _itens.FindById(id);

        if (item is null) return;

        if (item.SoftDeletedIn.HasValue)
            item.SoftDeletedIn = null;
        else
            item.SoftDeletedIn = DateTime.Now;

        _itens.Update(item);
    }

    public void HardDeleteOlds()
    {
        var dataLimite = DateTime.Now.Subtract(TimeToHardDelete);
        
        var itens = _itens.Find(i => 
            i.SoftDeletedIn != null
            && i.SoftDeletedIn < dataLimite);

        foreach (var item in itens)
        {
            _itens.Delete(item.Id);
        }
    }

    public long Count() => _itens.LongCount();

    public object GetDbConfiguration(string key, object? defaultValue = null)
    {
        var value = _configuration.FindOne(c => c.Key == key);

        if (value is null && defaultValue is not null)
        {
            value = new AppConfiguration(key, defaultValue);
            _configuration.Upsert(value);
        }
        
        return value?.Value ?? throw new KeyNotFoundException($"Configuration key '{key}' not found.");
    }

    private char[] GetPasswordsKey()
    {
        var randomNewKey = Guid.NewGuid().ToByteArray();
        var bytes = GetDbConfiguration("PasswordsKey", randomNewKey) as byte[];
        var key = System.Text.Encoding.UTF8.GetChars(bytes!);
        
        return key;
    }

    private ItemView MapToItemView(Item item) => MapToItemView(item, false);
    
    private ItemView MapToItemViewWithPassword(Item item) => MapToItemView(item, true);
    
    private ItemView MapToItemView(Item item, bool decryptPass)
    {
        var itemView = new ItemView
        {
            Id = item.Id,
            Title = item.Title,
            Group = item.Group,
            Username = item.Username,
            Email = item.Email,
            OtherInfo = item.OtherInfo,
            Password = !decryptPass || item.Password == null ? null : AesEncryption.Decrypt(item.Password, GetPasswordsKey()),
            SoftDeletedIn = item.SoftDeletedIn
        };

        return itemView;
    }

    private Item MapToItem(ItemView itemView)
    {
        var item = new Item
        {
            Id = itemView.Id,
            Title = itemView.Title,
            Group = itemView.Group,
            Username = itemView.Username,
            Email = itemView.Email,
            OtherInfo = itemView.OtherInfo,
            Password = itemView.Password == null ? null : AesEncryption.Encrypt(itemView.Password, GetPasswordsKey()),
            SoftDeletedIn = itemView.SoftDeletedIn
        };

        return item;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _db.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}