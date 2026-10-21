# Backup do banco do servidor (Neon) para backups\ (fora do git). Rodar no PowerShell do usuário:
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\backup-neon.ps1
# Pede a connection string no formato "postgresql://..." (painel do Neon → Connect → psql), sem mostrar
# o que é digitado e sem guardar no histórico.
$ErrorActionPreference = 'Stop'
$pgDump = 'C:\Users\pedro\Suporte em Tarefas\PostgreSQL\18\bin\pg_dump.exe'
$pasta = Join-Path $PSScriptRoot '..\backups'
New-Item -ItemType Directory -Force $pasta | Out-Null
$destino = Join-Path $pasta ("neon-{0}.dump" -f (Get-Date -Format 'yyyy-MM-dd-HHmm'))

$segura = Read-Host 'Cole a connection string do Neon (postgresql://...)' -AsSecureString
$ponteiro = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
try {
    $conexao = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ponteiro)
    & $pgDump --dbname=$conexao -Fc -f $destino
    if ($LASTEXITCODE -ne 0) { throw "pg_dump falhou (código $LASTEXITCODE)" }
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ponteiro)
    Remove-Variable conexao -ErrorAction SilentlyContinue
}
Write-Output ("Backup salvo: {0} ({1:N0} bytes)" -f $destino, (Get-Item $destino).Length)
