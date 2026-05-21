using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Program
{
    public const string Version = "1.3";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ClockForm());
    }
}

// ── Win32 ─────────────────────────────────────────────────────
static class W32
{
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr hdc);
    [DllImport("gdi32.dll")]  public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")]  public static extern bool   DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")]  public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")]  public static extern bool   DeleteObject(IntPtr h);
    [DllImport("gdi32.dll")]  public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi,
        uint usage, out IntPtr bits, IntPtr sec, uint off);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
        ref POINT dst, ref SIZE sz, IntPtr hdcSrc, ref POINT src,
        uint key, ref BLENDFUNCTION bf, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Ansi)] public static extern bool EnumDisplayDevices(
        string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    public const int  WM_NCHITTEST  = 0x84;
    public const int  HTTRANSPARENT = -1;
    public const int  HTCLIENT      = 1;
    public const uint ULW_ALPHA     = 2;
    public const byte AC_SRC_OVER   = 0;
    public const byte AC_SRC_ALPHA  = 1;

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] public struct SIZE  { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public int bmiColors; }

    public const uint DD_ATTACHED  = 0x00000001;  // DISPLAY_DEVICE_ATTACHED_TO_DESKTOP
    public const uint DD_REMOVABLE = 0x00000020;  // DISPLAY_DEVICE_REMOVABLE

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public struct DISPLAY_DEVICE {
        public int  cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]  public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceKey;
    }
}

// ── Settings ──────────────────────────────────────────────────
class Settings
{
    public int   Size           = 200;
    public Color FaceColor      = H("1E1E1E");
    public Color BorderColor    = H("5A5A5A");
    public Color TickMajorColor = H("B4B4B4");
    public Color TickMinorColor = H("505050");
    public Color NumberColor    = H("DCDCDC");
    public Color HourColor      = Color.White;
    public Color MinuteColor    = H("D2D2D2");
    public Color SecondColor    = H("FF4646");
    public Color CenterColor    = H("FF4646");
    public int   PosX = -1, PosY = -1;

    static string Path { get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clock_settings.ini"); } }

    public static Color H(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(Convert.ToInt32(hex.Substring(0,2),16),
                              Convert.ToInt32(hex.Substring(2,2),16),
                              Convert.ToInt32(hex.Substring(4,2),16));
    }
    public static string X(Color c) { return string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B); }

    public static Settings Load()
    {
        var s = new Settings();
        if (!File.Exists(Path)) return s;
        try {
            foreach (var line in File.ReadAllLines(Path)) {
                int i = line.IndexOf('='); if (i < 0) continue;
                string k = line.Substring(0,i).Trim(), v = line.Substring(i+1).Trim();
                switch (k) {
                    case "Size":           s.Size           = int.Parse(v); break;
                    case "FaceColor":      s.FaceColor      = H(v); break;
                    case "BorderColor":    s.BorderColor    = H(v); break;
                    case "TickMajorColor": s.TickMajorColor = H(v); break;
                    case "TickMinorColor": s.TickMinorColor = H(v); break;
                    case "NumberColor":    s.NumberColor    = H(v); break;
                    case "HourColor":      s.HourColor      = H(v); break;
                    case "MinuteColor":    s.MinuteColor    = H(v); break;
                    case "SecondColor":    s.SecondColor    = H(v); break;
                    case "CenterColor":    s.CenterColor    = H(v); break;
                    case "PosX":           s.PosX           = int.Parse(v); break;
                    case "PosY":           s.PosY           = int.Parse(v); break;
                }
            }
        } catch {}
        return s;
    }

    public void Save() {
        try { File.WriteAllLines(Path, new[] {
            "Size="+Size, "FaceColor="+X(FaceColor), "BorderColor="+X(BorderColor),
            "TickMajorColor="+X(TickMajorColor), "TickMinorColor="+X(TickMinorColor),
            "NumberColor="+X(NumberColor), "HourColor="+X(HourColor),
            "MinuteColor="+X(MinuteColor), "SecondColor="+X(SecondColor),
            "CenterColor="+X(CenterColor), "PosX="+PosX, "PosY="+PosY });
        } catch {}
    }

