namespace PassKeeper.Gtk.Services.Migrations;

public static class MigrationsDiscovery
{
    public static List<IDbMigration> GetListOfMigrations()
    {
        return typeof(DataStore).Assembly
            .GetTypes()
            .Where(t =>
                typeof(IDbMigration).IsAssignableFrom(t)
                && !t.IsAbstract
                && t.Namespace == "PassKeeper.Gtk.Services.Migrations"
                && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => Activator.CreateInstance(t) as IDbMigration)
            .Where(m => m != null)
            .ToList()!;
    }
}