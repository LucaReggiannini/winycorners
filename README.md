# WinYcorners
Sets a hot-corner that shows the Task View on mouse hover.

This tool wants to imitate the hot corners of Gnome Desktop 40+.\
Gnome like hot corners for Windows 11.

# How to compile
I use *csc.exe* (i like it).

Locate your C# compiler with `dir /S /B C:\Windows\Microsoft.NET\csc.exe`\
Mine is in *C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe*

Now you are ready to compile:
```
git clone https://github.com/LucaReggiannini/winycorners.git
cd winycorners
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:winexe /out:winycorners.exe winycorners.cs
```

# Usage
```
WinYcorners v2.0.0
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
```

# Start with Windows (as administrator)
Run in a PowerShell as administrator (change the path of `winycorners.exe`):
```powershell
$exe       = "C:\Tools\winycorners\winycorners.exe"
$user      = "$env:USERDOMAIN\$env:USERNAME"
$trigger   = New-ScheduledTaskTrigger -AtLogOn -User $user
$trigger.Delay = "PT10S"
$action    = New-ScheduledTaskAction -Execute $exe -Argument "--enhanced-task-view"
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Highest
$settings  = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
Register-ScheduledTask -TaskName "WinYcorners" -Trigger $trigger -Action $action -Principal $principal -Settings $settings
```

To remove it:
```powershell
Unregister-ScheduledTask -TaskName "WinYcorners" -Confirm:$false
```
