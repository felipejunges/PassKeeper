using Gtk;
using PassKeeper.Gtk.Dialogs;
using PassKeeper.Gtk.Extensions;
using PassKeeper.Gtk.Interfaces.Services;
using PassKeeper.Gtk.Services;

namespace PassKeeper.Gtk;

public class MainWindow : Window
{
    private TreeView? _treeView;
    private ListStore? _listStore;
    private IDataStore? _dataStore;

    private readonly string _defaultTitle;
    private void SetDbConnectionTitle(string fileName) => Title = $"{_defaultTitle} - {fileName}";

    private string? _textFilter;
    private bool _filterDeletedItems;
    
    public MainWindow(string title) : base(title)
    {
        _defaultTitle = title;
        
        SetDefaultSize(1200, 600);
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
        _listStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
        _treeView.Model = _listStore;
        _treeView.AppendColumn(NewTextColumn("ID", 0));
        _treeView.AppendColumn(NewTextColumn("Group", 1));
        _treeView.AppendColumn(NewTextColumn("Title", 2));
        _treeView.AppendColumn(NewTextColumn("Username", 3));
        _treeView.AppendColumn(NewTextColumn("Email", 4));
        _treeView.AppendColumn(NewTextColumn("DEL?", 5));

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

    private static TreeViewColumn NewTextColumn(string title, int index)
    {
        var column = new TreeViewColumn(title, new CellRendererText(), "text", index);
        column.Resizable = true;
        return column;
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

        var optionsMenuItem = new MenuItem("Options");
        var optionsMenu = new Menu();
        var filterDeletedItemsItem = new CheckMenuItem("Filter deleted items");

        optionsMenu.Append(filterDeletedItemsItem);
        optionsMenuItem.Submenu = optionsMenu;
        menuBar.Append(optionsMenuItem);
        
        changeDbPasswordItem.Activated += OnChangeDbPasswordActivated;
        exitItem.Activated += OnExitItemActivated;
        filterDeletedItemsItem.Activated += (_, _) =>
        {
            _filterDeletedItems = filterDeletedItemsItem.Active;
            GetItems();
        };

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
                
                GetItems();
                SelectItemOnTreeView(item.Id.ToString());
            }

            dialog.Dispose();
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
                    
                    GetItems();
                    SelectItemOnTreeView(idStr);
                }

                dialog.Dispose();
            };
                
            dialog.ShowAll();
        }
    }
    
    private void OnDeleteButtonClicked(object? o, EventArgs eventArgs)
    {
        if (_treeView is null || _listStore is null) return;
        
        if (_treeView.Selection.GetSelected(out TreeIter iter))
        {
            var confirmacao = GenericDialogs.ShowConfirmDialog(this, "Are you sure you want to delete the item?");
            if (!confirmacao) return;
            
            var idStr = (string)_listStore.GetValue(iter, 0);
            var id = Guid.Parse(idStr);
            _dataStore?.SoftDelete(id);
            
            GetItems();
            SelectItemOnTreeView(idStr);
        }
    }

    private readonly IClipboardService _clipboardService = new ClipboardService();

    private void OnTreeViewButtonPressEvent(object o, ButtonPressEventArgs args)
    {
        if (o is not TreeView treeView) return;
        
        if (args.Event.Button == 3) // Right mouse button
        {
            var menu = new Menu();

            var copyPasswordMenuItem = new MenuItem("Copy password");
            copyPasswordMenuItem.Activated += (_, _) =>
            {
                if (treeView.Selection.GetSelected(out TreeIter iter))
                {
                    if (_listStore is null || _dataStore is null) return;
                    
                    var idStr = (string)_listStore.GetValue(iter, 0);
                    var id = Guid.Parse(idStr);

                    var item = _dataStore.GetById(id);
                    if (item == null) return;
                    
                    if (string.IsNullOrEmpty(item.Password))
                        return;

                    // Use the centralized clipboard service
                    _clipboardService.SetGenericTextTemporary(item.Password, TimeSpan.FromSeconds(30));
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
        
        // TODO: ask to confirm the new password

        if (_dataStore is null) return;
        
        var keyResponse = GenericDialogs.ShowInputDialog(this, "Enter the new key:", true);
        if (keyResponse.Item1)
        {
            var value = keyResponse.Item2;
            
            _dataStore.ChangeDbPassword(value);

            OpenNewDbConnection(value);
            GetItems();
        }
    }

    private void OnExitItemActivated(object? o, EventArgs eventArgs)
    {
        if (GenericDialogs.ShowConfirmDialog(this, "Are you sure you want to exit?"))
        {
            Application.Quit();
        }
    }

    private void OnWindowDestroyed(object? o, EventArgs eventArgs)
    {
        _treeView?.Dispose();
        _listStore?.Dispose();
        _dataStore?.Dispose();
    }

    private static void OnWindowDeleteEvent(object o, DeleteEventArgs args)
    {
        Application.Quit();
    }

    private void OnWindowShown(object? sender, EventArgs e)
    {
        var keyResponse = GenericDialogs.ShowInputDialog(this, "Enter the key:", true);
        if (keyResponse.Item1)
        {
            string value = keyResponse.Item2;

            OpenNewDbConnection(value);
            
            GetItems();
        }
    }

    private void OpenNewDbConnection(string? password)
    {
        if (_dataStore != null)
        {
            _dataStore.Dispose();
            _dataStore = null;
        }

        _dataStore = new DataStore(password, IsDebug);

        SetDbConnectionTitle(_dataStore.FullDbPath);
    }

    private void OnFilterEntryActivated(object? o, EventArgs eventArgs)
    {
        if (o is not Entry filterEntry) return;
        
        _textFilter = string.IsNullOrWhiteSpace(filterEntry.Text) ? null : filterEntry.Text;
        
        GetItems();
    }

    private void GetItems()
    {
        if (_dataStore is null || _listStore is null) return;
        
        _dataStore.HardDeleteOlds();
        var itens = _dataStore.Get(_textFilter, _filterDeletedItems);

        _listStore.Clear();
        foreach (var item in itens)
        {
            var daysToHardDelete = item.SoftDeletedIn.HasValue
                ? (item.SoftDeletedIn.Value.Add(DataStore.TimeToHardDelete) - DateTime.Now).ToDiasHoras()
                : null;
            
            _listStore.AppendValues(
                item.Id.ToString(),
                item.Group,
                item.Title,
                item.Username,
                item.Email,
                daysToHardDelete);
        }
    }
    
    private void SelectItemOnTreeView(string id)
    {
        if (_listStore is null || _treeView is null) return;
        
        if (_listStore.GetIterFirst(out TreeIter it))
        {
            do
            {
                if ((string)_listStore.GetValue(it, 0) == id)
                {
                    _treeView.Selection.SelectIter(it);
                    break;
                }
            } while (_listStore.IterNext(ref it));
        }
    }
    
    private static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
        return false;
#endif
        }
    }
}
