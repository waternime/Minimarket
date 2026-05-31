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
        private static readonly CrystalReportDefinition[] Definitions =
        {
            new SalesCrystalReportDefinition(),
            new TransactionCrystalReportDefinition(),
            new UserCrystalReportDefinition(),
            new InventoryCrystalReportDefinition()
        };

        public static string GetReportTitle(ReportKind reportKind)
        {
            return GetDefinition(reportKind).Title;
        }

        public static string GetTemplateFileName(ReportKind reportKind)
        {
            return GetDefinition(reportKind).TemplateFileName;
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

        private static CrystalReportDefinition GetDefinition(ReportKind reportKind)
        {
            CrystalReportDefinition definition = Definitions.FirstOrDefault(item => item.Kind == reportKind);
            if (definition == null)
            {
                throw new ArgumentOutOfRangeException("reportKind");
            }

            return definition;
        }
    }

    internal abstract class CrystalReportDefinition
    {
        public abstract ReportKind Kind { get; }
        public abstract string Title { get; }
        public abstract string TemplateFileName { get; }
    }

    internal sealed class SalesCrystalReportDefinition : CrystalReportDefinition
    {
        public override ReportKind Kind { get { return ReportKind.Sales; } }
        public override string Title { get { return "Laporan Penjualan"; } }
        public override string TemplateFileName { get { return "SalesReport.rpt"; } }
    }

    internal sealed class TransactionCrystalReportDefinition : CrystalReportDefinition
    {
        public override ReportKind Kind { get { return ReportKind.Transaction; } }
        public override string Title { get { return "Laporan Transaksi"; } }
        public override string TemplateFileName { get { return "TransactionReport.rpt"; } }
    }

    internal sealed class UserCrystalReportDefinition : CrystalReportDefinition
    {
        public override ReportKind Kind { get { return ReportKind.User; } }
        public override string Title { get { return "Laporan User"; } }
        public override string TemplateFileName { get { return "UserReport.rpt"; } }
    }

    internal sealed class InventoryCrystalReportDefinition : CrystalReportDefinition
    {
        public override ReportKind Kind { get { return ReportKind.Inventory; } }
        public override string Title { get { return "Laporan Inventory"; } }
        public override string TemplateFileName { get { return "InventoryReport.rpt"; } }
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
