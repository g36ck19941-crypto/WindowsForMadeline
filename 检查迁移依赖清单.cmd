@echo off
chcp 65001 >nul
cd /d "%~dp0"
type docs\zh-CN\MIGRATION-INVENTORY-ENTRY.txt
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Inspect-MigrationInventory.ps1
set "inventory_exit=%errorlevel%"
if not "%CDR_NO_PAUSE%"=="1" pause
exit /b %inventory_exit%
