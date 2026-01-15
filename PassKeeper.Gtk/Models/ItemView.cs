namespace PassKeeper.Gtk.Models;

public class ItemView
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Group { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? OtherInfo { get; set; }
    public DateTime? SoftDeletedIn { get; set; }
}