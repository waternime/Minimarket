# Template Crystal Report

Folder ini dipakai untuk menyimpan file template Crystal Report (`.rpt`).

Buat 4 file berikut lewat SAP Crystal Reports for Visual Studio:

- `SalesReport.rpt`
- `TransactionReport.rpt`
- `UserReport.rpt`
- `InventoryReport.rpt`

Setiap report minimal berisi:

- Header judul laporan.
- Detail data dari DataTable aplikasi.
- Footer tanggal cetak atau nomor halaman.

Nama DataTable dari aplikasi:

- `SalesReport`
- `TransactionReport`
- `UserReport`
- `InventoryReport`

Kalau membuat `.rpt` langsung dari SQL Server, gunakan view berikut sebagai sumber data:

- `dbo.vwCrystalSalesReport`
- `dbo.vwCrystalTransactionReport`
- `dbo.vwCrystalUserReport`
- `dbo.vwCrystalInventoryReport`

Setelah file `.rpt` dibuat dan disimpan di folder ini, Visual Studio akan menyalinnya ke folder output aplikasi saat build.
