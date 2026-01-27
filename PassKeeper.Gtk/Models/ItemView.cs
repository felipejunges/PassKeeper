namespace PassKeeper.Gtk.Models;

public record ItemView
{
    public required Guid Id { get; init; }
    public required string Title { get; set; }
    public string? Group { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? OriginalPassword { get; set; }
    public string? OtherInfo { get; set; }
    public DateTime? SoftDeletedIn { get; set; }
    public DateTime? ModifiedAt { get; set; }
    
    public bool PasswordChanged =>  Password != OriginalPassword;
}