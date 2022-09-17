#!/usr/bin/env pwsh
#Requires -Version 7.2

.\Build-YR-net6.0.ps1

If ($IsWindows)
{
    .\Build-YR-net48.ps1
}