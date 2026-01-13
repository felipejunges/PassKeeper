using Gtk;
using PassKeeper.Gtk.Constants;
using PassKeeper.Gtk.Dialogs;
using PassKeeper.Gtk.Interfaces.Services;
using PassKeeper.Gtk.Services;

namespace PassKeeper.Gtk;

public class MainWindow : Window
{
    private static readonly ISecretStore SecretStore = new SecretStore("PassKeeper.Gtk");
    
    private static TreeView? _treeView;
    private static ListStore? _listStore;
    private static IDataStore? _dataStore;

    private readonly string _defaultTitle;
    private void SetDbConnectionTitle(string fileName) => Title = $"{_defaultTitle} - {fileName}";
    
    public MainWindow(string title) : base(title)
    {
        _defaultTitle = title;
        
        SetDefaultSize(700, 400);
        SetPosition(WindowPosition.Center);

        DeleteEvent += OnWindowDeleteEvent;
        Destroyed += OnWindowDestroyed;
        Shown += OnWindowShown;

        var vbox = new Box(Orientation.Vertical, 2);

        var menuBar = CreateWindowMenuBar();

        vbox.PackStart(menuBar, false, false, 0);

        // Filter input (place above the TreeView)
        var filterEntry = new Entry { PlaceholderText = "Type filter and press Enter" };
        filterEntry.ActivatesDefault = true;
        filterEntry.MarginTop = 1;
        filterEntry.MarginBottom = 1;
        filterEntry.MarginStart = 4;
        filterEntry.MarginEnd = 4;

        filterEntry.Activated += OnFilterEntryActivated;
        
        vbox.PackStart(filterEntry, false, false, 2);

        // TreeView setup
        _treeView = new TreeView();
        _listStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
        _treeView.Model = _listStore;
        _treeView.AppendColumn("ID", new CellRendererText(), "text", 0);
        _treeView.AppendColumn("Title", new CellRendererText(), "text", 1);
        _treeView.AppendColumn("Username", new CellRendererText(), "text", 2);
        _treeView.AppendColumn("Email", new CellRendererText(), "text", 3);

        // I.A. sugeriu por conta do problema de não interceptar clique direito
        _treeView.AddEvents((int)Gdk.EventMask.ButtonPressMask);

        _treeView.ButtonPressEvent += OnTreeViewButtonPressEvent;

        ScrolledWindow scrolledWindow = new ScrolledWindow();
        scrolledWindow.Add(_treeView);
        vbox.PackStart(scrolledWindow, true, true, 0);

        // Buttons
        Box buttonBox = new Box(Orientation.Horizontal, 2);
        Button addButton = new Button("Add");
        Button editButton = new Button("Edit");
        Button deleteButton = new Button("Delete");
        buttonBox.PackStart(addButton, false, false, 2);
        buttonBox.PackStart(editButton, false, false, 2);
        buttonBox.PackStart(deleteButton, false, false, 2);
        vbox.PackStart(buttonBox, false, false, 2);

        Add(vbox);

        addButton.Clicked += OnAddButtonClicked;
        editButton.Clicked += OnEditButtonClicked;
        deleteButton.Clicked += OnDeleteButtonClicked;
    }

    private MenuBar CreateWindowMenuBar()
    {
        var menuBar = new MenuBar();
        var fileMenuItem = new MenuItem("File");
        var fileMenu = new Menu();
        var changeDbPasswordItem = new MenuItem("Change DB password");
        var exitItem = new MenuItem("Exit");

        fileMenu.Append(changeDbPasswordItem);
        fileMenu.Append(new SeparatorMenuItem());
        fileMenu.Append(exitItem);
        fileMenuItem.Submenu = fileMenu;
        menuBar.Append(fileMenuItem);
        
        changeDbPasswordItem.Activated += OnChangeDbPasswordActivated;
        exitItem.Activated += OnExitItemActivated;
        
        return menuBar;
    }

    private void OnAddButtonClicked(object? o, EventArgs eventArgs)
    {
        var dialog = new ItemDialog(this, "Add Item");
        
        dialog.Response += (_, args) =>
        {
            if (args.ResponseId == ResponseType.Ok)
            {
                if (!dialog.Validate())
                {
                    args.RetVal = true;
                    return;
                }

                var item = dialog.UpdateItem();

                _dataStore?.Add(item);
                _listStore?.AppendValues(item.Id.ToString(), item.Title, item.Username, item.Email);
            }

            dialog.Destroy();
        };
        
        dialog.ShowAll();
    }
    
    private void OnEditButtonClicked(object? o, EventArgs eventArgs)
    {
        if (_treeView is null || _listStore is null) return;
        
        if (_treeView.Selection.GetSelected(out TreeIter iter))
        {
            var idStr = (string)_listStore.GetValue(iter, 0);
            var id = Guid.Parse(idStr);
            var item = _dataStore?.GetById(id);
            
            if (item == null) return;
            
            var dialog = new ItemDialog(this, "Edit Item", item);
                
            dialog.Response += (_, args) =>
            {
                if (args.ResponseId == ResponseType.Ok)
                {
                    if (!dialog.Validate())
                    {
                        args.RetVal = true;
                        return;
                    }

                    // TODO: melhorar, nao deveria ser o Dialog o responsável por atualizar o item
                    dialog.UpdateItem();

                    _dataStore?.Update(item);
                    _listStore.SetValues(iter, item.Id.ToString(), item.Title, item.Username, item.Email);
                }

                dialog.Destroy();
            };
                
            dialog.ShowAll();
        }
    }
    
