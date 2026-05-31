using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Minimarket
{
    public partial class MainForm : Form
    {
        private readonly CultureInfo culture = new CultureInfo("id-ID");
        private readonly List<CartItem> cartItems = new List<CartItem>();
        private DatabaseService database;
        private Exception startupError;
        private string currentUsername;
        private int currentUserId;
        private List<Product> products = new List<Product>();
        private Product selectedQuickProduct;

        private Button loginButton;
        private TextBox loginUsernameTextBox;
        private TextBox loginPasswordTextBox;
        private Panel contentPanel;

        private ComboBox productSearchComboBox;
        private bool updatingProductSearch;
        private TextBox productNameTextBox;
        private TextBox priceTextBox;
        private TextBox discountTextBox;
        private NumericUpDown qtyInput;
        private DataGridView productsGrid;
        private DataGridView cartGrid;
        private TextBox paidTextBox;
        private TextBox changeTextBox;
        private Label totalLabel;

        private ListBox historyListBox;
        private TextBox historySearchTextBox;
        private DataGridView historyItemsGrid;
        private Label historyTotalLabel;
        private List<SalesTransaction> historyTransactions = new List<SalesTransaction>();

        private DateTimePicker reportStartDatePicker;
        private DateTimePicker reportEndDatePicker;
        private TextBox reportKeywordTextBox;
        private DataGridView reportGrid;
        private Label reportInfoLabel;
        private ReportKind currentReportKind = ReportKind.Sales;
        private DataTable currentReportData;

        private TextBox accountUsernameTextBox;
        private TextBox accountPasswordTextBox;

        public MainForm()
        {
            InitializeComponent();

            Font = new Font("Segoe UI", 9F);
            Text = "Program Kasir Minimarket";
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterScreen;

            try
            {
                database = new DatabaseService();
                database.Initialize();
            }
            catch (Exception ex)
            {
                startupError = ex;
            }

            BuildLoginView();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (startupError != null)
            {
                MessageBox.Show(
                    "Database SQL Server belum bisa dipakai.\r\n\r\n" +
                    startupError.Message + "\r\n\r\n" +
                    "Pastikan SQL Server (SQLEXPRESS) aktif dan koneksi di App.config sudah sesuai.",
                    "Koneksi SQL Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                if (loginButton != null)
                {
                    loginButton.Enabled = false;
                }
            }
        }

        private void BuildLoginView()
        {
            Controls.Clear();
            BackColor = Color.FromArgb(245, 245, 245);

            TableLayoutPanel page = new TableLayoutPanel();
            page.Dock = DockStyle.Fill;
            page.RowCount = 3;
            page.ColumnCount = 1;
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            Controls.Add(page);

            Panel center = new Panel();
            center.Width = 560;
            center.Height = 250;
            center.Anchor = AnchorStyles.None;
            page.Controls.Add(center, 0, 1);

            Label title = new Label();
            title.Text = "Program Kasir Minimarket";
            title.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Top;
            title.Height = 62;
            center.Controls.Add(title);

            GroupBox loginBox = new GroupBox();
            loginBox.Text = "Login";
            loginBox.SetBounds(30, 80, 500, 116);
            center.Controls.Add(loginBox);

            Label usernameLabel = CreateFixedLabel("Username");
            usernameLabel.SetBounds(14, 30, 92, 26);
            loginBox.Controls.Add(usernameLabel);

            loginUsernameTextBox = new TextBox();
            loginUsernameTextBox.SetBounds(112, 30, 370, 26);
            loginUsernameTextBox.Text = "admin";
            loginBox.Controls.Add(loginUsernameTextBox);

            Label passwordLabel = CreateFixedLabel("Password");
            passwordLabel.SetBounds(14, 70, 92, 26);
            loginBox.Controls.Add(passwordLabel);

            loginPasswordTextBox = new TextBox();
            loginPasswordTextBox.SetBounds(112, 70, 370, 26);
            loginPasswordTextBox.PasswordChar = '*';
            loginPasswordTextBox.Text = "admin123";
            loginPasswordTextBox.KeyDown += LoginPasswordTextBox_KeyDown;
            loginBox.Controls.Add(loginPasswordTextBox);

            loginButton = CreateButton("Login", Color.FromArgb(82, 58, 209), Color.White);
            loginButton.SetBounds(30, 206, 500, 38);
            loginButton.Click += LoginButton_Click;
            center.Controls.Add(loginButton);

            Controls.Add(CreateFooter());
        }

        private void LoginPasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoginButton_Click(sender, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            if (database == null)
            {
                MessageBox.Show("Database belum siap.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string username = loginUsernameTextBox.Text.Trim();
            string password = loginPasswordTextBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username dan password wajib diisi.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (!database.ValidateLogin(username, password))
                {
                    MessageBox.Show("Username atau password salah.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                currentUsername = username;
                currentUserId = database.GetUserId(username);
                ShowMainTab(MainTab.Cashier);
            }
            catch (Exception ex)
            {
                ShowError("login", ex);
            }
        }

        private void ShowMainTab(MainTab tab)
        {
            Controls.Clear();
            BackColor = Color.FromArgb(246, 246, 246);

            Panel root = new Panel();
            root.Dock = DockStyle.Fill;
            Controls.Add(root);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 48;
            header.BackColor = Color.FromArgb(63, 68, 75);
            root.Controls.Add(header);

            Button cashierButton = CreateNavButton("Transaksi (Kasir)", MainTab.Cashier, tab == MainTab.Cashier);
            cashierButton.Left = 0;
            cashierButton.Click += delegate { ShowMainTab(MainTab.Cashier); };
            header.Controls.Add(cashierButton);

            Button historyButton = CreateNavButton("Histori", MainTab.History, tab == MainTab.History);
            historyButton.Left = cashierButton.Right;
            historyButton.Click += delegate { ShowMainTab(MainTab.History); };
            header.Controls.Add(historyButton);

            Button reportButton = CreateNavButton("Laporan Crystal", MainTab.Reports, tab == MainTab.Reports);
            reportButton.Left = historyButton.Right;
            reportButton.Click += delegate { ShowMainTab(MainTab.Reports); };
            header.Controls.Add(reportButton);

            Button settingsButton = CreateNavButton("Pengaturan", MainTab.Settings, tab == MainTab.Settings);
            settingsButton.Left = reportButton.Right;
            settingsButton.Click += delegate { ShowMainTab(MainTab.Settings); };
            header.Controls.Add(settingsButton);

            Panel accent = new Panel();
            accent.Dock = DockStyle.Top;
            accent.Height = 4;
            accent.BackColor = Color.FromArgb(240, 179, 0);
            root.Controls.Add(accent);
            accent.BringToFront();

            Label footer = CreateFooter();
            root.Controls.Add(footer);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(10);
            root.Controls.Add(contentPanel);
            contentPanel.BringToFront();

            if (tab == MainTab.Cashier)
            {
                BuildCashierView();
            }
            else if (tab == MainTab.History)
            {
                BuildHistoryView();
            }
            else if (tab == MainTab.Reports)
            {
                BuildReportsView();
            }
            else
            {
                BuildSettingsView();
            }
        }

        private void BuildCashierView()
        {
            contentPanel.Controls.Clear();

            SplitContainer split = CreateResponsiveSplit(430);
            contentPanel.Controls.Add(split);

            TableLayoutPanel left = new TableLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.RowCount = 2;
            left.ColumnCount = 1;
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            split.Panel1.Controls.Add(left);

            GroupBox quickBox = new GroupBox();
            quickBox.Text = "Input Transaksi Cepat";
            quickBox.Dock = DockStyle.Fill;
            left.Controls.Add(quickBox, 0, 0);
            BuildQuickInput(quickBox);

            GroupBox stockBox = new GroupBox();
            stockBox.Text = "Stock Manager";
            stockBox.Dock = DockStyle.Fill;
            left.Controls.Add(stockBox, 0, 1);
            BuildStockManager(stockBox);

            GroupBox summaryBox = new GroupBox();
            summaryBox.Text = "Ringkasan Transaksi & Pembayaran";
            summaryBox.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(summaryBox);
            BuildTransactionSummary(summaryBox);

            RefreshProducts();
            RefreshCartGrid();
        }

        private void BuildQuickInput(GroupBox parent)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10, 8, 10, 10);
            layout.RowCount = 5;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            parent.Controls.Add(layout);

            layout.Controls.Add(CreateLabel("Cari Produk"), 0, 0);
            Panel searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Fill;
            layout.Controls.Add(searchPanel, 1, 0);

            productSearchComboBox = new ComboBox();
            productSearchComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            productSearchComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            productSearchComboBox.IntegralHeight = false;
            productSearchComboBox.DropDownHeight = 180;
            productSearchComboBox.Width = Math.Max(80, searchPanel.Width - 86);
            productSearchComboBox.DropDownWidth = Math.Max(220, productSearchComboBox.Width);
            productSearchComboBox.Left = 0;
            productSearchComboBox.Top = 3;
            productSearchComboBox.TextUpdate += ProductSearchComboBox_TextUpdate;
            productSearchComboBox.KeyDown += ProductSearchComboBox_KeyDown;
            productSearchComboBox.SelectionChangeCommitted += ProductSearchComboBox_SelectionChangeCommitted;
            searchPanel.Controls.Add(productSearchComboBox);

            Button searchButton = CreateButton("Cari", Color.FromArgb(86, 96, 109), Color.White);
            searchButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            searchButton.SetBounds(Math.Max(0, searchPanel.Width - 78), 2, 78, 26);
            searchButton.Click += delegate { SearchProductByQuery(); };
            searchPanel.Controls.Add(searchButton);
            searchPanel.Resize += delegate
            {
                productSearchComboBox.Width = Math.Max(80, searchPanel.Width - 86);
                productSearchComboBox.DropDownWidth = Math.Max(220, productSearchComboBox.Width);
                searchButton.Left = searchPanel.Width - 78;
            };

            layout.Controls.Add(CreateLabel("Nama Produk"), 0, 1);
            productNameTextBox = CreateReadOnlyTextBox();
            layout.Controls.Add(productNameTextBox, 1, 1);

            layout.Controls.Add(CreateLabel("Harga Diskon"), 0, 2);
            priceTextBox = CreateReadOnlyTextBox();
            layout.Controls.Add(priceTextBox, 1, 2);

            layout.Controls.Add(CreateLabel("Diskon"), 0, 3);
            discountTextBox = CreateReadOnlyTextBox();
            layout.Controls.Add(discountTextBox, 1, 3);

            layout.Controls.Add(CreateLabel("Qty"), 0, 4);
            Panel addPanel = new Panel();
            addPanel.Dock = DockStyle.Fill;
            layout.Controls.Add(addPanel, 1, 4);

            qtyInput = new NumericUpDown();
            qtyInput.Minimum = 1;
            qtyInput.Maximum = 100000;
            qtyInput.Value = 1;
            qtyInput.SetBounds(0, 6, 76, 28);
            addPanel.Controls.Add(qtyInput);

            Button addButton = CreateButton("Tambah ke Keranjang", Color.FromArgb(30, 112, 184), Color.White);
            addButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            addButton.SetBounds(86, 5, addPanel.Width - 86, 30);
            addButton.Click += AddToCartButton_Click;
            addPanel.Controls.Add(addButton);
            addPanel.Resize += delegate { addButton.Width = Math.Max(140, addPanel.Width - 86); };
        }

        private void BuildStockManager(GroupBox parent)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 2;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            parent.Controls.Add(layout);

            productsGrid = CreateGrid();
            ConfigureProductsGrid();
            productsGrid.SelectionChanged += ProductsGrid_SelectionChanged;
            productsGrid.CellDoubleClick += ProductsGrid_CellDoubleClick;
            layout.Controls.Add(productsGrid, 0, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(0, 6, 0, 0);
            layout.Controls.Add(buttons, 0, 1);

            Button addProductButton = CreateButton("Tambah Produk", Color.FromArgb(30, 112, 184), Color.White);
            addProductButton.Width = 124;
            addProductButton.Click += AddProductButton_Click;
            buttons.Controls.Add(addProductButton);

            Button editProductButton = CreateButton("Edit", Color.FromArgb(86, 96, 109), Color.White);
            editProductButton.Width = 84;
            editProductButton.Click += EditProductButton_Click;
            buttons.Controls.Add(editProductButton);

            Button deleteProductButton = CreateButton("Hapus", Color.FromArgb(208, 32, 32), Color.White);
            deleteProductButton.Width = 84;
            deleteProductButton.Click += DeleteProductButton_Click;
            buttons.Controls.Add(deleteProductButton);
        }

        private void BuildTransactionSummary(GroupBox parent)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 5;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            parent.Controls.Add(layout);

            totalLabel = new Label();
            totalLabel.Dock = DockStyle.Fill;
            totalLabel.TextAlign = ContentAlignment.MiddleLeft;
            totalLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            totalLabel.ForeColor = Color.FromArgb(130, 130, 130);
            layout.Controls.Add(totalLabel, 0, 0);

            cartGrid = CreateGrid();
            ConfigureCartGrid();
            cartGrid.CellContentClick += CartGrid_CellContentClick;
            layout.Controls.Add(cartGrid, 0, 1);

            TableLayoutPanel paymentLayout = new TableLayoutPanel();
            paymentLayout.Dock = DockStyle.Fill;
            paymentLayout.ColumnCount = 2;
            paymentLayout.RowCount = 2;
            paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            paymentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            paymentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.Controls.Add(paymentLayout, 0, 2);

            paymentLayout.Controls.Add(CreateLabel("Uang Diterima"), 0, 0);
            paymentLayout.Controls.Add(CreateLabel("Kembalian"), 1, 0);

            paidTextBox = new TextBox();
            paidTextBox.Dock = DockStyle.Fill;
            paidTextBox.TextChanged += delegate { UpdateChange(); };
            paidTextBox.Leave += delegate { FormatMoneyTextBox(paidTextBox); };
            paymentLayout.Controls.Add(paidTextBox, 0, 1);

            changeTextBox = CreateReadOnlyTextBox();
            paymentLayout.Controls.Add(changeTextBox, 1, 1);

            Label paymentTitle = new Label();
            paymentTitle.Text = "Pembayaran";
            paymentTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            paymentTitle.Dock = DockStyle.Fill;
            paymentTitle.TextAlign = ContentAlignment.BottomLeft;
            layout.Controls.Add(paymentTitle, 0, 3);

            FlowLayoutPanel actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.RightToLeft;
            layout.Controls.Add(actionPanel, 0, 4);

            Button saveButton = CreateButton("Simpan Transaksi", Color.FromArgb(52, 145, 71), Color.White);
            saveButton.Width = 180;
            saveButton.Height = 36;
            saveButton.Click += SaveTransactionButton_Click;
            actionPanel.Controls.Add(saveButton);

            Button clearButton = CreateButton("Kosongkan", Color.FromArgb(86, 96, 109), Color.White);
            clearButton.Width = 120;
            clearButton.Height = 36;
            clearButton.Click += delegate
            {
                cartItems.Clear();
                RefreshCartGrid();
            };
            actionPanel.Controls.Add(clearButton);
        }

        private void BuildHistoryView()
        {
            contentPanel.Controls.Clear();

            SplitContainer split = CreateResponsiveSplit(420);
            contentPanel.Controls.Add(split);

            GroupBox listBox = new GroupBox();
            listBox.Text = "Daftar Transaksi";
            listBox.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(listBox);

            TableLayoutPanel historyListLayout = new TableLayoutPanel();
            historyListLayout.Dock = DockStyle.Fill;
            historyListLayout.Padding = new Padding(6);
            historyListLayout.ColumnCount = 1;
            historyListLayout.RowCount = 2;
            historyListLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            historyListLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listBox.Controls.Add(historyListLayout);

            Panel historySearchPanel = new Panel();
            historySearchPanel.Dock = DockStyle.Fill;
            historyListLayout.Controls.Add(historySearchPanel, 0, 0);

            Label historySearchLabel = CreateFixedLabel("Bulan/Tahun");
            historySearchLabel.SetBounds(0, 4, 86, 24);
            historySearchPanel.Controls.Add(historySearchLabel);

            historySearchTextBox = new TextBox();
            historySearchTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            historySearchTextBox.SetBounds(92, 4, Math.Max(120, historySearchPanel.Width - 92), 24);
            historySearchTextBox.TextChanged += HistorySearchTextBox_TextChanged;
            historySearchPanel.Controls.Add(historySearchTextBox);
            historySearchPanel.Resize += delegate
            {
                historySearchTextBox.Width = Math.Max(120, historySearchPanel.Width - 92);
            };

            historyListBox = new ListBox();
            historyListBox.Dock = DockStyle.Fill;
            historyListBox.Font = new Font("Segoe UI", 10F);
            historyListBox.SelectedIndexChanged += HistoryListBox_SelectedIndexChanged;
            historyListLayout.Controls.Add(historyListBox, 0, 1);

            GroupBox summaryBox = new GroupBox();
            summaryBox.Text = "Ringkasan Transaksi";
            summaryBox.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(summaryBox);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 4;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            summaryBox.Controls.Add(layout);

            historyTotalLabel = new Label();
            historyTotalLabel.Dock = DockStyle.Fill;
            historyTotalLabel.TextAlign = ContentAlignment.MiddleLeft;
            historyTotalLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            historyTotalLabel.ForeColor = Color.FromArgb(130, 130, 130);
            layout.Controls.Add(historyTotalLabel, 0, 0);

            historyItemsGrid = CreateGrid();
            ConfigureSaleItemsGrid(historyItemsGrid);
            layout.Controls.Add(historyItemsGrid, 0, 1);

            Label actionTitle = CreateLabel("Aksi Transaksi Terpilih");
            actionTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            layout.Controls.Add(actionTitle, 0, 2);

            FlowLayoutPanel historyActionPanel = new FlowLayoutPanel();
            historyActionPanel.Dock = DockStyle.Fill;
            historyActionPanel.FlowDirection = FlowDirection.RightToLeft;
            historyActionPanel.Padding = new Padding(0, 8, 0, 0);
            layout.Controls.Add(historyActionPanel, 0, 3);

            Button deleteHistoryButton = CreateButton("Hapus Terpilih", Color.FromArgb(224, 0, 0), Color.White);
            deleteHistoryButton.Width = 130;
            deleteHistoryButton.Height = 34;
            deleteHistoryButton.Click += DeleteHistoryButton_Click;
            historyActionPanel.Controls.Add(deleteHistoryButton);

            Button exportPdfButton = CreateButton("Export PDF", Color.FromArgb(86, 96, 109), Color.White);
            exportPdfButton.Width = 120;
            exportPdfButton.Height = 34;
            exportPdfButton.Click += ExportHistoryPdfButton_Click;
            historyActionPanel.Controls.Add(exportPdfButton);

            Button printHistoryButton = CreateButton("Cetak", Color.FromArgb(52, 145, 71), Color.White);
            printHistoryButton.Width = 100;
            printHistoryButton.Height = 34;
            printHistoryButton.Click += PrintHistoryButton_Click;
            historyActionPanel.Controls.Add(printHistoryButton);

            RefreshHistoryList();
        }

        private void BuildReportsView()
        {
            contentPanel.Controls.Clear();

            SplitContainer split = CreateResponsiveSplit(360);
            contentPanel.Controls.Add(split);

            GroupBox filterBox = new GroupBox();
            filterBox.Text = "Filter & Menu Report";
            filterBox.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(filterBox);

            TableLayoutPanel filterLayout = new TableLayoutPanel();
            filterLayout.Dock = DockStyle.Fill;
            filterLayout.Padding = new Padding(10);
            filterLayout.ColumnCount = 1;
            filterLayout.RowCount = 8;
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filterBox.Controls.Add(filterLayout);

            filterLayout.Controls.Add(CreateLabel("Dari Tanggal"), 0, 0);
            reportStartDatePicker = new DateTimePicker();
            reportStartDatePicker.Dock = DockStyle.Fill;
            reportStartDatePicker.Format = DateTimePickerFormat.Short;
            reportStartDatePicker.ShowCheckBox = true;
            reportStartDatePicker.Checked = true;
            reportStartDatePicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            filterLayout.Controls.Add(reportStartDatePicker, 0, 1);

            filterLayout.Controls.Add(CreateLabel("Sampai Tanggal"), 0, 2);
            reportEndDatePicker = new DateTimePicker();
            reportEndDatePicker.Dock = DockStyle.Fill;
            reportEndDatePicker.Format = DateTimePickerFormat.Short;
            reportEndDatePicker.ShowCheckBox = true;
            reportEndDatePicker.Checked = true;
            reportEndDatePicker.Value = DateTime.Today;
            filterLayout.Controls.Add(reportEndDatePicker, 0, 3);

            filterLayout.Controls.Add(CreateLabel("Keyword Produk/User/ID"), 0, 4);
            reportKeywordTextBox = new TextBox();
            reportKeywordTextBox.Dock = DockStyle.Fill;
            filterLayout.Controls.Add(reportKeywordTextBox, 0, 5);

            Label menuLabel = CreateLabel("4 Menu Generate Report");
            menuLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            filterLayout.Controls.Add(menuLabel, 0, 6);

            FlowLayoutPanel reportButtons = new FlowLayoutPanel();
            reportButtons.Dock = DockStyle.Fill;
            reportButtons.FlowDirection = FlowDirection.TopDown;
            reportButtons.WrapContents = false;
            reportButtons.AutoScroll = true;
            filterLayout.Controls.Add(reportButtons, 0, 7);

            AddReportMenuButton(reportButtons, "1. Laporan Penjualan", ReportKind.Sales);
            AddReportMenuButton(reportButtons, "2. Laporan Transaksi", ReportKind.Transaction);
            AddReportMenuButton(reportButtons, "3. Laporan User", ReportKind.User);
            AddReportMenuButton(reportButtons, "4. Laporan Inventory", ReportKind.Inventory);

            GroupBox previewBox = new GroupBox();
            previewBox.Text = "Preview Data Report";
            previewBox.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(previewBox);

            TableLayoutPanel previewLayout = new TableLayoutPanel();
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.Padding = new Padding(10);
            previewLayout.ColumnCount = 1;
            previewLayout.RowCount = 3;
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            previewBox.Controls.Add(previewLayout);

            reportInfoLabel = new Label();
            reportInfoLabel.Dock = DockStyle.Fill;
            reportInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            reportInfoLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            reportInfoLabel.Text = "Pilih salah satu menu report di kiri.";
            previewLayout.Controls.Add(reportInfoLabel, 0, 0);

            reportGrid = CreateGrid();
            reportGrid.AutoGenerateColumns = true;
            previewLayout.Controls.Add(reportGrid, 0, 1);

            FlowLayoutPanel actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.RightToLeft;
            actionPanel.Padding = new Padding(0, 8, 0, 0);
            previewLayout.Controls.Add(actionPanel, 0, 2);

            Button crystalPreviewButton = CreateButton("Preview Crystal", Color.FromArgb(52, 145, 71), Color.White);
            crystalPreviewButton.Width = 140;
            crystalPreviewButton.Height = 34;
            crystalPreviewButton.Click += PreviewCurrentCrystalButton_Click;
            actionPanel.Controls.Add(crystalPreviewButton);

            Button exportTxtButton = CreateButton("Export TXT", Color.FromArgb(86, 96, 109), Color.White);
            exportTxtButton.Width = 120;
            exportTxtButton.Height = 34;
            exportTxtButton.Click += ExportReportTxtButton_Click;
            actionPanel.Controls.Add(exportTxtButton);

            GenerateReport(ReportKind.Sales);
        }

        private void AddReportMenuButton(FlowLayoutPanel parent, string text, ReportKind reportKind)
        {
            Button button = CreateButton(text, Color.FromArgb(30, 112, 184), Color.White);
            button.Width = 300;
            button.Height = 38;
            button.Margin = new Padding(0, 4, 0, 4);
            button.Click += delegate { GenerateReport(reportKind); };
            parent.Controls.Add(button);
        }

        private void BuildSettingsView()
        {
            contentPanel.Controls.Clear();

            TableLayoutPanel page = new TableLayoutPanel();
            page.Dock = DockStyle.Fill;
            page.RowCount = 3;
            page.ColumnCount = 1;
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            contentPanel.Controls.Add(page);

            Panel center = new Panel();
            center.Width = 540;
            center.Height = 210;
            center.Anchor = AnchorStyles.None;
            page.Controls.Add(center, 0, 1);

            GroupBox accountBox = new GroupBox();
            accountBox.Text = "Akun";
            accountBox.SetBounds(0, 0, 540, 116);
            center.Controls.Add(accountBox);

            Label usernameLabel = CreateFixedLabel("Username");
            usernameLabel.SetBounds(14, 30, 110, 26);
            accountBox.Controls.Add(usernameLabel);

            accountUsernameTextBox = new TextBox();
            accountUsernameTextBox.SetBounds(130, 30, 390, 26);
            accountUsernameTextBox.Text = currentUsername;
            accountBox.Controls.Add(accountUsernameTextBox);

            Label passwordLabel = CreateFixedLabel("Password Baru");
            passwordLabel.SetBounds(14, 70, 110, 26);
            accountBox.Controls.Add(passwordLabel);

            accountPasswordTextBox = new TextBox();
            accountPasswordTextBox.SetBounds(130, 70, 390, 26);
            accountPasswordTextBox.PasswordChar = '*';
            accountBox.Controls.Add(accountPasswordTextBox);

            Button updateButton = CreateButton("Update Akun", Color.FromArgb(224, 0, 0), Color.White);
            updateButton.SetBounds(0, 130, 260, 40);
            updateButton.Click += UpdateAccountButton_Click;
            center.Controls.Add(updateButton);

            Button logoutButton = CreateButton("Keluar", Color.FromArgb(82, 58, 209), Color.White);
            logoutButton.SetBounds(280, 130, 260, 40);
            logoutButton.Click += delegate
            {
                currentUsername = null;
                currentUserId = 0;
                cartItems.Clear();
                BuildLoginView();
            };
            center.Controls.Add(logoutButton);
        }

        private void ProductSearchComboBox_TextUpdate(object sender, EventArgs e)
        {
            if (updatingProductSearch)
            {
                return;
            }

            string query = productSearchComboBox.Text;
            selectedQuickProduct = null;
            productNameTextBox.Clear();
            priceTextBox.Clear();
            discountTextBox.Clear();

            RefreshProductSearchItems(query);
            productSearchComboBox.Text = query;
            productSearchComboBox.SelectionStart = productSearchComboBox.Text.Length;
            productSearchComboBox.SelectionLength = 0;

            if (productSearchComboBox.Focused && productSearchComboBox.Items.Count > 0)
            {
                productSearchComboBox.DroppedDown = true;
            }
            else
            {
                productSearchComboBox.DroppedDown = false;
            }
        }

        private void ProductSearchComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchProductByQuery();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                productSearchComboBox.DroppedDown = false;
                e.SuppressKeyPress = true;
            }
        }

        private void ProductSearchComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ProductSearchItem item = productSearchComboBox.SelectedItem as ProductSearchItem;
            if (item != null)
            {
                SelectQuickProduct(item.Product);
            }
        }

        private void SearchProductByQuery()
        {
            if (string.IsNullOrWhiteSpace(productSearchComboBox.Text))
            {
                MessageBox.Show("Ketik kode atau nama produk dulu.", "Produk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ProductSearchItem selectedItem = productSearchComboBox.SelectedItem as ProductSearchItem;
                Product product = selectedItem == null ? FindBestProductMatch(productSearchComboBox.Text) : selectedItem.Product;
                if (product == null)
                {
                    MessageBox.Show("Produk tidak ditemukan.", "Produk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SelectQuickProduct(product);
            }
            catch (Exception ex)
            {
                ShowError("mencari produk", ex);
            }
        }

        private void ProductsGrid_SelectionChanged(object sender, EventArgs e)
        {
            Product product = GetSelectedProduct();
            if (product != null)
            {
                SelectQuickProduct(product);
            }
        }

        private void ProductsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                AddToCartButton_Click(sender, EventArgs.Empty);
            }
        }

        private void SelectQuickProduct(Product product)
        {
            selectedQuickProduct = product;
            if (productSearchComboBox != null)
            {
                updatingProductSearch = true;
                productSearchComboBox.Text = ProductSearchItem.GetDisplayText(product);
                productSearchComboBox.SelectionStart = productSearchComboBox.Text.Length;
                productSearchComboBox.SelectionLength = 0;
                productSearchComboBox.DroppedDown = false;
                updatingProductSearch = false;
            }

            productNameTextBox.Text = product.Name;
            priceTextBox.Text = FormatCurrency(product.FinalPrice);
            discountTextBox.Text = product.DiscountPercent.ToString("0.##", culture) + "% dari " + FormatCurrency(product.Price);
            qtyInput.Maximum = Math.Max(1, product.Stock);
            qtyInput.Value = 1;
        }

        private void AddToCartButton_Click(object sender, EventArgs e)
        {
            if (selectedQuickProduct == null)
            {
                if (!string.IsNullOrWhiteSpace(productSearchComboBox.Text))
                {
                    SearchProductByQuery();
                }

                if (selectedQuickProduct == null)
                {
                    MessageBox.Show("Pilih produk dulu.", "Keranjang", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            int qty = Convert.ToInt32(qtyInput.Value);
            CartItem existing = cartItems.FirstOrDefault(item => item.Product.ProductId == selectedQuickProduct.ProductId);
            int currentQty = existing == null ? 0 : existing.Qty;
            if (currentQty + qty > selectedQuickProduct.Stock)
            {
                MessageBox.Show("Qty melebihi stok produk.", "Keranjang", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (existing == null)
            {
                cartItems.Add(new CartItem { Product = CloneProduct(selectedQuickProduct), Qty = qty });
            }
            else
            {
                existing.Qty += qty;
            }

            RefreshCartGrid();
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            using (ProductEditorForm editor = new ProductEditorForm(null))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    database.AddProduct(editor.ResultProduct);
                    RefreshProducts();
                }
                catch (Exception ex)
                {
                    ShowError("menambah produk", ex);
                }
            }
        }

        private void EditProductButton_Click(object sender, EventArgs e)
        {
            Product product = GetSelectedProduct();
            if (product == null)
            {
                MessageBox.Show("Pilih produk yang mau diedit.", "Produk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (ProductEditorForm editor = new ProductEditorForm(CloneProduct(product)))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    database.UpdateProduct(editor.ResultProduct);
                    RefreshProducts();
                    SyncCartWithLatestProducts();
                    RefreshCartGrid();
                }
                catch (Exception ex)
                {
                    ShowError("mengedit produk", ex);
                }
            }
        }

        private void DeleteProductButton_Click(object sender, EventArgs e)
        {
            Product product = GetSelectedProduct();
            if (product == null)
            {
                MessageBox.Show("Pilih produk yang mau dihapus.", "Produk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show("Hapus produk " + product.Name + "?", "Produk", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                database.DeleteProduct(product.ProductId);
                cartItems.RemoveAll(item => item.Product.ProductId == product.ProductId);
                RefreshProducts();
                RefreshCartGrid();
            }
            catch (Exception ex)
            {
                ShowError("menghapus produk", ex);
            }
        }

        private void SaveTransactionButton_Click(object sender, EventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Keranjang masih kosong.", "Transaksi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal paid = ParseMoney(paidTextBox.Text);
            if (paid < GetCartTotal())
            {
                MessageBox.Show("Uang diterima belum cukup.", "Transaksi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int transactionId = database.SaveTransaction(cartItems.Select(CloneCartItem).ToList(), Money.Round(paid));
                DialogResult printConfirm = MessageBox.Show(
                    "Transaksi tersimpan. ID: " + transactionId + "\r\n\r\nCetak struk sekarang?",
                    "Transaksi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (printConfirm == DialogResult.Yes)
                {
                    SalesTransaction transaction = database.GetTransaction(transactionId);
                    List<SaleItem> items = database.GetTransactionItems(transactionId);
                    if (transaction != null)
                    {
                        ReceiptReportService.PrintReceipt(transaction, items, culture);
                    }
                }

                cartItems.Clear();
                paidTextBox.Clear();
                RefreshProducts();
                RefreshCartGrid();
            }
            catch (Exception ex)
            {
                ShowError("menyimpan transaksi", ex);
            }
        }

        private void CartGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || cartGrid.Columns[e.ColumnIndex].Name != "Remove")
            {
                return;
            }

            if (e.RowIndex < cartItems.Count)
            {
                cartItems.RemoveAt(e.RowIndex);
                RefreshCartGrid();
            }
        }

        private void RefreshProducts()
        {
            try
            {
                products = database.GetProducts();
                productsGrid.DataSource = null;
                productsGrid.DataSource = products;
                productsGrid.ClearSelection();
                RefreshProductSearchItems(productSearchComboBox == null ? string.Empty : productSearchComboBox.Text);

                if (products.Count > 0)
                {
                    productsGrid.Rows[0].Selected = true;
                    SelectQuickProduct(products[0]);
                }
                else
                {
                    selectedQuickProduct = null;
                    ClearQuickProduct();
                }
            }
            catch (Exception ex)
            {
                ShowError("memuat produk", ex);
            }
        }

        private void RefreshProductSearchItems(string query)
        {
            if (productSearchComboBox == null)
            {
                return;
            }

            updatingProductSearch = true;
            productSearchComboBox.BeginUpdate();
            try
            {
                productSearchComboBox.Items.Clear();

                IEnumerable<Product> filteredProducts = products;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    filteredProducts = products.Where(product => ProductMatchesSearch(product, query));
                }

                foreach (Product product in filteredProducts.Take(30))
                {
                    productSearchComboBox.Items.Add(new ProductSearchItem(product));
                }
            }
            finally
            {
                productSearchComboBox.EndUpdate();
                updatingProductSearch = false;
            }
        }

        private Product FindBestProductMatch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string normalizedQuery = query.Trim();
            Product exactMatch = products.FirstOrDefault(product =>
                string.Equals(product.Code, normalizedQuery, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(product.Name, normalizedQuery, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(ProductSearchItem.GetDisplayText(product), normalizedQuery, StringComparison.CurrentCultureIgnoreCase));

            if (exactMatch != null)
            {
                return exactMatch;
            }

            return products.FirstOrDefault(product => ProductMatchesSearch(product, normalizedQuery));
        }

        private static bool ProductMatchesSearch(Product product, string query)
        {
            string normalizedQuery = query.Trim();
            return (product.Code ?? string.Empty).IndexOf(normalizedQuery, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   (product.Name ?? string.Empty).IndexOf(normalizedQuery, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void SyncCartWithLatestProducts()
        {
            foreach (CartItem item in cartItems)
            {
                Product latest = products.FirstOrDefault(product => product.ProductId == item.Product.ProductId);
                if (latest != null)
                {
                    item.Product = CloneProduct(latest);
                    if (item.Qty > latest.Stock)
                    {
                        item.Qty = latest.Stock;
                    }
                }
            }

            cartItems.RemoveAll(item => item.Qty <= 0);
        }

        private void RefreshCartGrid()
        {
            if (cartGrid == null)
            {
                return;
            }

            List<CartRow> rows = new List<CartRow>();
            for (int i = 0; i < cartItems.Count; i++)
            {
                CartItem item = cartItems[i];
                rows.Add(new CartRow
                {
                    No = i + 1,
                    Name = item.Product.Name,
                    Qty = item.Qty,
                    OriginalPrice = FormatCurrency(item.Product.Price),
                    Discount = item.Product.DiscountPercent.ToString("0.##", culture) + "%",
                    FinalPrice = FormatCurrency(item.Product.FinalPrice),
                    Subtotal = FormatCurrency(item.Subtotal)
                });
            }

            cartGrid.DataSource = null;
            cartGrid.DataSource = rows;
            UpdateTotal();
            UpdateChange();
        }

        private void RefreshHistoryList()
        {
            try
            {
                historyTransactions = database.GetTransactions();
                ApplyHistorySearch();
            }
            catch (Exception ex)
            {
                ShowError("memuat histori", ex);
            }
        }

        private void HistorySearchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyHistorySearch();
        }

        private void ApplyHistorySearch()
        {
            if (historyListBox == null)
            {
                return;
            }

            int selectedTransactionId = 0;
            HistoryListItem currentItem = historyListBox.SelectedItem as HistoryListItem;
            if (currentItem != null)
            {
                selectedTransactionId = currentItem.Transaction.TransactionId;
            }

            string query = historySearchTextBox == null ? string.Empty : historySearchTextBox.Text.Trim();
            List<HistoryListItem> items = historyTransactions
                .Where(transaction => HistoryMatchesSearch(transaction, query))
                .Select(transaction => new HistoryListItem(transaction, FormatCurrency(transaction.TotalAmount)))
                .ToList();

            historyListBox.DataSource = null;
            historyListBox.DataSource = items;

            if (items.Count == 0)
            {
                historyTotalLabel.Text = "Total : " + FormatCurrency(0);
                historyItemsGrid.DataSource = null;
                return;
            }

            int selectedIndex = items.FindIndex(item => item.Transaction.TransactionId == selectedTransactionId);
            historyListBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private bool HistoryMatchesSearch(SalesTransaction transaction, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            int month;
            int year;
            bool hasMonth;
            bool hasYear;
            if (!TryParseHistoryMonthYear(query, out month, out year, out hasMonth, out hasYear))
            {
                return false;
            }

            return (!hasMonth || transaction.TransactionDate.Month == month) &&
                   (!hasYear || transaction.TransactionDate.Year == year);
        }

        private bool TryParseHistoryMonthYear(string query, out int month, out int year, out bool hasMonth, out bool hasYear)
        {
            month = 0;
            year = 0;
            hasMonth = false;
            hasYear = false;

            string normalized = (query ?? string.Empty).Trim().ToLower(culture);
            if (normalized.Length == 0)
            {
                return false;
            }

            char[] separators = { ' ', '/', '\\', '-', '.', ',', ';', ':' };
            string[] parts = normalized.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int number;
                if (int.TryParse(part, out number))
                {
                    if (part.Length == 4 && number >= 1900 && number <= 2100)
                    {
                        year = number;
                        hasYear = true;
                    }
                    else if (number >= 1 && number <= 12 && !hasMonth)
                    {
                        month = number;
                        hasMonth = true;
                    }

                    continue;
                }

                int parsedMonth = ParseMonthName(part);
                if (parsedMonth > 0)
                {
                    month = parsedMonth;
                    hasMonth = true;
                }
            }

            return hasMonth || hasYear;
        }

        private int ParseMonthName(string value)
        {
            string[] monthNames =
            {
                "januari|jan|january",
                "februari|feb|february",
                "maret|mar|march",
                "april|apr",
                "mei|may",
                "juni|jun|june",
                "juli|jul|july",
                "agustus|agu|aug|august",
                "september|sep|sept",
                "oktober|okt|oct|october",
                "november|nov",
                "desember|des|dec|december"
            };

            for (int i = 0; i < monthNames.Length; i++)
            {
                string[] aliases = monthNames[i].Split('|');
                foreach (string alias in aliases)
                {
                    if (alias.StartsWith(value, StringComparison.CurrentCultureIgnoreCase) ||
                        value.StartsWith(alias, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i + 1;
                    }
                }
            }

            return 0;
        }

        private void HistoryListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            HistoryListItem selectedHistory = historyListBox.SelectedItem as HistoryListItem;
            SalesTransaction transaction = selectedHistory == null ? null : selectedHistory.Transaction;
            if (transaction == null)
            {
                return;
            }

            try
            {
                historyTotalLabel.Text = "Total : " + FormatCurrency(transaction.TotalAmount);
                List<SaleItem> items = database.GetTransactionItems(transaction.TransactionId);
                List<SaleRow> rows = new List<SaleRow>();
                for (int i = 0; i < items.Count; i++)
                {
                    SaleItem saleItem = items[i];
                    rows.Add(new SaleRow
                    {
                        No = i + 1,
                        Name = saleItem.ProductName,
                        Qty = saleItem.Qty,
                        OriginalPrice = FormatCurrency(saleItem.OriginalPrice),
                        Discount = saleItem.DiscountPercent.ToString("0.##", culture) + "%",
                        FinalPrice = FormatCurrency(saleItem.FinalPrice),
                        Subtotal = FormatCurrency(saleItem.Subtotal)
                    });
                }

                historyItemsGrid.DataSource = null;
                historyItemsGrid.DataSource = rows;
            }
            catch (Exception ex)
            {
                ShowError("memuat detail histori", ex);
            }
        }

        private void DeleteHistoryButton_Click(object sender, EventArgs e)
        {
            SalesTransaction transaction = GetSelectedHistoryTransaction();
            if (transaction == null)
            {
                MessageBox.Show("Pilih transaksi yang mau dihapus.", "Histori", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show("Hapus transaksi " + transaction.InvoiceNo + "?", "Histori", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                database.DeleteTransaction(transaction.TransactionId);
                RefreshHistoryList();
            }
            catch (Exception ex)
            {
                ShowError("menghapus histori", ex);
            }
        }

        private void PrintHistoryButton_Click(object sender, EventArgs e)
        {
            SalesTransaction transaction = GetSelectedHistoryTransaction();
            if (transaction == null)
            {
                MessageBox.Show("Pilih transaksi yang mau dicetak.", "Histori", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ReceiptReportService.PrintReceipt(transaction, database.GetTransactionItems(transaction.TransactionId), culture);
            }
            catch (Exception ex)
            {
                ShowError("mencetak struk", ex);
            }
        }

        private void ExportHistoryPdfButton_Click(object sender, EventArgs e)
        {
            SalesTransaction transaction = GetSelectedHistoryTransaction();
            if (transaction == null)
            {
                MessageBox.Show("Pilih transaksi yang mau diexport.", "Histori", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Export Struk ke PDF";
                dialog.Filter = "PDF File (*.pdf)|*.pdf";
                dialog.FileName = "Struk-" + transaction.InvoiceNo + ".pdf";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    ReceiptReportService.ExportReceiptPdf(dialog.FileName, transaction, database.GetTransactionItems(transaction.TransactionId), culture);
                    MessageBox.Show("PDF berhasil dibuat:\r\n" + dialog.FileName, "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("export PDF", ex);
                }
            }
        }

        private void GenerateReport(ReportKind reportKind)
        {
            if (reportStartDatePicker != null && reportEndDatePicker != null &&
                reportStartDatePicker.Checked && reportEndDatePicker.Checked &&
                reportEndDatePicker.Value.Date < reportStartDatePicker.Value.Date)
            {
                MessageBox.Show("Tanggal akhir tidak boleh lebih kecil dari tanggal awal.", "Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime? startDate = reportStartDatePicker != null && reportStartDatePicker.Checked
                ? (DateTime?)reportStartDatePicker.Value.Date
                : null;
            DateTime? endDate = reportEndDatePicker != null && reportEndDatePicker.Checked
                ? (DateTime?)reportEndDatePicker.Value.Date
                : null;
            string keyword = reportKeywordTextBox == null ? string.Empty : reportKeywordTextBox.Text.Trim();

            try
            {
                currentReportKind = reportKind;
                currentReportData = database.GetReportData(reportKind, startDate, endDate, keyword);

                reportGrid.DataSource = null;
                reportGrid.DataSource = currentReportData;

                string crystalStatus = CrystalReportService.IsCrystalRuntimeAvailable()
                    ? "Crystal runtime siap"
                    : "Crystal runtime belum terpasang";
                reportInfoLabel.Text =
                    CrystalReportService.GetReportTitle(reportKind) + "\r\n" +
                    currentReportData.Rows.Count.ToString(culture) + " baris data | " +
                    GetReportFilterText(startDate, endDate, keyword) + " | " +
                    crystalStatus;
            }
            catch (Exception ex)
            {
                ShowError("membuat report", ex);
            }
        }

        private void PreviewCurrentCrystalButton_Click(object sender, EventArgs e)
        {
            if (currentReportData == null)
            {
                GenerateReport(currentReportKind);
            }

            if (currentReportData == null)
            {
                return;
            }

            try
            {
                CrystalReportService.ShowReport(this, currentReportKind, currentReportData);
            }
            catch (Exception ex)
            {
                ShowError("membuka Crystal Report", ex);
            }
        }

        private void ExportReportTxtButton_Click(object sender, EventArgs e)
        {
            if (currentReportData == null)
            {
                GenerateReport(currentReportKind);
            }

            if (currentReportData == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Export Report ke TXT";
                dialog.Filter = "Text File (*.txt)|*.txt";
                dialog.FileName = CrystalReportService.GetTemplateFileName(currentReportKind).Replace(".rpt", ".txt");

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    WriteReportTextFile(dialog.FileName, CrystalReportService.GetReportTitle(currentReportKind), currentReportData);
                    MessageBox.Show("TXT berhasil dibuat:\r\n" + dialog.FileName, "Export TXT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowError("export TXT", ex);
                }
            }
        }

        private void WriteReportTextFile(string path, string title, DataTable table)
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(title);
                writer.WriteLine("Dibuat: " + DateTime.Now.ToString("dd MMMM yyyy HH:mm", culture));
                writer.WriteLine("Jumlah Data: " + table.Rows.Count.ToString(culture));
                writer.WriteLine();

                List<string> headers = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    headers.Add(column.ColumnName);
                }

                writer.WriteLine(string.Join("\t", headers));

                foreach (DataRow row in table.Rows)
                {
                    List<string> values = new List<string>();
                    foreach (DataColumn column in table.Columns)
                    {
                        values.Add(EscapeTextFileValue(row[column]));
                    }

                    writer.WriteLine(string.Join("\t", values));
                }
            }
        }

        private string EscapeTextFileValue(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            DateTime dateTime;
            if (DateTime.TryParse(Convert.ToString(value, culture), out dateTime) && value is DateTime)
            {
                return dateTime.ToString("dd MMM yyyy HH:mm", culture);
            }

            return Convert.ToString(value, culture)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
        }

        private string GetReportFilterText(DateTime? startDate, DateTime? endDate, string keyword)
        {
            List<string> filters = new List<string>();
            if (startDate.HasValue)
            {
                filters.Add("dari " + startDate.Value.ToString("dd MMM yyyy", culture));
            }

            if (endDate.HasValue)
            {
                filters.Add("sampai " + endDate.Value.ToString("dd MMM yyyy", culture));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filters.Add("keyword " + keyword);
            }

            return filters.Count == 0 ? "tanpa filter" : string.Join(", ", filters);
        }

        private void UpdateAccountButton_Click(object sender, EventArgs e)
        {
            string username = accountUsernameTextBox.Text.Trim();
            string password = accountPasswordTextBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username dan password baru wajib diisi.", "Akun", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                database.UpdateAccount(currentUserId, username, password);
                currentUsername = username;
                accountPasswordTextBox.Clear();
                MessageBox.Show("Akun berhasil diperbarui.", "Akun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("mengupdate akun", ex);
            }
        }

        private Product GetSelectedProduct()
        {
            if (productsGrid == null || productsGrid.CurrentRow == null)
            {
                return null;
            }

            return productsGrid.CurrentRow.DataBoundItem as Product;
        }

        private SalesTransaction GetSelectedHistoryTransaction()
        {
            if (historyListBox == null)
            {
                return null;
            }

            HistoryListItem selectedHistory = historyListBox.SelectedItem as HistoryListItem;
            return selectedHistory == null ? null : selectedHistory.Transaction;
        }

        private void ClearQuickProduct()
        {
            if (productSearchComboBox == null)
            {
                return;
            }

            updatingProductSearch = true;
            productSearchComboBox.Items.Clear();
            productSearchComboBox.Text = string.Empty;
            updatingProductSearch = false;
            productNameTextBox.Clear();
            priceTextBox.Clear();
            discountTextBox.Clear();
            qtyInput.Value = 1;
        }

        private void UpdateTotal()
        {
            if (totalLabel != null)
            {
                totalLabel.Text = "Total : " + FormatCurrency(GetCartTotal());
            }
        }

        private void UpdateChange()
        {
            if (changeTextBox == null)
            {
                return;
            }

            decimal paid = ParseMoney(paidTextBox == null ? string.Empty : paidTextBox.Text);
            decimal change = paid - GetCartTotal();
            changeTextBox.Text = change < 0 ? FormatCurrency(0) : FormatCurrency(change);
        }

        private decimal GetCartTotal()
        {
            decimal total = 0m;
            foreach (CartItem item in cartItems)
            {
                total += item.Subtotal;
            }

            return Money.Round(total);
        }

        private void ConfigureProductsGrid()
        {
            productsGrid.AutoGenerateColumns = false;
            productsGrid.Columns.Clear();
            productsGrid.Columns.Add(CreateTextColumn("Code", "Kode", "Code", 84));
            productsGrid.Columns.Add(CreateTextColumn("Name", "Nama Produk", "Name", 150));
            productsGrid.Columns.Add(CreateTextColumn("Stock", "Stok", "Stock", 56));
            productsGrid.Columns.Add(CreateTextColumn("Price", "Harga Asli", "Price", 92));
            productsGrid.Columns.Add(CreateTextColumn("DiscountPercent", "Diskon", "DiscountPercent", 70));
            productsGrid.Columns.Add(CreateTextColumn("FinalPrice", "Harga Diskon", "FinalPrice", 104));
            productsGrid.CellFormatting += ProductsGrid_CellFormatting;
        }

        private void ProductsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null)
            {
                return;
            }

            string columnName = productsGrid.Columns[e.ColumnIndex].Name;
            if (columnName == "Price" || columnName == "FinalPrice")
            {
                e.Value = FormatCurrency(Convert.ToDecimal(e.Value));
                e.FormattingApplied = true;
            }
            else if (columnName == "DiscountPercent")
            {
                e.Value = Convert.ToDecimal(e.Value).ToString("0.##", culture) + "%";
                e.FormattingApplied = true;
            }
        }

        private void ConfigureCartGrid()
        {
            cartGrid.AutoGenerateColumns = false;
            cartGrid.Columns.Clear();
            cartGrid.Columns.Add(CreateTextColumn("No", "No.", "No", 44));
            cartGrid.Columns.Add(CreateTextColumn("Name", "Nama Produk", "Name", 160));
            cartGrid.Columns.Add(CreateTextColumn("Qty", "Qty", "Qty", 48));
            cartGrid.Columns.Add(CreateTextColumn("OriginalPrice", "Harga Asli", "OriginalPrice", 96));
            cartGrid.Columns.Add(CreateTextColumn("Discount", "Diskon", "Discount", 70));
            cartGrid.Columns.Add(CreateTextColumn("FinalPrice", "Harga Diskon", "FinalPrice", 106));
            cartGrid.Columns.Add(CreateTextColumn("Subtotal", "Subtotal", "Subtotal", 96));

            DataGridViewButtonColumn removeColumn = new DataGridViewButtonColumn();
            removeColumn.Name = "Remove";
            removeColumn.HeaderText = "";
            removeColumn.Text = "X";
            removeColumn.UseColumnTextForButtonValue = true;
            removeColumn.Width = 42;
            cartGrid.Columns.Add(removeColumn);
        }

        private void ConfigureSaleItemsGrid(DataGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.Columns.Add(CreateTextColumn("No", "No.", "No", 44));
            grid.Columns.Add(CreateTextColumn("Name", "Nama Produk", "Name", 180));
            grid.Columns.Add(CreateTextColumn("Qty", "Qty", "Qty", 48));
            grid.Columns.Add(CreateTextColumn("OriginalPrice", "Harga Asli", "OriginalPrice", 100));
            grid.Columns.Add(CreateTextColumn("Discount", "Diskon", "Discount", 72));
            grid.Columns.Add(CreateTextColumn("FinalPrice", "Harga Diskon", "FinalPrice", 110));
            grid.Columns.Add(CreateTextColumn("Subtotal", "Subtotal", "Subtotal", 100));
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, string property, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.DataPropertyName = property;
            column.Width = width;
            return column;
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            return grid;
        }

        private static SplitContainer CreateResponsiveSplit(int preferredDistance)
        {
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Panel1MinSize = 100;
            split.Panel2MinSize = 100;

            split.SizeChanged += delegate { ApplySplitterDistance(split, preferredDistance); };
            split.HandleCreated += delegate
            {
                split.BeginInvoke(new Action(delegate { ApplySplitterDistance(split, preferredDistance); }));
            };

            return split;
        }

        private static void ApplySplitterDistance(SplitContainer split, int preferredDistance)
        {
            if (split.IsDisposed || split.Width <= split.SplitterWidth)
            {
                return;
            }

            int minDistance = split.Panel1MinSize;
            int maxDistance = split.Width - split.SplitterWidth - split.Panel2MinSize;
            if (maxDistance < minDistance)
            {
                return;
            }

            int distance = Math.Max(minDistance, Math.Min(preferredDistance, maxDistance));
            if (split.SplitterDistance != distance)
            {
                split.SplitterDistance = distance;
            }
        }

        private static Label CreateLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static Label CreateFixedLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static TextBox CreateReadOnlyTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.ReadOnly = true;
            textBox.BackColor = Color.FromArgb(238, 238, 238);
            return textBox;
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            Button button = new Button();
            button.Text = text;
            button.Height = 30;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static Button CreateNavButton(string text, MainTab tab, bool selected)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 190;
            button.Height = 48;
            button.Top = 0;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = selected ? Color.FromArgb(32, 36, 41) : Color.FromArgb(63, 68, 75);
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 10F);
            button.Image = CreateNavIcon(tab, Color.Gainsboro);
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Padding = new Padding(18, 0, 0, 0);
            return button;
        }

        private static Image CreateNavIcon(MainTab tab, Color color)
        {
            Bitmap bitmap = new Bitmap(22, 22);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Pen pen = new Pen(color, 2F))
            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                if (tab == MainTab.Cashier)
                {
                    Point[] roof =
                    {
                        new Point(4, 11),
                        new Point(11, 5),
                        new Point(18, 11)
                    };
                    graphics.DrawLines(pen, roof);
                    graphics.DrawRectangle(pen, 6, 11, 10, 7);
                    graphics.FillRectangle(brush, 10, 14, 3, 5);
                }
                else if (tab == MainTab.History)
                {
                    graphics.DrawEllipse(pen, 4, 4, 14, 14);
                    graphics.DrawLine(pen, 11, 11, 11, 7);
                    graphics.DrawLine(pen, 11, 11, 15, 13);
                    graphics.DrawArc(pen, 3, 3, 16, 16, 205, 70);
                }
                else if (tab == MainTab.Reports)
                {
                    graphics.DrawRectangle(pen, 5, 4, 12, 15);
                    graphics.DrawLine(pen, 8, 15, 8, 11);
                    graphics.DrawLine(pen, 11, 15, 11, 8);
                    graphics.DrawLine(pen, 14, 15, 14, 10);
                    graphics.DrawLine(pen, 7, 7, 15, 7);
                }
                else
                {
                    graphics.DrawEllipse(pen, 7, 7, 8, 8);
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4D;
                        int x1 = 11 + (int)Math.Round(Math.Cos(angle) * 6D);
                        int y1 = 11 + (int)Math.Round(Math.Sin(angle) * 6D);
                        int x2 = 11 + (int)Math.Round(Math.Cos(angle) * 9D);
                        int y2 = 11 + (int)Math.Round(Math.Sin(angle) * 9D);
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }

            return bitmap;
        }

        private Label CreateFooter()
        {
            Label footer = new Label();
            footer.Text = "Copyright (c) 2026 - All Rights Reserved";
            footer.Dock = DockStyle.Bottom;
            footer.Height = 24;
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.Font = new Font("Segoe UI", 7F);
            footer.ForeColor = Color.FromArgb(40, 40, 40);
            return footer;
        }

        private string FormatCurrency(decimal value)
        {
            return string.Format(culture, "{0:C0}", Money.Round(value));
        }

        private decimal ParseMoney(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0m;
            }

            StringBuilder digits = new StringBuilder();
            foreach (char value in text)
            {
                if (char.IsDigit(value))
                {
                    digits.Append(value);
                }
            }

            decimal result;
            return decimal.TryParse(digits.ToString(), out result) ? result : 0m;
        }

        private void FormatMoneyTextBox(TextBox textBox)
        {
            decimal value = ParseMoney(textBox.Text);
            textBox.Text = value <= 0 ? string.Empty : FormatCurrency(value);
        }

        private void ShowError(string action, Exception ex)
        {
            SqlException sqlException = ex as SqlException;
            if (sqlException != null && (sqlException.Number == 2601 || sqlException.Number == 2627))
            {
                MessageBox.Show("Kode atau username sudah dipakai.", "Gagal " + action, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Gagal " + action + ".\r\n\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static Product CloneProduct(Product product)
        {
            return new Product
            {
                ProductId = product.ProductId,
                Code = product.Code,
                Name = product.Name,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                Stock = product.Stock
            };
        }

        private static CartItem CloneCartItem(CartItem item)
        {
            return new CartItem
            {
                Product = CloneProduct(item.Product),
                Qty = item.Qty
            };
        }

        private enum MainTab
        {
            Cashier,
            History,
            Reports,
            Settings
        }

        private sealed class ProductSearchItem
        {
            public ProductSearchItem(Product product)
            {
                Product = product;
            }

            public Product Product { get; private set; }

            public static string GetDisplayText(Product product)
            {
                if (string.IsNullOrWhiteSpace(product.Code))
                {
                    return product.Name;
                }

                return product.Code + " - " + product.Name;
            }

            public override string ToString()
            {
                return GetDisplayText(Product);
            }
        }

        private sealed class HistoryListItem
        {
            private readonly string totalText;

            public HistoryListItem(SalesTransaction transaction, string totalText)
            {
                Transaction = transaction;
                this.totalText = totalText;
            }

            public SalesTransaction Transaction { get; private set; }

            public override string ToString()
            {
                return Transaction.InvoiceNo + " | " +
                       Transaction.TransactionDate.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID")) +
                       " | " + totalText;
            }
        }

        private sealed class CartRow
        {
            public int No { get; set; }
            public string Name { get; set; }
            public int Qty { get; set; }
            public string OriginalPrice { get; set; }
            public string Discount { get; set; }
            public string FinalPrice { get; set; }
            public string Subtotal { get; set; }
        }

        private sealed class SaleRow
        {
            public int No { get; set; }
            public string Name { get; set; }
            public int Qty { get; set; }
            public string OriginalPrice { get; set; }
            public string Discount { get; set; }
            public string FinalPrice { get; set; }
            public string Subtotal { get; set; }
        }
    }
}
