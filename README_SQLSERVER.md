# Setup SQL Server Minimarket

Program ini memakai SQL Server Express dengan Windows Authentication.

## Koneksi aplikasi

Koneksi ada di `Minimarket/App.config`.

```xml
Data Source=.\SQLEXPRESS;Initial Catalog=MinimarketDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=10
```

Kalau nama server SQL Server berbeda, ganti bagian `Data Source` saja. Contoh:

```text
KANNEKY-MITSUKI\SQLEXPRESS
localhost
```

## Cara setup database

Saat aplikasi pertama kali dibuka, database dan tabel akan dibuat otomatis jika akun Windows punya izin membuat database.

Kalau ingin setup manual:

1. Buka SSMS / Azure Data Studio.
2. Connect ke `.\SQLEXPRESS` atau nama server SQL Server yang tersedia dengan Windows Authentication.
3. Buka file `setup-sqlserver.sql`.
4. Jalankan seluruh script.

Default login aplikasi:

```text
Username: admin
Password: admin123
```

Password bisa diganti dari tab Pengaturan setelah login.

## Catatan koneksi

Kalau muncul error SSPI seperti `Cannot generate SSPI context`, biasanya bukan masalah kode aplikasi. Coba:

1. Pastikan service `SQL Server (SQLEXPRESS)` sedang Running.
2. Coba ubah `Data Source` di `App.config` menjadi `.\SQLEXPRESS`.
3. Di SQL Server Configuration Manager, pastikan protokol `Shared Memory` aktif untuk koneksi lokal.
4. Restart service `SQL Server (SQLEXPRESS)` setelah mengubah konfigurasi.
