/*
WinYcorners

Sets a hot-corner that shows the Task View on mouse hover.
This tool wants to imitate the hot corners of Gnome Desktop 40+.
Gnome like hot corners for Windows 11.

Created by Luca Reggiannini. Original repo: https://github.com/LucaReggiannini/winycorners

WinYcorners should run as administrator: Windows does not allow a program to interact with the windows of
programs running with higher privileges (e.g. send Win+Tab while Task Manager is in the foreground, or move
the tray icon menu of a program running as administrator).

Search your C# compiler with `dir /S /B C:\Windows\Microsoft.NET\csc.exe`
Compile & run with (from a Command Prompt running as administrator):
cls && taskkill /F /T /IM winycorners.exe & C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:winexe /out:winycorners.exe winycorners.cs && winycorners.exe --enhanced-task-view
*/

using System;                          // IntPtr, Environment
using System.Collections.Generic;      // List
using System.ComponentModel;           // Win32Exception
using System.IO;                       // Path, File (--debug)
using System.Drawing;                  // Rectangle, Point, Bitmap, Graphics, Icon, Color
using System.Drawing.Drawing2D;        // GraphicsPath, SmoothingMode
using System.Windows.Forms;            // NotifyIcon, ContextMenuStrip, Screen, Cursor, MessageBox
using System.Threading;                // Thread, ManualResetEvent
using System.Runtime.InteropServices;  // DllImport
using System.Security.Principal;       // WindowsPrincipal (--debug)
using System.Text;                     // StringBuilder
using System.Diagnostics;              // Process
using Microsoft.Win32;                 // Registry, SystemEvents


static class Program {

    const string VERSION = "2.0.0";

    [STAThread]
    static void Main(string[] args) {

        Application.EnableVisualStyles();

        bool   enhancedTaskView      = false;
        string position              = "--top-left";
        short  hotCornerSize         = 8;
        short  updateTime            = 16;
        short  enhancedTaskViewDelay = 150;

        /* Parse arguments. Without arguments WinYcorners starts with the default values */
        for (int c = 0; c < args.Length; c++) {
            if (args[c]=="--enhanced-task-view")
                enhancedTaskView = true;
            else if (args[c]=="--top-left" || args[c]=="--bottom-left" || args[c]=="--top-right" || args[c]=="--bottom-right")
                position = args[c];
            else if (args[c]=="--corner-size")
                hotCornerSize = ParseValue(args, ref c, 1);
            else if (args[c]=="--update-time")
                updateTime = ParseValue(args, ref c, 1);
            else if (args[c]=="--enhanced-task-view-delay")
                enhancedTaskViewDelay = ParseValue(args, ref c, 0);
            else if (args[c]=="--debug")
                DebugLog.Enabled = true;
            else if (args[c]=="--help" || args[c]=="-h" || args[c]=="/?") {
                ShowHelp();
                Environment.Exit(0);
            } else
                ExitWithHelp();
        }

        KillExistingProcesses();

        /*
        When WinYcorners starts with Windows the Taskbar may not exist yet: wait for it, otherwise the
        original auto-hide setting could not be read (see TaskbarAutoHide)
        */
        WaitForTaskbar();

        if (DebugLog.Enabled) {
            DebugLog.Write("WinYcorners " + VERSION + " started with: " + string.Join(" ", args));
            DebugLog.Write("Running as administrator: " + (IsAdministrator() ? "yes" : "NO"));
            DebugLog.Write(TaskbarGeometry.DescribeScaling());
            DebugLog.Write(TaskbarGeometry.Describe());
        }

        Application.Run(
            new TrayContext(
                position,
                enhancedTaskView,
                hotCornerSize,
                updateTime,
                enhancedTaskViewDelay));
    }

    /* Reads the number that follows an option (e.g. "--corner-size 8"). If it is missing or invalid, shows the help and exits */
    private static short ParseValue(string[] args, ref int c, short minimum) {
        short value = 0;
        if (c + 1 >= args.Length || short.TryParse(args[c + 1], out value)==false || value < minimum)
            ExitWithHelp();
        c = c + 1;
        return value;
    }

