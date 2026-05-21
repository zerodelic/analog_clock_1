using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class MakeIcon
{
    static void Main()
    {
        int[] sizes = { 16, 32, 48, 256 };
        var pngs = new byte[sizes.Length][];

        for (int si = 0; si < sizes.Length; si++) {
            int sz = sizes[si];
            using (var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp)) {
                g.Clear(Color.Transparent);
                g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                DrawClockIcon(g, sz);
                using (var ms = new MemoryStream()) {
                    bmp.Save(ms, ImageFormat.Png);
                    pngs[si] = ms.ToArray();
                }
            }
        }

        WriteIco(pngs, @"C:\Git_Repository\analog_clock_1\clock.ico");
        Console.WriteLine("clock.ico created.");
    }

    static void DrawClockIcon(Graphics g, int sz)
    {
        float cx = sz / 2f, cy = sz / 2f, r = sz / 2f - 1f;

        // 外枠グラデーション風（濃いグレー）
        using (var b = new SolidBrush(Color.FromArgb(40, 40, 40)))
            g.FillEllipse(b, 1, 1, sz - 2, sz - 2);

        // 文字盤（少し明るいグレー）
        using (var b = new SolidBrush(Color.FromArgb(55, 55, 65)))
            g.FillEllipse(b, sz*0.08f, sz*0.08f, sz*0.84f, sz*0.84f);

        // 外周ライン
        using (var p = new Pen(Color.FromArgb(120, 120, 130), Math.Max(1f, sz * 0.03f)))
            g.DrawEllipse(p, 1, 1, sz - 2, sz - 2);

        // 目盛り
        for (int i = 0; i < 12; i++) {
            double a = i * 30 * Math.PI / 180;
            float len = (i % 3 == 0) ? r * 0.18f : r * 0.10f;
            float w   = (i % 3 == 0) ? Math.Max(1.5f, sz * 0.04f) : Math.Max(1f, sz * 0.02f);
            var  col  = (i % 3 == 0) ? Color.FromArgb(200, 200, 210) : Color.FromArgb(130, 130, 140);
            using (var p = new Pen(col, w)) {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                float x1 = cx + r * 0.82f * (float)Math.Sin(a);
                float y1 = cy - r * 0.82f * (float)Math.Cos(a);
                float x2 = cx + (r * 0.82f - len) * (float)Math.Sin(a);
                float y2 = cy - (r * 0.82f - len) * (float)Math.Cos(a);
                g.DrawLine(p, x1, y1, x2, y2);
            }
        }

        // 時針（10時10分頃）
        double hA = (10 + 10.0/60) * 30 * Math.PI / 180;
        double mA = 10 * 6 * Math.PI / 180;
        float  hw = Math.Max(1.5f, sz * 0.07f);

        using (var p = new Pen(Color.FromArgb(230, 230, 235), hw)) {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            g.DrawLine(p, cx, cy,
                cx + r * 0.45f * (float)Math.Sin(hA),
                cy - r * 0.45f * (float)Math.Cos(hA));
        }
        // 分針
        using (var p = new Pen(Color.FromArgb(210, 210, 220), hw * 0.6f)) {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            g.DrawLine(p, cx, cy,
                cx + r * 0.65f * (float)Math.Sin(mA),
                cy - r * 0.65f * (float)Math.Cos(mA));
        }
        // 秒針
        using (var p = new Pen(Color.FromArgb(255, 70, 70), Math.Max(1f, sz * 0.025f))) {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            g.DrawLine(p, cx, cy,
                cx + r * 0.72f * (float)Math.Sin(mA + 0.3),
                cy - r * 0.72f * (float)Math.Cos(mA + 0.3));
        }
        // 中心点
        float dr = Math.Max(1.5f, sz * 0.06f);
        using (var b = new SolidBrush(Color.FromArgb(255, 70, 70)))
            g.FillEllipse(b, cx - dr, cy - dr, dr * 2, dr * 2);
    }

    static void WriteIco(byte[][] pngs, string path)
    {
        int count = pngs.Length;
        int headerSize = 6 + 16 * count;
        int offset = headerSize;

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs)) {
            // ICO header
            bw.Write((short)0);      // reserved
            bw.Write((short)1);      // type: icon
            bw.Write((short)count);

            // directory entries
            int[] offsets = new int[count];
            int cur = headerSize;
            for (int i = 0; i < count; i++) {
                offsets[i] = cur;
                cur += pngs[i].Length;
            }
            int[] widths = { 16, 32, 48, 256 };
            for (int i = 0; i < count; i++) {
                int w = widths[i];
                bw.Write((byte)(w == 256 ? 0 : w));  // width
                bw.Write((byte)(w == 256 ? 0 : w));  // height
                bw.Write((byte)0);   // color count
                bw.Write((byte)0);   // reserved
                bw.Write((short)1);  // planes
                bw.Write((short)32); // bit count
                bw.Write(pngs[i].Length);
                bw.Write(offsets[i]);
            }
            // image data
            foreach (var png in pngs)
                bw.Write(png);
        }
    }
}
