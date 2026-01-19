namespace PassKeeper.Gtk.Models;

public class Item
{
    public required Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Group { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public byte[]? Password { get; set; }
    public string? OtherInfo { get; set; }
    public DateTime? SoftDeletedIn { get; set; }
}