    private static void KillExistingProcesses() {
        MessageBoxButtons buttons = MessageBoxButtons.YesNo;

        Process currentProcess = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
        foreach (var process in processes) {
            if (process.Id != currentProcess.Id) {
                DialogResult result = MessageBox.Show(
                    "Another WinYcorners process was found (PID:" + process.Id + "). " +
                    "Do you want to terminate it?",
                    "WinYcorners",
                    buttons,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes) {
                    try {
                        process.Kill();
                        process.WaitForExit(2000);
                    } catch (Win32Exception) {
                        /*
                        Access denied: the other process runs as administrator and this one does not.
                        Two WinYcorners processes would fight over the Taskbar, so this one exits.
                        */
                        MessageBox.Show(
                            "The other WinYcorners process (PID:" + process.Id + ") could not be terminated: " +
                            "it is probably running as administrator.\n\n" +
                            "Close it from its tray icon, or run this one as administrator.",
                            "WinYcorners",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        Environment.Exit(1);
                    } catch (InvalidOperationException) {
                        /* The other process has already exited */
                    }
                }
            }
        }
    }

    private static void WaitForTaskbar() {
        const int TIMEOUT = 60000; // milliseconds
        const int STEP    = 500;   // milliseconds

        for (int waited = 0; TaskbarGeometry.Taskbar == IntPtr.Zero && waited < TIMEOUT; waited += STEP)
            Thread.Sleep(STEP);
    }

    /* --debug: tells whether WinYcorners runs as administrator */
    private static bool IsAdministrator() {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool helpVisible = false;

    /*

    Shows the help without exiting (used by the tray icon menu too).

    The help is too long for a MessageBox (it has no scroll bar and it would be cut on a 1080p screen),
    so it is shown in a small window with a scroll bar and a fixed-width font: this way the aligned
    columns of the text are also displayed correctly. Esc closes it.

    */
    public static void ShowHelp() {
        if (helpVisible) return; // Avoid opening the help twice from the tray icon
        helpVisible = true;
        try {
            using (Form    window = new Form())
            using (TextBox text   = new TextBox())
            using (Font    font   = new Font("Consolas", 10)) {
                text.Multiline  = true;
                text.ReadOnly   = true;
                text.WordWrap   = false;
                text.ScrollBars = ScrollBars.Vertical;
                text.Dock       = DockStyle.Fill;
                text.Font       = font;
                text.BackColor  = SystemColors.Window;
                text.Text       = HELP_TEXT.Replace("\r\n", "\n").Replace("\n", "\r\n").Trim('\r', '\n'); // TextBox needs \r\n line breaks

                window.Text          = "WinYcorners";
                window.ClientSize    = new Size(560, 640);
                window.StartPosition = FormStartPosition.CenterScreen;
                window.ShowIcon      = false;
                window.MinimizeBox   = false;
                window.TopMost       = true; // Opened from the tray icon it could end up behind other windows
                window.KeyPreview    = true;
                window.KeyDown      += delegate (object sender, KeyEventArgs e) {
                    if (e.KeyCode == Keys.Escape)
                        window.Close();
                };
                window.Shown        += delegate { text.Select(0, 0); }; // Otherwise all the text is selected
                window.Controls.Add(text);
                window.ShowDialog();
            }
        } finally {
            helpVisible = false;
        }
    }

    private static void ExitWithHelp() {
        ShowHelp();
        Environment.Exit(1);
    }

    const string HELP_TEXT = @"
WinYcorners v" + VERSION + @"
https://github.com/LucaReggiannini/winycorners/

Sets a hot-corner that shows the Task View on mouse hover.

This tool wants to imitate the hot corners of Gnome Desktop 40+.

IMPORTANT: run WinYcorners as administrator.
    Windows does not allow a program to interact with the windows
    of programs running with higher privileges. Without it:
    - the hot corner may not work while a program running as
      administrator (e.g. Task Manager) is in the foreground;
    - in Enhanced Task View mode, the tray icon menus of these
      programs are not moved above the Taskbar.

SYNOPSIS:
    winycorners [POSITION] [OPTIONS]

    Without arguments WinYcorners starts with the default values.
    WinYcorners runs in the notification area (system tray):
    right-click its icon to show this help or to exit.

POSITION:
    --top-left
    --top-right
    --bottom-left
    --bottom-right

    default position is top-left

OPTIONS:
    --corner-size <pixels>
        The  size of the hot corner in pixels.  Default value is 8px

    --update-time <milliseconds>
        Determines  every how many milliseconds  the position of the
        Windows cursor is checked (in order to trigger a hot corner).
        The  default  value  is  16  milliseconds, the lowest useful
        value:  lower values have no effect (it is the resolution of
        the  Windows  timer). Increasing this value reduces the work
        done  by WinYcorners (useful on battery), but the hot corner
        becomes less responsive

    --enhanced-task-view
        Hides the Taskbar  and shows it only when Task View is visible
        (like Gnome).  The Windows  Taskbar auto-hide setting  is used
        so that  maximized and snapped windows  use the  whole screen:
        it is turned off while Task View is visible and it is restored
        to its original value when WinYcorners exits

    --enhanced-task-view-delay <milliseconds>
        In  Windows 11  the  Taskbar  visibility  can't  be  changed
        while the Task View is active.  For Enhanced Task View  mode
        to  work  properly, the Taskbar  is made visible immediately
        after triggering the hot corner.
        Every N milliseconds it is checked if the task view is still
        active: if it is not, the task bar is made invisible  again.
        The '--enhanced-task-view-delay'  option  allows  you to set
        the  number  of   milliseconds  with  which  this  check  is
        performed. The default value is 150 milliseconds. Decreasing
        this value makes the Taskbar disappear sooner after the Task
        View  is  closed,  increasing  it  may  be useful for slower
        systems. It does not affect the hot corner, which is checked
        every '--update-time' milliseconds

    --debug
        Saves diagnostic information to the file
        %TEMP%\winycorners-debug.log
        Open it with Notepad, or follow it live with:
        powershell Get-Content $env:TEMP\winycorners-debug.log -Wait
        It logs:
        - the class name and title of the foreground window every
          time it changes: useful if a new version of Windows changes
          the class names of the Task View, Start or Taskbar windows;
        - Taskbar position, auto-hide state, display scaling and
          whether WinYcorners runs as administrator;
        - the tray icon menus moved above the Taskbar (or why not).

    --help
        Shows this help and exits
";
}


/* Runs the hot corner and the tray icon (Help / Exit menu) */
class TrayContext : ApplicationContext {

    private readonly NotifyIcon       trayIcon;
    private readonly ContextMenuStrip trayMenu;
    private readonly NativeWindow     trayMenuOwner = new NativeWindow();
    private readonly HotCorner        hotCorner;
    private readonly string           position;
    private          IntPtr           trayIconHandle = IntPtr.Zero;

    public TrayContext(string position, bool enhancedTaskView, short hotCornerSize, short updateTime, short enhancedTaskViewDelay) {
        this.position = position;

        /* Tray icon menu */
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Help", null, delegate { Program.ShowHelp(); });
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, delegate { ExitApplication(); });

        /* Invisible window used to show the tray icon menu (see ShowTrayMenu) */
        trayMenuOwner.CreateHandle(new CreateParams());

        trayIcon              = new NotifyIcon();
        trayIcon.Text         = "WinYcorners (" + position.Substring(2) + (enhancedTaskView ? ", enhanced Task View" : "") + ")";
        trayIcon.MouseUp     += OnTrayIconMouseUp;
        trayIcon.DoubleClick += delegate { Program.ShowHelp(); };
        UpdateTrayIcon();
        trayIcon.Visible      = true;

        /* Redraw the icon when the user switches between light and dark mode */
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        /* Exit cleanly on logoff/shutdown too, so the original Taskbar auto-hide setting is restored */
        SystemEvents.SessionEnding += OnSessionEnding;

        hotCorner = new HotCorner(
            position,
            enhancedTaskView,
            hotCornerSize,
            updateTime,
            enhancedTaskViewDelay);
        hotCorner.Start();

        /* With auto-hide the Taskbar covers the tray icon menus opened over it: move them above the Taskbar */
        if (enhancedTaskView)
            TrayMenus.StartWatching();
    }

    private bool exiting = false;

    private void ExitApplication() {
        if (exiting) return;
        exiting = true;

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        SystemEvents.SessionEnding         -= OnSessionEnding;
        TrayMenus.StopWatching();
        hotCorner.Stop();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        trayMenuOwner.DestroyHandle();
        if (trayIconHandle != IntPtr.Zero)
            DestroyIcon(trayIconHandle);
        ExitThread();
    }


    /*

    The tray icon menu is shown above the Taskbar, like the menus of the Windows applications.
    NotifyIcon.ContextMenuStrip would show it at the cursor position, partially over the Taskbar
    (see TrayMenus).

    */
    private void OnTrayIconMouseUp(object sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Right)
            ShowTrayMenu();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private void ShowTrayMenu() {
        Point cursor = Cursor.Position;
        Point anchor = cursor;

        /* If the cursor is over the Taskbar, the menu opens from the top edge of the Taskbar */
        int taskbarTop = TaskbarGeometry.GetTop();
        if (anchor.Y > taskbarTop)
            anchor.Y = taskbarTop;

        /*
        Like NotifyIcon does: an invisible window of WinYcorners is brought to the foreground before showing the menu,
        so the menu closes when the user clicks somewhere else (see TrackPopupMenu remarks)
        */
        SetForegroundWindow(trayMenuOwner.Handle);
        trayMenu.Show(anchor, ToolStripDropDownDirection.AboveLeft);

        if (DebugLog.Enabled)
            DebugLog.Write("WinYcorners tray menu: cursor " + cursor + ", anchor " + anchor + ", shown at " + trayMenu.Bounds + ", " + TaskbarGeometry.Describe());
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
        if (e.Category == UserPreferenceCategory.General)
            UpdateTrayIcon();
    }

    private void OnSessionEnding(object sender, SessionEndingEventArgs e) {
        ExitApplication();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void UpdateTrayIcon() {
        Color color = IsLightTaskbar() ? Color.FromArgb(28, 28, 28) : Color.White;

        using (Bitmap bitmap = CreateTrayBitmap(position, color)) {
            IntPtr newHandle = bitmap.GetHicon();
            trayIcon.Icon = Icon.FromHandle(newHandle);

            /* Icon.FromHandle does not own the handle: the previous one must be freed manually */
            if (trayIconHandle != IntPtr.Zero)
                DestroyIcon(trayIconHandle);
            trayIconHandle = newHandle;
        }
    }

    /*

    Draws the tray icon: a small screen with the active hot corner highlighted
    (like the ripple Gnome shows when a hot corner is triggered).
    The icon is generated at runtime, so no .ico file is needed to compile WinYcorners.

    */
    private static Bitmap CreateTrayBitmap(string position, Color color) {
        const int   SIZE   = 32;
        const float RADIUS = 13;

        RectangleF screen = new RectangleF(2, 4, 28, 24);
        float cornerX = (position=="--top-left" || position=="--bottom-left") ? screen.Left : screen.Right;
        float cornerY = (position=="--top-left" || position=="--top-right")   ? screen.Top  : screen.Bottom;

        Bitmap bitmap = new Bitmap(SIZE, SIZE);
        using (Graphics g = Graphics.FromImage(bitmap))
        using (GraphicsPath outline = RoundedRectangle(screen, 4))
        using (SolidBrush brush = new SolidBrush(color))
        using (Pen pen = new Pen(color, 2.5f)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.SetClip(outline);
            g.FillEllipse(brush, cornerX - RADIUS, cornerY - RADIUS, RADIUS * 2, RADIUS * 2);
            g.ResetClip();
            g.DrawPath(pen, outline);
        }
        return bitmap;
    }

    private static GraphicsPath RoundedRectangle(RectangleF r, float radius) {
        float d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(r.Left,      r.Top,        d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top,        d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.Left,      r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    private static bool IsLightTaskbar() {
        /* The taskbar follows the "Windows mode" setting (SystemUsesLightTheme), not the "app mode" one */
        try {
            object value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "SystemUsesLightTheme",
                0);
            return value is int && (int)value == 1;
        } catch {
            return false;
        }
    }
}


/*

Checks the cursor position and opens the Task View when the cursor reaches the hot corner.
In Enhanced Task View mode it also shows the Taskbar only while the Task View is visible.

*/
class HotCorner {

    readonly int       SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
    readonly int       SCREEN_WIDTH  = Screen.PrimaryScreen.Bounds.Width;

    readonly Rectangle hotCorner;
    readonly bool      enhancedTaskView;
    readonly short     updateTime;
    readonly short     enhancedTaskViewDelay;

    readonly ManualResetEvent stopRequested = new ManualResetEvent(false);
    Thread worker;

    public HotCorner(string position, bool enhancedTaskView, short hotCornerSize, short updateTime, short enhancedTaskViewDelay) {

        this.enhancedTaskView      = enhancedTaskView;
        this.updateTime            = updateTime;
        this.enhancedTaskViewDelay = enhancedTaskViewDelay;

        /* Defining hot corner position (location is the upper-left point) */
        Point location = new Point(0, 0); // --top-left
        if (position=="--top-right")
            location = new Point(SCREEN_WIDTH - hotCornerSize, 0);
        else if (position=="--bottom-left")
            location = new Point(0, SCREEN_HEIGHT - hotCornerSize);
        else if (position=="--bottom-right")
            location = new Point(SCREEN_WIDTH - hotCornerSize, SCREEN_HEIGHT - hotCornerSize);

        /* Defining hot corner size (exactly hotCornerSize x hotCornerSize pixels) */
        hotCorner = new Rectangle(location, new Size(hotCornerSize, hotCornerSize));
    }

    /* The hot corner runs on a background thread, so the main thread is free to handle the tray icon */
    public void Start() {
        if (enhancedTaskView) {
            /* Remember the user's auto-hide setting, then use auto-hide to free the space of the Taskbar */
            TaskbarAutoHide.SaveOriginal();
            TaskbarAutoHide.Set(true);
        } else
            TaskbarAutoHide.RestoreOriginal(); // Only does something if a previous Enhanced Task View run did not exit cleanly

        worker              = new Thread(Run);
        worker.IsBackground = true;
        worker.Start();
    }

    public void Stop() {
        stopRequested.Set();
        worker.Join(2000);

        /* When exiting, restore the original auto-hide setting and make sure the taskbar does not stay hidden */
        if (enhancedTaskView) {
            TaskbarAutoHide.RestoreOriginal();
            SetTaskbarVisible(true);
        }
    }

    /* Waits for the given milliseconds. Returns true if Stop() has been called in the meantime */
    private bool Wait(int milliseconds) {
        return stopRequested.WaitOne(milliseconds);
    }

    private void Run() {

        bool triggeredHotCorner = false;
        int  nextTaskbarUpdate  = Environment.TickCount; // Enhanced Task View: when the Taskbar is checked again

        while (true) {

            if (DebugLog.Enabled)
                LogForegroundWindow();

            /*

            Note: WinYcorners does not trigger hot corner if there is a full screen application.
            Windows 11 Task View is considered a full screen application so is white listed
            using "...|| IsTaskViewVisible()" in the following conditional block (Start and Search too).

            */
            if (CheckFullScreenForegroundApp()==false || IsTaskViewVisible() || IsStartOrSearchVisible()) {
                /*

                Trigger hot corner based on the cursor position.
                If the left mouse button is pressed, the hot corner is not triggered: maybe a drag and drop operation is in progress.

                */
                bool cursorInHotCorner = hotCorner.Contains(Cursor.Position);

                if (cursorInHotCorner && isDown(Keys.LButton)==false) {
                    if (triggeredHotCorner==false) {
                        triggeredHotCorner = true;

                        /* Win+Tab also closes the Task View when it is already open */
                        bool openingTaskView = IsTaskViewVisible()==false;

                        if (enhancedTaskView && openingTaskView) {
                            /* The Taskbar window is shown before the Task View opens: then it can't be changed (see --enhanced-task-view-delay) */
                            SetTaskbarVisible(true);
                            SwitchTaskView();
                            /* Auto-hide is changed only after Win+Tab: it is slow and the Task View must not wait for it */
                            ShowTaskbarForTaskView();
                        } else
                            SwitchTaskView();
                    }
                } else if (cursorInHotCorner==false && triggeredHotCorner)
                    triggeredHotCorner = false;


                /*

                Enhanced Task View: the Taskbar is checked every "enhancedTaskViewDelay" milliseconds,
                independently from the hot corner, which is checked every "updateTime" milliseconds.
                Note: "Environment.TickCount - nextTaskbarUpdate >= 0" also works when TickCount overflows.

                */
                if (enhancedTaskView && Environment.TickCount - nextTaskbarUpdate >= 0) {
                    UpdateTaskbar();
                    nextTaskbarUpdate = Environment.TickCount + enhancedTaskViewDelay;
                }
            }

            /*
            Always wait before the next check, also when the hot corner is disabled because of a
            full screen application: without this pause the loop would use 100% of a CPU core.
            */
            if (Wait(updateTime)) return;
        }
    }


    /*

    Enhanced Task View: Taskbar handling.

    Auto-hide frees the space of the Taskbar, so maximized and snapped windows use the whole screen.
    It is turned off only while the Task View is visible, otherwise the Taskbar would slide away immediately.
    Changing auto-hide is slow (Windows resizes the maximized windows), so it is changed only when the Task View
    opens or closes, never at every check.

    */
    const int TASK_VIEW_OPEN_TIMEOUT = 1000; // milliseconds

    bool taskbarShownForTaskView = false; // The Taskbar is visible and auto-hide is off
    bool taskViewSeen            = false; // The Task View has been visible since the Taskbar was shown
    int  taskbarShownAt          = 0;     // Environment.TickCount when the Taskbar was shown

    private void ShowTaskbarForTaskView() {
        SetTaskbarVisible(true);
        TaskbarAutoHide.Set(false);
        taskbarShownForTaskView = true;
        taskViewSeen            = false;
        taskbarShownAt          = Environment.TickCount;
    }

    private void HideTaskbarAfterTaskView() {
        SetTaskbarVisible(false);   // First: the Taskbar disappears at once, without the auto-hide slide animation
        TaskbarAutoHide.Set(true);
        SetTaskbarVisible(false);   // Again: changing auto-hide may show the Taskbar window
        taskbarShownForTaskView = false;
    }

    private void UpdateTaskbar() {
        bool startOrSearchVisible = IsStartOrSearchVisible();

        if (IsTaskViewVisible()) {
            if (taskbarShownForTaskView==false)
                ShowTaskbarForTaskView(); // The Task View was opened with Win+Tab instead of the hot corner
            taskViewSeen = true;
        } else if (taskbarShownForTaskView) {
            /*
            Right after the hot corner is triggered the Task View may not be visible yet: hiding the Taskbar now
            would make it appear and disappear. It is hidden only once the Task View has been visible and then
            closed (or if the Task View did not open at all)...
            */
            bool taskViewClosed = taskViewSeen || Environment.TickCount - taskbarShownAt > TASK_VIEW_OPEN_TIMEOUT;

            /* ...and only when the Taskbar is not used anymore (e.g. Start opened from the Task View, hidden icons, tray menus) */
            if (taskViewClosed && startOrSearchVisible==false && IsTaskbarInUse()==false)
                HideTaskbarAfterTaskView();
        }

        if (taskbarShownForTaskView)
            SetTaskbarVisible(true);
        else {
            /* Keep auto-hide on, even if it is changed from the Windows settings */
            TaskbarAutoHide.Set(true);

            /*
            Outside the Task View the Taskbar is shown while Start or Search are open (auto-hide stays on).
            Once visible, it stays visible while it is used.
            */
            SetTaskbarVisible(startOrSearchVisible || (TaskbarGeometry.IsVisible() && IsTaskbarInUse()));
        }

        /* Safety net for TrayMenus.StartWatching(): menus over the Taskbar that were not moved when they appeared */
        if (TaskbarGeometry.IsVisible())
            TrayMenus.MoveMenusAboveTaskbar();
    }


    /*

    Code used to simulate a keypress to show the Windows Task View

    Using "keybd_event" from user32.dll since "System.Windows.Forms.SendKeys.Send" does not support Win key.
    Emulating with CTRL+ESC, SendWait("^({ESC}{TAB})") or Send("^({ESC}{TAB}})") does not work

    */
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const int KEYEVENTF_KEYUP       = 0x0002;

    private static void KeyDown(Keys vKey) {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
    }
    private static void KeyUp(Keys vKey) {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }
    private void SwitchTaskView() {
        /* Press Win+Tab to open the task view */
        KeyDown(Keys.LWin);
        KeyDown(Keys.Tab);
        KeyUp(Keys.LWin);
        KeyUp(Keys.Tab);
    }

    /*

    Code used to detect if a Keyboard or mouse button is pressed or not.

    If the mouse button is pressed while the cursor is at the corner of the screen,
    it may indicate that a drag-and-drop operation is in progress and therefore it
    is not necessary to activate the hotcorner

    */
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    private static bool isDown(Keys button) {
        /* The most significant bit of the result is set while the key is down */
        return (GetAsyncKeyState((int)button) & 0x8000) != 0;
    }


    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    public static bool CheckFullScreenForegroundApp() {

        /*
        Code used to detect if the foreground window is a full screen application (e.g. a game or a video).
        Thanks to/original code:
        https://github.com/RobertSmits/Windows-10-HotCorners/blob/master/Windows10.HotCorners/Business/ForegroundDetector.cs */

        var desktopHandle = GetDesktopWindow();
        var shellHandle   = GetShellWindow();
        var hWnd          = Win32.GetForegroundWindow();

        if (hWnd.Equals(IntPtr.Zero)) return false;
        if (hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)) return false;

        RECT appBounds;
        Win32.GetWindowRect(hWnd, out appBounds);
        var screenBounds = Screen.FromHandle(hWnd).Bounds;

        if (
            (appBounds.bottom - appBounds.top)  == screenBounds.Height &&
            (appBounds.right  - appBounds.left) == screenBounds.Width
            )
            return true; // Return True if Fullscreen application is found
        else
            return false;
    }


    /* Code used to show or hide the Taskbar window (Enhanced Task View mode) */
    const uint SWP_HIDEWINDOW = 0x0080;
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_NOZORDER   = 0x0004;

    private static void SetTaskbarVisible(bool visible) {

        IntPtr hwnd = TaskbarGeometry.Taskbar;

        /* The Taskbar is changed only when needed: this is called at every check of the hot corner */
        if (Win32.IsWindowVisible(hwnd) == visible)
            return;

        if (visible)
            Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_SHOWWINDOW); // Show Taskbar
        else
            Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_HIDEWINDOW | SWP_NOZORDER); // Hide Taskbar

    }


