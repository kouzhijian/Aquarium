' Detach the aquarium from the wallpaper layer back into a normal window. ASCII only.
Option Explicit
Dim sh
Set sh = CreateObject("WScript.Shell")
sh.Run """C:\Users\bestv\Aquarium\AquariumWallpaper.exe"" restore", 0, False
