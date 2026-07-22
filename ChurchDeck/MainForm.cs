using System.Drawing;

namespace ChurchDeck;

internal sealed class MainForm : Form
{
    private readonly ChurchDatabase _database;
    private readonly ComboBox _books = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown _chapter = new() { Minimum = 1, Maximum = 200, Value = 1, Width = 70 };
    private readonly NumericUpDown _verseNumber = new() { Minimum = 1, Maximum = 200, Value = 1, Width = 70 };
    private readonly TextBox _verseText = new() { Multiline = true, Height = 90, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly ListBox _verses = new() { Dock = DockStyle.Fill };
    private readonly ListBox _planner = new() { Dock = DockStyle.Fill };
    private readonly Panel _preview = new() { Dock = DockStyle.Fill, Padding = new Padding(30) };
    private readonly Label _previewText = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };
    private readonly ComboBox _plannerPlan = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TextBox _plannerPlanName = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _plannerType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TextBox _plannerTitle = new() { Dock = DockStyle.Fill };
    private readonly TextBox _plannerBody = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _plannerImagePath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _plannerSearch = new() { Dock = DockStyle.Fill };
    private readonly ListBox _plannerSearchResults = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _plannerBibleBook = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown _plannerBibleChapter = new() { Minimum = 1, Maximum = 200, Value = 1, Width = 70 };
    private readonly NumericUpDown _plannerBibleFrom = new() { Minimum = 1, Maximum = 200, Value = 1, Width = 70 };
    private readonly NumericUpDown _plannerBibleTo = new() { Minimum = 1, Maximum = 200, Value = 1, Width = 70 };
    private readonly Button _plannerLoadBible = new() { Text = "Load Range" };
    private readonly NumericUpDown _plannerFontSize = new() { Minimum = 8, Maximum = 200, Value = 48, Width = 120 };
    private readonly Panel _plannerImagePanel = new() { Dock = DockStyle.Fill };
    private readonly Panel _plannerSearchPanel = new() { Dock = DockStyle.Fill };
    private readonly Panel _plannerBiblePanel = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _operatorPlan = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ListBox _operatorItems = new() { Dock = DockStyle.Fill };
    private readonly Panel _operatorPreview = new() { Dock = DockStyle.Fill, Padding = new Padding(30) };
    private readonly Label _operatorPreviewText = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };
    private readonly Button _presentationToggle = new() { Text = "Open Presentation" };
    private readonly TextBox _songSearch = new() { Width = 220 };
    private readonly ListBox _songs = new() { Dock = DockStyle.Fill };
    private readonly TextBox _songTitle = new() { Dock = DockStyle.Fill };
    private readonly TextBox _songLyrics = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _mediaTypeFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly TextBox _mediaSearch = new() { Width = 220 };
    private readonly ListBox _mediaItems = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _mediaType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly TextBox _mediaTitle = new() { Dock = DockStyle.Fill };
    private readonly TextBox _mediaPath = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _fontSize = new() { Minimum = 12, Maximum = 200, Value = 48, Width = 120 };
    private readonly ComboBox _fontFamily = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private Color _background = Color.Black;
    private Color _fontColor = Color.White;
    private long? _selectedVerseId;
    private long? _selectedSongId;
    private long? _selectedMediaId;
    private long? _selectedPlannerPlanId;
    private long? _selectedOperatorPlanId;
    private long? _draggedPlannerItemId;
    private bool _suppressPlannerSync;
    private PresentationForm? _presentationForm;

    public MainForm(ChurchDatabase database)
    {
        _database = database;
        Text = "ChurchDeck";
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        _preview.Controls.Add(_previewText);
        _plannerType.Items.AddRange(["Heading", "Heading + Text", "Image + Text", "Song", "Bible Verse Range", "Media"]);
        _plannerType.SelectedIndex = 0;
        _mediaType.Items.AddRange(["Image", "Video", "Music"]);
        _mediaType.SelectedIndex = 0;
        _mediaTypeFilter.Items.AddRange(["All", "Image", "Video", "Music"]);
        _mediaTypeFilter.SelectedIndex = 0;
        _fontFamily.Items.AddRange(FontFamily.Families.Select(family => family.Name).OrderBy(name => name).ToArray());

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateDashboard());
        tabs.TabPages.Add(CreateBible());
        tabs.TabPages.Add(CreateSongLibrary());
        tabs.TabPages.Add(CreateMediaLibrary());
        tabs.TabPages.Add(CreateSettings());
        tabs.TabPages.Add(CreatePlanner());
        tabs.TabPages.Add(CreateOperatorConsole());
        tabs.SelectedIndex = 0;
        Controls.Add(tabs);

        FormClosed += (_, _) => _presentationForm?.Close();

        LoadSettings();
        ReloadPlans();
        ReloadBooks();
        ReloadSongs();
        ReloadMedia();
    }

    private TabPage CreateDashboard() => new("Dashboard")
    {
        Controls =
        {
            new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 16),
                Text = "Welcome to ChurchDeck\nBuild a planner, then present your service offline."
            }
        }
    };

    private TabPage CreatePlanner()
    {
        var page = new TabPage("Planner");
        var root = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 420 };
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 3, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var planPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        planPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        planPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        planPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var newPlan = new Button { Text = "New Plan" };
        newPlan.Click += (_, _) => NewPlannerPlan();
        var savePlan = new Button { Text = "Save Plan" };
        savePlan.Click += (_, _) => SavePlannerPlan();
        var deletePlan = new Button { Text = "Delete" };
        deletePlan.Click += (_, _) => DeletePlannerPlan();
        _plannerPlan.SelectedIndexChanged += (_, _) => SelectPlannerPlan();
        planPanel.Controls.Add(new Label { Text = "Plan", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 0);
        planPanel.Controls.Add(_plannerPlan, 1, 0);
        planPanel.SetColumnSpan(_plannerPlan, 2);
        planPanel.Controls.Add(new Label { Text = "Name", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 1);
        planPanel.Controls.Add(_plannerPlanName, 1, 1);
        planPanel.Controls.Add(new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Controls = { newPlan, savePlan, deletePlan } }, 2, 1);
        left.Controls.Add(planPanel, 0, 0);

        var itemHeader = new Panel { Dock = DockStyle.Fill };
        var itemButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, WrapContents = false };
        var newItem = new Button { Text = "New Item" };
        newItem.Click += (_, _) => ClearPlannerEditor();
        var deleteItem = new Button { Text = "Delete" };
        deleteItem.Click += (_, _) => DeletePlannerItem();
        var colorItem = new Button { Text = "Font Colour" };
        colorItem.Click += (_, _) => ChoosePlannerItemColor();
        itemButtons.Controls.AddRange([newItem, deleteItem, colorItem]);
        itemHeader.Controls.Add(itemButtons);
        itemHeader.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Planned items" });
        left.Controls.Add(itemHeader, 0, 2);

        _planner.AllowDrop = true;
        _planner.MouseDown += (_, e) => BeginPlannerDrag(e);
        _planner.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(typeof(PlannerItem)) == true ? DragDropEffects.Move : DragDropEffects.None;
        _planner.DragOver += (_, e) => e.Effect = e.Data?.GetDataPresent(typeof(PlannerItem)) == true ? DragDropEffects.Move : DragDropEffects.None;
        _planner.DragDrop += (_, e) => DropPlannerItem(e);
        left.Controls.Add(_planner, 0, 1);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 10,
            Padding = new Padding(14),
            AutoScroll = true
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        editor.Controls.Add(new Label { Text = "Component", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 0);
        editor.Controls.Add(_plannerType, 1, 0);
        editor.SetColumnSpan(_plannerType, 2);
        editor.Controls.Add(new Label { Text = "Title / heading", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 1);
        editor.Controls.Add(_plannerTitle, 1, 1);
        editor.SetColumnSpan(_plannerTitle, 2);
        editor.Controls.Add(new Label { Text = "Body / text", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 2);
        editor.Controls.Add(_plannerBody, 1, 2);
        editor.SetColumnSpan(_plannerBody, 2);

        _plannerImagePanel.Controls.Add(_plannerImagePath);
        var browseImage = new Button { Text = "Browse" };
        browseImage.Click += (_, _) => BrowsePlannerImagePath();
        _plannerImagePanel.Controls.Add(browseImage);
        browseImage.Dock = DockStyle.Right;
        _plannerImagePanel.Controls.SetChildIndex(browseImage, 0);
        editor.Controls.Add(new Label { Text = "Image path", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 3);
        editor.Controls.Add(_plannerImagePanel, 1, 3);
        editor.SetColumnSpan(_plannerImagePanel, 2);

        _plannerSearchPanel.Controls.Add(_plannerSearchResults);
        var searchBoxPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false };
        searchBoxPanel.Controls.AddRange([new Label { Text = "Search", AutoSize = true, Padding = new Padding(0, 8, 4, 0) }, _plannerSearch]);
        _plannerSearchPanel.Controls.Add(searchBoxPanel);
        _plannerSearch.TextChanged += (_, _) => ReloadPlannerSearchResults();
        _plannerSearchResults.SelectedIndexChanged += (_, _) => ApplyPlannerSearchSelection();
        editor.Controls.Add(new Label { Text = "Search source", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 4);
        editor.Controls.Add(_plannerSearchPanel, 1, 4);
        editor.SetColumnSpan(_plannerSearchPanel, 2);

        var bibleLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
        bibleLayout.Controls.AddRange([
            new Label { Text = "Book", AutoSize = true, Padding = new Padding(0, 8, 4, 0) },
            _plannerBibleBook,
            new Label { Text = "Chapter", AutoSize = true, Padding = new Padding(12, 8, 4, 0) },
            _plannerBibleChapter,
            new Label { Text = "From", AutoSize = true, Padding = new Padding(12, 8, 4, 0) },
            _plannerBibleFrom,
            new Label { Text = "To", AutoSize = true, Padding = new Padding(12, 8, 4, 0) },
            _plannerBibleTo,
            _plannerLoadBible
        ]);
        _plannerLoadBible.Click += (_, _) => LoadPlannerBibleRange();
        editor.Controls.Add(new Label { Text = "Bible range", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 5);
        editor.Controls.Add(_plannerBiblePanel, 1, 5);
        editor.SetColumnSpan(_plannerBiblePanel, 2);
        _plannerBiblePanel.Controls.Add(bibleLayout);

        editor.Controls.Add(new Label { Text = "Font size", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, 6);
        editor.Controls.Add(_plannerFontSize, 1, 6);
        editor.SetColumnSpan(_plannerFontSize, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var saveItem = new Button { Text = "Save Item" };
        saveItem.Click += (_, _) => SavePlannerItem();
        var clearItem = new Button { Text = "New / Clear" };
        clearItem.Click += (_, _) => ClearPlannerEditor();
        actions.Controls.AddRange([saveItem, clearItem]);
        editor.Controls.Add(actions, 1, 7);
        editor.SetColumnSpan(actions, 2);
        editor.Controls.Add(new Label { Text = "Build and save the plan here. The operator console only selects and plays saved plans.", AutoSize = true, MaximumSize = new Size(620, 0) }, 1, 8);
        editor.SetColumnSpan(editor.Controls[editor.Controls.Count - 1], 2);
        editor.Controls.Add(new Label { Text = "Drag items to reorder.", AutoSize = true, ForeColor = SystemColors.GrayText }, 1, 9);
        editor.SetColumnSpan(editor.Controls[editor.Controls.Count - 1], 2);

        left.Controls.SetChildIndex(_planner, 0);
        root.Panel1.Controls.Add(left);
        root.Panel2.Controls.Add(editor);
        page.Controls.Add(root);

        UpdatePlannerEditorMode();
        return page;
    }

    private TabPage CreateOperatorConsole()
    {
        var page = new TabPage("Operator Console");
        var root = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 390 };
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 4 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var planPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
        _operatorPlan.SelectedIndexChanged += (_, _) => SelectOperatorPlan();
        _presentationToggle.Click += (_, _) => TogglePresentationWindow();
        planPanel.Controls.AddRange([
            new Label { Text = "Plan", AutoSize = true, Padding = new Padding(0, 8, 4, 0) },
            _operatorPlan,
            _presentationToggle
        ]);
        left.Controls.Add(planPanel, 0, 0);

        _operatorItems.SelectedIndexChanged += (_, _) => LoadSelectedOperatorItem();
        left.Controls.Add(_operatorItems, 0, 1);

        var navButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var previousItem = new Button { Text = "Previous", Width = 88 };
        previousItem.Click += (_, _) => MoveOperatorSelection(-1);
        var nextItem = new Button { Text = "Next", Width = 88 };
        nextItem.Click += (_, _) => MoveOperatorSelection(1);
        navButtons.Controls.AddRange([previousItem, nextItem]);
        left.Controls.Add(navButtons, 0, 2);
        left.Controls.Add(new Label { Text = "Use the selected plan to run the presentation.", AutoSize = true, ForeColor = SystemColors.GrayText }, 0, 3);

        _operatorPreview.Controls.Add(_operatorPreviewText);
        root.Panel1.Controls.Add(left);
        root.Panel2.Controls.Add(_operatorPreview);
        page.Controls.Add(root);
        return page;
    }

    private void ReloadPlans()
    {
        var plans = _database.GetPlans();
        if (plans.Count == 0)
        {
            _database.SavePlan(null, "Default Plan");
            plans = _database.GetPlans();
        }

        var builderSelection = _selectedPlannerPlanId;
        var operatorSelection = _selectedOperatorPlanId;

        _plannerPlan.DataSource = null;
        _operatorPlan.DataSource = null;

        _plannerPlan.DataSource = plans;
        _operatorPlan.DataSource = plans;

        _plannerPlan.SelectedItem = builderSelection.HasValue ? plans.FirstOrDefault(plan => plan.Id == builderSelection.Value) : plans.FirstOrDefault();
        _operatorPlan.SelectedItem = operatorSelection.HasValue ? plans.FirstOrDefault(plan => plan.Id == operatorSelection.Value) : plans.FirstOrDefault();

        SelectPlannerPlan();
        SelectOperatorPlan();
    }

    private long GetSelectedPlannerPlanId()
    {
        if (_plannerPlan.SelectedItem is ServicePlan plan)
            return plan.Id;

        var first = (_plannerPlan.DataSource as List<ServicePlan>)?.FirstOrDefault();
        if (first is not null)
            return first.Id;

        return _database.SavePlan(null, "Default Plan");
    }

    private long GetSelectedOperatorPlanId()
    {
        if (_operatorPlan.SelectedItem is ServicePlan plan)
            return plan.Id;

        var first = (_operatorPlan.DataSource as List<ServicePlan>)?.FirstOrDefault();
        if (first is not null)
            return first.Id;

        return _database.SavePlan(null, "Default Plan");
    }

    private void SelectPlannerPlan()
    {
        if (_plannerPlan.SelectedItem is not ServicePlan plan)
        {
            _selectedPlannerPlanId = null;
            return;
        }

        _selectedPlannerPlanId = plan.Id;
        _plannerPlanName.Text = plan.Name;
        ReloadPlanner();
    }

    private void SelectOperatorPlan()
    {
        if (_operatorPlan.SelectedItem is not ServicePlan plan)
        {
            _selectedOperatorPlanId = null;
            return;
        }

        _selectedOperatorPlanId = plan.Id;
        ReloadOperatorItems();
    }

    private void NewPlannerPlan()
    {
        _selectedPlannerPlanId = null;
        _plannerPlanName.Clear();
        _planner.ClearSelected();
        _planner.DataSource = null;
        _previewText.Text = string.Empty;
    }

    private void SavePlannerPlan()
    {
        var name = _plannerPlanName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a plan name.");
            return;
        }

        var savedId = _database.SavePlan(_selectedPlannerPlanId, name);
        _selectedPlannerPlanId = savedId;
        ReloadPlans();
        _plannerPlan.SelectedItem = (_plannerPlan.DataSource as List<ServicePlan>)?.FirstOrDefault(plan => plan.Id == savedId);
        ReloadPlanner();
    }

    private void DeletePlannerPlan()
    {
        if (_selectedPlannerPlanId is null)
            return;

        if (MessageBox.Show("Delete this plan and its items?", "Delete Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _database.DeletePlan(_selectedPlannerPlanId.Value);
        _selectedPlannerPlanId = null;
        ReloadPlans();
    }

    private TabPage CreateBible()
    {
        var page = new TabPage("Bible Library");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var addBook = new Button { Text = "Add Book" };
        addBook.Click += (_, _) => AddBook();
        controls.Controls.AddRange([
            new Label { Text = "Book", AutoSize = true, Padding = new Padding(0, 7, 4, 0) },
            _books,
            new Label { Text = "Chapter", AutoSize = true, Padding = new Padding(12, 7, 4, 0) },
            _chapter,
            addBook
        ]);

        _books.SelectedIndexChanged += (_, _) => ReloadVerses();
        _chapter.ValueChanged += (_, _) => ReloadVerses();
        _verses.SelectedIndexChanged += (_, _) => SelectVerse();

        root.Controls.Add(controls, 0, 0);
        root.SetColumnSpan(controls, 2);
        root.Controls.Add(_verses, 0, 1);

        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5 };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.Controls.Add(new Label { Text = "Verse", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        editor.Controls.Add(_verseNumber, 1, 0);
        editor.Controls.Add(new Label { Text = "Text", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        editor.Controls.Add(_verseText, 1, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var save = new Button { Text = "Save Verse" };
        save.Click += (_, _) => SaveVerse();
        var delete = new Button { Text = "Delete" };
        delete.Click += (_, _) => DeleteVerse();
        var addToPlanner = new Button { Text = "Add to Planner" };
        addToPlanner.Click += (_, _) => AddVerseToPlanner();
        actions.Controls.AddRange([save, delete, addToPlanner]);
        editor.Controls.Add(actions, 1, 3);
        root.Controls.Add(editor, 1, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage CreateSongLibrary()
    {
        var page = new TabPage("Song Library");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        var newSong = new Button { Text = "New Song" };
        newSong.Click += (_, _) => ClearSongEditor();
        top.Controls.AddRange([
            new Label { Text = "Search", AutoSize = true, Padding = new Padding(0, 7, 4, 0) },
            _songSearch,
            newSong
        ]);
        _songSearch.TextChanged += (_, _) => ReloadSongs();

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 360 };
        split.Panel1.Controls.Add(_songs);
        split.Panel1.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Songs" });
        _songs.SelectedIndexChanged += (_, _) => SelectSong();

        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        editor.Controls.Add(new Label { Text = "Title", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        editor.Controls.Add(_songTitle, 1, 0);
        editor.Controls.Add(new Label { Text = "Lyrics", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        editor.Controls.Add(_songLyrics, 1, 1);
        var songActions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var saveSong = new Button { Text = "Save" };
        saveSong.Click += (_, _) => SaveSong();
        var deleteSong = new Button { Text = "Delete" };
        deleteSong.Click += (_, _) => DeleteSong();
        songActions.Controls.AddRange([saveSong, deleteSong]);
        editor.Controls.Add(songActions, 1, 2);

        split.Panel2.Controls.Add(editor);
        split.Panel2.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Add, view, edit, and delete songs for your planner." });

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage CreateMediaLibrary()
    {
        var page = new TabPage("Media Library");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        var newItem = new Button { Text = "New Item" };
        newItem.Click += (_, _) => ClearMediaEditor();
        top.Controls.AddRange([
            new Label { Text = "Search", AutoSize = true, Padding = new Padding(0, 7, 4, 0) },
            _mediaSearch,
            new Label { Text = "Type", AutoSize = true, Padding = new Padding(12, 7, 4, 0) },
            _mediaTypeFilter,
            newItem
        ]);
        _mediaSearch.TextChanged += (_, _) => ReloadMedia();
        _mediaTypeFilter.SelectedIndexChanged += (_, _) => ReloadMedia();

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 360 };
        split.Panel1.Controls.Add(_mediaItems);
        split.Panel1.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Media items" });
        _mediaItems.SelectedIndexChanged += (_, _) => SelectMedia();

        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5 };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.Controls.Add(new Label { Text = "Type", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        editor.Controls.Add(_mediaType, 1, 0);
        editor.Controls.Add(new Label { Text = "Title", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        editor.Controls.Add(_mediaTitle, 1, 1);
        editor.Controls.Add(new Label { Text = "File", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 2);
        editor.Controls.Add(_mediaPath, 1, 2);
        var browse = new Button { Text = "Browse" };
        browse.Click += (_, _) => BrowseMediaPath();
        editor.Controls.Add(browse, 2, 2);

        var mediaActions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var saveItem = new Button { Text = "Save" };
        saveItem.Click += (_, _) => SaveMediaItem();
        var deleteItem = new Button { Text = "Delete" };
        deleteItem.Click += (_, _) => DeleteMediaItem();
        mediaActions.Controls.AddRange([saveItem, deleteItem]);
        editor.Controls.Add(mediaActions, 1, 3);

        split.Panel2.Controls.Add(editor);
        split.Panel2.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "Store file references for image, video, and music assets." });

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(split, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage CreateSettings()
    {
        var page = new TabPage("Settings");
        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        var background = new Button { Text = "Presentation Background Colour", AutoSize = true };
        background.Click += (_, _) => ChooseColor(true);
        var font = new Button { Text = "Presentation Font Colour", AutoSize = true };
        font.Click += (_, _) => ChooseColor(false);
        _fontFamily.SelectedIndexChanged += (_, _) =>
        {
            if (_fontFamily.SelectedItem is string family)
            {
                _database.SetSetting("PresentationFontFamily", family);
                ApplyPreview();
            }
        };
        _fontSize.ValueChanged += (_, _) =>
        {
            _database.SetSetting("PresentationFontSize", _fontSize.Value.ToString());
            ApplyPreview();
        };

        root.Controls.AddRange([
            new Label { Text = "Global presentation settings", Font = new Font(Font.FontFamily, 14), AutoSize = true },
            background,
            font,
            new Label { Text = "Font family", AutoSize = true },
            _fontFamily,
            new Label { Text = "Font size", AutoSize = true },
            _fontSize,
            new Label
            {
                Text = "These defaults apply to the preview and presentation controls. Individual controls can use their own font colour and size.",
                AutoSize = true,
                MaximumSize = new Size(650, 0)
            }
        ]);

        page.Controls.Add(root);
        return page;
    }

    private void AddBook()
    {
        var name = Prompt.Show("Bible book name (for example, Genesis)", "Add Bible Book");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            _database.AddBook(name);
            ReloadBooks();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not add book");
        }
    }

    private void ReloadBooks()
    {
        var selected = (_books.SelectedItem as BibleBook)?.Id;
        var books = _database.GetBooks();
        _books.DataSource = books;
        _plannerBibleBook.DataSource = books;
        if (selected.HasValue)
        {
            _books.SelectedItem = books.FirstOrDefault(x => x.Id == selected.Value);
            _plannerBibleBook.SelectedItem = books.FirstOrDefault(x => x.Id == selected.Value);
        }
        else if (_books.Items.Count > 0)
        {
            _books.SelectedIndex = 0;
            _plannerBibleBook.SelectedIndex = 0;
        }
        ReloadVerses();
    }

    private void ReloadVerses()
    {
        if (_books.SelectedItem is not BibleBook book)
            return;

        _verses.DataSource = _database.GetVerses(book.Id, (int)_chapter.Value);
        if (_verses.Items.Count == 0)
            ClearVerseEditor();
    }

    private void SelectVerse()
    {
        if (_verses.SelectedItem is not BibleVerse verse)
            return;

        _selectedVerseId = verse.Id;
        _verseNumber.Value = verse.Number;
        _verseText.Text = verse.Text;
    }

    private void SaveVerse()
    {
        if (_books.SelectedItem is not BibleBook book)
        {
            MessageBox.Show("Add and select a Bible book first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_verseText.Text))
        {
            MessageBox.Show("Enter the verse text.");
            return;
        }

        _database.SaveVerse(book.Id, (int)_chapter.Value, (int)_verseNumber.Value, _verseText.Text);
        ReloadVerses();
    }

    private void DeleteVerse()
    {
        if (!_selectedVerseId.HasValue)
            return;

        _database.DeleteVerse(_selectedVerseId.Value);
        ClearVerseEditor();
        ReloadVerses();
    }

    private void ClearVerseEditor()
    {
        _selectedVerseId = null;
        _verseNumber.Value = 1;
        _verseText.Clear();
        _verses.ClearSelected();
    }

    private void AddVerseToPlanner()
    {
        if (_books.SelectedItem is not BibleBook book || _verses.SelectedItem is not BibleVerse verse)
        {
            MessageBox.Show("Select a saved verse first.");
            return;
        }

        var reference = $"{book.Name} {_chapter.Value}:{verse.Number}";
        _database.AddPlannerVerse(GetSelectedPlannerPlanId(), reference, verse.Text, (int)_fontSize.Value);
        ReloadPlanner();
    }

    private void ReloadPlanner()
    {
        _planner.DataSource = _database.GetPlannerItems(GetSelectedPlannerPlanId());
        if (_planner.Items.Count > 0)
            _planner.SelectedIndex = 0;
        else
            _previewText.Text = string.Empty;
        LoadSelectedPlannerItem();
    }

    private void LoadSelectedPlannerItem()
    {
        if (_planner.SelectedItem is not PlannerItem item)
        {
            ClearPlannerEditor(false);
            ApplyPreview();
            return;
        }

        _suppressPlannerSync = true;
        try
        {
            _plannerType.SelectedItem = NormalizePlannerType(item.ItemType);
            _plannerTitle.Text = item.Reference;
            _plannerBody.Text = item.Content;
            _plannerImagePath.Text = string.Empty;
            _plannerFontSize.Value = item.FontSize ?? (int)_fontSize.Value;

            if (TryParsePlannerSourceValue(item.SourceValue, out var sourceType, out var values))
            {
                if (sourceType == "song" && values.Count > 0)
                    _plannerSearch.Text = item.Reference;
                else if (sourceType == "media" && values.Count > 0)
                    _plannerSearch.Text = item.Reference;
                else if (sourceType == "image" && values.Count > 0)
                    _plannerImagePath.Text = values[0];
                else if (sourceType == "bible" && values.Count >= 4)
                {
                    if (long.TryParse(values[0], out var bookId))
                        _plannerBibleBook.SelectedItem = (_plannerBibleBook.DataSource as List<BibleBook>)?.FirstOrDefault(book => book.Id == bookId);
                    if (int.TryParse(values[1], out var chapter))
                        _plannerBibleChapter.Value = chapter;
                    if (int.TryParse(values[2], out var fromVerse))
                        _plannerBibleFrom.Value = fromVerse;
                    if (int.TryParse(values[3], out var toVerse))
                        _plannerBibleTo.Value = toVerse;
                }
            }

            UpdatePlannerEditorMode();
            if (_plannerType.SelectedItem is string type && type == "Song")
            {
                ReloadPlannerSearchResults();
                if (TryParsePlannerSourceValue(item.SourceValue, out _, out var songValues) && songValues.Count > 0 && long.TryParse(songValues[0], out var songId) && _plannerSearchResults.DataSource is List<Song> songs)
                    _plannerSearchResults.SelectedItem = songs.FirstOrDefault(song => song.Id == songId);
            }
            if (_plannerType.SelectedItem is string mediaType && mediaType == "Media")
            {
                ReloadPlannerSearchResults();
                if (TryParsePlannerSourceValue(item.SourceValue, out _, out var mediaValues) && mediaValues.Count > 0 && long.TryParse(mediaValues[0], out var mediaId) && _plannerSearchResults.DataSource is List<MediaItem> mediaItems)
                    _plannerSearchResults.SelectedItem = mediaItems.FirstOrDefault(media => media.Id == mediaId);
            }
            if (_plannerType.SelectedItem is string bibleType && bibleType == "Bible Verse Range")
                LoadPlannerBibleRange(false);
        }
        finally
        {
            _suppressPlannerSync = false;
        }

        _previewText.Text = $"{item.Reference}\n\n{item.Content}";
        ApplyPreview(item.FontColor, item.FontSize);
    }

    private void SavePlannerItem()
    {
        var type = _plannerType.SelectedItem as string ?? "Heading";
        if (!TryBuildPlannerItem(type, out var reference, out var content, out var sourceValue, out var fontSize))
            return;

        var selectedId = (_planner.SelectedItem as PlannerItem)?.Id;
        var savedId = _database.SavePlannerItem(GetSelectedPlannerPlanId(), selectedId, type, reference, content, sourceValue, fontSize);
        ReloadPlanner();
        SelectPlannerItemById(savedId);
    }

    private void DeletePlannerItem()
    {
        if (_planner.SelectedItem is not PlannerItem item)
            return;

        if (MessageBox.Show($"Delete '{item.Reference}'?", "Delete Planner Item", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _database.DeletePlannerItem(item.Id);
        ClearPlannerEditor();
        ReloadPlanner();
    }

    private void ClearPlannerEditor(bool keepListSelection = true)
    {
        _suppressPlannerSync = true;
        try
        {
            if (!keepListSelection)
                _planner.ClearSelected();
            _plannerType.SelectedIndex = 0;
            _plannerTitle.Clear();
            _plannerBody.Clear();
            _plannerImagePath.Clear();
            _plannerSearch.Clear();
            _plannerSearchResults.DataSource = null;
            _plannerSearchResults.Items.Clear();
            _plannerBibleFrom.Value = 1;
            _plannerBibleTo.Value = 1;
            _plannerBibleChapter.Value = 1;
            _plannerFontSize.Value = _fontSize.Value;
            if (_plannerBibleBook.Items.Count > 0)
                _plannerBibleBook.SelectedIndex = 0;
        }
        finally
        {
            _suppressPlannerSync = false;
        }

        UpdatePlannerEditorMode();
        ApplyPreview();
    }

    private void UpdatePlannerEditorMode()
    {
        var type = _plannerType.SelectedItem as string ?? "Heading";
        var usesSearch = type is "Song" or "Media";
        var usesBible = type == "Bible Verse Range";
        var usesImage = type == "Image + Text";
        _plannerSearchPanel.Visible = usesSearch;
        _plannerBiblePanel.Visible = usesBible;
        _plannerImagePanel.Visible = usesImage;
        if (!_suppressPlannerSync)
        {
            if (usesSearch)
                ReloadPlannerSearchResults();
            else if (usesBible)
                LoadPlannerBibleRange(false);
        }
    }

    private void ReloadPlannerSearchResults()
    {
        if (_suppressPlannerSync)
            return;

        var type = _plannerType.SelectedItem as string ?? "Heading";
        if (type == "Song")
        {
            var songs = _database.GetSongs(_plannerSearch.Text);
            _plannerSearchResults.DataSource = songs;
            SelectPlannerSearchResult(songs.FirstOrDefault(song => song.Title.Equals(_plannerTitle.Text, StringComparison.OrdinalIgnoreCase)));
        }
        else if (type == "Media")
        {
            var items = _database.GetMediaItems(null, _plannerSearch.Text);
            _plannerSearchResults.DataSource = items;
            SelectPlannerSearchResult(items.FirstOrDefault(item => item.Title.Equals(_plannerTitle.Text, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            _plannerSearchResults.DataSource = null;
            _plannerSearchResults.Items.Clear();
        }
    }

    private void ApplyPlannerSearchSelection()
    {
        if (_suppressPlannerSync)
            return;

        if (_plannerType.SelectedItem is not string type)
            return;

        if (type == "Song" && _plannerSearchResults.SelectedItem is Song song)
        {
            _suppressPlannerSync = true;
            try
            {
                _plannerTitle.Text = song.Title;
                _plannerBody.Text = song.Lyrics;
            }
            finally
            {
                _suppressPlannerSync = false;
            }
        }
        else if (type == "Media" && _plannerSearchResults.SelectedItem is MediaItem media)
        {
            _suppressPlannerSync = true;
            try
            {
                _plannerTitle.Text = media.Title;
                _plannerBody.Text = media.FilePath;
                _plannerImagePath.Text = media.FilePath;
            }
            finally
            {
                _suppressPlannerSync = false;
            }
        }
    }

    private void BrowsePlannerImagePath()
    {
        using var dialog = new OpenFileDialog { Title = "Select image file", Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        _plannerImagePath.Text = dialog.FileName;
    }

    private void LoadPlannerBibleRange(bool updatePreview = true)
    {
        if (_plannerBibleBook.SelectedItem is not BibleBook book)
            return;

        var fromVerse = (int)_plannerBibleFrom.Value;
        var toVerse = (int)_plannerBibleTo.Value;
        var verses = _database.GetVerseRange(book.Id, (int)_plannerBibleChapter.Value, fromVerse, toVerse);
        if (verses.Count == 0)
        {
            if (updatePreview)
                MessageBox.Show("No verses were found for that range.");
            return;
        }

        var start = Math.Min(fromVerse, toVerse);
        var end = Math.Max(fromVerse, toVerse);
        var reference = $"{book.Name} {(int)_plannerBibleChapter.Value}:{start}-{end}";
        var content = string.Join(Environment.NewLine, verses.Select(verse => $"{verse.Number}. {verse.Text}"));

        _suppressPlannerSync = true;
        try
        {
            _plannerTitle.Text = reference;
            _plannerBody.Text = content;
        }
        finally
        {
            _suppressPlannerSync = false;
        }

        if (updatePreview && _plannerType.SelectedItem as string == "Bible Verse Range")
            _previewText.Text = $"{reference}\n\n{content}";
    }

    private bool TryBuildPlannerItem(string type, out string reference, out string content, out string? sourceValue, out int? fontSize)
    {
        reference = string.Empty;
        content = string.Empty;
        sourceValue = null;
        fontSize = (int)_plannerFontSize.Value;

        switch (type)
        {
            case "Heading":
                reference = _plannerTitle.Text.Trim();
                if (string.IsNullOrWhiteSpace(reference))
                {
                    MessageBox.Show("Enter a heading.");
                    return false;
                }
                break;
            case "Heading + Text":
                reference = _plannerTitle.Text.Trim();
                content = _plannerBody.Text.Trim();
                if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(content))
                {
                    MessageBox.Show("Enter both a heading and text.");
                    return false;
                }
                break;
            case "Image + Text":
                reference = _plannerTitle.Text.Trim();
                content = _plannerBody.Text.Trim();
                sourceValue = _plannerImagePath.Text.Trim();
                if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(sourceValue))
                {
                    MessageBox.Show("Enter a heading, text, and image file path.");
                    return false;
                }
                sourceValue = $"image|{sourceValue}";
                break;
            case "Song":
                if (_plannerSearchResults.SelectedItem is not Song song)
                {
                    MessageBox.Show("Search and select a song first.");
                    return false;
                }
                reference = song.Title;
                content = song.Lyrics;
                sourceValue = $"song|{song.Id}";
                break;
            case "Bible Verse Range":
                if (_plannerBibleBook.SelectedItem is not BibleBook book)
                {
                    MessageBox.Show("Select a Bible book first.");
                    return false;
                }
                var verses = _database.GetVerseRange(book.Id, (int)_plannerBibleChapter.Value, (int)_plannerBibleFrom.Value, (int)_plannerBibleTo.Value);
                if (verses.Count == 0)
                {
                    MessageBox.Show("No verses were found for that range.");
                    return false;
                }
                var start = Math.Min((int)_plannerBibleFrom.Value, (int)_plannerBibleTo.Value);
                var end = Math.Max((int)_plannerBibleFrom.Value, (int)_plannerBibleTo.Value);
                reference = $"{book.Name} {(int)_plannerBibleChapter.Value}:{start}-{end}";
                content = string.Join(Environment.NewLine, verses.Select(verse => $"{verse.Number}. {verse.Text}"));
                sourceValue = $"bible|{book.Id}|{(int)_plannerBibleChapter.Value}|{start}|{end}";
                break;
            case "Media":
                if (_plannerSearchResults.SelectedItem is not MediaItem media)
                {
                    MessageBox.Show("Search and select a media item first.");
                    return false;
                }
                reference = media.Title;
                content = media.FilePath;
                sourceValue = $"media|{media.Id}";
                break;
            default:
                MessageBox.Show("Choose a component type.");
                return false;
        }

        return true;
    }

    private void SelectPlannerItemById(long id)
    {
        var items = _planner.DataSource as List<PlannerItem>;
        if (items is null)
            return;

        _planner.SelectedItem = items.FirstOrDefault(item => item.Id == id);
    }

    private void BeginPlannerDrag(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        var index = _planner.IndexFromPoint(e.Location);
        if (index < 0 || index >= _planner.Items.Count)
            return;

        var item = _planner.Items[index] as PlannerItem;
        if (item is null)
            return;

        _draggedPlannerItemId = item.Id;
        _planner.SelectedIndex = index;
        _planner.DoDragDrop(item, DragDropEffects.Move);
    }

    private void DropPlannerItem(DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(PlannerItem)) is not PlannerItem draggedItem)
            return;

        var dropPoint = _planner.PointToClient(new Point(e.X, e.Y));
        var targetIndex = _planner.IndexFromPoint(dropPoint);
        var items = (_planner.DataSource as List<PlannerItem>)?.ToList() ?? new List<PlannerItem>();
        if (items.Count == 0)
            return;

        var sourceIndex = items.FindIndex(item => item.Id == draggedItem.Id);
        if (sourceIndex < 0)
            return;

        items.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex)
            targetIndex--;
        if (targetIndex < 0 || targetIndex >= items.Count)
            items.Add(draggedItem);
        else
            items.Insert(targetIndex, draggedItem);

        _database.ReorderPlannerItems(GetSelectedPlannerPlanId(), items.Select(item => item.Id).ToList());
        ReloadPlanner();
        SelectPlannerItemById(draggedItem.Id);
        _draggedPlannerItemId = null;
    }

    private void MovePlannerSelection(int delta)
    {
        var items = _planner.DataSource as List<PlannerItem>;
        if (items is null || items.Count == 0)
            return;

        var currentIndex = _planner.SelectedIndex;
        if (currentIndex < 0)
            currentIndex = 0;

        var nextIndex = (currentIndex + delta) % items.Count;
        if (nextIndex < 0)
            nextIndex += items.Count;

        _planner.SelectedIndex = nextIndex;
    }

    private void ReloadOperatorItems()
    {
        _operatorItems.DataSource = _database.GetPlannerItems(GetSelectedOperatorPlanId());
        if (_operatorItems.Items.Count > 0)
            _operatorItems.SelectedIndex = 0;
        else
            _operatorPreviewText.Text = string.Empty;
        LoadSelectedOperatorItem();
    }

    private void LoadSelectedOperatorItem()
    {
        if (_operatorItems.SelectedItem is not PlannerItem item)
        {
            _operatorPreviewText.Text = string.Empty;
            ApplyPreview();
            return;
        }

        _operatorPreviewText.Text = $"{item.Reference}\n\n{item.Content}";
        ApplyPreview(item.FontColor, item.FontSize);
        if (_presentationForm is not null && !_presentationForm.IsDisposed)
        {
            var family = _fontFamily.SelectedItem as string ?? _database.GetSetting("PresentationFontFamily", "Segoe UI");
            _presentationForm.UpdateDisplay(_background, _fontColor, family, (int)_fontSize.Value, _operatorPreviewText.Text, item.FontColor, item.FontSize);
        }
    }

    private void MoveOperatorSelection(int delta)
    {
        var items = _operatorItems.DataSource as List<PlannerItem>;
        if (items is null || items.Count == 0)
            return;

        var currentIndex = _operatorItems.SelectedIndex;
        if (currentIndex < 0)
            currentIndex = 0;

        var nextIndex = (currentIndex + delta) % items.Count;
        if (nextIndex < 0)
            nextIndex += items.Count;

        _operatorItems.SelectedIndex = nextIndex;
    }

    private static string NormalizePlannerType(string itemType) => itemType switch
    {
        "Bible" => "Bible Verse Range",
        "HeadingText" => "Heading + Text",
        "ImageText" => "Image + Text",
        _ => itemType
    };

    private static bool TryParsePlannerSourceValue(string? sourceValue, out string sourceType, out List<string> values)
    {
        sourceType = string.Empty;
        values = new List<string>();
        if (string.IsNullOrWhiteSpace(sourceValue))
            return false;

        values = sourceValue.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (values.Count == 0)
            return false;

        sourceType = values[0];
        values = values.Skip(1).ToList();
        return true;
    }

    private void SelectPlannerSearchResult<T>(T? match) where T : class
    {
        if (match is null)
            return;

        _plannerSearchResults.SelectedItem = match;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F7)
        {
            MoveOperatorSelection(-1);
            return true;
        }

        if (keyData == Keys.F8)
        {
            MoveOperatorSelection(1);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void LoadSettings()
    {
        _background = ColorTranslator.FromHtml(_database.GetSetting("PresentationBackground", "#FFFFFF"));
        _fontColor = ColorTranslator.FromHtml(_database.GetSetting("PresentationFontColor", "#000000"));
        _fontSize.Value = decimal.Parse(_database.GetSetting("PresentationFontSize", "48"));
        var savedFamily = _database.GetSetting("PresentationFontFamily", "Segoe UI");
        if (_fontFamily.Items.Contains(savedFamily))
            _fontFamily.SelectedItem = savedFamily;
        else if (_fontFamily.Items.Count > 0)
            _fontFamily.SelectedIndex = 0;
        ApplyPreview();
    }

    private void ChooseColor(bool isBackground)
    {
        using var dialog = new ColorDialog { Color = isBackground ? _background : _fontColor };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        if (isBackground)
        {
            _background = dialog.Color;
            _database.SetSetting("PresentationBackground", ColorTranslator.ToHtml(_background));
        }
        else
        {
            _fontColor = dialog.Color;
            _database.SetSetting("PresentationFontColor", ColorTranslator.ToHtml(_fontColor));
        }

        ApplyPreview();
    }

    private void ChoosePlannerItemColor()
    {
        if (_planner.SelectedItem is not PlannerItem item)
        {
            MessageBox.Show("Select a planner item first.");
            return;
        }

        using var dialog = new ColorDialog { Color = string.IsNullOrWhiteSpace(item.FontColor) ? _fontColor : ColorTranslator.FromHtml(item.FontColor) };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        _database.SetPlannerItemFontColor(item.Id, ColorTranslator.ToHtml(dialog.Color));
        ReloadPlanner();
    }

    private void ApplyPreview(string? itemFontColor = null, int? itemFontSize = null)
    {
        _operatorPreview.BackColor = _background;
        _operatorPreviewText.ForeColor = string.IsNullOrWhiteSpace(itemFontColor) ? _fontColor : ColorTranslator.FromHtml(itemFontColor);
        var family = _fontFamily.SelectedItem as string ?? _database.GetSetting("PresentationFontFamily", "Segoe UI");
        var size = itemFontSize ?? (int)_fontSize.Value;
        _operatorPreviewText.Font = new Font(family, size, FontStyle.Bold);
        _presentationForm?.UpdateDisplay(_background, _fontColor, family, size, _operatorPreviewText.Text, itemFontColor, itemFontSize);
    }

    private void OpenPresentationWindow()
    {
        if (_presentationForm is not null && !_presentationForm.IsDisposed)
            return;

        _presentationForm = new PresentationForm();
        _presentationForm.FormClosed += (_, _) => UpdatePresentationToggle(false);
        _presentationForm.ShowOnBestScreen();
        var family = _fontFamily.SelectedItem as string ?? _database.GetSetting("PresentationFontFamily", "Segoe UI");
        var item = _operatorItems.SelectedItem as PlannerItem;
        _presentationForm.UpdateDisplay(_background, _fontColor, family, (int)_fontSize.Value, _operatorPreviewText.Text, item?.FontColor, item?.FontSize);
        UpdatePresentationToggle(true);
    }

    private void TogglePresentationWindow()
    {
        if (_presentationForm is null || _presentationForm.IsDisposed)
        {
            OpenPresentationWindow();
            return;
        }

        ClosePresentationWindow();
    }

    private void ClosePresentationWindow()
    {
        if (_presentationForm is null || _presentationForm.IsDisposed)
        {
            UpdatePresentationToggle(false);
            return;
        }

        _presentationForm.Hide();
        _presentationForm.Close();
        _presentationForm = null;
        UpdatePresentationToggle(false);
    }

    private void UpdatePresentationToggle(bool open)
    {
        _presentationToggle.Text = open ? "Close Presentation" : "Open Presentation";
    }

    private void ReloadSongs()
    {
        var selected = _selectedSongId;
        var songs = _database.GetSongs(_songSearch.Text);
        _songs.DataSource = songs;

        if (selected.HasValue)
        {
            _songs.SelectedItem = songs.FirstOrDefault(song => song.Id == selected.Value);
        }
        else if (_songs.Items.Count > 0)
        {
            _songs.SelectedIndex = 0;
        }
        else
        {
            ClearSongEditor();
        }
    }

    private void SelectSong()
    {
        if (_songs.SelectedItem is not Song song)
            return;

        _selectedSongId = song.Id;
        _songTitle.Text = song.Title;
        _songLyrics.Text = song.Lyrics;
    }

    private void SaveSong()
    {
        var title = _songTitle.Text.Trim();
        var lyrics = _songLyrics.Text.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(lyrics))
        {
            MessageBox.Show("Enter both a title and lyrics.");
            return;
        }

        _selectedSongId = _database.SaveSong(_selectedSongId, title, lyrics);
        ReloadSongs();
    }

    private void DeleteSong()
    {
        if (_songs.SelectedItem is not Song song)
            return;

        if (MessageBox.Show($"Delete '{song.Title}'?", "Delete Song", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _database.DeleteSong(song.Id);
        ClearSongEditor();
        ReloadSongs();
    }

    private void ClearSongEditor()
    {
        _selectedSongId = null;
        _songTitle.Clear();
        _songLyrics.Clear();
        _songs.ClearSelected();
    }

    private void ReloadMedia()
    {
        var selected = _selectedMediaId;
        var type = _mediaTypeFilter.SelectedItem as string;
        var media = _database.GetMediaItems(type, _mediaSearch.Text);
        _mediaItems.DataSource = media;

        if (selected.HasValue)
        {
            _mediaItems.SelectedItem = media.FirstOrDefault(item => item.Id == selected.Value);
        }
        else if (_mediaItems.Items.Count > 0)
        {
            _mediaItems.SelectedIndex = 0;
        }
        else
        {
            ClearMediaEditor();
        }
    }

    private void SelectMedia()
    {
        if (_mediaItems.SelectedItem is not MediaItem item)
            return;

        _selectedMediaId = item.Id;
        _mediaType.SelectedItem = item.MediaType;
        _mediaTitle.Text = item.Title;
        _mediaPath.Text = item.FilePath;
    }

    private void BrowseMediaPath()
    {
        using var dialog = new OpenFileDialog { Title = "Select media file", Filter = "All files (*.*)|*.*" };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        _mediaPath.Text = dialog.FileName;
    }

    private void SaveMediaItem()
    {
        var mediaType = _mediaType.SelectedItem as string ?? "Image";
        var title = _mediaTitle.Text.Trim();
        var path = _mediaPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Enter both a title and a file path.");
            return;
        }

        _selectedMediaId = _database.SaveMediaItem(_selectedMediaId, mediaType, title, path);
        ReloadMedia();
    }

    private void DeleteMediaItem()
    {
        if (_mediaItems.SelectedItem is not MediaItem item)
            return;

        if (MessageBox.Show($"Delete '{item.Title}'?", "Delete Media Item", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _database.DeleteMediaItem(item.Id);
        ClearMediaEditor();
        ReloadMedia();
    }

    private void ClearMediaEditor()
    {
        _selectedMediaId = null;
        _mediaType.SelectedIndex = 0;
        _mediaTitle.Clear();
        _mediaPath.Clear();
        _mediaItems.ClearSelected();
    }
}

internal static class Prompt
{
    public static string? Show(string text, string title)
    {
        using var form = new Form
        {
            Width = 420,
            Height = 150,
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label { Left = 15, Top = 15, Text = text, AutoSize = true };
        var input = new TextBox { Left = 15, Top = 42, Width = 370 };
        var ok = new Button { Text = "Save", Left = 230, Top = 75, DialogResult = DialogResult.OK };
        form.Controls.AddRange([label, input, ok]);
        form.AcceptButton = ok;
        return form.ShowDialog() == DialogResult.OK ? input.Text : null;
    }
}