    public Settings Clone() { return (Settings)MemberwiseClone(); }

    public void CopyColorsFrom(Settings s) {
        FaceColor=s.FaceColor; BorderColor=s.BorderColor;
        TickMajorColor=s.TickMajorColor; TickMinorColor=s.TickMinorColor;
        NumberColor=s.NumberColor; HourColor=s.HourColor;
        MinuteColor=s.MinuteColor; SecondColor=s.SecondColor; CenterColor=s.CenterColor;
    }
}

// ── Clock window ──────────────────────────────────────────────
class ClockForm : Form
{
    Settings     cfg;
    bool         isDragging, isResizing;
    Point        dragOffset;
    Point        resizeCenter;
    SettingsForm settingsDlg;
    const int    RESIZE_ZONE = 16;

    protected override CreateParams CreateParams {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x80000; /* WS_EX_LAYERED */ return cp; }
    }

    public ClockForm()
    {
        cfg = Settings.Load();
        Text = "Clock";
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch {}
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.Manual;
        TopMost         = true;
        ClientSize      = new Size(cfg.Size, cfg.Size);

        var builtin = FindBuiltinScreen();
        var scr = builtin.WorkingArea;
        // 複数モニター時は常に内蔵スクリーンの左上、単体時は保存済み位置を使用
        if (Screen.AllScreens.Length > 1) {
            Location = new Point(scr.Left + 20, scr.Top + 20);
        } else {
            Location = (cfg.PosX >= 0)
                ? new Point(cfg.PosX, cfg.PosY)
                : new Point(scr.Right - cfg.Size - 20, scr.Bottom - cfg.Size - 20);
        }

        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick    += (s, e) => UpdateDisplay();
        timer.Start();
        FormClosed    += (s, e) => { timer.Stop(); timer.Dispose(); };
        FormClosing   += (s, e) => { cfg.PosX = Left; cfg.PosY = Top; cfg.Save(); };
        Shown         += (s, e) => UpdateDisplay();
        MouseDown     += OnMouseDown;
        MouseMove     += OnMouseMove;
        MouseUp       += (s, e) => { isDragging = false; isResizing = false; };

        var ctx  = new ContextMenuStrip();
        var mSet = new ToolStripMenuItem("Settings");
        mSet.Click += (s, e) => OpenSettings();
        var mExit = new ToolStripMenuItem("Exit");
        mExit.Click += (s, e) => { cfg.PosX = Left; cfg.PosY = Top; cfg.Save(); Close(); };
        ctx.Items.Add(mSet); ctx.Items.Add(mExit);
        ContextMenuStrip = ctx;
    }

    void OpenSettings()
    {
        if (settingsDlg != null && !settingsDlg.IsDisposed) { settingsDlg.BringToFront(); return; }
        var saved = cfg.Clone();
        settingsDlg = new SettingsForm(cfg.Clone(), saved, ApplyCfg);
        settingsDlg.TopMost    = true;
        settingsDlg.FormClosed += (s, e) => { cfg.PosX = Left; cfg.PosY = Top; cfg.Save(); };
        settingsDlg.Show();
    }

    void ApplyCfg(Settings newCfg)
    {
        cfg = newCfg;
        ClientSize = new Size(cfg.Size, cfg.Size);
        var scr = Screen.FromControl(this).WorkingArea;
        if (Left + cfg.Size > scr.Right)  Left = scr.Right  - cfg.Size - 10;
        if (Top  + cfg.Size > scr.Bottom) Top  = scr.Bottom - cfg.Size - 10;
        UpdateDisplay();
    }

    void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        float cx = cfg.Size/2f, cy = cfg.Size/2f, r = cfg.Size/2f - 3f;
        double d = Math.Sqrt(Math.Pow(e.X-cx,2)+Math.Pow(e.Y-cy,2));
        if (d > r - RESIZE_ZONE) {
            isResizing   = true;
            resizeCenter = new Point(Left + cfg.Size/2, Top + cfg.Size/2);
        } else {
            isDragging  = true;
            dragOffset  = e.Location;
        }
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        float cx = cfg.Size/2f, cy = cfg.Size/2f, r = cfg.Size/2f - 3f;
        double d = Math.Sqrt(Math.Pow(e.X-cx,2)+Math.Pow(e.Y-cy,2));

        if (isResizing) {
            Point sm = PointToScreen(e.Location);
            int newSz = (int)(Math.Sqrt(Math.Pow(sm.X-resizeCenter.X,2)+Math.Pow(sm.Y-resizeCenter.Y,2))*2);
            newSz = Math.Max(120, Math.Min(400, newSz));
            cfg.Size   = newSz;
            ClientSize = new Size(newSz, newSz);
            Left       = resizeCenter.X - newSz/2;
            Top        = resizeCenter.Y - newSz/2;
            UpdateDisplay();
            if (settingsDlg != null && !settingsDlg.IsDisposed) settingsDlg.SyncSize(newSz);
        } else if (isDragging) {
            Left += e.X - dragOffset.X;
            Top  += e.Y - dragOffset.Y;
            UpdateDisplay();
        } else {
            Cursor = (d > r - RESIZE_ZONE && d <= r) ? Cursors.SizeNWSE : Cursors.SizeAll;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == W32.WM_NCHITTEST) {
            long lp = m.LParam.ToInt64();
            int sx = (short)(lp & 0xFFFF), sy = (short)((lp >> 16) & 0xFFFF);
            Point cp = PointToClient(new Point(sx, sy));
            float cx = cfg.Size/2f, cy = cfg.Size/2f, r = cfg.Size/2f - 2f;
            double d = Math.Sqrt(Math.Pow(cp.X-cx,2)+Math.Pow(cp.Y-cy,2));
            m.Result = new IntPtr(d > r ? W32.HTTRANSPARENT : W32.HTCLIENT);
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnPaint(PaintEventArgs e) {}
    protected override void OnPaintBackground(PaintEventArgs e) {}

    public void UpdateDisplay()
    {
        if (!IsHandleCreated) return;
        int sz = cfg.Size;
        using (var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp)) {
                g.Clear(Color.Transparent);
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                DrawClock(g, sz);
            }
            PushLayered(bmp);
        }
    }

    void PushLayered(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        var bd = bmp.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = Math.Abs(bd.Stride);
        byte[] px = new byte[stride * h];
        Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);

        // pre-multiply alpha (required by UpdateLayeredWindow)
        for (int i = 0; i < px.Length; i += 4) {
            float a = px[i+3] / 255f;
            px[i]   = (byte)(px[i]   * a);
            px[i+1] = (byte)(px[i+1] * a);
            px[i+2] = (byte)(px[i+2] * a);
        }

        IntPtr sDC = W32.GetDC(IntPtr.Zero);
        IntPtr mDC = W32.CreateCompatibleDC(sDC);
        IntPtr hBmp = IntPtr.Zero, oldBmp = IntPtr.Zero;
        try {
            var bmi = new W32.BITMAPINFO();
            bmi.bmiHeader.biSize     = Marshal.SizeOf(bmi.bmiHeader);
            bmi.bmiHeader.biWidth    = w;
            bmi.bmiHeader.biHeight   = -h;
            bmi.bmiHeader.biPlanes   = 1;
            bmi.bmiHeader.biBitCount = 32;
            IntPtr bits;
            hBmp = W32.CreateDIBSection(mDC, ref bmi, 0, out bits, IntPtr.Zero, 0);
            if (hBmp == IntPtr.Zero || bits == IntPtr.Zero) return;
            oldBmp = W32.SelectObject(mDC, hBmp);
            Marshal.Copy(px, 0, bits, px.Length);

            var ptDst = new W32.POINT { x=Left, y=Top };
            var ptSrc = new W32.POINT { x=0, y=0 };
            var size  = new W32.SIZE  { cx=w, cy=h };
            var blend = new W32.BLENDFUNCTION { BlendOp=W32.AC_SRC_OVER, SourceConstantAlpha=255, AlphaFormat=W32.AC_SRC_ALPHA };
            W32.UpdateLayeredWindow(Handle, sDC, ref ptDst, ref size, mDC, ref ptSrc, 0, ref blend, W32.ULW_ALPHA);
        } finally {
            if (hBmp != IntPtr.Zero) { W32.SelectObject(mDC, oldBmp); W32.DeleteObject(hBmp); }
            W32.DeleteDC(mDC); W32.ReleaseDC(IntPtr.Zero, sDC);
        }
    }

    void DrawClock(Graphics g, int sz)
    {
        float cx = sz/2f, cy = sz/2f;
        float bw = Math.Max(3f, sz * 0.03f);   // ベゼル幅
        float r  = sz/2f - bw;                 // 文字盤の半径
        float fx = bw, fsz = sz - bw*2;        // 文字盤の矩形

        // ── ベゼル（外枠）グラデーション ──
        using (var path = new GraphicsPath()) {
            path.AddEllipse(1, 1, sz-2, sz-2);
            using (var pgb = new PathGradientBrush(path)) {
                pgb.CenterPoint    = new PointF(cx * 0.6f, cy * 0.4f);
                pgb.CenterColor    = Blend(cfg.BorderColor, Color.White, 0.30f);
                pgb.SurroundColors = new[] { Blend(cfg.BorderColor, Color.Black, 0.50f) };
                g.FillPath(pgb, path);
            }
        }
        // ベゼル内縁の影リング
        using (var p = new Pen(Color.FromArgb(70, 0, 0, 0), bw * 0.30f))
            g.DrawEllipse(p, bw * 0.4f, bw * 0.4f, sz - bw * 0.8f, sz - bw * 0.8f);

        // ── 文字盤ベースカラー ──
        using (var b = new SolidBrush(cfg.FaceColor))
            g.FillEllipse(b, fx, fx, fsz, fsz);

        // ── ドームグラデーションオーバーレイ（立体感） ──
        using (var facePath = new GraphicsPath()) {
            facePath.AddEllipse(fx, fx, fsz, fsz);
            using (var pgb = new PathGradientBrush(facePath)) {
                pgb.CenterPoint    = new PointF(cx, cy * 0.80f);
                pgb.CenterColor    = Color.FromArgb(95, 255, 255, 255);
                pgb.SurroundColors = new[] { Color.FromArgb(30, 0, 0, 0) };
                g.FillPath(pgb, facePath);
            }
        }

        // ── 目盛り ──
        for (int i = 0; i < 60; i++) {
            double a = i*6*Math.PI/180; bool hr = (i%5==0);
            float  n = hr ? r-Math.Max(6f,sz*0.05f) : r-Math.Max(3f,sz*0.025f);
            using (var p = new Pen(hr?cfg.TickMajorColor:cfg.TickMinorColor, hr?2f:1f)) {
                p.StartCap=LineCap.Round; p.EndCap=LineCap.Round;
                g.DrawLine(p, cx+r*(float)Math.Sin(a), cy-r*(float)Math.Cos(a),
                              cx+n*(float)Math.Sin(a), cy-n*(float)Math.Cos(a));
            }
        }

        // ── 数字 ──
        float fSz=Math.Max(7f,sz*0.062f), nr=r-Math.Max(14f,sz*0.115f);
        using (var font=new Font("Segoe UI",fSz)) using (var b=new SolidBrush(cfg.NumberColor)) {
            for (int n=1; n<=12; n++) {
                double a=n*30*Math.PI/180; string st=n.ToString();
                SizeF ms=g.MeasureString(st,font);
                g.DrawString(st,font,b, cx+nr*(float)Math.Sin(a)-ms.Width/2,
                                        cy-nr*(float)Math.Cos(a)-ms.Height/2);
            }
        }

        // ── 針 ──
        DateTime now=DateTime.Now;
        double hA=(now.Hour%12+now.Minute/60.0)*30*Math.PI/180;
        double mA=(now.Minute+now.Second/60.0)*6*Math.PI/180;
        double sA=now.Second*6*Math.PI/180;
        float  hw=Math.Max(3f,sz*0.025f);

        using (var p=new Pen(cfg.HourColor,  hw*2)) { p.StartCap=LineCap.Round; p.EndCap=LineCap.Round;
            g.DrawLine(p,cx,cy, cx+r*0.50f*(float)Math.Sin(hA), cy-r*0.50f*(float)Math.Cos(hA)); }
        using (var p=new Pen(cfg.MinuteColor, hw )) { p.StartCap=LineCap.Round; p.EndCap=LineCap.Round;
            g.DrawLine(p,cx,cy, cx+r*0.70f*(float)Math.Sin(mA), cy-r*0.70f*(float)Math.Cos(mA)); }
        using (var p=new Pen(cfg.SecondColor,1.5f)) { p.StartCap=LineCap.Round; p.EndCap=LineCap.Round;
            g.DrawLine(p,cx,cy,  cx+r*0.80f*(float)Math.Sin(sA), cy-r*0.80f*(float)Math.Cos(sA));
            g.DrawLine(p,cx,cy,  cx-r*0.20f*(float)Math.Sin(sA), cy+r*0.20f*(float)Math.Cos(sA)); }

        // ── 中心点 ──
        float dr=Math.Max(3f,sz*0.022f);
        using (var b=new SolidBrush(cfg.CenterColor)) g.FillEllipse(b, cx-dr, cy-dr, dr*2, dr*2);
    }

    static Color Blend(Color a, Color b, float t) {
        return Color.FromArgb(
            (int)(a.R*(1-t)+b.R*t),
            (int)(a.G*(1-t)+b.G*t),
            (int)(a.B*(1-t)+b.B*t));
    }

    // 内蔵ディスプレイを検出して返す。見つからない場合はプライマリスクリーンを返す。
    static Screen FindBuiltinScreen() { string _; return FindBuiltinScreen(out _); }

    public static Screen FindBuiltinScreen(out string status)
    {
        int total = Screen.AllScreens.Length;
        if (total == 1) {
            var s = Screen.PrimaryScreen;
            status = string.Format("シングルモニター  {0}x{1}", s.Bounds.Width, s.Bounds.Height);
            return s;
        }
        try {
            var dd = new W32.DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);
            for (uint i = 0; W32.EnumDisplayDevices(null, i, ref dd, 0); i++) {
                if ((dd.StateFlags & W32.DD_ATTACHED) == 0) { dd.cb = Marshal.SizeOf(dd); continue; }
                string adapterName = dd.DeviceName;
                var mon = new W32.DISPLAY_DEVICE();
                mon.cb = Marshal.SizeOf(mon);
                if (W32.EnumDisplayDevices(adapterName, 0, ref mon, 0)) {
                    if ((mon.StateFlags & W32.DD_REMOVABLE) == 0) {
                        foreach (var scr in Screen.AllScreens) {
                            if (scr.DeviceName == adapterName) {
                                status = string.Format("内蔵モニター検出  {0}  {1}x{2}",
                                    adapterName.Replace("\\\\.\\", ""),
                                    scr.Bounds.Width, scr.Bounds.Height);
                                return scr;
                            }
                        }
                    }
                }
                dd.cb = Marshal.SizeOf(dd);
            }
        } catch {}
        var fallback = Screen.PrimaryScreen;
        status = string.Format("検出失敗 → プライマリ使用  {0}x{1}",
            fallback.Bounds.Width, fallback.Bounds.Height);
        return fallback;
    }
}

