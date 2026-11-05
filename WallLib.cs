
using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

class AquariumWallpaper
{
    public delegate bool EnumProc(IntPtr h, IntPtr l);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string c, string n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string c, string n);
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr h, uint m, IntPtr w, IntPtr l, uint f, uint t, out IntPtr r);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr c, IntPtr p);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool SetWindowText(IntPtr h, string t);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr dc, int index);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int L, T, R, B; }

    const int GWL_STYLE = -16;
    const int WS_CHILD = 0x40000000;
    const int WS_VISIBLE = 0x10000000;
    const int WS_POPUP = unchecked((int)0x80000000);
    const int WS_CAPTION = 0x00C00000;
    const int WS_THICKFRAME = 0x00040000;
    const int WS_SYSMENU = 0x00080000;
    const int WS_MINIMIZEBOX = 0x00020000;
    const int WS_MAXIMIZEBOX = 0x00010000;
    const int WS_OVERLAPPEDWINDOW = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_SHOWWINDOW = 0x0040;

    static string BaseDir { get { return AppDomain.CurrentDomain.BaseDirectory; } }

    static string StatePath(string name)
    {
        string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aquarium");
        Directory.CreateDirectory(d);
        return Path.Combine(d, name);
    }
    static void WritePid(string name, int pid) { try { File.WriteAllText(StatePath(name), pid.ToString()); } catch { } }
    static int ReadPid(string name)
    {
        try { if (!File.Exists(StatePath(name))) return 0; int v; return int.TryParse(File.ReadAllText(StatePath(name)).Trim(), out v) ? v : 0; }
        catch { return 0; }
    }
    static void KillPid(int pid)
    {
        try { if (pid > 0) { Process q = Process.GetProcessById(pid); q.Kill(); } } catch { }
    }
    static void KillChrome()
    {
        int pid = ReadPid("chrome.pid");
        if (pid <= 0) return;
        try {
            ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/PID " + pid + " /T /F");
            psi.UseShellExecute = false; psi.CreateNoWindow = true;
            Process.Start(psi);
        } catch { }
        try { File.Delete(StatePath("chrome.pid")); } catch { }
    }

    static void Log(string s)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aquarium"));
            File.AppendAllText(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aquarium", "wallpaper.log"),
                DateTime.Now.ToString("s") + "  " + s + Environment.NewLine);
        }
        catch { }
    }

    static IntPtr found = IntPtr.Zero;

    static bool TitleHas(IntPtr h, string part)
    {
        int len = GetWindowTextLength(h);
        if (len <= 0) return false;
        StringBuilder sb = new StringBuilder(len + 2);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString().IndexOf(part, StringComparison.Ordinal) >= 0;
    }

    static IntPtr WalkTank(IntPtr h, string part, int depth)
    {
        if (depth > 3) return IntPtr.Zero;
        IntPtr c = GetWindow(h, 5);
        while (c != IntPtr.Zero)
        {
            if (TitleHas(c, part)) return c;
            IntPtr r = WalkTank(c, part, depth + 1);
            if (r != IntPtr.Zero) return r;
            c = GetWindow(c, 2);
        }
        return IntPtr.Zero;
    }

    static IntPtr WalkClass(IntPtr h, int depth)
    {
        if (depth > 3) return IntPtr.Zero;
        IntPtr c = GetWindow(h, 5);
        while (c != IntPtr.Zero)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(c, sb, 256);
            if (sb.ToString().StartsWith("Chrome_WidgetWin")) return c;
            IntPtr r = WalkClass(c, depth + 1);
            if (r != IntPtr.Zero) return r;
            c = GetWindow(c, 2);
        }
        return IntPtr.Zero;
    }

    static IntPtr FindTankByClass()
    {
        /* WorkerW 可能被 Explorer 换成顶层的另一个，所以遍历所有顶层窗口的子窗口 */
        found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            IntPtr r = WalkClass(h, 0);
            if (r != IntPtr.Zero) { found = r; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /* Safe class fallback: only look INSIDE desktop windows (WorkerW/Progman), never at top-level user windows. */
    static IntPtr FindTankInDesktop()
    {
        found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            StringBuilder sb = new StringBuilder(64);
            GetClassName(h, sb, 64);
            string cn = sb.ToString();
            if (cn == "WorkerW" || cn == "Progman" || cn == "SHELLDLL_DefView")
            {
                IntPtr r = WalkClass(h, 0);
                if (r != IntPtr.Zero) { found = r; return false; }
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
    static string StateFile() { return Path.Combine(Path.GetTempPath(), "aquarium.hwnd"); }
    static void WriteHwnd(IntPtr h)
    {
        try { File.WriteAllText(StateFile(), h.ToString()); } catch { }
    }
    static IntPtr ReadHwnd()
    {
        try
        {
            string t = File.ReadAllText(StateFile()).Trim();
            long v;
            if (long.TryParse(t, out v) && v != 0) return (IntPtr)v;
        }
        catch { }
        return IntPtr.Zero;
    }
    static bool IsOurWindow(IntPtr h)
    {
        if (h == IntPtr.Zero || !IsWindow(h)) return false;
        StringBuilder sb = new StringBuilder(64);
        GetClassName(h, sb, 64);
        return sb.ToString().StartsWith("Chrome_WidgetWin");
    }
    static IntPtr FindTank(string part)
    {
        IntPtr memo = ReadHwnd();
        if (IsOurWindow(memo) && TitleHas(memo, part)) return memo;
        found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            if (TitleHas(h, part)) { found = h; return false; }
            IntPtr r = WalkTank(h, part, 0);
            if (r != IntPtr.Zero) { found = r; return false; }
            return true;
        }, IntPtr.Zero);
        if (found == IntPtr.Zero) found = FindTankInDesktop();
        return found;
    }

    static IntPtr GetWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null);
        IntPtr res;
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out res);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            IntPtr w1 = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (w1 != IntPtr.Zero) return w1;

            IntPtr w2 = IntPtr.Zero;
            EnumWindows(delegate(IntPtr top, IntPtr l)
            {
                IntPtr defView = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    IntPtr w = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
                    if (w != IntPtr.Zero) { w2 = w; return false; }
                }
                return true;
            }, IntPtr.Zero);
            if (w2 != IntPtr.Zero) return w2;
            System.Threading.Thread.Sleep(300);
        }
        return FindWindow("Progman", null);
    }

    static string ChromePath()
    {
        string[] cands = new string[] {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        };
        foreach (string c in cands) if (File.Exists(c)) return c;
        return null;
    }

    static int Launch(int sw, int sh)
    {
        sw = GetSystemMetrics(0); sh = GetSystemMetrics(1);
        // ask Chrome to render 1 CSS px == 1 physical px, window sized in physical px
        int dpi = 96;
        try { IntPtr dc = GetDC(IntPtr.Zero); dpi = GetDeviceCaps(dc, 88); ReleaseDC(IntPtr.Zero, dc); } catch { }
        if (dpi <= 0) dpi = 96;
        sw = sw * dpi / 96;
        sh = sh * dpi / 96;
        Log("screen physical=" + sw + "x" + sh + " dpi=" + dpi);

        string exe = ChromePath();
        if (exe == null) { Log("NO_BROWSER"); return 3; }

        string url = "file:///" + Path.Combine(BaseDir, "index.html").Replace('\\', '/') + "?wallpaper"
                     + (File.Exists(Path.Combine(BaseDir, "geo.on")) ? "&geo" : "");
        string profile = Path.Combine(BaseDir, "profile");
        Directory.CreateDirectory(profile);
        string args = "--app=" + url
            + " --user-data-dir=\"" + profile + "\""
            + " --window-size=" + sw + "," + sh
            + " --window-position=0,0 --force-device-scale-factor=1"
            + " --no-first-run --no-default-browser-check --allow-file-access-from-files"
            + " --enable-gpu-rasterization --ignore-gpu-blocklist"
            + " --disable-backgrounding-occluded-windows --disable-renderer-backgrounding"
            + " --disable-features=Translate,MediaRouter,CalculateNativeWinOcclusion";

        Process p = new Process();
        p.StartInfo.FileName = exe;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = true;
        p.Start();
        WritePid("chrome.pid", p.Id);
        Log("launched pid=" + p.Id);
        return 0;
    }

    static void EmbedTank(IntPtr hwnd, bool launched)
    {
            IntPtr workerw = GetWorkerW();
            if (workerw == IntPtr.Zero) { Log("WORKERW_NOT_FOUND"); return; }

            int style = GetWindowLong(hwnd, GWL_STYLE);
            style = (style & ~WS_POPUP) & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_SYSMENU & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX;
            style = style | WS_CHILD | WS_VISIBLE;
            SetWindowLong(hwnd, GWL_STYLE, style);
            SetParent(hwnd, workerw);

            RECT r;
            GetClientRect(workerw, out r);
            int cw = r.R - r.L, ch = r.B - r.T;
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, cw, ch,
                SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            SendMessage(hwnd, 0x0005, (IntPtr)0, (IntPtr)((ch << 16) | (cw & 0xFFFF)));

            // tell DWM not to draw the non-client (caption/border) at all
            try { int ncrp = 1; DwmSetWindowAttribute(hwnd, 2, ref ncrp, 4); } catch { }

            // Chrome re-adds its caption frame after resize; slide the frame off-screen so the
            // client area exactly covers the desktop
            for (int i = 0; i < 14; i++)
            {
                RECT wr, cr2;
                GetWindowRect(hwnd, out wr);
                GetClientRect(hwnd, out cr2);
                int ncH = (wr.B - wr.T) - cr2.B;
                int ncW = (wr.R - wr.L) - cr2.R;
                if (ncH < 0) ncH = 0;
                if (ncW < 0) ncW = 0;
                try { int ncrp = 1; DwmSetWindowAttribute(hwnd, 2, ref ncrp, 4); } catch { }
                SetWindowPos(hwnd, IntPtr.Zero, -(ncW / 2), -(ncH + 26), cw + ncW, ch + ncH + 26,
                    SWP_NOZORDER | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                System.Threading.Thread.Sleep(300);
            }
            Log("EMBEDDED parent=" + workerw + " client=" + cw + "x" + ch + (launched ? " (launched)" : " (existing)"));
            return;
    }

    /* lightweight embed: only SetParent + position, no style changes (style changes make Chrome quit) */
    static void EmbedTank2(IntPtr hwnd)
    {
        IntPtr workerw = GetWorkerW();
        if (workerw == IntPtr.Zero) { Log("WORKERW_NOT_FOUND"); return; }
        SetParent(hwnd, workerw);
        RECT r;
        GetClientRect(workerw, out r);
        int cw = r.R - r.L, ch = r.B - r.T;
        SetWindowPos(hwnd, IntPtr.Zero, 0, -26, cw, ch + 26,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        SendMessage(hwnd, 0x0005, (IntPtr)0, (IntPtr)(((ch + 26) << 16) | (cw & 0xFFFF)));
        try { int ncrp = 1; DwmSetWindowAttribute(hwnd, 2, ref ncrp, 4); } catch { }
        WriteHwnd(hwnd);
        Log("EMBEDDED2 parent=" + workerw + " client=" + cw + "x" + ch);
    }
    static void StartWatcher()
    {
        try
        {
            string self = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo psi = new ProcessStartInfo(self, "watch");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = BaseDir;
            Process.Start(psi);
        }
        catch (Exception e) { Log("WATCHER_FAIL " + e.Message); }
    }

    static void Watch(string part)
    {
        WritePid("watch.pid", Process.GetCurrentProcess().Id);
        Log("WATCH start pid=" + Process.GetCurrentProcess().Id);
        while (true)
        {
            try
            {
                IntPtr hwnd = FindTank(part);
                if (hwnd == IntPtr.Zero)
                {
                    int cp = ReadPid("chrome.pid");
                    bool alive = false;
                    try { alive = cp > 0 && Process.GetProcessById(cp) != null; } catch { }
                    if (alive) { KillChrome(); System.Threading.Thread.Sleep(1500); }
                    Launch(0, 0);
                    System.Threading.Thread.Sleep(6000);
                    hwnd = FindTank(part);
                    if (hwnd == IntPtr.Zero) Log("WATCH no window after launch");
                }
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr ww = GetWorkerW();
                    if (ww != IntPtr.Zero && GetParent(hwnd) != ww)
                    {
                        EmbedTank(hwnd, false);
                        Log("WATCH re-embedded");
                    }
                }
            }
            catch (Exception e) { Log("WATCH_ERR " + e.Message); }
            System.Threading.Thread.Sleep(4000);
        }
    }

    static int Main(string[] a)
    {
        string mode = (a.Length > 0 ? a[0] : "start").ToLowerInvariant();
        bool userInitiated = (a.Length > 1 && a[1].ToLowerInvariant() == "user");
        string part = "AQUARIUM_WALL_LAYER";


        if (mode == "watchx") { Watch(part); return 0; }


        if (mode == "stop")
        {
            KillPid(ReadPid("watch.pid"));
            IntPtr t = IntPtr.Zero;
            for (int i = 0; i < 10 && t == IntPtr.Zero; i++) { t = FindTank(part); System.Threading.Thread.Sleep(200); }
            if (t != IntPtr.Zero) { PostClose(t); Log("CLOSED"); }
            KillChrome();
            Log("STOPPED");
            return 0;
        }

        bool launched = false;
        if (mode == "start")
        {
            /* 已经在壁纸层里就直接返回：不再重挂、不重启，避免桌面被自动刷新 */
            IntPtr probe = FindTank(part);
            if (probe != IntPtr.Zero)
            {
                StringBuilder pn = new StringBuilder(64);
                GetClassName(GetParent(probe), pn, 64);
                if (pn.ToString().StartsWith("WorkerW")) { Log("SKIP already embedded"); return 0; }
            }
            if (!userInitiated) { Log("IDLE no window, ignore (auto-start disabled)"); return 0; }
            Launch(0, 0);
            launched = true;
            System.Threading.Thread.Sleep(5000);
            mode = "embed";
        }

        IntPtr hwnd = IntPtr.Zero;
        for (int i = 0; i < 60 && hwnd == IntPtr.Zero; i++)
        {
            hwnd = FindTank(part);
            if (hwnd == IntPtr.Zero) System.Threading.Thread.Sleep(400);
        }
        if (hwnd == IntPtr.Zero) { Log("WINDOW_NOT_FOUND"); return 1; }

        if (mode == "restore")
        {
            KillPid(ReadPid("watch.pid"));
            SetParent(hwnd, IntPtr.Zero);
            int st = GetWindowLong(hwnd, GWL_STYLE);
            st = (st & ~WS_CHILD) | WS_OVERLAPPEDWINDOW | WS_VISIBLE;
            SetWindowLong(hwnd, GWL_STYLE, st);
            SetWindowPos(hwnd, IntPtr.Zero, 160, 70, 1280, 820, SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            ShowWindow(hwnd, 9);
            Log("RESTORED");
            return 0;
        }

        EmbedTank2(hwnd);
        return 0;
    }

    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    static void PostClose(IntPtr h) { PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}