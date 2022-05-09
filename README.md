# WinYcorners
Sets a hot-corner that shows the Task View on mouse hover. 

This tool wants to imitate the hot corners of Gnome Desktop 40+.
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
SYNOPSIS: 
    winycorners [OPTIONS]

OPTIONS:
    --top-left
    --top-right
    --bottom-left
    --bottom right
```
