using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Minimarket
{
    internal static class ReceiptReportService
    {
        public static void PrintReceipt(SalesTransaction transaction, List<SaleItem> items, CultureInfo culture)
        {
            List<string> lines = BuildReceiptLines(transaction, items, culture);
            int lineIndex = 0;

            using (PrintDocument document = new PrintDocument())
            using (Font font = new Font("Consolas", 9F))
            using (PrintDialog dialog = new PrintDialog())
            {
                document.DocumentName = "Struk " + transaction.InvoiceNo;
                document.PrintPage += delegate(object sender, PrintPageEventArgs e)
                {
                    float y = e.MarginBounds.Top;
                    float lineHeight = font.GetHeight(e.Graphics) + 3F;

                    while (lineIndex < lines.Count)
                    {
                        e.Graphics.DrawString(lines[lineIndex], font, Brushes.Black, e.MarginBounds.Left, y);
                        y += lineHeight;
                        lineIndex++;

                        if (y + lineHeight > e.MarginBounds.Bottom)
                        {
                            e.HasMorePages = true;
                            return;
                        }
                    }

                    e.HasMorePages = false;
                };

                dialog.Document = document;
                dialog.UseEXDialog = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    document.Print();
                }
            }
        }

        public static void ExportReceiptPdf(string path, SalesTransaction transaction, List<SaleItem> items, CultureInfo culture)
        {
            List<string> lines = BuildReceiptLines(transaction, items, culture);
            SimplePdfWriter.WriteTextPdf(path, lines);
        }

        private static List<string> BuildReceiptLines(SalesTransaction transaction, List<SaleItem> items, CultureInfo culture)
        {
            List<string> lines = new List<string>();
            lines.Add("PROGRAM KASIR MINIMARKET");
            lines.Add("========================================");
            lines.Add("ID Transaksi : " + transaction.InvoiceNo);
            lines.Add("Tanggal      : " + transaction.TransactionDate.ToString("dd MMMM yyyy HH:mm", culture));
            lines.Add("----------------------------------------");
            lines.Add("Produk                 Qty Harga   Sub");
            lines.Add("----------------------------------------");

            foreach (SaleItem item in items)
            {
                string name = item.ProductName;
                if (name.Length > 20)
                {
                    name = name.Substring(0, 20);
                }

                lines.Add(
                    name.PadRight(20) + " " +
                    item.Qty.ToString(culture).PadLeft(3) + " " +
                    FormatCurrency(item.FinalPrice, culture).PadLeft(7) + " " +
                    FormatCurrency(item.Subtotal, culture).PadLeft(8));

                if (item.DiscountPercent > 0)
                {
                    lines.Add("  Diskon " + item.DiscountPercent.ToString("0.##", culture) + "% dari " + FormatCurrency(item.OriginalPrice, culture));
                }
            }

            lines.Add("----------------------------------------");
            lines.Add("Total        : " + FormatCurrency(transaction.TotalAmount, culture));
            lines.Add("Uang Diterima: " + FormatCurrency(transaction.PaidAmount, culture));
            lines.Add("Kembalian    : " + FormatCurrency(transaction.ChangeAmount, culture));
            lines.Add("========================================");
            lines.Add("Terima kasih sudah berbelanja");
            return lines;
        }

        private static string FormatCurrency(decimal value, CultureInfo culture)
        {
            return string.Format(culture, "{0:C0}", Money.Round(value));
        }

        private static class SimplePdfWriter
        {
            public static void WriteTextPdf(string path, List<string> lines)
            {
                List<string> pdfLines = new List<string>();
                int maxLines = 46;
                int pageCount = Math.Max(1, (int)Math.Ceiling(lines.Count / (double)maxLines));

                List<byte[]> objects = new List<byte[]>();
                objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));

                StringBuilder kids = new StringBuilder();
                for (int i = 0; i < pageCount; i++)
                {
                    kids.Append(3 + (i * 2)).Append(" 0 R ");
                }
                objects.Add(Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [" + kids + "] /Count " + pageCount + " >>"));

                for (int page = 0; page < pageCount; page++)
                {
                    int pageObjectNumber = 3 + (page * 2);
                    int contentObjectNumber = pageObjectNumber + 1;
                    string pageObject = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 " + (3 + (pageCount * 2)) + " 0 R >> >> /Contents " + contentObjectNumber + " 0 R >>";
                    objects.Add(Encoding.ASCII.GetBytes(pageObject));

                    List<string> pageLines = lines.GetRange(page * maxLines, Math.Min(maxLines, lines.Count - (page * maxLines)));
                    string content = BuildContentStream(pageLines);
                    objects.Add(Encoding.ASCII.GetBytes("<< /Length " + Encoding.ASCII.GetByteCount(content) + " >>\nstream\n" + content + "\nendstream"));
                }

                objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"));

                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    WriteAscii(stream, "%PDF-1.4\n");
                    List<long> offsets = new List<long>();
                    for (int i = 0; i < objects.Count; i++)
                    {
                        offsets.Add(stream.Position);
                        WriteAscii(stream, (i + 1) + " 0 obj\n");
                        stream.Write(objects[i], 0, objects[i].Length);
                        WriteAscii(stream, "\nendobj\n");
                    }

                    long xref = stream.Position;
                    WriteAscii(stream, "xref\n0 " + (objects.Count + 1) + "\n");
                    WriteAscii(stream, "0000000000 65535 f \n");
                    foreach (long offset in offsets)
                    {
                        WriteAscii(stream, offset.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                    }

                    WriteAscii(stream, "trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
                }
            }

            private static string BuildContentStream(List<string> lines)
            {
                StringBuilder content = new StringBuilder();
                content.Append("BT\n/F1 10 Tf\n14 TL\n50 790 Td\n");
                foreach (string line in lines)
                {
                    content.Append("(").Append(EscapePdf(line)).Append(") Tj\nT*\n");
                }
                content.Append("ET");
                return content.ToString();
            }

            private static string EscapePdf(string text)
            {
                string asciiText = RemoveNonAscii(text ?? string.Empty);
                return asciiText.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }

            private static string RemoveNonAscii(string text)
            {
                StringBuilder builder = new StringBuilder(text.Length);
                foreach (char value in text)
                {
                    builder.Append(value <= 127 ? value : '?');
                }

                return builder.ToString();
            }

            private static void WriteAscii(Stream stream, string text)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
