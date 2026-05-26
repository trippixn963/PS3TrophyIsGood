# Builds PS3TrophiesIsPerfect and deploys a tidy run folder:
#   F:\PS3TrophiesIsPerfect\
#     PS3TrophiesIsPerfect.exe        (+ .exe.config)
#     pfdtool\                        (invoked by relative path, must stay at root)
#     lib\                            (all dependency assemblies; found via <probing privatePath="lib">)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'PS3TrophiesIsPerfect\PS3TrophiesIsPerfect.csproj'
$out  = Join-Path $root 'PS3TrophiesIsPerfect\bin\Debug\net48'
$dst  = 'F:\PS3TrophiesIsPerfect'

Get-Process PS3TrophiesIsPerfect -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build $proj -c Debug -v minimal

if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
New-Item -ItemType Directory -Path $dst | Out-Null
New-Item -ItemType Directory -Path (Join-Path $dst 'lib') | Out-Null

Copy-Item (Join-Path $out 'PS3TrophiesIsPerfect.exe') $dst
Copy-Item (Join-Path $out 'PS3TrophiesIsPerfect.exe.config') $dst
Copy-Item (Join-Path $out 'pfdtool') $dst -Recurse

$keepAtRoot = @('PS3TrophiesIsPerfect.exe', 'PS3TrophiesIsPerfect.exe.config')
Get-ChildItem $out -File | Where-Object {
    $keepAtRoot -notcontains $_.Name -and
    $_.Extension -ne '.pdb' -and $_.Extension -ne '.config' -and
    $_.Name -notlike '*.deps.json' -and $_.Name -notlike '*.runtimeconfig.json'
} | ForEach-Object { Copy-Item $_.FullName (Join-Path $dst 'lib') }

Write-Host "Deployed organised run folder to $dst"
