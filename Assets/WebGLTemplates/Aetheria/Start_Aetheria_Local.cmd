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

if exist "%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" (
  set "PYTHON_EXE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
  goto :python_found
)

echo [ERROR] Python was not found.
echo Install Unity WebGL Build Support or Python 3, then try again.
pause
popd
exit /b 1

:python_found
"%PYTHON_EXE%" %PYTHON_ARGS% "%~dp0Start_Aetheria_Local.py"

if errorlevel 1 (
  echo.
  echo [ERROR] The local server could not start.
  pause
)

popd
endlocal
