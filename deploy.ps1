# Builds PS3TrophiesIsPerfect and deploys a tidy run folder:
#   F:\PS3TrophiesIsPerfect\
#     PS3TrophiesIsPerfect.exe (+ .exe.config)
#     pfdtool\         decrypt/encrypt (relative path, must stay at root)
#     flaresolverr\    Cloudflare proxy for the PSNProfiles scrape (auto-started by the app)
#     profiles\        optional PSN profile .sfo files for signing on save
#     lib\             all dependency assemblies (found via <probing privatePath="lib">)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'PS3TrophiesIsPerfect\PS3TrophiesIsPerfect.csproj'
$out  = Join-Path $root 'PS3TrophiesIsPerfect\bin\Debug\net48'
$dst  = 'F:\PS3TrophiesIsPerfect'
$old  = 'F:\PS3TrophyIsGood'   # source for flaresolverr/profiles

Get-Process PS3TrophiesIsPerfect -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build $proj -c Debug -v minimal

New-Item -ItemType Directory -Force -Path $dst | Out-Null

# Replace only the app payload; preserve flaresolverr\ and profiles\ (big / user data).
Remove-Item (Join-Path $dst 'lib') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $dst 'pfdtool') -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem $dst -File -ErrorAction SilentlyContinue | Remove-Item -Force
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

# One-time: bring over flaresolverr\ and profiles\ if not already deployed.
if (-not (Test-Path (Join-Path $dst 'flaresolverr')) -and (Test-Path (Join-Path $old 'flaresolverr'))) {
    Copy-Item (Join-Path $old 'flaresolverr') $dst -Recurse
}
if (-not (Test-Path (Join-Path $dst 'profiles'))) {
    if (Test-Path (Join-Path $old 'profiles')) { Copy-Item (Join-Path $old 'profiles') $dst -Recurse }
    else { New-Item -ItemType Directory -Path (Join-Path $dst 'profiles') | Out-Null }
}

Write-Host "Deployed organised run folder to $dst"
