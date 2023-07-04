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


class HotCornerForm : System.Windows.Forms.Form {

    
    public HotCornerForm(string position, bool enhancedTaskView, short cornerSize) {
        /* Create an invisible Windows Form */
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState     = FormWindowState.Minimized;
        this.ShowInTaskbar   = false;

        HotCorner hc = new HotCorner(position, enhancedTaskView, cornerSize);
    }


    static void Main(string[] args) {
        
        bool   enhancedTaskView = false;
        string position         = "--top-left";
	short  cornerSize       = 8;

        /* Parse arguments */
        if (args == null || args.Length == 0)
            help();
        else {
            for (int c = 0; c < args.Length; c++) {
                if (args[c]=="--enhanced-task-view") {
                    enhancedTaskView = true;
                }
                else if (args[c]=="--top-left" || args[c]=="--bottom-left" || args[c]=="--top-right" || args[c]=="--bottom-right") {
                    position=args[c];
                }
                else if (args[c]=="--corner-size") {
		    try {
                        cornerSize=short.Parse(args[c+1]);
			c=c+1;
                    } catch {
                        help();
                    }
                }
                else 
                    help();
            }

            System.Windows.Forms.Application.Run(new HotCornerForm(position, enhancedTaskView, cornerSize));
        }
            
    }


    static void help() {
        MessageBox.Show(@"
WinYcorners v1.5.0
https://github.com/LucaReggiannini/winycorners/

Sets a hot-corner that shows the Task View on mouse hover.

This tool wants to imitate the hot corners of Gnome Desktop 40+.

SYNOPSIS: 
    winycorners [OPTIONS] [POSITION]

OPTIONS:
    --enhanced-task-view
        Hides taskbar and maximize the desktop working area.
        Show the taskbar only when taskview is visible (like Gnome)

    --corner-size
        The size of the hot corner in pixels. Default value is 8px

POSITION:
    --top-left
    --top-right
    --bottom-left
    --bottom right

    default position is top-left
                    ");
    }
}


class HotCorner {

    int   SCREEN_HEIGHT         = Screen.PrimaryScreen.Bounds.Height;
    int   SCREEN_WIDTH          = Screen.PrimaryScreen.Bounds.Width;
    // const short HOT_CORNER_SIZE = 8; // Corner size is now passed as argument
    const short REFRESH_TIME    = 10;

