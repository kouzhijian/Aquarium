param([string]$Out = "$env:TEMP\aq-shot\cap.png")
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public class D3 {
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowW(string c, string n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  public struct RECT { public int L, T, R, B; }
  public static string Txt(IntPtr h){ StringBuilder s = new StringBuilder(256); GetWindowTextW(h, s, 256); return s.ToString(); }
  public static string Cls(IntPtr h){ StringBuilder s = new StringBuilder(256); GetClassNameW(h, s, 256); return s.ToString(); }
  public static IntPtr hit = IntPtr.Zero;
  public static void Walk(IntPtr h, int d){
    if (d > 4 || hit != IntPtr.Zero) return;
    IntPtr c = GetWindow(h, 5);
    while (c != IntPtr.Zero){
      if (Cls(c).StartsWith("Chrome_WidgetWin")){ hit = c; return; }
      Walk(c, d + 1);
      if (hit != IntPtr.Zero) return;
      c = GetWindow(c, 2);
    }
  }
  public static IntPtr Find(){
    IntPtr prog = FindWindowW("Progman", null);
    hit = IntPtr.Zero;
    if (prog != IntPtr.Zero) Walk(prog, 0);
    return hit;
  }
}
'@
$h = [D3]::Find()
if ($h -eq [IntPtr]::Zero) { 'NO_WINDOW'; exit 1 }
'hwnd=' + $h + ' title=[' + [D3]::Txt($h) + ']'
$r = New-Object D3+RECT
[D3]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.R - $r.L; $hh = $r.B - $r.T
'rect=' + $w + 'x' + $hh + ' at ' + $r.L + ',' + $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $hh
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
$ok = [D3]::PrintWindow($h, $hdc, 2)
$g.ReleaseHdc($hdc)
$g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
'printed=' + $ok