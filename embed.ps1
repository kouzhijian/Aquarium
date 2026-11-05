
<#
  Aquarium -> Desktop wallpaper layer (behind desktop icons).
  Embed:   reparent the Chrome app window into WorkerW (the layer behind icons)
  Restore: detach it back to a normal window
  NOTE: ASCII only (Windows PowerShell 5.1 reads .ps1 as ANSI).
#>
param([ValidateSet('Embed','Restore')][string]$Mode = 'Embed')

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if (-not ('Win32Wall' -as [type])) {
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;

public class Win32Wall {
  public delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);

  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string name);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string name);
  [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
  [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hwnd, int index);
  [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hwnd, int index, int value);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out RECT r);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextLength(IntPtr hwnd);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int cmd);

  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

  public const int GWL_STYLE = -16;
  public const int WS_CHILD = 0x40000000;
  public const int WS_VISIBLE = 0x10000000;
  public const int WS_POPUP = unchecked((int)0x80000000);
  public const int WS_CAPTION = 0x00C00000;
  public const int WS_THICKFRAME = 0x00040000;
  public const int WS_SYSMENU = 0x00080000;
  public const int WS_MINIMIZEBOX = 0x00020000;
  public const int WS_MAXIMIZEBOX = 0x00010000;
  public const int WS_OVERLAPPEDWINDOW = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
  public const uint SWP_NOZORDER = 0x0004;
  public const uint SWP_NOACTIVATE = 0x0010;
  public const uint SWP_FRAMECHANGED = 0x0020;
  public const uint SWP_SHOWWINDOW = 0x0040;

  public static IntPtr FoundHwnd = IntPtr.Zero;

  public static IntPtr FindByTitlePart(string part) {
    FoundHwnd = IntPtr.Zero;
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      int len = GetWindowTextLength(h);
      if (len > 0) {
        StringBuilder sb = new StringBuilder(len + 2);
        GetWindowText(h, sb, sb.Capacity);
        if (sb.ToString().IndexOf(part, StringComparison.Ordinal) >= 0) { FoundHwnd = h; return false; }
      }
      return true;
    }, IntPtr.Zero);
    return FoundHwnd;
  }

  public static IntPtr GetWorkerW() {
    IntPtr progman = FindWindow("Progman", null);
    IntPtr result;
    SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out result);

    IntPtr workerw = IntPtr.Zero;
    for (int attempt = 0; attempt < 10 && workerw == IntPtr.Zero; attempt++) {
      // Windows 11: Progman hosts SHELLDLL_DefView (icons) plus a WorkerW sibling behind it
      IntPtr w1 = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
      if (w1 != IntPtr.Zero) return w1;

      // Windows 10 classic: top-level WorkerW after the window owning SHELLDLL_DefView
      EnumWindows(delegate(IntPtr top, IntPtr l) {
        IntPtr defView = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView != IntPtr.Zero) {
          IntPtr w = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
          if (w != IntPtr.Zero) { workerw = w; return false; }
        }
        return true;
      }, IntPtr.Zero);
      if (workerw == IntPtr.Zero) System.Threading.Thread.Sleep(300);
    }
    return workerw;
  }

  public static string Embed(IntPtr hwnd, IntPtr parent) {
    int style = GetWindowLong(hwnd, GWL_STYLE);
    style = (style & ~WS_POPUP) & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_SYSMENU & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX;
    style = style | WS_CHILD | WS_VISIBLE;
    SetWindowLong(hwnd, GWL_STYLE, style);
    SetParent(hwnd, parent);
    RECT r;
    GetClientRect(parent, out r);
    SetWindowPos(hwnd, IntPtr.Zero, 0, 0, r.Right - r.Left, r.Bottom - r.Top,
      SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
    return "parent=" + GetParent(hwnd) + " size=" + (r.Right - r.Left) + "x" + (r.Bottom - r.Top);
  }

  public static string Detach(IntPtr hwnd, int w, int h) {
    SetParent(hwnd, IntPtr.Zero);
    int style = GetWindowLong(hwnd, GWL_STYLE);
    style = (style & ~WS_CHILD) | WS_OVERLAPPEDWINDOW | WS_VISIBLE;
    SetWindowLong(hwnd, GWL_STYLE, style);
    SetWindowPos(hwnd, IntPtr.Zero, 160, 70, w, h, SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    ShowWindow(hwnd, 9);
    return "detached";
  }
}
"@
}

# 'yu gang' = the window title marker, built from code points so this file stays ASCII
$titlePart = [string]([char]0x9C7C) + [string]([char]0x7F38)

if ($Mode -eq 'Embed') {
  $hwnd = [IntPtr]::Zero
  for ($i = 0; $i -lt 40 -and $hwnd -eq [IntPtr]::Zero; $i++) {
    $hwnd = [Win32Wall]::FindByTitlePart($titlePart)
    if ($hwnd -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 400 }
  }
  if ($hwnd -eq [IntPtr]::Zero) { Write-Output 'WINDOW_NOT_FOUND'; exit 1 }

  $workerw = [Win32Wall]::GetWorkerW()
  if ($workerw -eq [IntPtr]::Zero) {
    $workerw = [Win32Wall]::FindWindow('Progman', $null)
    Write-Output ('FALLBACK_PROGMAN ' + $workerw)
  }
  if ($workerw -eq [IntPtr]::Zero) { Write-Output 'WORKERW_NOT_FOUND'; exit 2 }

  $msg = [Win32Wall]::Embed($hwnd, $workerw)
  Write-Output ('EMBEDDED ' + $msg)
}
else {
  $hwnd = [Win32Wall]::FindByTitlePart($titlePart)
  if ($hwnd -eq [IntPtr]::Zero) { Write-Output 'WINDOW_NOT_FOUND'; exit 1 }
  Write-Output ([Win32Wall]::Detach($hwnd, 1280, 820))
}
