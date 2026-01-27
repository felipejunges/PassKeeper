using LiteDB;

namespace PassKeeper.Gtk.Services.Migrations;

public interface IDbMigration
{
    int TargetVersion { get; }
    void Apply(LiteDatabase db);
}