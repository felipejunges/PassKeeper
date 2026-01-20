using Gtk;
using PassKeeper.Gtk.Interfaces.Services;

namespace PassKeeper.Gtk.Services;

public class ClipboardService : IClipboardService
{
    private readonly Clipboard _clipboard;

    public ClipboardService()
    {
        _clipboard = Clipboard.Get(Gdk.Selection.Clipboard);
    }

    public void SetText(string text)
    {
        _clipboard.Text = text;
    }

    public void Clear()
    {
        _clipboard.Clear();
    }

    public void SetGenericTextTemporary(string text, TimeSpan duration)
    {
        _clipboard.Text = text;

        GLib.Timeout.Add((uint)duration.TotalMilliseconds, () =>
        {
            // We check if the clipboard content is still what we set it to be.
            // If the user copied something else in the meantime, we don't want to clear it.
            // Note: WaitForText() can be blocking/slow in some cases, but for local copy it's usually fine.
            // However, a simple Clear() if match is safer.

            // Getting text might be tricky if clipboard changed ownership.
            // But let's try to be safe.

            string? currentText = _clipboard.WaitForText();
            if (currentText == text)
            {
                Clear();
            }

            return false; // Don't repeat
        });
    }
}