// ── Color row definition ──────────────────────────────────────
class CR
{
    public string Label;
    public Func<Settings,Color>   Get;
    public Action<Settings,Color> Set;
    public CR(string l, Func<Settings,Color> g, Action<Settings,Color> s) { Label=l; Get=g; Set=s; }
}

// ── Settings window (modeless) ────────────────────────────────
class SettingsForm : Form
{
    Settings          temp;
    Settings          saved;
    Action<Settings>  onChange;
    Button[]          colorBtns;
    TrackBar          track;
    Label             lbSV;

    static readonly CR[] Rows = {
        new CR("Face",        s=>s.FaceColor,      (s,c)=>s.FaceColor      =c),
        new CR("Border",      s=>s.BorderColor,    (s,c)=>s.BorderColor    =c),
        new CR("Major Ticks", s=>s.TickMajorColor, (s,c)=>s.TickMajorColor =c),
        new CR("Minor Ticks", s=>s.TickMinorColor, (s,c)=>s.TickMinorColor =c),
        new CR("Numbers",     s=>s.NumberColor,    (s,c)=>s.NumberColor    =c),
        new CR("Hour Hand",   s=>s.HourColor,      (s,c)=>s.HourColor      =c),
        new CR("Minute Hand", s=>s.MinuteColor,    (s,c)=>s.MinuteColor    =c),
        new CR("Second Hand", s=>s.SecondColor,    (s,c)=>s.SecondColor    =c),
        new CR("Center Dot",  s=>s.CenterColor,    (s,c)=>s.CenterColor    =c),
    };

