/*
WinYcorners

Sets a hot-corner that shows the Task View on mouse hover.
This tool wants to imitate the hot corners of Gnome Desktop 40+.
Gnome like hot corners for Windows.

Created by Luca Reggiannini. Original repo: https://github.com/LucaReggiannini/winycorners

Search your C# compiler with `cd C:/ %% dir /S /B csc.exe`
Compile & run with: 
cls && taskkill /F /T /IM winycorners.exe & C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /t:winexe /out:winycorners.exe winycorners.cs && winycorners.exe --top-left
*/

using System;                          // Tuples
using System.Windows;                  // Rect, Point
using System.Windows.Forms;            // System.Windows.Forms.Form, Screen.PrimaryScreen.Bounds, MessageBox.Show
using System.Threading;                // Thread.Sleep
using System.Runtime.InteropServices;  // System.Diagnostics.Process.Start
using System.Text;                     // StringBuilder
using System.Diagnostics;              // Process

class HotCornerForm : System.Windows.Forms.Form {
   
    public HotCornerForm(string position, bool enhancedTaskView, short hotCornerSize, short updateTime, short enhancedTaskViewDelay) {
        /* Create an invisible Windows Form */
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState     = FormWindowState.Minimized;
        this.ShowInTaskbar   = false;

        HotCorner hc = new HotCorner(
            position,
            enhancedTaskView,
            hotCornerSize,
            updateTime,
            enhancedTaskViewDelay);
    }


    static void Main(string[] args) {

        bool   enhancedTaskView      = false;
        string position              = "--top-left";
        short  hotCornerSize         = 8;
        short  updateTime            = 100;
        short  enhancedTaskViewDelay = 150;

        /* Parse arguments */
        if (args == null || args.Length == 0)
            help();
        else {
            for (int c = 0; c < args.Length; c++) {
                if (args[c]=="--enhanced-task-view")
                    enhancedTaskView = true;
                else if (args[c]=="--top-left" || args[c]=="--bottom-left" || args[c]=="--top-right" || args[c]=="--bottom-right")
                    position=args[c];
                else if (args[c]=="--corner-size") {
                    try {
                        hotCornerSize=short.Parse(args[c+1]);
                        c=c+1;
                    } catch {
                        help();
                    }
                } else if (args[c]=="--update-time") {
                    try {
                        updateTime=short.Parse(args[c+1]);
                        c=c+1;
                    } catch {
                        help();
                    }
                } else if (args[c]=="--enhanced-task-view-delay") {
                    try {
                        enhancedTaskViewDelay=short.Parse(args[c+1]);
                        c=c+1;
                    } catch {
                        help();
                    }
                } else 
                    help();
            }

            KillExistingProcesses();

            System.Windows.Forms.Application.Run(
                new HotCornerForm(
                    position,
                    enhancedTaskView,
                    hotCornerSize,
                    updateTime,
                    enhancedTaskViewDelay));
        }
    }

