using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class Matte12
{
    /* rotate every sprite so the body's principal axis is exactly horizontal */
    public static double Go(string path)
    {
        Bitmap bmp;
        using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))
        using (var tmp = new Bitmap(ms)) { bmp = new Bitmap(tmp); }
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);

        double sx = 0, sy = 0; long n = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[(y * w + x) * 4 + 3] > 128) { sx += x; sy += y; n++; }
        if (n < 100) return 0;
        double cx = sx / n, cy = sy / n;
        double Sxx = 0, Syy = 0, Sxy = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[(y * w + x) * 4 + 3] <= 128) continue;
                double dx = x - cx, dy = y - cy;
                Sxx += dx * dx; Syy += dy * dy; Sxy += dx * dy;
            }
        double theta = 0.5 * Math.Atan2(2 * Sxy, Sxx - Syy);
        while (theta > Math.PI / 2) theta -= Math.PI;
        while (theta <= -Math.PI / 2) theta += Math.PI;
        double deg = -theta * 180.0 / Math.PI;
        if (Math.Abs(deg) < 0.6) { bmp.Dispose(); return deg; }

        Bitmap outB = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outB))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TranslateTransform(w / 2f, h / 2f);
            g.RotateTransform((float)deg);
            g.TranslateTransform(-w / 2f, -h / 2f);
            g.DrawImage(bmp, 0, 0, w, h);
        }
        bmp.Dispose();
        outB.Save(path, ImageFormat.Png);
        outB.Dispose();
        return deg;
    }
}
