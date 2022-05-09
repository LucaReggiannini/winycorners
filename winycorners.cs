// Search your C# compiler with `cd C:/ %% dir /S /B csc.exe`
// Compile with: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /t:winexe /out:winycorners.exe winycorners.cs

using System;                         // Tuples
using System.Windows;                 // Rect, Point
using System.Windows.Forms;           // System.Windows.Forms.Form, Screen.PrimaryScreen.Bounds, MessageBox.Show
using System.Threading;               // Thread.Sleep
using System.Runtime.InteropServices; //System.Diagnostics.Process.Start



class HotCornerForm : System.Windows.Forms.Form {

    
    public HotCornerForm(string position) {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;

        HotCorner hc = new HotCorner(position);
    }


    static void Main(string[] args) {
        if (args == null || args.Length == 0) {
            help();
        } else {
            if (args[0]=="--top-left" || args[0]=="--bottom-left" || args[0]=="--top-right" || args[0]=="--bottom-right") {
                System.Windows.Forms.Application.Run(new HotCornerForm(args[0]));
            } else {
                help();
            }
        }
    }


    static void help() {
        MessageBox.Show(@"
WinYcorners

Sets a hot-corner that shows the Task View on mouse hover.

This tool wants to imitate the hot corners of Gnome Desktop 40+.

SYNOPSIS: 
    winycorners [OPTIONS]

OPTIONS:
    --top-left
    --top-right
    --bottom-left
    --bottom right
                    ");
    }
}


class HotCorner {

          int   SCREEN_HEIGHT   = Screen.PrimaryScreen.Bounds.Height;
          int   SCREEN_WIDTH    = Screen.PrimaryScreen.Bounds.Width;
    const short HOT_CORNER_SIZE = 8;
    const short REFRESH_TIME    = 10;

    
    public HotCorner(string position) {

        Rect hotCorner = new Rect();
        hotCorner.Size = new Size(HOT_CORNER_SIZE, HOT_CORNER_SIZE);

        // Set `hotCorner` upper-left point location based on `position`
        if (position=="--top-left") {
            hotCorner.Location = new Point(0, 0);
        } else if (position=="--top-right") {
            hotCorner.Location = new Point(SCREEN_WIDTH - HOT_CORNER_SIZE, 0);
        } else if (position=="--bottom-left") {
            hotCorner.Location = new Point(0, SCREEN_HEIGHT - HOT_CORNER_SIZE);
        } else if (position=="--bottom-right") {
            hotCorner.Location = new Point(SCREEN_WIDTH - HOT_CORNER_SIZE, SCREEN_HEIGHT - HOT_CORNER_SIZE);   
        }

        Cursor c = new Cursor(Cursor.Current.Handle);
        bool triggeredHotCorner = false;

        while(true) {
            if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y)) == true && isDown(Keys.LButton) == false) {
                if (triggeredHotCorner == false) {
                    triggeredHotCorner = true;
                    SwitchTaskView();
                } else { }
            } else if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y)) == false && triggeredHotCorner) {
                triggeredHotCorner = false;
            }
            Thread.Sleep(REFRESH_TIME);
            }
    }


    public void SwitchTaskView() {
        // Method used previously (Opening Task View via Explorer). Sometimes it gets stuck:
        // System.Diagnostics.Process.Start("explorer.exe", "shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
        KeyDown(Keys.LWin);
        KeyDown(Keys.Tab);
        KeyUp(Keys.LWin);
        KeyUp(Keys.Tab);
    }

    // Using `keybd_event` from user32.dll since `System.Windows.Forms.SendKeys.Send` does not support Win key.
    // Emulating with CTRL+ESC, SendWait("^({ESC}{TAB})") or Send("^({ESC}{TAB}})") does not work
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    private const int KEYEVENTF_EXTENDEDKEY = 1;
    private const int KEYEVENTF_KEYUP = 2;
    public static void KeyDown(Keys vKey) {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
    }
    public static void KeyUp(Keys vKey) {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    // Used to detect if a Key button is pressed or not
    // Used to detect if the mouse button is pressed while the cursor is at the corner of the screen.
    // This may indicate that a drag-and-drop operation is in progress and therefore it is not necessary to activate the hotcorner
    [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int GetAsyncKeyState(int vKey);
    public static bool isDown(Keys button) {
            return GetAsyncKeyState((int)button) > 1;
    }
}