    private static void KillExistingProcesses() {
        MessageBoxButtons buttons = MessageBoxButtons.YesNo;
        
        Process currentProcess = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
        foreach (var process in processes) {
            if (process.Id != currentProcess.Id) {
                DialogResult result = MessageBox.Show(
                    "Another WinYcorners process was found (PID:" + process.Id + "). " +
                    "Do you want to terminate it?", 
                    "WinYcorners", 
                    buttons,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                    process.Kill();
            }
        }
    }

    static void help() {
        MessageBox.Show(@"
WinYcorners v1.7.0
https://github.com/LucaReggiannini/winycorners/

Sets a hot-corner that shows the Task View on mouse hover.

This tool wants to imitate the hot corners of Gnome Desktop 40+.

SYNOPSIS: 
    winycorners [POSITION] [OPTIONS]

POSITION:
    --top-left
    --top-right
    --bottom-left
    --bottom right

    default position is top-left

OPTIONS:
    --corner-size <pixels>
        The  size of the hot corner in pixels.  Default value is 8px

    --update-time <milliseconds>
        Determines  every how many milliseconds  the position of the
        Windows cursor is checked (in order to trigger a hot corner).
        The default value is 100 milliseconds. Decreasing this value
        can increase the responsiveness of WinYconer but can lead to
        performance problems on the system

    --enhanced-task-view
        Hides Taskbar and maximize the desktop working area.
        Show the Taskbar only when Task View is visible (like Gnome)

    --enhanced-task-view-delay <milliseconds>
        In  Windows 11  the  Taskbar  visibility  can't  be  changed
        while the Task View is active.  For Enhanced Task View  mode
        to  work  properly, the Taskbar  is made visible immediately
        after triggering the hot corner. 
        Every N milliseconds it is checked if the task view is still
        active: if it is not, the task bar is made invisible  again.
        The '--enhanced-task-view-delay'  option  allows  you to set
        the  number  of   milliseconds  with  which  this  check  is
        performed. The default value is 150 milliseconds (this value
        is  based  on the time it takes  the Task View  animation to
        finish).
        Decreasing  this value  can  increase  the responsiveness of
        WinYconers in  Enhanced  Task  View  mode,  but  can lead to
        problems in properly displaying the Taskbar.
        Decreasing   the   value  is  recommended  only  if  Windows
        animations are not enabled on your system.
        Increasing the value may be useful for slower systems
");

    System.Environment.Exit(0);

    }
}


class HotCorner {

    int         SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
    int         SCREEN_WIDTH  = Screen.PrimaryScreen.Bounds.Width;

    public HotCorner(string position, bool enhancedTaskView, short hotCornerSize, short updateTime, short enhancedTaskViewDelay) {

        /* Defining hot corner size */
        Size  s         = new Size(hotCornerSize, hotCornerSize);
        Rect  hotCorner = new Rect(s);

        /* Defining hot corner position (.Location is the upper-left point) */
        if (position=="--top-left")
            hotCorner.Location = new Point(0, 0);
        else if (position=="--top-right") 
        hotCorner.Location = new Point(SCREEN_WIDTH - hotCornerSize, 0);
        else if (position=="--bottom-left")
        hotCorner.Location = new Point(0, SCREEN_HEIGHT - hotCornerSize);
        else if (position=="--bottom-right")
        hotCorner.Location = new Point(SCREEN_WIDTH - hotCornerSize, SCREEN_HEIGHT - hotCornerSize);
        
        Cursor c                  = new Cursor(Cursor.Current.Handle);
        bool   triggeredHotCorner = false;

        while(true) {

            if (CheckFullScreenForegroundApp()==false || IsTaskViewVisible()==true) { 
                /* 
                
                Trigger hot corner based on the cursor position.
                If the left mouse button is pressed, the hot corner is not triggered: maybe a drag and drop operation is in progress.

                Note: WinYcorners does not trigger hot corner if there is a full screen application.
                Windows 11 Task View is considered a full screen application so is white listed 
                using "...|| IsTaskViewVisible()==true" in the following conditional block.
                
                */
                if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y))==true && isDown(Keys.LButton)==false) {
                    SetTaskbarVisible(true);
                    if (triggeredHotCorner == false) {
                        triggeredHotCorner = true;
                        SwitchTaskView();
                    }
                } else if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y))==false && triggeredHotCorner)
                    triggeredHotCorner = false;
                
                
                if (enhancedTaskView==true) {

                    /* Wait for Task View animations to terminate to determine if it is visible or not*/
                    Thread.Sleep(enhancedTaskViewDelay);

                    /* Display the taskbar only during the task view */
                    if (IsTaskViewVisible()==true)
                        SetTaskbarVisible(true);
                    else
                        SetTaskbarVisible(false);

                    /* Make sure that the desktop working area is always full size */
                    ExpandWorkingArea();
                }

                Thread.Sleep(updateTime);
            }
        }
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
    [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int GetAsyncKeyState(int vKey);
    private static bool isDown(Keys button) {
            return GetAsyncKeyState((int)button) > 1;
    }


    /* 

    More infos on the RECT struct:
    https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rect
    https://docs.microsoft.com/it-it/dotnet/api/system.runtime.interopservices.layoutkind?view=net-6.0 
    
    Needed by "systemparametersinfoa" while working with "SPI_SETWORKAREA":
    https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
    
    */
    [StructLayout(LayoutKind.Sequential)] 
    struct RECT {
        public Int32 left;
        public Int32 top;   
        public Int32 right;
        public Int32 bottom;
    }


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);
    const int SPI_SETWORKAREA    = 0x002F;
    const int SPIF_UPDATEINIFILE = 0x0001; // https://www.pinvoke.net/default.aspx/Enums/SPIF.html
    
    private void ExpandWorkingArea() {

        /* 

        Code used to resize the desktop working area.
        The working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool 
        bars.
        
        */

        RECT workingArea;
        workingArea.left = 0;
        workingArea.top = 0;
        workingArea.right = SCREEN_WIDTH;
        workingArea.bottom = SCREEN_HEIGHT;
        SystemParametersInfo(SPI_SETWORKAREA, 0, ref workingArea, SPIF_UPDATEINIFILE);    
    }
    

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowRect(IntPtr hwnd, out RECT lpRect);

    public static bool CheckFullScreenForegroundApp() {
        
        /* 
        Thanks to/original code: 
        https://github.com/RobertSmits/Windows-10-HotCorners/blob/master/Windows10.HotCorners/Business/ForegroundDetector.cs */
    
        var desktopHandle = GetDesktopWindow();
        var shellHandle   = GetShellWindow();
        var hWnd          = GetForegroundWindow();

        if (hWnd.Equals(IntPtr.Zero)) return false;
        if (hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)) return false;
        
        RECT appBounds;
        GetWindowRect(hWnd, out appBounds);
        var screenBounds = Screen.FromHandle(hWnd).Bounds;

        if (
            (appBounds.bottom - appBounds.top)  == screenBounds.Height && 
            (appBounds.right  - appBounds.left) == screenBounds.Width
            )
            return true; // Return True if Fullscreen application is found 
        else
            return false;
    }
    

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string strClassName, string strWindowName);
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")] // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
    private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, uint wFlags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);
    
    const uint   SWP_HIDEWINDOW           = 0x0080;
    const uint   SWP_SHOWWINDOW           = 0x0040;
    const string TASKBAR_CLASSNAME_LEGACY = "System_TrayWnd";
    const string TASKBAR_CLASSNAME        = "Shell_TrayWnd";

    private void SetTaskbarVisible(bool visible) {

        IntPtr hwnd = FindWindow(TASKBAR_CLASSNAME_LEGACY, null);

        if (hwnd == IntPtr.Zero) 
            hwnd = FindWindow(TASKBAR_CLASSNAME, null);
        if (visible) 
            SetWindowPos(hwnd,0,0,0,0,0,SWP_SHOWWINDOW); // Show Taskbar
        else 
            SetWindowPos(hwnd,0,0,0,0,0,SWP_HIDEWINDOW); // Hide Taskbar

    }

    /* [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow(); // Already declared */
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private bool IsTaskViewVisible() {

        /* Get window Class Name */
        const int N_CHARS         = 256;
        string    windowClassName = "";
        var       hWnd            = GetForegroundWindow();

        StringBuilder sbWindowClassName = new StringBuilder(N_CHARS);
        GetClassName(hWnd, sbWindowClassName, sbWindowClassName.Capacity);
        windowClassName = sbWindowClassName.ToString();

        IntPtr xamlExplorerHostIslandWindow = FindWindow("XamlExplorerHostIslandWindow", null); // Windows 11
        IntPtr windowsUICoreCoreWindow      = FindWindow("Windows.UI.Core.CoreWindow"  , null); // Windows 10  

        if (windowClassName=="XamlExplorerHostIslandWindow" || windowClassName=="Windows.UI.Core.CoreWindow")
            return true;
        else 
            return false;
    }
}