@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify-Order03Restart.ps1" -TestRoot "%~dp0."
pause
