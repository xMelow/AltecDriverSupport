@echo off
rem Launches PhantomPrinter.ps1 (which self-elevates via UAC).
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0PhantomPrinter.ps1" %*
