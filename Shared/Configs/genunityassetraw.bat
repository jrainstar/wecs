set WORKSPACE=..
set LUBAN_DLL=./Tools/Luban/Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t all ^
    -d json ^
    -c cs-simple-json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputDataDir=../Assets/AssetRaws/Cfgs^
    -x outputCodeDir=../Assets/Scripts/GameConf/Gen
pause