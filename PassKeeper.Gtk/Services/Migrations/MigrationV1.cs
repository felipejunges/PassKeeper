using LiteDB;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Services.Migrations;

public class MigrationV1 : IDbMigration
{
    public int TargetVersion => 1;
    
    public void Apply(LiteDatabase db)
    {
        var items = db.GetCollection<Item>("items");
        items.EnsureIndex<string>(x => x.Title);
    }
}