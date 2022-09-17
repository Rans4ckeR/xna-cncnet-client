#!/usr/bin/env pwsh
#Requires -Version 7.2

.\Build-Ares-net6.0.ps1

If ($IsWindows)
{
    .\Build-Ares-net48.ps1
}