set WORKSPACE=..
set LUBAN_DLL=./Tools/Luban/Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t all ^
    -d json ^
    -c cs-simple-json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputDataDir=./Outputs/Data ^
    -x outputCodeDir=./Outputs/Code
pause