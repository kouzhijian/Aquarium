param([string]$Mode = "kill", [string]$Out = "")
Add-Type -TypeDefinition @"
using System;using System.Text;using System.Runtime.InteropServices;
public class Win9 {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, EnumProc cb, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr h);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string c, string n);
  public static int Kill(string part){
    int n = 0;
    EnumProc cb = delegate(IntPtr h, IntPtr l){
      StringBuilder cn = new StringBuilder(256); GetClassName(h, cn, 256);
      if (cn.ToString() == "Chrome_WidgetWin_1"){
        int len = GetWindowTextLength(h);
        if (len > 0){
          StringBuilder sb = new StringBuilder(len + 2); GetWindowText(h, sb, sb.Capacity);
          if (sb.ToString().IndexOf(part, StringComparison.Ordinal) >= 0){
            PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); n++;
          }
        }
      }
      return true;
    };
    EnumWindows(cb, IntPtr.Zero);
    IntPtr pm = FindWindow("Progman", null);
    IntPtr ww = FindWindowEx(pm, IntPtr.Zero, "WorkerW", null);
    while (ww != IntPtr.Zero){
      EnumChildWindows(ww, cb, IntPtr.Zero);
      ww = FindWindowEx(pm, ww, "WorkerW", null);
    }
    return n;
  }
  public static int Count(string part){
    int n = 0;
    EnumProc cb = delegate(IntPtr h, IntPtr l){
      int len = GetWindowTextLength(h);
      if (len > 0){
        StringBuilder sb = new StringBuilder(len + 2); GetWindowText(h, sb, sb.Capacity);
        if (sb.ToString().IndexOf(part, StringComparison.Ordinal) >= 0) n++;
      }
      return true;
    };
    EnumWindows(cb, IntPtr.Zero);
    return n;
  }
  public static string Titles(string part){
    string o = "";
    EnumProc cb = delegate(IntPtr h, IntPtr l){
      int len = GetWindowTextLength(h);
      if (len > 0){
        StringBuilder sb = new StringBuilder(len + 2); GetWindowText(h, sb, sb.Capacity);
        string s = sb.ToString();
        if (s.IndexOf(part, StringComparison.Ordinal) >= 0) o += s + " | ";
      }
      return true;
    };
    EnumWindows(cb, IntPtr.Zero);
    return o;
  }
}
"@
$part = [string]([char]0x9C7C)
if ($Mode -eq "kill") {
  $k = [Win9]::Kill($part)
  Start-Sleep -Seconds 3
  $k2 = [Win9]::Kill($part)
  Start-Sleep -Seconds 2
  "killed=" + $k + ",+" + $k2 + " remaining=" + [Win9]::Count($part)
} else {
  "count=" + [Win9]::Count($part) + " titles=" + [Win9]::Titles($part)
}
