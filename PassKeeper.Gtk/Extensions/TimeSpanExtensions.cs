namespace PassKeeper.Gtk.Extensions;

public static class TimeSpanExtensions
{
    public static string ToDiasHoras(this TimeSpan time)
    {
        if (time.Days > 0)
            return $"{time.Days}d{time.Hours}h";
        
        if (time.Hours > 0)
            return $"{time.Hours}h{time.Minutes}m";
        
        return $"{time.Minutes}m";
    }
}