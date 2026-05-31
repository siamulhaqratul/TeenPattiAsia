@echo off
title Graphify Graph Watcher — TeenPattiAsia
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0watch-graph.ps1"
pause
