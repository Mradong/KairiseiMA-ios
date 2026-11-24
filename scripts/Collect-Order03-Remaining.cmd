@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Collect-Order03RemainingEvidence.ps1" -TestRoot "%~dp0."
if errorlevel 1 (
  echo Collection failed. Keep this window so the error can be read.
) else (
  echo Collection complete. Return the new Order03-Remaining-*.zip file.
)
pause
