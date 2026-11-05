
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
public class PxE {
  static double Cl(double v, double a, double b){ return v < a ? a : (v > b ? b : (v - a) / (b - a)); }
  static double Sm(double t){ t = Cl(t, 0, 1); return t * t * (3 - 2 * t); }
  static bool IsBg(Color c){
    int lum = Math.Max(c.R, Math.Max(c.G, c.B));
    int mx = Math.Max(c.R, Math.Max(c.G, c.B)), mn = Math.Min(c.R, Math.Min(c.G, c.B));
    double sat = mx <= 0 ? 0 : (double)(mx - mn) / mx;
    if (lum < 74) return true;
    if (c.B >= c.G - 4 && c.B > c.R + 4 && lum < 132) return true;
    if (sat < 0.17 && lum > 104) return true;
    if (c.G > c.R * 1.14 + 12 && c.G > c.B + 8) return true;
    return false;
  }
  static double Tighten(double a){
    if (a < 0.55) return 0;
    return 0.86 + 0.14 * a;
  }
  public static string Fish(string src, string outPath, int rx, int ry, int rw, int rh, int darkCut){
    Bitmap b = new Bitmap(src);
    int w = Math.Min(rw, b.Width - rx), h = Math.Min(rh, b.Height - ry);
    Bitmap crop = b.Clone(new Rectangle(rx, ry, w, h), PixelFormat.Format32bppArgb);
    bool[,] bg = new bool[w, h];
    bool[,] seen = new bool[w, h];
    Queue<int> q = new Queue<int>();
    for (int x = 0; x < w; x++){ q.Enqueue(x); q.Enqueue(0); q.Enqueue(x); q.Enqueue(h - 1); }
    for (int y = 0; y < h; y++){ q.Enqueue(0); q.Enqueue(y); q.Enqueue(w - 1); q.Enqueue(y); }
    while (q.Count > 0){
      int y = q.Dequeue(); int x = q.Dequeue();
      if (x < 0 || y < 0 || x >= w || y >= h || seen[x, y]) continue;
      if (!IsBg(crop.GetPixel(x, y))) continue;
      seen[x, y] = true; bg[x, y] = true;
      q.Enqueue(x + 1); q.Enqueue(y); q.Enqueue(x - 1); q.Enqueue(y);
      q.Enqueue(x); q.Enqueue(y + 1); q.Enqueue(x); q.Enqueue(y - 1);
    }
    bool[,] body = new bool[w, h];
    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) body[x, y] = !IsBg(crop.GetPixel(x, y));
    int R = Math.Max(6, Math.Min(w, h) / 16);
    bool[,] near = new bool[w, h];
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        if (!body[x, y]) continue;
        for (int dy2 = -R; dy2 <= R; dy2++){
          int yy = y + dy2; if (yy < 0 || yy >= h) continue;
          for (int dx2 = -R; dx2 <= R; dx2++){
            int xx = x + dx2; if (xx < 0 || xx >= w) continue;
            near[xx, yy] = true;
          }
        }
      }
    }
    double[,] am = new double[w, h];
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        Color c0 = crop.GetPixel(x, y);
        bool kill0 = bg[x, y] || (IsBg(c0) && !near[x, y]);
        if (kill0) { am[x, y] = 0; continue; }
        int lum0 = Math.Max(c0.R, Math.Max(c0.G, c0.B));
        double a0 = 0.62 + 0.38 * Sm((lum0 - 80) / 90.0);
        a0 *= Sm((lum0 - darkCut) / 42.0);
        if (c0.G > c0.R * 1.14 + 12 && c0.G > c0.B + 8) a0 *= 0.05;
        am[x, y] = Tighten(a0);
      }
    }
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        if (am[x, y] <= 0) continue;
        int n = 0;
        for (int dy3 = -1; dy3 <= 1; dy3++){
          for (int dx3 = -1; dx3 <= 1; dx3++){
            int xx = x + dx3, yy = y + dy3;
            if (xx >= 0 && yy >= 0 && xx < w && yy < h && am[xx, yy] > 0.5) n++;
          }
        }
        if (n < 3) am[x, y] = 0;
      }
    }
    Bitmap o = new Bitmap(w, h, PixelFormat.Format32bppArgb);
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        Color c = crop.GetPixel(x, y);
        bool kill = am[x, y] <= 0;
        if (kill){ o.SetPixel(x, y, Color.FromArgb(0, c.R, c.G, c.B)); continue; }
        int lum = Math.Max(c.R, Math.Max(c.G, c.B));
        double a = 0.62 + 0.38 * Sm((lum - 80) / 90.0);
        a *= Sm((lum - darkCut) / 42.0);
        if (c.G > c.R * 1.14 + 12 && c.G > c.B + 8) a *= 0.05;
        bool edge = (x > 0 && bg[x - 1, y]) || (x < w - 1 && bg[x + 1, y]) ||
                    (y > 0 && bg[x, y - 1]) || (y < h - 1 && bg[x, y + 1]);
        if (edge) a *= 0.6;
        a = am[x, y];
        double rim = (x > 0 && am[x - 1, y] <= 0) || (x < w - 1 && am[x + 1, y] <= 0) || (y > 0 && am[x, y - 1] <= 0) || (y < h - 1 && am[x, y + 1] <= 0) ? 0.72 : 1.0;
        o.SetPixel(x, y, Color.FromArgb((int)(a * 255), (int)(c.R * rim), (int)(c.G * rim), (int)(c.B * rim)));
      }
    }
    o.Save(outPath, ImageFormat.Png);
    b.Dispose(); crop.Dispose(); o.Dispose();
    return "fish " + w + "x" + h;
  }
  public static string Plant(string src, string outPath, int rx, int ry, int rw, int rh){
    Bitmap b = new Bitmap(src);
    int w = Math.Min(rw, b.Width - rx), h = Math.Min(rh, b.Height - ry);
    Bitmap crop = b.Clone(new Rectangle(rx, ry, w, h), PixelFormat.Format32bppArgb);
    Bitmap o = new Bitmap(w, h, PixelFormat.Format32bppArgb);
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        Color c = crop.GetPixel(x, y);
        double greenness = Sm((c.G - c.B - 6) / 16.0);
        double bright = Sm((c.G - 55) / 60.0);
        double a = greenness * bright;
        if (c.G < 50) a = 0;
        a = Tighten(a);
        o.SetPixel(x, y, Color.FromArgb((int)(a * 255), c.R, c.G, c.B));
      }
    }
    o.Save(outPath, ImageFormat.Png);
    b.Dispose(); crop.Dispose(); o.Dispose();
    return "plant " + w + "x" + h;
  }
}
