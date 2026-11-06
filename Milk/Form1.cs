using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;


namespace Milk
{
    public partial class Form1 : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int WM_NCHITTEST = 0x84;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const string EditorTextMessagePrefix = "editorText:";
        private Region? cornerRegion;
        private object nativeScriptBox;
        private readonly RichTextBox nativeScriptTextBox = new();

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            uint attribute,
            ref int attributeValue,
            uint attributeSize);

        public Form1()
        {
            InitializeComponent();
            components ??= new System.ComponentModel.Container();
            components.Add(nativeScriptTextBox);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateWindowCorners();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateWindowCorners();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState != FormWindowState.Maximized)
            {
                var screenX = unchecked((short)((long)m.LParam & 0xFFFF));
                var screenY = unchecked((short)(((long)m.LParam >> 16) & 0xFFFF));
                var point = PointToClient(new Point(screenX, screenY));
                var resizeBorder = (int)Math.Ceiling(8 * DeviceDpi / 96d);
                var left = point.X <= resizeBorder;
                var right = point.X >= ClientSize.Width - resizeBorder;
                var top = point.Y <= resizeBorder;
                var bottom = point.Y >= ClientSize.Height - resizeBorder;

                if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left) m.Result = (IntPtr)HTLEFT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (top) m.Result = (IntPtr)HTTOP;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                else
                {
                    base.WndProc(ref m);
                    return;
                }

                return;
            }

            base.WndProc(ref m);
        }

        private void UpdateWindowCorners()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            var preference = WindowState == FormWindowState.Maximized
                ? DWMWCP_DONOTROUND
                : DWMWCP_ROUND;

            try
            {
                var result = DwmSetWindowAttribute(
                    Handle,
                    DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref preference,
                    sizeof(int));

                if (result == 0)
                {
                    SetRoundedRegion(false);
                    return;
                }
            }
            catch (DllNotFoundException)
            {
            }

            SetRoundedRegion(WindowState != FormWindowState.Maximized);
        }

        private void SetRoundedRegion(bool rounded)
        {
            Region = null;
            cornerRegion?.Dispose();
            cornerRegion = null;

            if (!rounded || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            var diameter = Math.Min(16 * DeviceDpi / 96, Math.Min(ClientSize.Width, ClientSize.Height));
            var bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            using var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            cornerRegion = new Region(path);
            Region = cornerRegion;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                await webView21.EnsureCoreWebView2Async();
                webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var htmlPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "wwwroot",
                    "milk-shell.html");

                if (!File.Exists(htmlPath))
                {
                    throw new FileNotFoundException("The bundled HTML UI could not be found.", htmlPath);
                }

                webView21.Source = new Uri(htmlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"The HTML UI could not be loaded.\n\n{ex.Message}",
                    "Milk",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CoreWebView2_WebMessageReceived(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            if (message.StartsWith(EditorTextMessagePrefix, StringComparison.Ordinal))
            {
                nativeScriptTextBox.Text = message.Substring(EditorTextMessagePrefix.Length);
                return;
            }

            switch (message)
            {
                case "execute":
                    button1_Click(webView21, EventArgs.Empty);
                    break;
                case "executeNative":
                    button2_Click(webView21, EventArgs.Empty);
                    break;
                case "beginDrag":
                    BeginNativeWindowOperation(HTCAPTION);
                    break;
                case "resizeLeft":
                    BeginNativeWindowOperation(HTLEFT);
                    break;
                case "resizeRight":
                    BeginNativeWindowOperation(HTRIGHT);
                    break;
                case "resizeTop":
                    BeginNativeWindowOperation(HTTOP);
                    break;
                case "resizeBottom":
                    BeginNativeWindowOperation(HTBOTTOM);
                    break;
                case "resizeTopLeft":
                    BeginNativeWindowOperation(HTTOPLEFT);
                    break;
                case "resizeTopRight":
                    BeginNativeWindowOperation(HTTOPRIGHT);
                    break;
                case "resizeBottomLeft":
                    BeginNativeWindowOperation(HTBOTTOMLEFT);
                    break;
                case "resizeBottomRight":
                    BeginNativeWindowOperation(HTBOTTOMRIGHT);
                    break;
                case "min":
                    WindowState = FormWindowState.Minimized;
                    break;
                case "max":
                    WindowState = WindowState == FormWindowState.Maximized
                        ? FormWindowState.Normal
                        : FormWindowState.Maximized;
                    break;
                case "close":
                    Close();
                    break;
            }
        }

        private void button1_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object? sender, EventArgs e)
        {
            if (nativeScriptTextBox.Text == "print(\"kill\")")
            {
                Application.Exit();
            }
        }

        private void BeginNativeWindowOperation(int hitTest)
        {
            if (WindowState == FormWindowState.Maximized && hitTest != HTCAPTION)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)hitTest, IntPtr.Zero);
        }
    }
}