    private void OnDeleteButtonClicked(object? o, EventArgs eventArgs)
    {
        if (_treeView is null || _listStore is null) return;
        
        if (_treeView.Selection.GetSelected(out TreeIter iter))
        {
            var confirm = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "Are you sure you want to delete the item?");
            int resp = confirm.Run();
            confirm.Destroy();
            if (resp != (int)ResponseType.Yes) return;

            var idStr = (string)_listStore.GetValue(iter, 0);
            var id = Guid.Parse(idStr);
            _dataStore?.Delete(id);
            _listStore.Remove(ref iter);
        }
    }

    private void OnTreeViewButtonPressEvent(object o, ButtonPressEventArgs args)
    {
        var treeView = o as TreeView;
        if (treeView == null) return;
        
        if (args.Event.Button == 3) // Right mouse button
        {
            var menu = new Menu();

            var copyPasswordMenuItem = new MenuItem("Copy password");
            copyPasswordMenuItem.Activated += (_, _) =>
            {
                if (treeView.Selection.GetSelected(out TreeIter iter))
                {
                    if (_listStore == null) return;
                    
                    var idStr = (string)_listStore.GetValue(iter, 0);
                    var id = Guid.Parse(idStr);

                    // TODO: não obter em string, mas talvez em char[]
                    var senha = _dataStore?.GetPassword(id);

                    var clipboard = Clipboard.Get(Gdk.Selection.Clipboard);
                    clipboard.Text = senha;

                    // Clear clipboard after 30 seconds
                    // TODO: melhorar, não parece estar funcionando corretamente
                    GLib.Timeout.Add(30000, () =>
                    {
                        if (clipboard.WaitForText() == senha)
                        {
                            clipboard.Text = string.Empty;
                        }
                        
                        return false; // Do not repeat
                    });
                }
            };

            menu.Append(copyPasswordMenuItem);

            menu.ShowAll();
            menu.Popup();
        }
    }
    
    private void OnChangeDbPasswordActivated(object? sender, EventArgs e)
    {
        // TODO: ask the current password and validate it before changing to a new one

        if (_dataStore == null) return;
        
        var keyDialog = new InputDialog(this, "Enter the new key:", initial: "", isPassword: true);
        if (keyDialog.Run() == (int)ResponseType.Ok)
        {
            string value = keyDialog.Text;
            
            _dataStore.ChangeDbPassword(value);

            SecretStore.SaveSecret(SecretStoreConsts.DbPasswordKey, value.ToCharArray());
            
            OpenNewDbConnection(value);
            GetItems();
        }

        keyDialog.Destroy();
    }

    private void OnExitItemActivated(object? o, EventArgs eventArgs)
    {
        var confirm = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "Are you sure you want to exit?");
        int resp = confirm.Run();
        confirm.Destroy();
        if (resp == (int)ResponseType.Yes) Application.Quit();
    }

    private void OnWindowDestroyed(object? o, EventArgs eventArgs)
    {
        _dataStore?.Dispose();
        SecretStore.Dispose();
    }

    private static void OnWindowDeleteEvent(object o, DeleteEventArgs args)
    {
        Application.Quit();
    }

    private void OnWindowShown(object? sender, EventArgs e)
    {
        var keyDialog = new InputDialog(this, "Enter the key:", initial: "", isPassword: true);
        if (keyDialog.Run() == (int)ResponseType.Ok)
        {
            string value = keyDialog.Text;

            OpenNewDbConnection(value);
            GetItems();
        }

        keyDialog.Destroy();
    }

    private void OpenNewDbConnection(string password)
    {
        if (_dataStore != null)
        {
            _dataStore.Dispose();
            _dataStore = null;
        }

        SecretStore.SaveSecret(SecretStoreConsts.DbPasswordKey, password.ToCharArray());

        _dataStore = new DataStore(SecretStore);

        SetDbConnectionTitle(_dataStore.FullDbPath);
    }

    private void OnFilterEntryActivated(object? o, EventArgs eventArgs)
    {
        var filterEntry = o as Entry;
        if (filterEntry == null) return;
        
        var filter = string.IsNullOrWhiteSpace(filterEntry.Text) ? null : filterEntry.Text;
        GetItems(filter);
    }

    private static void GetItems(string? filter = null)
    {
        _listStore?.Clear();

        if (_dataStore is null) return;
        
        var itens = _dataStore.Get(filter);

        foreach (var item in itens)
        {
            _listStore?.AppendValues(item.Id.ToString(), item.Title, item.Username, item.Email);
        }
    }
}
