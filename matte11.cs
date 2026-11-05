using System;
using System.Drawing;
using System.Drawing.Imaging;

public class Matte11
{
    /* erpaint isolated dark specks inside the body using the local bright mean */
    public static int Go(string path, double ratio)
    {
        Bitmap bmp;
        using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))
        using (var tmp = new Bitmap(ms)) { bmp = new Bitmap(tmp); }
        int w = bmp.Width, h = bmp.Height;
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        byte[] px = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        byte[] src = (byte[])px.Clone();
        int fixedN = 0;
        int R = 3;
        for (int y = R; y < h - R; y++)
            for (int x = R; x < w - R; x++)
            {
                int i = (y * w + x) * 4;
                if (src[i + 3] < 200) continue;
                double lum = 0.114 * src[i] + 0.587 * src[i + 1] + 0.299 * src[i + 2];
                double sr = 0, sg = 0, sb = 0, sl = 0; int n = 0;
                for (int dy = -R; dy <= R; dy++)
                    for (int dx = -R; dx <= R; dx++)
                    {
                        int j = ((y + dy) * w + (x + dx)) * 4;
                        if (src[j + 3] < 200) continue;
                        double l2 = 0.114 * src[j] + 0.587 * src[j + 1] + 0.299 * src[j + 2];
                        sr += src[j + 2]; sg += src[j + 1]; sb += src[j]; sl += l2; n++;
                    }
                if (n < 20) continue;
                double ml = sl / n;
                if (lum > ml * ratio) continue;
                px[i] = (byte)(sb / n);
                px[i + 1] = (byte)(sg / n);
                px[i + 2] = (byte)(sr / n);
                fixedN++;
            }
        System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, px.Length);
        bmp.UnlockBits(bd);
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
        return fixedN;
    }
}
