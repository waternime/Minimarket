using System;
using System.Drawing;
using System.Windows.Forms;

namespace Minimarket
{
    internal sealed class ProductEditorForm : Form
    {
        private readonly TextBox codeTextBox;
        private readonly TextBox nameTextBox;
        private readonly NumericUpDown priceInput;
        private readonly NumericUpDown discountInput;
        private readonly NumericUpDown stockInput;
        private readonly Label finalPriceLabel;
        private readonly Product sourceProduct;

        public Product ResultProduct { get; private set; }

        public ProductEditorForm(Product product)
        {
            sourceProduct = product;

            Text = product == null ? "Tambah Produk" : "Edit Produk";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(430, 330);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 2;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++)
            {
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            }
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(CreateLabel("Kode"), 0, 0);
            codeTextBox = new TextBox();
            codeTextBox.Dock = DockStyle.Fill;
            root.Controls.Add(codeTextBox, 1, 0);

            root.Controls.Add(CreateLabel("Nama Produk"), 0, 1);
            nameTextBox = new TextBox();
            nameTextBox.Dock = DockStyle.Fill;
            root.Controls.Add(nameTextBox, 1, 1);

            root.Controls.Add(CreateLabel("Harga Asli"), 0, 2);
            priceInput = CreateMoneyInput();
            priceInput.ValueChanged += delegate { UpdateFinalPrice(); };
            root.Controls.Add(priceInput, 1, 2);

            root.Controls.Add(CreateLabel("Diskon (%)"), 0, 3);
            discountInput = new NumericUpDown();
            discountInput.Dock = DockStyle.Fill;
            discountInput.DecimalPlaces = 2;
            discountInput.Maximum = 100;
            discountInput.Increment = 1;
            discountInput.TextAlign = HorizontalAlignment.Right;
            discountInput.ValueChanged += delegate { UpdateFinalPrice(); };
            root.Controls.Add(discountInput, 1, 3);

            root.Controls.Add(CreateLabel("Stok"), 0, 4);
            stockInput = new NumericUpDown();
            stockInput.Dock = DockStyle.Fill;
            stockInput.Maximum = 1000000;
            stockInput.TextAlign = HorizontalAlignment.Right;
            root.Controls.Add(stockInput, 1, 4);

            root.Controls.Add(CreateLabel("Harga Diskon"), 0, 5);
            finalPriceLabel = new Label();
            finalPriceLabel.Dock = DockStyle.Fill;
            finalPriceLabel.TextAlign = ContentAlignment.MiddleRight;
            finalPriceLabel.Font = new Font(Font, FontStyle.Bold);
            root.Controls.Add(finalPriceLabel, 1, 5);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Padding = new Padding(0, 14, 0, 0);
            root.SetColumnSpan(buttons, 2);
            root.Controls.Add(buttons, 0, 6);

            Button saveButton = new Button();
            saveButton.Text = "Simpan";
            saveButton.Width = 120;
            saveButton.Height = 34;
            saveButton.BackColor = Color.FromArgb(46, 139, 67);
            saveButton.ForeColor = Color.White;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.Click += SaveButton_Click;
            buttons.Controls.Add(saveButton);

            Button cancelButton = new Button();
            cancelButton.Text = "Batal";
            cancelButton.Width = 100;
            cancelButton.Height = 34;
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            if (product != null)
            {
                codeTextBox.Text = product.Code;
                nameTextBox.Text = product.Name;
                priceInput.Value = Clamp(product.Price, priceInput.Minimum, priceInput.Maximum);
                discountInput.Value = Clamp(product.DiscountPercent, discountInput.Minimum, discountInput.Maximum);
                stockInput.Value = Clamp(product.Stock, stockInput.Minimum, stockInput.Maximum);
            }

            UpdateFinalPrice();
        }

        private static Label CreateLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static NumericUpDown CreateMoneyInput()
        {
            NumericUpDown input = new NumericUpDown();
            input.Dock = DockStyle.Fill;
            input.Maximum = 1000000000;
            input.ThousandsSeparator = true;
            input.TextAlign = HorizontalAlignment.Right;
            return input;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Nama produk wajib diisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameTextBox.Focus();
                return;
            }

            ResultProduct = new Product
            {
                ProductId = sourceProduct == null ? 0 : sourceProduct.ProductId,
                Code = codeTextBox.Text.Trim(),
                Name = nameTextBox.Text.Trim(),
                Price = Money.Round(priceInput.Value),
                DiscountPercent = discountInput.Value,
                Stock = Convert.ToInt32(stockInput.Value)
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateFinalPrice()
        {
            decimal finalPrice = Money.Round(priceInput.Value - (priceInput.Value * discountInput.Value / 100m));
            finalPriceLabel.Text = FormatCurrency(finalPrice);
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string FormatCurrency(decimal value)
        {
            return string.Format(new System.Globalization.CultureInfo("id-ID"), "{0:C0}", value);
        }
    }
}
