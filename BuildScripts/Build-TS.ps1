#!/usr/bin/env pwsh
#Requires -Version 7.2

.\Build-TS-net6.0.ps1

If ($IsWindows)
{
    .\Build-TS-net48.ps1
}