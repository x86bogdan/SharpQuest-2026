@echo off
rem SharpQuest helper for Windows. Usage: sq test, sq update, sq help
rem Works with a .NET 9 or .NET 10 SDK. The first run builds sq itself (10-20 s).
rem The exit on the same line stops cmd from reading this file again after sq update replaces it.
dotnet run --project "%~dp0tools\sq" -- %* & exit /b