    static readonly KeyValuePair<string,Settings>[] Presets = {
        new KeyValuePair<string,Settings>("Dark",     P("1E1E1E","5A5A5A","B4B4B4","505050","DCDCDC","FFFFFF","D2D2D2","FF4646","FF4646")),
        new KeyValuePair<string,Settings>("Light",    P("F5F5F5","A0A0A0","303030","C0C0C0","1A1A1A","111111","333333","CC2200","CC2200")),
        new KeyValuePair<string,Settings>("Ocean",    P("0D1B2A","1B4F72","5DADE2","2E4057","AED6F1","D6EAF8","85C1E9","F0B27A","F0B27A")),
        new KeyValuePair<string,Settings>("Midnight", P("050510","2C2C54","B8860B","4A4A6A","FFD700","FFD700","DAA520","FF6347","FF6347")),
        new KeyValuePair<string,Settings>("RoseGold", P("2D1B1B","7B4F3A","E8B4A0","6B3A2A","F5CBA7","FAD7A0","F0B27A","E74C3C","E74C3C")),
    };

    static Settings P(string fc,string bc,string tm,string tn,string nc,string hc,string mc,string sc,string cc) {
        return new Settings { FaceColor=Settings.H(fc), BorderColor=Settings.H(bc),
            TickMajorColor=Settings.H(tm), TickMinorColor=Settings.H(tn),
            NumberColor=Settings.H(nc), HourColor=Settings.H(hc),
            MinuteColor=Settings.H(mc), SecondColor=Settings.H(sc), CenterColor=Settings.H(cc) };
    }