    /*

    Window class names of the Windows 11 shell elements (they can be checked with --debug):
    - Task View:      XamlExplorerHostIslandWindow
    - Start / Search: Windows.UI.Core.CoreWindow

    */
    const string TASK_VIEW_CLASSNAME    = "XamlExplorerHostIslandWindow";
    const string START_SEARCH_CLASSNAME = "Windows.UI.Core.CoreWindow";

    private static string GetForegroundWindowClassName() {
        return Win32.GetClassName(Win32.GetForegroundWindow());
    }

    private static bool IsTaskViewVisible() {
        return GetForegroundWindowClassName() == TASK_VIEW_CLASSNAME;
    }

    private static bool IsStartOrSearchVisible() {
        return GetForegroundWindowClassName() == START_SEARCH_CLASSNAME;
    }


    /*

    --debug: logs the foreground window every time it changes (this replaces the old fghelper tool).

    Useful to find the class names of the Windows shell elements (Task View, Start, Taskbar flyouts...)
    if a new version of Windows changes them: e.g. start WinYcorners with --debug, press Win+Tab and
    look for the "Foreground window" line in the log.

    */
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    IntPtr lastForegroundWindow = IntPtr.Zero;
    string lastForegroundTitle  = null;

    private void LogForegroundWindow() {
        IntPtr hWnd = Win32.GetForegroundWindow();

        const int     N_CHARS = 256;
        StringBuilder title   = new StringBuilder(N_CHARS);
        GetWindowText(hWnd, title, title.Capacity);

        if (hWnd == lastForegroundWindow && title.ToString() == lastForegroundTitle)
            return;
        lastForegroundWindow = hWnd;
        lastForegroundTitle  = title.ToString();

        /* How WinYcorners sees the window */
        string className = Win32.GetClassName(hWnd);
        string meaning   = "";
        if (className == TASK_VIEW_CLASSNAME)
            meaning = " -> Task View";
        else if (className == START_SEARCH_CLASSNAME)
            meaning = " -> Start / Search";
        else if (Array.IndexOf(TASKBAR_UI_CLASSNAMES, className) >= 0)
            meaning = " -> Taskbar user interface";
        else if (hWnd != IntPtr.Zero && Win32.IsWindowVisible(hWnd)==false)
            meaning = " -> invisible window (a tray icon menu may be open)";
        if (CheckFullScreenForegroundApp())
            meaning += " (full screen)";

        string process = "?";
        try {
            uint processId;
            Win32.GetWindowThreadProcessId(hWnd, out processId);
            process = Process.GetProcessById((int)processId).ProcessName;
        } catch { }

        DebugLog.Write("Foreground window: class \"" + className + "\", title \"" + lastForegroundTitle + "\", process " + process + meaning);
    }