    public HotCorner(string position, bool enhancedTaskView, short HOT_CORNER_SIZE) {

        /* Defining hot corner size */
        Size  s         = new Size(HOT_CORNER_SIZE, HOT_CORNER_SIZE);
        Rect  hotCorner = new Rect(s);

        /* Defining hot corner position (.Location is the upper-left point) */
        if (position=="--top-left")
            hotCorner.Location = new Point(0, 0);
        else if (position=="--top-right") 
          hotCorner.Location = new Point(SCREEN_WIDTH - HOT_CORNER_SIZE, 0);
        else if (position=="--bottom-left")
          hotCorner.Location = new Point(0, SCREEN_HEIGHT - HOT_CORNER_SIZE);
        else if (position=="--bottom-right")
          hotCorner.Location = new Point(SCREEN_WIDTH - HOT_CORNER_SIZE, SCREEN_HEIGHT - HOT_CORNER_SIZE);   
        

        Cursor c = new Cursor(Cursor.Current.Handle);
        bool triggeredHotCorner = false;

        while(true) {

            if (ForegroundDetect.CheckFullScreen()==false || IsTaskViewVisible()==true) { 
                /* Trigger hot corner based on the cursor position.
                If the left mouse button is pressed the hot corner is not triggered: maybe a drag and drop operation is in progress.

                Note: hot corner is not triggered if there is a full screen application.
                Windows 11 task view is considered a full screen application so is white
                listed using "|| IsTaskViewVisible()==true" in the conditional block. */
                if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y))==true && isDown(Keys.LButton)==false) {
		    SetTaskbarVisible(true);
                    if (triggeredHotCorner == false) {
                        triggeredHotCorner = true;
                        SwitchTaskView();
                    }
                }
                else if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y))==false && triggeredHotCorner) {
                    triggeredHotCorner = false;
		}
                
                if (enhancedTaskView==true) {
		    /* Wait for Task View animations to terminate to determine if it is visible or not*/
		    Thread.Sleep(300);

                    /* Display the taskbar only during the task view */
                    if (IsTaskViewVisible()==true)
                        SetTaskbarVisible(true);
                    else
                        SetTaskbarVisible(false);

		    /* Make sure that the desktop working area is always full size */
                    ExpandWorkingArea();
                }

                Thread.Sleep(REFRESH_TIME);
            }

        }
    }


    /* Code used to show the task view ______________________________________________________________________*/
    
    /* Code used to simulate a keypress.
    Using "keybd_event" from user32.dll since "System.Windows.Forms.SendKeys.Send" does not support Win key.
    Emulating with CTRL+ESC, SendWait("^({ESC}{TAB})") or Send("^({ESC}{TAB}})") does not work */
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const int KEYEVENTF_KEYUP = 0x0002;
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

    /* Code used to detect if a Keyboard or mouse button is pressed or not
    If the mouse button is pressed while the cursor is at the corner of the screenmay indicate that a drag-and-drop 
    operation is in progress and therefore it is not necessary to activate the hotcorner */
    [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int GetAsyncKeyState(int vKey);
    private static bool isDown(Keys button) {
            return GetAsyncKeyState((int)button) > 1;
    }
    /*_______________________________________________________________________________________________________*/


    /* Code used to resize the desktop working area__________________________________________________________
    The working area is the desktop area of the display, excluding taskbars, docked windows, and docked tool 
    bars. */

    /* More infos on this struct:
    https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rect
    https://docs.microsoft.com/it-it/dotnet/api/system.runtime.interopservices.layoutkind?view=net-6.0 
    Needed by "systemparametersinfoa" while working with "SPI_SETWORKAREA":
    https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa*/
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
    const int SPI_SETWORKAREA = 0x002F;
    const int SPIF_UPDATEINIFILE = 0x0001; /* Source: https://www.pinvoke.net/default.aspx/Enums/SPIF.html */
    private void ExpandWorkingArea() {
        RECT workingArea;
        workingArea.left = 0;
        workingArea.top = 0;
        workingArea.right = SCREEN_WIDTH;
        workingArea.bottom = SCREEN_HEIGHT;
        SystemParametersInfo(SPI_SETWORKAREA, 0, ref workingArea, SPIF_UPDATEINIFILE);    
    }
    /*_______________________________________________________________________________________________________*/


    /* Code used to show/hide the taskbar____________________________________________________________________*/
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string strClassName, string strWindowName);
    /* https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos */
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, uint wFlags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);
    const uint SWP_HIDEWINDOW = 0x0080;
    const uint SWP_SHOWWINDOW = 0x0040;
    const string TASKBAR_CLASSNAME_LEGACY = "System_TrayWnd";
    const string TASKBAR_CLASSNAME        = "Shell_TrayWnd";
    private void SetTaskbarVisible(bool visible) {
        IntPtr hwnd = FindWindow(TASKBAR_CLASSNAME_LEGACY, null);
        if (hwnd == IntPtr.Zero) 
            hwnd = FindWindow(TASKBAR_CLASSNAME, null);
        if (visible) 
            SetWindowPos(hwnd,0,0,0,0,0,SWP_SHOWWINDOW);
        else 
            SetWindowPos(hwnd,0,0,0,0,0,SWP_HIDEWINDOW);       
    }
    /*_______________________________________________________________________________________________________*/


    /* Code used to check if taskview is visible____________________________________________________________ */
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    private bool IsTaskViewVisible() {
       
        var hWnd = GetForegroundWindow();
        const int N_CHARS = 256;
        StringBuilder Buff = new StringBuilder(N_CHARS);
        String windowsText = "";
        if (GetWindowText(hWnd, Buff, N_CHARS) > 0)
        {
            windowsText = Buff.ToString();
        }
        if (windowsText=="Task View") {
            return true;
        } else {
            return false;
        }
    }
    /*_______________________________________________________________________________________________________*/

}

class ForegroundDetect { 

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowRect(IntPtr hwnd, out RECT lpRect);

        public static bool CheckFullScreen() {
            
            /* 
            Return True if there is a fullscreen application 
            Thanks to/original code: https://github.com/RobertSmits/Windows-10-HotCorners/blob/master/Windows10.HotCorners/Business/ForegroundDetector.cs    
            */
        
            var desktopHandle = GetDesktopWindow();
            var shellHandle = GetShellWindow();
            var hWnd = GetForegroundWindow();

            if (hWnd.Equals(IntPtr.Zero)) return false;
            if (hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)) return false;
            RECT appBounds;
            GetWindowRect(hWnd, out appBounds);
            var screenBounds = Screen.FromHandle(hWnd).Bounds;
            return (appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
    }
