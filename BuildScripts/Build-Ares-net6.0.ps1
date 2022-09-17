#!/usr/bin/env pwsh
#Requires -Version 7.2

dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=Ares -p:ENGINE=UniversalGL -f net6.0 -o ..\Compiled\Ares\net6.0\any\Resources\Binaries\OpenGL

If ($IsWindows)
{
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=Ares -p:ENGINE=WindowsXNA -f net6.0-windows10.0.22000.0 -a x86 -o ..\Compiled\Ares\net6.0-windows10.0.22000.0\Resources\Binaries\XNA
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=Ares -p:ENGINE=WindowsDX -f net6.0-windows10.0.22000.0 -o ..\Compiled\Ares\net6.0-windows10.0.22000.0\Resources\Binaries\Windows
    dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:GAME=Ares -p:ENGINE=WindowsGL -f net6.0-windows10.0.22000.0 -o ..\Compiled\Ares\net6.0-windows10.0.22000.0\Resources\Binaries\OpenGL
}