    /*

    The Taskbar is "in use" while the user interacts with it. A visible Taskbar is not hidden while it is in use:
    when the foreground window changes (e.g. from the Task View to Start, or to the hidden icons) for a moment
    the foreground window is the Taskbar itself, or a menu, or a flyout of the Taskbar.

    Window class names of the Taskbar user interface (they can be checked with --debug; if a Taskbar flyout
    still makes the Taskbar disappear, add its class name here):

    */
    static readonly string[] TASKBAR_UI_CLASSNAMES = {
        TaskbarGeometry.TASKBAR_CLASSNAME,     // Taskbar
        "Shell_SecondaryTrayWnd",              // Taskbar on the other monitors
        "TopLevelWindowForOverflowXamlIsland", // Hidden icons
        "ControlCenterWindow"                  // Quick Settings
    };

    [StructLayout(LayoutKind.Sequential)]
    struct GUITHREADINFO {
        public int    cbSize;
        public int    flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT   rcCaret;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui); // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo
    const int GUI_INMENUMODE    = 0x0004;
    const int GUI_POPUPMENUMODE = 0x0010;

    private static bool IsTaskbarInUse() {

        /* The cursor is over the Taskbar (e.g. while clicking the Start button or the hidden icons arrow) */
        IntPtr taskbar = TaskbarGeometry.Taskbar;
        RECT   taskbarBounds;
        if (taskbar != IntPtr.Zero && Win32.GetWindowRect(taskbar, out taskbarBounds)) {
            Point cursor = Cursor.Position;
            if (cursor.X >= taskbarBounds.left && cursor.X < taskbarBounds.right &&
                cursor.Y >= taskbarBounds.top  && cursor.Y < taskbarBounds.bottom)
                return true;
        }

        /* A standard Windows menu of the foreground application is open (e.g. right-click on a tray icon) */
        GUITHREADINFO info = new GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
        if (GetGUIThreadInfo(0, ref info) && (info.flags & (GUI_INMENUMODE | GUI_POPUPMENUMODE)) != 0) // 0: foreground thread
            return true;

        /* A tray icon menu drawn by the application itself is open (WinForms like WinYcorners, Qt...) */
        if (TrayMenus.IsCustomTrayMenuOpen())
            return true;

        /* The foreground window is part of the Taskbar user interface */
        return Array.IndexOf(TASKBAR_UI_CLASSNAMES, GetForegroundWindowClassName()) >= 0;
    }
}


