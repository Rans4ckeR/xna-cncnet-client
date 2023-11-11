# if ($___COMMON__H___) { return }
# $___COMMON__H___ = $true

$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot DXMainClient DXMainClient.csproj
$CompiledRoot = Join-Path $RepoRoot Compiled
$EngineMap = @{
  'UniversalGL' = 'UniversalGL'
  'WindowsDX'   = 'Windows'
  'WindowsGL'   = 'OpenGL'
  'WindowsXNA'  = 'XNA'
}

function Build-Project($Configuration, $Game, $Engine, $Framework) {
  $Output = Join-Path $CompiledRoot $Game ($Framework -Split "-")[0] $Output Resources Binaries ($EngineMap[$Engine])
  if ($Engine -EQ 'WindowsXNA') {
    dotnet publish $ProjectPath -c $Configuration -p:GAME=$Game -p:ENGINE=$Engine -f $Framework -o $Output -p:SatelliteResourceLanguages=en -p:AssemblyVersion=$env:GitVersion_AssemblySemVer -p:FileVersion=$env:GitVersion_AssemblySemFileVer -p:InformationalVersion=$env:GitVersion_InformationalVersion -a x86
  }
  else {
    dotnet publish $ProjectPath -c $Configuration -p:GAME=$Game -p:ENGINE=$Engine -f $Framework -o $Output -p:SatelliteResourceLanguages=en -p:AssemblyVersion=$env:GitVersion_AssemblySemVer -p:FileVersion=$env:GitVersion_AssemblySemFileVer -p:InformationalVersion=$env:GitVersion_InformationalVersion
  }
  if ($LASTEXITCODE) {
    throw "Build failed for $Game $Engine $Framework $Configuration"
  }
}