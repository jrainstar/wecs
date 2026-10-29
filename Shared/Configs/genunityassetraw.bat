set WORKSPACE=..
set LUBAN_DLL=./Tools/Luban/Luban.dll
set CONF_ROOT=.

dotnet %LUBAN_DLL% ^
    -t all ^
    -d json ^
    -c cs-simple-json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputDataDir=../../Client/Assets/AssetRaws/Configs^
    -x outputCodeDir=../../Client/Assets/Scripts/GameConf/Gen
pause