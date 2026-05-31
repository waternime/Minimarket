# Program Kasir Minimarket

Aplikasi kasir minimarket berbasis C# Windows Forms, .NET Framework 4.7.2, dan Microsoft SQL Server.

## Cara Menjalankan

1. Buka `Minimarket.slnx` atau `Minimarket/Minimarket.csproj` di Visual Studio.
2. Pastikan SQL Server Express sudah berjalan.
3. Cek koneksi database di `Minimarket/App.config`.
4. Jalankan `setup-sqlserver.sql` di SSMS jika database belum dibuat otomatis.
5. Build dan run project.

## Login Default

```text
Username: admin
Password: admin123
```

## Koneksi Database

Default connection string:

```text
Data Source=.\SQLEXPRESS;Initial Catalog=MinimarketDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=10
```

Jika nama server berbeda, ubah bagian `Data Source` saja. Contoh:

```text
KANNEKY-MITSUKI\SQLEXPRESS
localhost
.\SQLEXPRESS
```

## Fitur Utama

- Login pengguna.
- Tambah, edit, hapus, dan lihat produk.
- Diskon produk dan perhitungan harga otomatis.
- Live search produk berdasarkan kode atau nama.
- Transaksi kasir dengan keranjang, total, uang diterima, dan kembalian.
- Histori transaksi dengan filter bulan/tahun.
- Cetak struk dan export PDF.
- Laporan Crystal untuk penjualan, transaksi, user, dan inventory.
- Export data laporan ke TXT.

## Catatan Crystal Report

Untuk memakai tombol `Preview Crystal`, komputer perlu SAP Crystal Reports runtime dan file `.rpt` di folder `Minimarket/Reports`. Jika belum ada, data laporan tetap bisa dilihat di grid aplikasi.