/*

Taskbar auto-hide setting, used by the Enhanced Task View mode.

Hiding the Taskbar window is not enough: the Taskbar is registered as an "AppBar" and keeps reserving its
space at the bottom of the screen. Maximized windows can be forced to use the whole screen by changing the
desktop working area, but Windows Snap does not follow it. With auto-hide on, Windows itself frees that space.

The original auto-hide value is saved in the registry (HKCU\Software\WinYcorners) while WinYcorners changes
it. This way, if WinYcorners does not exit cleanly (killed, crashed...), the next run still knows the
original value and restores it, instead of treating "auto-hide on" as the user's setting.

More infos: https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shappbarmessage

*/
static class TaskbarAutoHide {

    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA {
        public int    cbSize;
        public IntPtr hWnd;
        public uint   uCallbackMessage;
        public uint   uEdge;
        public RECT   rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    const uint ABM_GETSTATE = 0x00000004;
    const uint ABM_SETSTATE = 0x0000000A;
    const int  ABS_AUTOHIDE = 0x0000001;

    const string SETTINGS_KEY            = @"Software\WinYcorners";
    const string ORIGINAL_AUTOHIDE_VALUE = "OriginalTaskbarAutoHide";

    public static bool Get() {
        APPBARDATA data = new APPBARDATA();
        data.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
        long state = SHAppBarMessage(ABM_GETSTATE, ref data).ToInt64();
        return (state & ABS_AUTOHIDE) != 0;
    }

    public static void Set(bool enabled) {
        /* Only change the setting when needed: this is called at every check of the hot corner */
        if (Get() == enabled) return;

        APPBARDATA data = new APPBARDATA();
        data.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
        data.hWnd   = TaskbarGeometry.Taskbar;
        data.lParam = new IntPtr(enabled ? ABS_AUTOHIDE : 0);
        SHAppBarMessage(ABM_SETSTATE, ref data);
    }

    /* Saves the current value as the original one, unless a previous run already saved it */
    public static void SaveOriginal() {
        bool saved;
        if (ReadSavedOriginal(out saved)) return;
        try {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SETTINGS_KEY))
                key.SetValue(ORIGINAL_AUTOHIDE_VALUE, Get() ? 1 : 0, RegistryValueKind.DWord);
        } catch { }
    }

    /* Restores the saved original value (if any) and removes it from the registry */
    public static void RestoreOriginal() {
        bool original;
        if (ReadSavedOriginal(out original) == false) return;
        Set(original);
        try {
            Registry.CurrentUser.DeleteSubKey(SETTINGS_KEY, false);
        } catch { }
    }

    private static bool ReadSavedOriginal(out bool autoHide) {
        autoHide = false;
        try {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SETTINGS_KEY)) {
                object value = (key == null) ? null : key.GetValue(ORIGINAL_AUTOHIDE_VALUE);
                if (value is int) {
                    autoHide = (int)value == 1;
                    return true;
                }
            }
        } catch { }
        return false;
    }
}


