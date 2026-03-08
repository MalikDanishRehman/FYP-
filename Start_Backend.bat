@echo off
REM Start HydroAI backend only. Run from repo root, or double-click.
cd /d "%~dp0HydroAI.Backend"
if not exist ".venv\Scripts\activate.bat" (
    echo Creating .venv and installing dependencies...
    python -m venv .venv
    call .venv\Scripts\activate.bat
    pip install -r requirements.txt
) else (
    call .venv\Scripts\activate.bat
)
echo Backend: http://127.0.0.1:8000
uvicorn main:app --reload --host 127.0.0.1 --port 8000