    public SettingsForm(Settings initial, Settings savedSettings, Action<Settings> onChangeCb)
    {
        temp     = initial.Clone();
        saved    = savedSettings.Clone();
        onChange = onChangeCb;

        Text            = "Clock Settings";
        Width           = 320;
        Height          = 620;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false; MinimizeBox = false;
        StartPosition   = FormStartPosition.Manual;
        Location        = new Point(20, 20);

        colorBtns = new Button[Rows.Length];
        int y = 14;

        // Size
        Add(new Label { Text="Size", Font=new Font("Segoe UI",9,FontStyle.Bold), Location=new Point(14,y), AutoSize=true }); y+=22;
        lbSV  = new Label { Text=temp.Size+" px", Location=new Point(214,y+8), AutoSize=true }; Add(lbSV);
        track = new TrackBar { Minimum=120, Maximum=400, Value=Clamp(temp.Size,120,400),
            TickFrequency=40, LargeChange=20, SmallChange=10, Location=new Point(14,y), Size=new Size(194,45) };
        track.Scroll += (s,e) => { temp.Size=track.Value; lbSV.Text=track.Value+" px"; Notify(); };
        Add(track); y+=50;

        Hsep(ref y);

        // Colors
        Add(new Label { Text="Colors", Font=new Font("Segoe UI",9,FontStyle.Bold), Location=new Point(14,y), AutoSize=true }); y+=22;
        for (int i=0; i<Rows.Length; i++) {
            var row = Rows[i];
            Add(new Label { Text=row.Label, Location=new Point(14,y+4), Size=new Size(140,20) });
            var btn = new Button { Location=new Point(160,y), Size=new Size(44,24), BackColor=row.Get(temp), FlatStyle=FlatStyle.Flat };
            btn.FlatAppearance.BorderColor = Color.FromArgb(100,100,100);
            int idx=i;
            btn.Click += (s,e) => {
                using (var cd=new ColorDialog { FullOpen=true, Color=colorBtns[idx].BackColor }) {
                    if (cd.ShowDialog()==DialogResult.OK) {
                        colorBtns[idx].BackColor = cd.Color;
                        Rows[idx].Set(temp, cd.Color);
                        Notify();
                    }
                }
            };
            Add(btn); colorBtns[i]=btn; y+=28;
        }
        y+=6; Hsep(ref y);

        // Presets
        Add(new Label { Text="Presets", Font=new Font("Segoe UI",9,FontStyle.Bold), Location=new Point(14,y), AutoSize=true }); y+=24;
        int px=14;
        foreach (var kvp in Presets) {
            string pn=kvp.Key; Settings ps=kvp.Value;
            var pb=new Button { Text=pn, Location=new Point(px,y), Size=new Size(54,26), Font=new Font("Segoe UI",7.5f) };
            pb.Click += (s,e) => { temp.CopyColorsFrom(ps); RefreshColors(); Notify(); };
            Add(pb); px+=56;
        }
        y+=34; Hsep(ref y);

        // Reset / Close
        var bReset = new Button { Text="Reset", Location=new Point(14,y), Size=new Size(72,28) };
        bReset.Click += (s,e) => { temp=saved.Clone(); RefreshAll(); Notify(); };
        Add(bReset);

        var bClose = new Button { Text="Close", Location=new Point(222,y), Size=new Size(72,28) };
        bClose.Click += (s,e) => Close();
        Add(bClose); CancelButton=bClose;

        y += 38;
        Hsep(ref y);

        // モニター検出状況
        string screenStatus;
        ClockForm.FindBuiltinScreen(out screenStatus);
        Add(new Label { Text="Monitor", Font=new Font("Segoe UI",9,FontStyle.Bold),
            Location=new Point(14,y), AutoSize=true }); y+=22;
        Add(new Label {
            Text      = screenStatus,
            Location  = new Point(14, y),
            Size      = new Size(278, 32),
            ForeColor = Color.DimGray,
            Font      = new Font("Segoe UI", 8f),
        }); y += 34;

        // バージョン
        var lbVer = new Label {
            Text      = "Analog Clock  v" + Program.Version,
            Location  = new Point(14, y),
            Size      = new Size(278, 18),
            ForeColor = Color.Gray,
            Font      = new Font("Segoe UI", 8f),
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
        };
        Add(lbVer);
    }

    void Add(Control c) { Controls.Add(c); }
    void Hsep(ref int y) {
        Add(new Label { BorderStyle=BorderStyle.Fixed3D, Location=new Point(14,y), Size=new Size(278,2) }); y+=10;
    }
    static int Clamp(int v, int lo, int hi) { return v<lo?lo:v>hi?hi:v; }
    void Notify() { if (onChange!=null) onChange(temp.Clone()); }

    void RefreshColors() {
        for (int i=0; i<Rows.Length; i++) colorBtns[i].BackColor = Rows[i].Get(temp);
    }
    void RefreshAll() {
        RefreshColors();
        track.Value  = Clamp(temp.Size, 120, 400);
        lbSV.Text    = temp.Size+" px";
    }

    // called by ClockForm when user resizes via drag
    public void SyncSize(int sz) {
        temp.Size   = sz;
        track.Value = Clamp(sz, 120, 400);
        lbSV.Text   = sz+" px";
    }
}