/*

Position of the Taskbar: used to open the tray icon menus above the Taskbar, and by --debug.

*/
static class TaskbarGeometry {

    public const string TASKBAR_CLASSNAME = "Shell_TrayWnd";

    public static IntPtr Taskbar {
        get { return Win32.FindWindow(TASKBAR_CLASSNAME, null); }
    }

    public static bool IsVisible() {
        return Win32.IsWindowVisible(Taskbar);
    }

    /*

    Top edge of the Taskbar. Two sources are combined:
    - the desktop working area, which excludes the Taskbar while auto-hide is off;
    - the Taskbar window, used only if its top edge is inside the screen (with auto-hide on and the Taskbar
      hidden, the window is moved down, almost or completely out of the screen).

    */
    public static int GetTop() {
        Rectangle screen = Screen.PrimaryScreen.Bounds;
        int top = Math.Min(screen.Bottom, Screen.PrimaryScreen.WorkingArea.Bottom);

        RECT taskbar;
        if (Win32.GetWindowRect(Taskbar, out taskbar) && taskbar.top > screen.Top && taskbar.top < screen.Bottom)
            top = Math.Min(top, taskbar.top);

        return top;
    }


    /* --debug: descriptions written to the log */

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    const int HORZRES        = 8;
    const int DESKTOPHORZRES = 118;

