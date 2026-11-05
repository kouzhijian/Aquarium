using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

public class Matte2
{
    static double Hue(int r, int g, int b, out double sat, out double val)
    {
        double rr = r / 255.0, gg = g / 255.0, bb = b / 255.0;
        double mx = Math.Max(rr, Math.Max(gg, bb));
        double mn = Math.Min(rr, Math.Min(gg, bb));
        val = mx;
        sat = mx <= 0.0001 ? 0 : (mx - mn) / mx;
        double h = 0;
        double d = mx - mn;
        if (d > 0.0001)
        {
            if (mx == rr) h = ((gg - bb) / d) % 6;
            else if (mx == gg) h = (bb - rr) / d + 2;
            else h = (rr - gg) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
        }
        return h;
    }

    static byte[] Erode(byte[] m, int w, int h, int n)
    {
        byte[] o = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte v = 1;
                for (int dy = -1; dy <= 1 && v == 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h) { v = 0; break; }
                        if (m[yy * w + xx] == 0) { v = 0; break; }
                    }
                o[y * w + x] = v;
            }
        return o;
    }

    static byte[] Dilate(byte[] m, int w, int h)
    {
        byte[] o = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte v = 0;
                for (int dy = -1; dy <= 1 && v == 0; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        if (m[yy * w + xx] == 1) { v = 1; break; }
                    }
                o[y * w + x] = v;
            }
        return o;
    }

    static int[] Components(byte[] m, int w, int h, int[] label)
    {
        int n = w * h;
        for (int i = 0; i < n; i++) label[i] = 0;
        List<int> sizes = new List<int>();
        sizes.Add(0);
        int cur = 0;
        int[] stack = new int[n];
        for (int i = 0; i < n; i++)
        {
            if (m[i] == 0 || label[i] != 0) continue;
            cur++;
            int sp = 0, cnt = 0;
            stack[sp++] = i;
            label[i] = cur;
            while (sp > 0)
            {
                int p = stack[--sp];
                cnt++;
                int x = p % w, y = p / w;
                int[] nx = new int[] { x - 1, x + 1, x, x };
                int[] ny = new int[] { y, y, y - 1, y + 1 };
                for (int k = 0; k < 4; k++)
                {
                    int xx = nx[k], yy = ny[k];
                    if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                    int q = yy * w + xx;
                    if (m[q] == 0 || label[q] != 0) continue;
                    label[q] = cur;
                    stack[sp++] = q;
                }
            }
            sizes.Add(cnt);
        }
        int[] res = new int[] { cur, 0, 0 };
        int best = 0, bestIdx = 0;
        for (int i = 1; i < sizes.Count; i++) if (sizes[i] > best) { best = sizes[i]; bestIdx = i; }
        res[1] = bestIdx;
        res[2] = best;
        return res;
    }

    public static string Go(string src, string dst, int rx, int ry, int rw, int rh)
    {
        Bitmap bmp = new Bitmap(src);
        Bitmap crop = bmp.Clone(new Rectangle(rx, ry, rw, rh), PixelFormat.Format32bppArgb);
        int w = crop.Width, h = crop.Height, n = w * h;
        Color[] c = new Color[n];
        byte[] m = new byte[n];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color p = crop.GetPixel(x, y);
                c[y * w + x] = p;
                double sat, val;
                double hue = Hue(p.R, p.G, p.B, out sat, out val);
                bool bg = false;
                if (val < 0.15) bg = true;
                else if (hue >= 150 && hue <= 215 && sat > 0.12 && val < 0.80) bg = true;
                else if (sat < 0.10 && val > 0.86) bg = true;
                else if (hue > 62 && hue < 155 && sat > 0.30) bg = true;
                m[y * w + x] = bg ? (byte)0 : (byte)1;
            }

        byte[] m2 = Erode(m, w, h, n);
        m2 = Dilate(m2, w, h);
        m2 = Dilate(m2, w, h);
        int[] lab = new int[n];
        int[] comp = Components(m2, w, h, lab);
        int keep = comp[1];
        for (int i = 0; i < n; i++) m2[i] = (lab[i] == keep) ? (byte)1 : (byte)0;

        m2 = Dilate(m2, w, h);
        m2 = Dilate(m2, w, h);
        m2 = Erode(m2, w, h, n);
        m2 = Erode(m2, w, h, n);

        // fill holes: flood the background from the border, unreached 0s are holes
        byte[] out2 = new byte[n];
        int[] stack = new int[n];
        int sp = 0;
        for (int x = 0; x < w; x++)
        {
            int pa = x, pb = (h - 1) * w + x;
            if (m2[pa] == 0 && out2[pa] == 0) { out2[pa] = 1; stack[sp++] = pa; }
            if (m2[pb] == 0 && out2[pb] == 0) { out2[pb] = 1; stack[sp++] = pb; }
        }
        for (int y = 0; y < h; y++)
        {
            int pa = y * w, pb = y * w + w - 1;
            if (m2[pa] == 0 && out2[pa] == 0) { out2[pa] = 1; stack[sp++] = pa; }
            if (m2[pb] == 0 && out2[pb] == 0) { out2[pb] = 1; stack[sp++] = pb; }
        }
        while (sp > 0)
        {
            int p = stack[--sp];
            int x = p % w, y = p / w;
            int[] nx = new int[] { x - 1, x + 1, x, x };
            int[] ny = new int[] { y, y, y - 1, y + 1 };
            for (int k = 0; k < 4; k++)
            {
                int xx = nx[k], yy = ny[k];
                if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                int q = yy * w + xx;
                if (m2[q] == 1 || out2[q] == 1) continue;
                out2[q] = 1;
                stack[sp++] = q;
            }
        }
        for (int i = 0; i < n; i++) if (out2[i] == 0) m2[i] = 1;

        // soft alpha: two box blurs
        float[] a = new float[n];
        for (int i = 0; i < n; i++) a[i] = m2[i];
        for (int pass = 0; pass < 2; pass++)
        {
            float[] t = new float[n];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0; int q = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx;
                        if (xx < 0 || xx >= w) continue;
                        s += a[y * w + xx]; q++;
                    }
                    t[y * w + x] = s / q;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0; int q = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy;
                        if (yy < 0 || yy >= h) continue;
                        s += t[yy * w + x]; q++;
                    }
                    a[y * w + x] = s / q;
                }
        }

        // fins: bright non-green pixels near the body become translucent fins
        byte[] near = (byte[])m2.Clone();
        for (int k = 0; k < 9; k++) near = Dilate(near, w, h);
        for (int i2 = 0; i2 < n; i2++)
        {
            if (m2[i2] == 1 || near[i2] == 0) continue;
            double sat2, val2;
            double hue2 = Hue(c[i2].R, c[i2].G, c[i2].B, out sat2, out val2);
            bool isGreen = (hue2 > 62 && hue2 < 155 && sat2 > 0.28);
            bool isWater = (hue2 >= 148 && hue2 <= 218 && sat2 > 0.20 && val2 < 0.62);
            if (isGreen || isWater) continue;
            if (val2 < 0.60) continue;
            double fa = (val2 - 0.56) / 0.55;
            if (fa > 0.72) fa = 0.72;
            if (fa > a[i2]) a[i2] = (float)fa;
        }
        // body mean color
        double sr = 0, sg = 0, sb = 0; int cnt = 0;
        for (int i = 0; i < n; i++)
        {
            if (a[i] < 0.6) continue;
            sr += c[i].R; sg += c[i].G; sb += c[i].B; cnt++;
        }
        if (cnt > 0) { sr /= cnt; sg /= cnt; sb /= cnt; }

        Bitmap outp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int i = 0; i < n; i++)
        {
            float al = a[i];
            if (al < 0.02) { outp.SetPixel(i % w, i / w, Color.FromArgb(0, 0, 0, 0)); continue; }
            double r = c[i].R, g = c[i].G, b = c[i].B;
            double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
            double lum = 0.299 * r + 0.587 * g + 0.114 * b;
            double sat = mx > 0 ? (mx - mn) / mx : 0;
            if (lum > 130 && sat < 0.45)
            {
                double k = Math.Min(1, (lum - 130) / 70) * 0.6 + 0.15;
                r += (sr - r) * k; g += (sg - g) * k; b += (sb - b) * k;
                r *= 0.93; g *= 0.93; b *= 0.93;
            }
            if (al < 0.92)
            {
                double k = (1 - al) * 0.8;
                r += (sr - r) * k; g += (sg - g) * k; b += (sb - b) * k;
            }
            int ir = (int)Math.Round(r), ig = (int)Math.Round(g), ib = (int)Math.Round(b);
            if (ir < 0) ir = 0; if (ir > 255) ir = 255;
            if (ig < 0) ig = 0; if (ig > 255) ig = 255;
            if (ib < 0) ib = 0; if (ib > 255) ib = 255;
            int ia = (int)Math.Round(al * 255);
            if (ia < 0) ia = 0; if (ia > 255) ia = 255;
            outp.SetPixel(i % w, i / w, Color.FromArgb(ia, ir, ig, ib));
        }
        outp.Save(dst, ImageFormat.Png);
        outp.Dispose();
        crop.Dispose();
        bmp.Dispose();
        return dst + ' ' + w + 'x' + h;
    }
}