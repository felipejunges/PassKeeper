namespace PassKeeper.Gtk.Interfaces.Services;

public interface IClipboardService
{
    void SetText(string text);
    void Clear();
    void SetGenericTextTemporary(string text, TimeSpan duration);
}
