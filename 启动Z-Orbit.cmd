@echo off
if exist "%~dp0bin\Release\net8.0-windows\win-x64\Z-Orbit.exe" (
  start "" "%~dp0bin\Release\net8.0-windows\win-x64\Z-Orbit.exe"
) else if exist "%~dp0publish\Z-Orbit\Z-Orbit.exe" (
  start "" "%~dp0publish\Z-Orbit\Z-Orbit.exe"
) else (
  echo Z-Orbit executable not found. Please build the project first.
  pause
)
