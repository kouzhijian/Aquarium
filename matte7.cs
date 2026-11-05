using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

public class Matte7
{
    public static void Go(string src, string dst, double tol, int closeR)
    {
        Bitmap bmp = new Bitmap(src);
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);

        /* background colour = mean of the 4 border strips */
        double br = 0, bg = 0, bb = 0; int nb = 0;
        int strip = Math.Max(1, Math.Min(w, h) / 40);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (x >= strip && x < w - strip && y >= strip && y < h - strip) continue;
                int i = (y * w + x) * 4;
                bb += px[i]; bg += px[i + 1]; br += px[i + 2]; nb++;
            }
        br /= nb; bg /= nb; bb /= nb;

        byte[] bgm = new byte[w * h];
        for (int i = 0; i < w * h; i++)
        {
            double dr = px[i * 4 + 2] - br, dg = px[i * 4 + 1] - bg, db = px[i * 4] - bb;
            bgm[i] = (byte)(Math.Sqrt(dr * dr + dg * dg + db * db) < tol ? 1 : 0);
        }

        bool[] seen = new bool[w * h];
        int[] st = new int[w * h];
        int top = 0;
        for (int x = 0; x < w; x++) { Seed(bgm, seen, st, ref top, w, x, 0); Seed(bgm, seen, st, ref top, w, x, h - 1); }
        for (int y = 0; y < h; y++) { Seed(bgm, seen, st, ref top, w, 0, y); Seed(bgm, seen, st, ref top, w, w - 1, y); }
        while (top > 0)
        {
            int i = st[--top];
            int x = i % w, y = i / w;
            if (x > 0) Seed(bgm, seen, st, ref top, w, x - 1, y);
            if (x < w - 1) Seed(bgm, seen, st, ref top, w, x + 1, y);
            if (y > 0) Seed(bgm, seen, st, ref top, w, x, y - 1);
            if (y < h - 1) Seed(bgm, seen, st, ref top, w, x, y + 1);
        }

        byte[] fg = new byte[w * h];
        for (int i = 0; i < w * h; i++) fg[i] = (byte)(seen[i] ? 0 : 255);

        byte[] c1 = Dilate(fg, w, h, closeR);
        c1 = Erode(c1, w, h, closeR);

        int[] lab = new int[w * h];
        int cur = 0, best = 0, bestArea = 0;
        for (int i = 0; i < w * h; i++)
        {
            if (c1[i] == 0 || lab[i] != 0) continue;
            cur++;
            int sp = 0, area = 0; st[sp++] = i; lab[i] = cur;
            while (sp > 0)
            {
                int j = st[--sp]; area++;
                int x = j % w, y = j / w;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + (k == 0 ? -1 : k == 1 ? 1 : 0);
                    int ny = y + (k == 2 ? -1 : k == 3 ? 1 : 0);
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int nj = ny * w + nx;
                    if (c1[nj] == 0 || lab[nj] != 0) continue;
                    lab[nj] = cur; st[sp++] = nj;
                }
            }
            if (area > bestArea) { bestArea = area; best = cur; }
        }

        double[] a = new double[w * h];
        for (int i = 0; i < w * h; i++) a[i] = (lab[i] == best) ? 1.0 : 0.0;

        double[] bl = new double[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double s = 0; int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        s += a[ny * w + nx]; n++;
                    }
                bl[y * w + x] = s / n;
            }
        for (int i = 0; i < w * h; i++)
        {
            double v = bl[i];
            a[i] = v >= 0.96 ? 1.0 : (v > 0.45 ? (v - 0.45) / 0.51 : 0.0);
        }

        byte[] outpx = new byte[px.Length];
        for (int i = 0; i < w * h; i++)
        {
            double al = a[i];
            if (al <= 0.02) continue;
            outpx[i * 4 + 0] = px[i * 4 + 0];
            outpx[i * 4 + 1] = px[i * 4 + 1];
            outpx[i * 4 + 2] = px[i * 4 + 2];
            outpx[i * 4 + 3] = (byte)(al * 255);
        }

        Bitmap outb = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        BitmapData ob = outb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(outpx, 0, ob.Scan0, outpx.Length);
        outb.UnlockBits(ob);
        outb.Save(dst, ImageFormat.Png);
        outb.Dispose(); bmp.Dispose();
    }

    /* trim fully transparent margins */
    public static void Trim(string path)
    {
        Bitmap bmp = new Bitmap(path);
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[(y * w + x) * 4 + 3] < 12) continue;
                if (x < x0) x0 = x; if (x > x1) x1 = x;
                if (y < y0) y0 = y; if (y > y1) y1 = y;
            }
        if (x1 < 0) { bmp.Dispose(); return; }
        int nw = x1 - x0 + 1, nh = y1 - y0 + 1;
        Bitmap outb = new Bitmap(nw, nh, PixelFormat.Format32bppArgb);
        BitmapData ob = outb.LockBits(new Rectangle(0, 0, nw, nh), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        byte[] o = new byte[nw * nh * 4];
        for (int y = 0; y < nh; y++)
            for (int x = 0; x < nw; x++)
            {
                int si = ((y + y0) * w + (x + x0)) * 4;
                int di = (y * nw + x) * 4;
                o[di] = px[si]; o[di + 1] = px[si + 1]; o[di + 2] = px[si + 2]; o[di + 3] = px[si + 3];
            }
        System.Runtime.InteropServices.Marshal.Copy(o, 0, ob.Scan0, o.Length);
        outb.UnlockBits(ob);
        bmp.Dispose();
        outb.Save(path, ImageFormat.Png);
        outb.Dispose();
    }

    static void Seed(byte[] m, bool[] seen, int[] st, ref int top, int w, int x, int y)
    {
        int i = y * w + x;
        if (i < 0 || i >= m.Length) return;
        if (seen[i] || m[i] == 0) return;
        seen[i] = true; st[top++] = i;
    }

    static byte[] Dilate(byte[] m, int w, int h, int r)
    {
        byte[] o = new byte[m.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (m[y * w + x] == 0) continue;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        o[ny * w + nx] = 255;
                    }
            }
        return o;
    }

    static byte[] Erode(byte[] m, int w, int h, int r)
    {
        byte[] o = new byte[m.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool all = true;
                for (int dy = -r; dy <= r && all; dy++)
                    for (int dx = -r; dx <= r && all; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) { all = false; break; }
                        if (m[ny * w + nx] == 0) all = false;
                    }
                o[y * w + x] = (byte)(all ? 255 : 0);
            }
        return o;
    }
}
