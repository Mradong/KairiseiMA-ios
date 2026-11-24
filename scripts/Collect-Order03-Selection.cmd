@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Collect-Order03RuntimeSelection.ps1" -TestRoot "%~dp0."
pause
