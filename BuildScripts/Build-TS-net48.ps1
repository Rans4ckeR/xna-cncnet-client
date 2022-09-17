#!/usr/bin/env pwsh
#Requires -Version 7.2

dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=TS -p:ENGINE=WindowsXNA -f net48 -a x86 -o ..\Compiled\TS\net48\Resources\Binaries\XNA
dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=TS -p:ENGINE=WindowsDX -f net48 -o ..\Compiled\TS\net48\Resources\Binaries\Windows
dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=TS -p:ENGINE=WindowsGL -f net48 -o ..\Compiled\TS\net48\Resources\Binaries\OpenGL