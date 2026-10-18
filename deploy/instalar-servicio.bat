@echo off
rem Doble clic para instalar o actualizar el servicio. Si no se abrio como administrador, se
rem vuelve a lanzar solo pidiendo permiso (aviso de Windows: responder "Si"). Ademas evita el
rem bloqueo de PowerShell por "ExecutionPolicy", que salta si la carpeta vino de una descarga.
fltmc >nul 2>&1 || (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0instalar-servicio.ps1" %*
pause
