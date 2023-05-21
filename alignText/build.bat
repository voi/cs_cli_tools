@set MYPATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319
@set PATH=%MYPATH%;%PATH%

@cls
@REM @csc.exe /t:winexe /optimize+ /out:TextLineViewer.exe TextLineViewer.cs /r:system.dll,system.drawing.dll,system.windows.forms.dll,system.io.dll,System.Reflection.dll
@REM @csc.exe test.cs
@csc.exe alignText.cs

@set MYPATH=
