' Virtual Aquarium launcher (ASCII only: VBScript reads .vbs as ANSI)
Option Explicit
Dim sh, fso, exe, url, args

Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

If fso.FileExists("C:\Program Files\Google\Chrome\Application\chrome.exe") Then
  exe = "C:\Program Files\Google\Chrome\Application\chrome.exe"
ElseIf fso.FileExists("C:\Program Files (x86)\Google\Chrome\Application\chrome.exe") Then
  exe = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
ElseIf fso.FileExists("C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe") Then
  exe = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
Else
  MsgBox "Chrome or Edge not found. Please open index.html with any browser.", 48, "Aquarium"
  WScript.Quit
End If

url = "file:///C:/Users/bestv/Aquarium/index.html"
args = "--app=" & url & " --window-size=1280,820 --window-position=160,70" & _
       " --no-first-run --no-default-browser-check --allow-file-access-from-files --disable-features=Translate,MediaRouter"

sh.Run """" & exe & """ " & args, 1, False
