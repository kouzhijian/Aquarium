
using System;
using System.Drawing;
using System.Drawing.Imaging;
public class Tint {
  static void Rgb2Hsv(double r, double g, double b, out double h, out double s, out double v){
    double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
    v = mx; double d = mx - mn;
    s = mx <= 0 ? 0 : d / mx;
    if (d <= 0) { h = 0; return; }
    if (mx == r) h = 60 * (((g - b) / d) % 6);
    else if (mx == g) h = 60 * (((b - r) / d) + 2);
    else h = 60 * (((r - g) / d) + 4);
    if (h < 0) h += 360;
  }
  static void Hsv2Rgb(double h, double s, double v, out int r, out int g, out int b){
    h = ((h % 360) + 360) % 360;
    double c = v * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = v - c;
    double rr = 0, gg = 0, bb = 0;
    if (h < 60) { rr = c; gg = x; }
    else if (h < 120) { rr = x; gg = c; }
    else if (h < 180) { gg = c; bb = x; }
    else if (h < 240) { gg = x; bb = c; }
    else if (h < 300) { rr = x; bb = c; }
    else { rr = c; bb = x; }
    r = (int)Math.Round((rr + m) * 255); g = (int)Math.Round((gg + m) * 255); b = (int)Math.Round((bb + m) * 255);
    if (r > 255) r = 255; if (g > 255) g = 255; if (b > 255) b = 255;
    if (r < 0) r = 0; if (g < 0) g = 0; if (b < 0) b = 0;
  }
  public static string Go(string src, string dst, double hue, double sat, double val){
    Bitmap bmp = new Bitmap(src);
    int w = bmp.Width, h = bmp.Height;
    Bitmap o = new Bitmap(w, h, PixelFormat.Format32bppArgb);
    for (int y = 0; y < h; y++){
      for (int x = 0; x < w; x++){
        Color c = bmp.GetPixel(x, y);
        if (c.A == 0){ o.SetPixel(x, y, c); continue; }
        double hh, ss, vv;
        Rgb2Hsv(c.R / 255.0, c.G / 255.0, c.B / 255.0, out hh, out ss, out vv);
        hh += hue; ss *= sat; vv *= val;
        if (ss > 1) ss = 1; if (vv > 1) vv = 1;
        int r, g, b2;
        Hsv2Rgb(hh, ss, vv, out r, out g, out b2);
        o.SetPixel(x, y, Color.FromArgb(c.A, r, g, b2));
      }
    }
    o.Save(dst, ImageFormat.Png);
    bmp.Dispose(); o.Dispose();
    return System.IO.Path.GetFileName(dst) + " " + w + "x" + h;
  }
}