    public static string Describe() {
        RECT taskbar;
        bool found = Win32.GetWindowRect(Taskbar, out taskbar);
        return "screen " + Screen.PrimaryScreen.Bounds +
               ", working area " + Screen.PrimaryScreen.WorkingArea +
               ", taskbar window " + (found ? taskbar.ToString() : "not found") + (Win32.IsTopmost(Taskbar) ? " topmost" : " NOT topmost") +
               ", taskbar top " + GetTop() +
               ", auto-hide " + (TaskbarAutoHide.Get() ? "on" : "off");
    }

    public static string DescribeWindow(IntPtr hWnd) {
        RECT bounds;
        Win32.GetWindowRect(hWnd, out bounds);
        return Win32.GetClassName(hWnd) + " " + bounds + (Win32.IsTopmost(hWnd) ? " topmost" : " NOT topmost");
    }

    public static string DescribeScaling() {
        IntPtr hdc      = GetDC(IntPtr.Zero);
        int    logical  = GetDeviceCaps(hdc, HORZRES);
        int    physical = GetDeviceCaps(hdc, DESKTOPHORZRES);
        ReleaseDC(IntPtr.Zero, hdc);
        return "screen width " + logical + " (as seen by WinYcorners) / " + physical + " (physical) = " +
               (logical > 0 ? physical * 100 / logical : 0) + "% scaling";
    }
}


/* --debug: diagnostic information written to %TEMP%\winycorners-debug.log */
static class DebugLog {

    public static bool Enabled = false;
    public static readonly string FilePath = Path.Combine(Path.GetTempPath(), "winycorners-debug.log");

    static readonly object sync = new object();

    public static void Write(string message) {
        if (Enabled == false) return;
        lock (sync) {
            try {
                File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine);
            } catch { }
        }
    }
}


/*

Tray icon menus over the Taskbar (Enhanced Task View mode).

Many applications open their tray icon menu at the cursor position, so the bottom part of the menu ends up over the
Taskbar. With auto-hide on, Windows 11 draws the Taskbar in front of those menus even though they are above it in the
windows order (bringing them to the front does not help). So the menus are moved right above the Taskbar instead,
like the menus of the Windows applications.

A window over the visible Taskbar is moved when it is a visible popup window, at least MIN_MENU_SIZE pixels wide and
high, and:
- it is a standard Windows menu (class #32768), of any application;
- or it is a menu drawn by the application (WinForms, Qt...): a "topmost" popup window that belongs to the thread of
  the foreground window (before showing a tray icon menu, the application brings one of its windows to the foreground,
  see TrackPopupMenu remarks), and not to Explorer (the menus of the Taskbar itself already open in the right place).

The menus are moved as soon as they appear, using WinEvent hooks.
Note: Windows does not allow WinYcorners to move the windows of programs running as administrator, unless
WinYcorners runs as administrator too (see --debug: "Windows refused (error 5)").

*/
static class TrayMenus {

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    const int    MIN_MENU_SIZE           = 16;
    const string STANDARD_MENU_CLASSNAME = "#32768"; // Standard Windows menus
    const uint   SWP_NOSIZE              = 0x0001;
    const uint   SWP_NOZORDER            = 0x0004;
    const uint   SWP_NOACTIVATE          = 0x0010;

    const uint EVENT_SYSTEM_MENUPOPUPSTART = 0x0006; // A standard Windows menu appears
    const uint EVENT_OBJECT_SHOW           = 0x8002; // Any window appears
    const uint WINEVENT_OUTOFCONTEXT       = 0x0000;
    const uint WINEVENT_SKIPOWNPROCESS     = 0x0002; // The WinYcorners menu already opens above the Taskbar
    const int  OBJID_WINDOW                = 0;
    const int  CHILDID_SELF                = 0;

    static WinEventProc winEventProc;                // Kept here, so the garbage collector does not free it while the hooks are active
    static IntPtr       showHook      = IntPtr.Zero;
    static IntPtr       menuPopupHook = IntPtr.Zero;

    /* Must be called from a thread with a message loop (the hook events are delivered as messages) */
    public static void StartWatching() {
        winEventProc  = OnWinEvent;
        showHook      = SetWinEventHook(EVENT_OBJECT_SHOW,           EVENT_OBJECT_SHOW,           IntPtr.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        menuPopupHook = SetWinEventHook(EVENT_SYSTEM_MENUPOPUPSTART, EVENT_SYSTEM_MENUPOPUPSTART, IntPtr.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }

    public static void StopWatching() {
        if (showHook != IntPtr.Zero)
            UnhookWinEvent(showHook);
        if (menuPopupHook != IntPtr.Zero)
            UnhookWinEvent(menuPopupHook);
        showHook      = IntPtr.Zero;
        menuPopupHook = IntPtr.Zero;
    }

    private static void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime) {
        try {
            if (hwnd == IntPtr.Zero)
                return;
            if (eventType == EVENT_OBJECT_SHOW && (idObject != OBJID_WINDOW || idChild != CHILDID_SELF))
                return; // Not a window: EVENT_OBJECT_SHOW is also sent for carets, cursors and other objects
            if (TaskbarGeometry.IsVisible())
                MoveAboveTaskbar(hwnd, true);
        } catch { }
    }

    /* Tray icon menu drawn by the application itself (WinForms, Qt...): the foreground window is invisible */
    public static bool IsCustomTrayMenuOpen() {
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground == IntPtr.Zero || Win32.IsWindowVisible(foreground))
            return false;
        foreach (IntPtr window in GetThreadWindows(foreground))
            if (WhyNotAMenu(window) == null)
                return true;
        return false;
    }

