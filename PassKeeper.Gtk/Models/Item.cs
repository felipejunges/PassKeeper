namespace PassKeeper.Gtk.Models;

public class Item
{
    public required Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? OtherInfo { get; set; }
}