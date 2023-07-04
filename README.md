# WinYcorners
Sets a hot-corner that shows the Task View on mouse hover. 

This tool wants to imitate the hot corners of Gnome Desktop 40+.\
Gnome like hot corners for Windows.

# How to compile
I use *csc.exe* (i like it).

Locate your C# compiler with `cd C:/ %% dir /S /B csc.exe`\
Mine is in *C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe*

Locate your *WindowsBase.dll* with `cd C:/ %% dir /S /B WindowsBase.dll`\
Mine is in *C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll*

Now you are ready to compile:
```
git clone https://github.com/LucaReggiannini/winycorners.git
cd winycorners
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /r:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /t:winexe /out:winycorners.exe winycorners.cs
```

# Usage
```
WinYcorners v1.6.0
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
```

# Update: Fghelper
Fghelper is a small tool that logs basic information about the current foreground window.

It is useful to debug WinYcorners problems in case class or window names for desktop elements are changed in new versions of Windows.

The procedure to compile fghelper is the same as WinYcorners. Example:
```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /out:fghelper.exe fghelper.cs
```

You'll just need to start the tool and select the window of which you want to obtain the information. The information will be written to the terminal window. For example, to get the details of the Task View it will be enough to press the `Win+Tab` keys and the information will be shown in the output. Example:

![fghelper.png](fghelper.png)
