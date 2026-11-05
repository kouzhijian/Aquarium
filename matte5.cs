using System;
using System.Drawing;
using System.Drawing.Imaging;

public class Matte5
{
    public static void Go(string src, string dst, int closeR, double satMin, int hueLo, int hueHi)
    {
        Bitmap bmp = new Bitmap(src);
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        bmp.UnlockBits(bd);

        /* foreground = NOT purple-ish AND NOT near-black */
        byte[] fg = new byte[w * h];
        for (int i = 0; i < w * h; i++)
        {
            int r = px[i * 4 + 2], g = px[i * 4 + 1], b = px[i * 4];
            double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
            double val = mx / 255.0;
            double sat = mx <= 0 ? 0 : (mx - mn) / mx;
            double hue = 0;
            if (mx > mn)
            {
                double d = mx - mn;
                if (mx == r) hue = 60 * (((g - b) / d) % 6);
                else if (mx == g) hue = 60 * (((b - r) / d) + 2);
                else hue = 60 * (((r - g) / d) + 4);
                if (hue < 0) hue += 360;
            }
            bool purple = (hue >= hueLo && hue <= hueHi && sat >= satMin);
            bool dark = val < 0.16;
            bool washed = (sat < 0.22 && val < 0.60);
            fg[i] = (byte)((purple || dark) ? 0 : 255);
        }

        /* closing: dilate then erode, reconnects fins separated by dark bands */
        byte[] d1 = Dilate(fg, w, h, closeR);
        byte[] c1 = Erode(d1, w, h, closeR);
        c1 = Erode(c1, w, h, 6);
        c1 = Dilate(c1, w, h, 6);

        /* largest connected component of the closed mask */
        int[] lab = new int[w * h];
        int[] stack = new int[w * h];
        int cur = 0, best = 0, bestArea = 0;
        for (int i = 0; i < w * h; i++)
        {
            if (c1[i] == 0 || lab[i] != 0) continue;
            cur++;
            int sp = 0, area = 0; stack[sp++] = i; lab[i] = cur;
            while (sp > 0)
            {
                int j = stack[--sp]; area++;
                int x = j % w, y = j / w;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + (k == 0 ? -1 : k == 1 ? 1 : 0);
                    int ny = y + (k == 2 ? -1 : k == 3 ? 1 : 0);
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int nj = ny * w + nx;
                    if (c1[nj] == 0 || lab[nj] != 0) continue;
                    lab[nj] = cur; stack[sp++] = nj;
                }
            }
            if (area > bestArea) { bestArea = area; best = cur; }
        }

        double[] a = new double[w * h];
        for (int i = 0; i < w * h; i++) a[i] = (lab[i] == best) ? 1.0 : 0.0;

        /* feather */
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
            a[i] = v >= 0.95 ? 1.0 : (v > 0.40 ? (v - 0.40) / 0.55 : 0.0);
        }

        /* darkPurple: strip leftover dark-mauve background patches */
        for (int i = 0; i < w * h; i++){
            if (a[i] <= 0.02) continue;
            int rr = px[i * 4 + 2], gg = px[i * 4 + 1], bb2 = px[i * 4];
            double mmx = Math.Max(rr, Math.Max(gg, bb2)), mmn = Math.Min(rr, Math.Min(gg, bb2));
            double vv = mmx / 255.0;
            double ss = mmx <= 0 ? 0 : (mmx - mmn) / mmx;
            double hh = 0;
            if (mmx > mmn){
                double dd2 = mmx - mmn;
                if (mmx == rr) hh = 60 * (((gg - bb2) / dd2) % 6);
                else if (mmx == gg) hh = 60 * (((bb2 - rr) / dd2) + 2);
                else hh = 60 * (((rr - gg) / dd2) + 4);
                if (hh < 0) hh += 360;
            }
            if (vv < 0.92 && hh >= 275 && hh <= 356 && ss >= 0.15) a[i] = 0;
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
