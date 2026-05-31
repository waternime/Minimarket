using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Minimarket
{
    internal static class CrystalReportService
    {
        public static string GetReportTitle(ReportKind reportKind)
        {
            switch (reportKind)
            {
                case ReportKind.Sales:
                    return "Laporan Penjualan";
                case ReportKind.Transaction:
                    return "Laporan Transaksi";
                case ReportKind.User:
                    return "Laporan User";
                case ReportKind.Inventory:
                    return "Laporan Inventory";
                default:
                    throw new ArgumentOutOfRangeException("reportKind");
            }
        }

        public static string GetTemplateFileName(ReportKind reportKind)
        {
            switch (reportKind)
            {
                case ReportKind.Sales:
                    return "SalesReport.rpt";
                case ReportKind.Transaction:
                    return "TransactionReport.rpt";
                case ReportKind.User:
                    return "UserReport.rpt";
                case ReportKind.Inventory:
                    return "InventoryReport.rpt";
                default:
                    throw new ArgumentOutOfRangeException("reportKind");
            }
        }

        public static string GetTemplatePath(ReportKind reportKind)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", GetTemplateFileName(reportKind));
        }

        public static bool IsCrystalRuntimeAvailable()
        {
            return GetReportDocumentType(false) != null && GetViewerType(false) != null;
        }

        public static void ShowReport(IWin32Window owner, ReportKind reportKind, DataTable data)
        {
            string templatePath = GetTemplatePath(reportKind);
            if (!IsCrystalRuntimeAvailable())
            {
                throw new InvalidOperationException(
                    "Crystal Reports runtime belum terpasang di komputer ini.\r\n\r\n" +
                    "Install SAP Crystal Reports for Visual Studio, lalu buka ulang Visual Studio/aplikasi.");
            }

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    "Template Crystal Report belum ada.\r\n\r\n" +
                    "Buat file .rpt lewat Crystal Reports Designer dan simpan ke folder Reports:\r\n" +
                    templatePath,
                    templatePath);
            }

            using (CrystalReportPreviewForm form = new CrystalReportPreviewForm(GetReportTitle(reportKind), templatePath, data))
            {
                form.ShowDialog(owner);
            }
        }

        internal static Type GetReportDocumentType(bool throwOnError)
        {
            return Type.GetType(
                "CrystalDecisions.CrystalReports.Engine.ReportDocument, CrystalDecisions.CrystalReports.Engine",
                throwOnError);
        }

        internal static Type GetViewerType(bool throwOnError)
        {
            return Type.GetType(
                "CrystalDecisions.Windows.Forms.CrystalReportViewer, CrystalDecisions.Windows.Forms",
                throwOnError);
        }
    }

    internal sealed class CrystalReportPreviewForm : Form
    {
        private object reportDocument;

        public CrystalReportPreviewForm(string title, string templatePath, DataTable data)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            Width = 1024;
            Height = 720;

            LoadCrystalViewer(templatePath, data);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            DisposeReportDocument();
            base.OnFormClosed(e);
        }

        private void LoadCrystalViewer(string templatePath, DataTable data)
        {
            Type reportType = CrystalReportService.GetReportDocumentType(true);
            Type viewerType = CrystalReportService.GetViewerType(true);

            reportDocument = Activator.CreateInstance(reportType);
            InvokeInstance(reportDocument, "Load", templatePath);

            MethodInfo setDataSource = reportType.GetMethods()
                .FirstOrDefault(method =>
                    method.Name == "SetDataSource" &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(DataTable));

            if (setDataSource == null)
            {
                throw new MissingMethodException("Crystal ReportDocument.SetDataSource(DataTable) tidak ditemukan.");
            }

            setDataSource.Invoke(reportDocument, new object[] { data });

            Control viewer = (Control)Activator.CreateInstance(viewerType);
            viewer.Dock = DockStyle.Fill;
            viewerType.GetProperty("ReportSource").SetValue(viewer, reportDocument, null);
            Controls.Add(viewer);
        }

        private static void InvokeInstance(object instance, string methodName, params object[] args)
        {
            Type[] argumentTypes = args.Select(arg => arg.GetType()).ToArray();
            MethodInfo method = instance.GetType().GetMethod(methodName, argumentTypes);
            if (method == null)
            {
                throw new MissingMethodException(instance.GetType().FullName, methodName);
            }

            method.Invoke(instance, args);
        }

        private void DisposeReportDocument()
        {
            if (reportDocument == null)
            {
                return;
            }

            IDisposable disposable = reportDocument as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }

            reportDocument = null;
        }
    }
}
