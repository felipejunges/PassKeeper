using LiteDB;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Services.Migrations;

public class MigrationV2 : IDbMigration
{
    public int TargetVersion => 2;
    
    public void Apply(LiteDatabase db)
    {
        var now = DateTime.Now;

        var items = db.GetCollection<Item>("items");

        // carrega os ids e depois cada doc, pois ocorre problema se items da lista são modificados dentro do loop
        var ids = items.FindAll().Select(x => x.Id).ToList();
        
        foreach (var id in ids)
        {
            var doc = items.FindById(id);
            if (doc == null)
                continue;
            
            if (doc.ModifiedAt != default)
                continue;

            doc.ModifiedAt = now;
            items.Update(doc);
        }
    }
}