    /* Safety net for the hooks, called by the hot corner loop while the Taskbar is visible */
    public static void MoveMenusAboveTaskbar() {
        /* Popup windows of the foreground thread */
        IntPtr foreground = Win32.GetForegroundWindow();
        if (foreground != IntPtr.Zero)
            foreach (IntPtr window in GetThreadWindows(foreground))
                MoveAboveTaskbar(window, false);

        /* Standard Windows menus of any application */
        for (IntPtr menu = FindWindowEx(IntPtr.Zero, IntPtr.Zero, STANDARD_MENU_CLASSNAME, null);
             menu != IntPtr.Zero;
             menu = FindWindowEx(IntPtr.Zero, menu, STANDARD_MENU_CLASSNAME, null))
            MoveAboveTaskbar(menu, false);
    }

    private static List<IntPtr> GetThreadWindows(IntPtr window) {
        List<IntPtr> windows = new List<IntPtr>();
        uint processId;
        uint threadId = Win32.GetWindowThreadProcessId(window, out processId);
        EnumThreadWindows(threadId, delegate (IntPtr hWnd, IntPtr lParam) {
            windows.Add(hWnd);
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    /* Returns why the window is not a menu to move, or null if it is one */
    private static string WhyNotAMenu(IntPtr hwnd) {
        if (Win32.IsWindowVisible(hwnd) == false)
            return "not visible";
        if ((Win32.GetWindowLong(hwnd, Win32.GWL_STYLE) & Win32.WS_POPUP) == 0)
            return "not a popup window";

        /* Excludes invisible helper windows (e.g. 0x0 or 1905x4 windows of Explorer) */
        RECT bounds;
        if (Win32.GetWindowRect(hwnd, out bounds) == false || bounds.right - bounds.left < MIN_MENU_SIZE || bounds.bottom - bounds.top < MIN_MENU_SIZE)
            return "too small";

        /* Standard Windows menus are always moved, whatever application or thread they belong to (e.g. Bluetooth, which runs inside Explorer) */
        if (Win32.GetClassName(hwnd) == STANDARD_MENU_CLASSNAME)
            return null;

        /* Menus drawn by the applications */
        if ((Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_TOPMOST) == 0)
            return "not topmost";

        uint processId, foregroundProcessId, taskbarProcessId;
        if (Win32.GetWindowThreadProcessId(hwnd, out processId) != Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out foregroundProcessId))
            return "not on the thread of the foreground window";

        /* The menus of the Taskbar itself (Explorer) already open in the right place */
        Win32.GetWindowThreadProcessId(TaskbarGeometry.Taskbar, out taskbarProcessId);
        if (processId == taskbarProcessId)
            return "Explorer window";

        return null;
    }

    /* logRejected (--debug): log why a popup window over the Taskbar is not moved (only for the hooks, the loop would repeat it) */
    private static void MoveAboveTaskbar(IntPtr hwnd, bool logRejected) {
        RECT menu;
        if (Win32.GetWindowRect(hwnd, out menu) == false)
            return;

        /* Only windows over the Taskbar of the primary screen */
        Rectangle screen     = Screen.PrimaryScreen.Bounds;
        int               taskbarTop = TaskbarGeometry.GetTop();
        if (menu.bottom <= taskbarTop || menu.top >= screen.Bottom || menu.right <= screen.Left || menu.left >= screen.Right)
            return;

        string notAMenu = WhyNotAMenu(hwnd);
        if (notAMenu != null) {
            if (DebugLog.Enabled && logRejected && notAMenu != "not visible" && notAMenu != "not a popup window")
                DebugLog.Write("Popup over the Taskbar NOT moved (" + notAMenu + "): " + TaskbarGeometry.DescribeWindow(hwnd) + ", taskbar top " + taskbarTop);
            return;
        }

        int  y     = Math.Max(screen.Top, taskbarTop - (menu.bottom - menu.top));
        bool moved = Win32.SetWindowPos(hwnd, IntPtr.Zero, menu.left, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

        if (DebugLog.Enabled) {
            if (moved)
                DebugLog.Write("Menu moved from y=" + menu.top + " to y=" + y + ": " + TaskbarGeometry.DescribeWindow(hwnd) + ", taskbar top " + taskbarTop);
            else
                DebugLog.Write("Menu NOT moved, Windows refused (error " + Marshal.GetLastWin32Error() + "): " + TaskbarGeometry.DescribeWindow(hwnd));
        }
    }
}


/*

Win32 structures and functions used by more than one class.
The ones used by a single feature are declared next to the code that uses them.

More infos on the RECT struct:
https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rect
https://docs.microsoft.com/it-it/dotnet/api/system.runtime.interopservices.layoutkind?view=net-6.0

*/
[StructLayout(LayoutKind.Sequential)]
struct RECT {
    public Int32 left;
    public Int32 top;
    public Int32 right;
    public Int32 bottom;

    public override string ToString() { // --debug
        return "{" + left + "," + top + "," + right + "," + bottom + "}";
    }
}

static class Win32 {

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string strClassName, string strWindowName);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", SetLastError = true)] // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    public const int GWL_STYLE     = -16;
    public const int GWL_EXSTYLE   = -20;
    public const int WS_POPUP      = unchecked((int)0x80000000);
    public const int WS_EX_TOPMOST = 0x0008;

    public static string GetClassName(IntPtr hWnd) {
        const int     N_CHARS   = 256;
        StringBuilder className = new StringBuilder(N_CHARS);
        GetClassName(hWnd, className, className.Capacity);
        return className.ToString();
    }

    public static bool IsTopmost(IntPtr hWnd) {
        return (GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;
    }
}
