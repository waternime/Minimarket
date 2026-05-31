# Program Kasir Minimarket

Aplikasi kasir minimarket berbasis C# Windows Forms dan Microsoft SQL Server.

## Fitur

- Login akun pengguna.
- Kelola produk: tambah, edit, hapus, stok, harga, dan diskon.
- Live search produk berdasarkan kode atau nama produk.
- Keranjang transaksi, total otomatis, uang diterima, dan kembalian.
- Histori transaksi dengan filter bulan/tahun.
- Cetak struk dan export struk transaksi ke PDF.
- Hapus transaksi histori yang dipilih.
- Tab Laporan Crystal dengan 4 menu report: penjualan, transaksi, user, dan inventory.
- Export data report ke file TXT.

## Teknologi

- C# Windows Forms
- .NET Framework 4.7.2
- Microsoft SQL Server Express
- ADO.NET / System.Data.SqlClient
- SAP Crystal Reports for Visual Studio

## Database

Default connection string ada di `Minimarket/App.config`:

```text
Data Source=.\SQLEXPRESS;Initial Catalog=MinimarketDb;Integrated Security=True
```

Jika nama SQL Server berbeda, ubah bagian `Data Source`.

Database bisa dibuat otomatis saat aplikasi pertama dibuka, atau manual dengan menjalankan:

```text
setup-sqlserver.sql
```

Default login:

```text
Username: admin
Password: admin123
```

## Cara Menjalankan

1. Buka `Minimarket.slnx` atau `Minimarket/Minimarket.csproj` di Visual Studio.
2. Pastikan SQL Server Express berjalan.
3. Jalankan `setup-sqlserver.sql` jika database belum ada.
4. Build dan run project.

## Crystal Report

Aplikasi sudah menyiapkan menu dan kode preview untuk Crystal Report. Agar tombol `Preview Crystal` berjalan di komputer lain, install SAP Crystal Reports for Visual Studio lalu buat/simpan template berikut di folder `Minimarket/Reports`:

```text
SalesReport.rpt
TransactionReport.rpt
UserReport.rpt
InventoryReport.rpt
```

Jika Crystal runtime atau file `.rpt` belum ada, aplikasi tetap bisa menampilkan preview data report di grid dan akan memberi pesan bagian mana yang belum tersedia.
