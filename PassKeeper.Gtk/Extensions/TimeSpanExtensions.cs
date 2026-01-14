namespace PassKeeper.Gtk.Extensions;

public static class TimeSpanExtensions
{
    public static string ToDiasHoras(this TimeSpan time)
    {
        return $"{time.Days}d{time.Hours:00}h";
    }
}