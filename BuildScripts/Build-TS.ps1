#!/usr/bin/env pwsh
#Requires -Version 7.2

param($Configuration = "Release", $Framework = "net7.0")

. $PSScriptRoot\Common.ps1

Build-Project $Configuration TS UniversalGL $Framework
if ($IsWindows) {
  @('WindowsDX', 'WindowsGL', 'WindowsXNA') | ForEach-Object {
    Build-Project $Configuration TS $_ $Framework'-windows'
  }
}