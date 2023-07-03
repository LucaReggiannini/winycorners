/*
fghelper

Small utility that logs informations about the current foreground windows.
This is part of the WinYcorners project.

Created by Luca Reggiannini. Original repo: https://github.com/LucaReggiannini/winycorners

Search your C# compiler with `cd C:/ %% dir /S /B csc.exe`
Compile & run with: 
cls && taskkill /F /T /IM fghelper.exe & C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /out:fghelper.exe fghelper.cs && fghelper.exe
*/

using System;                          
using System.Threading;                
using System.Text;                     
using System.Runtime.InteropServices;

class fghelper {

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    static void Main(string[] args) {

        const int N_CHARS = 256;
        string windowClassName     = "";
        string windowTitle         = "";
        string windowClassNameLast = "";
        string windowTitleLast     = "";

        while(true) {

            /* Get window Class Name */
            var hWnd = GetForegroundWindow();
            StringBuilder sbWindowClassName = new StringBuilder(N_CHARS);
            GetClassName(hWnd, sbWindowClassName, sbWindowClassName.Capacity);
            windowClassName = sbWindowClassName.ToString();
            
            /* Get window Title */
            StringBuilder sbWindowTitle = new StringBuilder(N_CHARS);
            if (GetWindowText(hWnd, sbWindowTitle, N_CHARS) > 0)
                windowTitle = sbWindowTitle.ToString();
            
            if (windowClassName!=windowClassNameLast || windowTitle!=windowTitleLast) {
                DateTime now = DateTime.Now;
                Console.WriteLine("");
                Console.WriteLine("Time : " + now);
                Console.WriteLine("Class: " + windowClassName);
                Console.WriteLine("Title: " + windowTitle);
                Console.WriteLine("");
            }

            windowClassNameLast = windowClassName;
            windowTitleLast = windowTitle;
            Thread.Sleep(1000);
        }
    }
}
