#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release", $Framework = "net8.0", $AssemblySemVer = "0.0.0", $AssemblySemFileVer = "0.0.0.0", $InformationalVersion = "0.0.0-local")

. $PSScriptRoot\Common.ps1

Build-Project $Configuration Ares UniversalGL $Framework $AssemblySemVer $AssemblySemFileVer $InformationalVersion
if ($IsWindows) {
  @('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
    Build-Project $Configuration Ares $_ $Framework'-windows' $AssemblySemVer $AssemblySemFileVer $InformationalVersion
  }
}