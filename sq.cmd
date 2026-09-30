@echo off
rem SharpQuest helper for Windows. Usage: sq test, sq update, sq help
rem The exit on the same line stops cmd from reading this file again after sq update replaces it.
dotnet run --file "%~dp0sq.cs" -- %* & exit /b
