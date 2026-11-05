param([string]$Out = "$env:TEMP\aq-shot\cap.png")
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System;using System.Text;using System.Drawing;using System.Runtime.InteropServices;
public class Cap3 {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr GetDesktopWindow();
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
  [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
  [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
  [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr o);
  [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
  [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  static IntPtr found = IntPtr.Zero;
  static string part;
  static int scanned = 0;
  public static string Diag = "";
  public static IntPtr Find(string p){
    part = p; found = IntPtr.Zero; scanned = 0; Diag = "";
    EnumProc cb = delegate(IntPtr h, IntPtr l){
      scanned++;
      StringBuilder cn = new StringBuilder(256); GetClassName(h, cn, 256);
      if (cn.ToString().IndexOf("Chrome_WidgetWin", StringComparison.Ordinal) >= 0){
        int len = GetWindowTextLength(h);
        if (len > 0){
          StringBuilder sb = new StringBuilder(len + 2); GetWindowText(h, sb, sb.Capacity);
          string s = sb.ToString();
          Diag += s + " | ";
          if (found == IntPtr.Zero && s.IndexOf(part, StringComparison.Ordinal) >= 0) found = h;
        }
      }
      return true;
    };
    EnumWindows(cb, IntPtr.Zero);
    EnumChildWindows(GetDesktopWindow(), cb, IntPtr.Zero);
    return found;
  }
  public static string Shot(IntPtr h, string path){
    RECT r; GetWindowRect(h, out r);
    int w = r.R - r.L, ht = r.B - r.T;
    RECT cr; GetClientRect(h, out cr);
    if (w <= 0 || ht <= 0){ w = cr.R; ht = cr.B; }
    if (w <= 0 || ht <= 0) return "BAD_RECT";
    IntPtr screen = GetDC(IntPtr.Zero);
    IntPtr mem = CreateCompatibleDC(screen);
    IntPtr bmp = CreateCompatibleBitmap(screen, w, ht);
    IntPtr old = SelectObject(mem, bmp);
    bool ok = PrintWindow(h, mem, 2);
    Image img = Image.FromHbitmap(bmp);
    img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    img.Dispose();
    SelectObject(mem, old); DeleteObject(bmp); DeleteDC(mem); ReleaseDC(IntPtr.Zero, screen);
    return (ok ? "OK " : "PRINTFAIL ") + w + "x" + ht + " scanned=" + scanned;
  }
}
"@ -ReferencedAssemblies System.Drawing
[Cap3]::SetProcessDPIAware() | Out-Null
$part = [string]([char]0x9C7C) + [string]([char]0x7F38)
$h = [Cap3]::Find($part)
if ($h -eq 0) { "NO_WINDOW | " + [Cap3]::Diag; exit 1 }
[Cap3]::Shot($h, $Out)
