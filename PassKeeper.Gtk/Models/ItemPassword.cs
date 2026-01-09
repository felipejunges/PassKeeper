namespace PassKeeper.Gtk.Models;

public class ItemPassword
{
    public Guid Id { get; set; }
    public byte[] Password { get; set; } = [];
}