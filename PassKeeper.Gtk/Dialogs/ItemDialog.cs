using Gtk;
using PassKeeper.Gtk.Models;

namespace PassKeeper.Gtk.Dialogs;

public class ItemDialog : Dialog
{
    private ItemView? Item { get; set; }

    private readonly Entry _titleEntry = new Entry();
    private readonly Entry _groupEntry = new Entry();
    private readonly Entry _usernameEntry = new Entry();
    private readonly Entry _emailEntry = new Entry();
    private readonly Entry _passwordEntry = new Entry();
    private readonly Button _togglePasswordButton = new Button();
    private readonly Entry _otherInfoEntry = new Entry();

    public ItemDialog(Window parent, string title, ItemView? item = null) : base(title, parent, DialogFlags.Modal)
    {
        Item = item;

        SetDefaultSize(400, 200);
        Box content = new Box(Orientation.Vertical, 6);

        content.Margin = 8;
        content.PackStart(new Label("Title:"), false, false, 2);
        content.PackStart(_titleEntry, false, false, 2);
        content.PackStart(new Label("Group:"), false, false, 2);
        content.PackStart(_groupEntry, false, false, 2);
        content.PackStart(new Label("Username:"), false, false, 2);
        content.PackStart(_usernameEntry, false, false, 2);
        content.PackStart(new Label("Email:"), false, false, 2);
        content.PackStart(_emailEntry, false, false, 2);
        content.PackStart(new Label("Password:"), false, false, 2);

        // Create a horizontal box for password entry and toggle button
        var passwordBox = new Box(Orientation.Horizontal, 2);
        passwordBox.PackStart(_passwordEntry, true, true, 0);
        
        _passwordEntry.Visibility = false;
        _togglePasswordButton.Label = "👁";
        _togglePasswordButton.TooltipText = "Show/Hide Password";
        _togglePasswordButton.WidthRequest = 32;
        _togglePasswordButton.Clicked += (_, _) =>
        {
            _passwordEntry.Visibility = !_passwordEntry.Visibility;
            _togglePasswordButton.Label = _passwordEntry.Visibility ? "🙈" : "👁";
        };
        passwordBox.PackStart(_togglePasswordButton, false, false, 0);
        
        content.PackStart(passwordBox, false, false, 2);
        content.PackStart(new Label("Other info:"), false, false, 2);
        content.PackStart(_otherInfoEntry, false, false, 2);

        if (item != null)
        {
            _titleEntry.Text = item.Title;
            _groupEntry.Text = item.Group ?? string.Empty;
            _usernameEntry.Text = item.Username ?? string.Empty;
            _emailEntry.Text = item.Email ?? string.Empty;
            _passwordEntry.Text = item.Password ?? string.Empty;
            _otherInfoEntry.Text = item.OtherInfo ?? string.Empty;
        }

        ContentArea.PackStart(content, true, true, 0);
        AddButton("Cancel", ResponseType.Cancel);
        AddButton("OK", ResponseType.Ok);
        DefaultResponse = ResponseType.Ok;
    }

    public ItemView UpdateItem()
    {
        if (Item == null)
        {
            Item = new ItemView()
            {
                Id = Guid.CreateVersion7(),
                Title = _titleEntry.Text,
                Group = _groupEntry.Text,
                Username = _usernameEntry.Text,
                Email = _emailEntry.Text,
                Password = _passwordEntry.Text,
                OtherInfo = _otherInfoEntry.Text
            };

            return Item;
        }

        Item.Title = _titleEntry.Text;
        Item.Group = _groupEntry.Text;
        Item.Username = _usernameEntry.Text;
        Item.Email = _emailEntry.Text;
        Item.Password = _passwordEntry.Text;
        Item.OtherInfo = _otherInfoEntry.Text;

        return Item;
    }
    
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(_titleEntry.Text))
        {
            GenericDialogs.ShowErrorDialog(this, "Title is required.");
            
            return false;
        }
        
        return true;
    }
}