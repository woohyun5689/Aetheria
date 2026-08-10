@echo off
setlocal EnableExtensions

title Aetheria Local WebGL Server
pushd "%~dp0" || (
  echo [ERROR] Could not open the build folder.
  pause
  exit /b 1
)

set "PYTHON_EXE="
set "PYTHON_ARGS="

for /f "delims=" %%D in ('dir /b /ad /o-n "%ProgramFiles%\Unity\Hub\Editor" 2^>nul') do (
  if exist "%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Data\PlaybackEngines\WebGLSupport\BuildTools\Emscripten\python\python.exe" (
    set "PYTHON_EXE=%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Data\PlaybackEngines\WebGLSupport\BuildTools\Emscripten\python\python.exe"
    goto :python_found
  )
)

py -3 -c "import sys" >nul 2>&1
if not errorlevel 1 (
  set "PYTHON_EXE=py"
  set "PYTHON_ARGS=-3"
  goto :python_found
)

python -c "import sys" >nul 2>&1
if not errorlevel 1 (
  set "PYTHON_EXE=python"
  goto :python_found
)

echo [ERROR] Python was not found.
echo Install Unity WebGL Build Support or Python 3, then try again.
pause
popd
exit /b 1

:python_found
set "PORT=8765"
set "URL=http://127.0.0.1:%PORT%/"

echo.
echo Aetheria is starting at %URL%
echo Keep this window open while playing.
echo Close this window or press Ctrl+C to stop the server.
echo.

if /I not "%~1"=="--no-browser" (
  start "" /b powershell.exe -NoProfile -WindowStyle Hidden -Command "Start-Sleep -Milliseconds 900; Start-Process '%URL%'" >nul 2>&1
)
"%PYTHON_EXE%" %PYTHON_ARGS% -m http.server %PORT% --bind 127.0.0.1

if errorlevel 1 (
  echo.
  echo [ERROR] The local server could not start. Port %PORT% may already be in use.
  pause
)

popd
endlocal
