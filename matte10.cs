using System;
using System.Drawing;
using System.Drawing.Imaging;

public class Matte10
{
    /* repaint the dark rim: edge pixels take the colour of clean interior pixels nearby */
    public static int Go(string path)
    {
        Bitmap bmp;
        using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))
        using (var tmp = new Bitmap(ms)) { bmp = new Bitmap(tmp); }
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        byte[] a = new byte[w * h];
        for (int i = 0; i < w * h; i++) a[i] = px[i * 4 + 3];

        bool[] edge = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (a[i] < 250) { edge[i] = true; continue; }
                bool near = false;
                for (int dy = -2; dy <= 2 && !near; dy++)
                    for (int dx = -2; dx <= 2 && !near; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (a[ny * w + nx] < 20) near = true;
                    }
                edge[i] = near;
            }

        int fixedN = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!edge[i] || a[i] < 24) continue;
                double sr = 0, sg = 0, sb = 0; int n = 0;
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int j = ny * w + nx;
                        if (a[j] < 250 || edge[j]) continue;
                        sb += px[j * 4]; sg += px[j * 4 + 1]; sr += px[j * 4 + 2]; n++;
                    }
                if (n < 3) continue;
                px[i * 4] = (byte)(sb / n);
                px[i * 4 + 1] = (byte)(sg / n);
                px[i * 4 + 2] = (byte)(sr / n);
                fixedN++;
            }

        System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, px.Length);
        bmp.UnlockBits(bd);
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
        return fixedN;
    }
}
