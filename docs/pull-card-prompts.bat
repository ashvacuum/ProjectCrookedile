@echo off
rem Pulls the Google Sheet (source of truth) down over docs\crookedile-card-art-prompts.xlsx.
rem Needs the Sheet shared as "Anyone with the link can view" (Share > General access).
rem ponytail: link-share export, no OAuth. Use the Sheets API + a service account if it must stay private.
setlocal
set "SHEET_ID=1EHYOcsz7TXy8RCCnFHVJ3ahjenv5YOQ2pYVa4n5KnOg"
set "OUT=%~dp0crookedile-card-art-prompts.xlsx"
set "TMP_OUT=%TEMP%\card-art-prompts-%RANDOM%.xlsx"

curl -sfL -o "%TMP_OUT%" "https://docs.google.com/spreadsheets/d/%SHEET_ID%/export?format=xlsx"
if errorlevel 1 (
    echo Download failed. Is the Sheet shared as "Anyone with the link can view"?
    del "%TMP_OUT%" 2>nul
    exit /b 1
)

rem A sign-in page instead of a workbook means the share setting is off; never overwrite with it.
findstr /m /i "<html" "%TMP_OUT%" >nul && (
    echo Got a web page, not a spreadsheet. Check the Sheet's share setting.
    del "%TMP_OUT%"
    exit /b 1
)

move /y "%TMP_OUT%" "%OUT%" >nul
echo Updated %OUT%
