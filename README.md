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

## Teknologi

- C# Windows Forms
- .NET Framework 4.7.2
- Microsoft SQL Server Express
- ADO.NET / System.Data.SqlClient

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
