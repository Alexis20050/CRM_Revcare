#nullable enable
using System;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    public partial class ReportPreviewDialogForm : Form
    {
        private string htmlContent = string.Empty;
        private string documentTitle = string.Empty;
        private int currentGeneration = 0;
        private string? currentDocumentToken = null;
        private System.Windows.Forms.Timer? renderTimeoutTimer = null;

        public bool IsDocumentReady => btnPrint.Enabled;
        public string? CurrentDocumentToken => currentDocumentToken;
        public int CurrentGeneration => currentGeneration;
        public string StatusText => lblStatus.Text;

        public ReportPreviewDialogForm()
        {
            InitializeComponent();
            btnPrint.Enabled = false;
            btnPageSetup.Enabled = false;
            lblStatus.Text = "Loading preview...";

            renderTimeoutTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            renderTimeoutTimer.Tick += (s, e) => HandleRenderTimeout();

            webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
            btnPrint.Click += (s, e) => PrintDocument();
            btnPageSetup.Click += (s, e) => webBrowser.ShowPageSetupDialog();
            btnClose.Click += (s, e) => Close();
        }

        public ReportPreviewDialogForm(string title, string html) : this()
        {
            SetDocument(title, html);
        }

        public void SetDocument(string title, string html)
        {
            int generation = Interlocked.Increment(ref currentGeneration);
            string token = $"revcare-doc-{generation}-{Guid.NewGuid():N}";
            currentDocumentToken = token;

            documentTitle = title;
            lblTitle.Text = title;
            Text = $"RevCare Auto Hub - {title}";

            // Immediately reset readiness
            btnPrint.Enabled = false;
            btnPageSetup.Enabled = false;
            lblStatus.Text = "Rendering preview...";
            lblStatus.ForeColor = Color.FromArgb(100, 110, 125);

            // Reset and start timeout timer
            renderTimeoutTimer?.Stop();
            renderTimeoutTimer?.Start();

            // Inject the correlation token into the document HTML
            htmlContent = InjectDocumentToken(html, token);
            webBrowser.DocumentText = htmlContent;
        }

        public static string InjectDocumentToken(string html, string token)
        {
            string metaTag = $"<meta name=\"revcare-doc-token\" content=\"{WebUtility.HtmlEncode(token)}\" />\n";
            int headIndex = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            if (headIndex >= 0)
            {
                return html.Insert(headIndex + 6, "\n" + metaTag);
            }
            int htmlIndex = html.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
            if (htmlIndex >= 0)
            {
                int closeHtmlTag = html.IndexOf('>', htmlIndex);
                if (closeHtmlTag >= 0)
                {
                    return html.Insert(closeHtmlTag + 1, "\n<head>" + metaTag + "</head>");
                }
            }
            return metaTag + html;
        }

        private static string? ExtractTokenFromDocument(HtmlDocument? doc)
        {
            if (doc == null) return null;
            try
            {
                var metas = doc.GetElementsByTagName("meta");
                if (metas != null)
                {
                    foreach (HtmlElement meta in metas)
                    {
                        if (string.Equals(meta.GetAttribute("name"), "revcare-doc-token", StringComparison.OrdinalIgnoreCase))
                        {
                            return meta.GetAttribute("content");
                        }
                    }
                }
            }
            catch
            {
                // Ignore DOM reading errors
            }
            return null;
        }

        private void WebBrowser_DocumentCompleted(object? sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (IsDisposed || Disposing) return;
            if (e.Url != webBrowser.Url) return; // Ignore child-frame completion.
            string? token = ExtractTokenFromDocument(webBrowser.Document);
            ProcessDocumentCompleted(token, webBrowser.ReadyState);
        }

        public void ProcessDocumentCompleted(string? token, WebBrowserReadyState readyState)
        {
            if (IsDisposed || Disposing) return;

            // Reject stale completion events or documents with unmatching/missing tokens
            if (string.IsNullOrEmpty(currentDocumentToken) || !string.Equals(token, currentDocumentToken, StringComparison.Ordinal))
            {
                return;
            }

            if (readyState != WebBrowserReadyState.Complete)
            {
                return;
            }

            renderTimeoutTimer?.Stop();
            btnPrint.Enabled = true;
            btnPageSetup.Enabled = true;
            lblStatus.Text = "Ready to print";
            lblStatus.ForeColor = Color.FromArgb(40, 167, 69);
        }

        public void HandleRenderTimeout()
        {
            if (IsDisposed || Disposing) return;
            // A timed-out generation stays invalid even if its completion arrives later.
            CancelPendingReadiness();
            lblStatus.Text = "Document preview timed out. Printing remains disabled.";
            lblStatus.ForeColor = Color.FromArgb(220, 53, 69);
        }

        public void CancelPendingReadiness()
        {
            renderTimeoutTimer?.Stop();
            currentDocumentToken = null;
            btnPrint.Enabled = false;
            btnPageSetup.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelPendingReadiness();
            base.OnFormClosing(e);
        }

        public void PrintDocument()
        {
            if (!btnPrint.Enabled)
            {
                MessageBox.Show(this, "The document is still rendering. Please wait until loading completes.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                webBrowser.ShowPrintDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open print dialog: " + ex.Message, "Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void ShowPreview(IWin32Window? owner, string title, string html)
        {
            using var form = new ReportPreviewDialogForm(title, html);
            if (owner != null)
                form.ShowDialog(owner);
            else
                form.ShowDialog();
        }
    }
}
