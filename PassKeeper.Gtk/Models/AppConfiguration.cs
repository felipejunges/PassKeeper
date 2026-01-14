namespace PassKeeper.Gtk.Models;

public class AppConfiguration
{
    public string Key { get; set; }
    public object Value { get; set; }
    
    public AppConfiguration(string key, object value)
    {
        Key = key;
        Value = value;
    }
}