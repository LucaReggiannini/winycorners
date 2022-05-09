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
            if (hotCorner.Contains(new Point(Cursor.Position.X, Cursor.Position.Y)) == true) {
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
        System.Diagnostics.Process.Start("explorer.exe", "shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}");
    }
}
