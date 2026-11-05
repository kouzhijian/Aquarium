param([string]$Out = 'C:\Users\bestv\AppData\Local\Temp\aq-shot\a.png', [long]$Hwnd = 0)
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class CP8 {
  [DllImport("user32.dll")] public static extern bool EnumWindows(P cb, IntPtr l);
  public delegate bool P(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, P cb, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
  [StructLayout(LayoutKind.Sequential)] public struct R { public int L,T,Rr,B; }
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f);
  public static IntPtr Hit = IntPtr.Zero;
  public static string Txt(IntPtr h){ var sb=new StringBuilder(256); GetWindowText(h,sb,256); return sb.ToString(); }
  public static void Scan(){
    EnumWindows(delegate(IntPtr h, IntPtr l){
      if (Txt(h) == "AQUARIUM_WALL_LAYER") { Hit = h; return false; }
      bool f = false;
      EnumChildWindows(h, delegate(IntPtr c, IntPtr l2){
        if (Txt(c) == "AQUARIUM_WALL_LAYER") { Hit = c; f = true; return false; }
        return true;
      }, IntPtr.Zero);
      if (f) return false;
      return true;
    }, IntPtr.Zero);
  }
}
'@
if ($Hwnd -gt 0) { $h = [IntPtr]$Hwnd } else { [CP8]::Scan(); $h = [CP8]::Hit }
if ($h -eq [IntPtr]::Zero) { 'NO_WINDOW'; exit 1 }
$r = New-Object CP8+R
[CP8]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Rr - $r.L; $ht = $r.B - $r.T
if ($w -le 0 -or $ht -le 0) { 'BAD_RECT'; exit 1 }
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[CP8]::PrintWindow($h, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
'hwnd=' + $h + ' size=' + $w + 'x' + $ht + ' saved=' + $Out