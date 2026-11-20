# Cria 3 tarefas no Agendador do Windows (08:00, 13:00 e 19:00) que rodam o projeto.
# Execute uma vez, em PowerShell. Para remover: Unregister-ScheduledTask -TaskName "LinkedinDicas-*" -Confirm:$false

$projeto = Split-Path -Parent $PSScriptRoot
$horarios = @("08:00", "13:00", "19:00")

foreach ($h in $horarios) {
    $nome = "LinkedinDicas-$($h.Replace(':', 'h'))"
    $acao = New-ScheduledTaskAction -Execute "dotnet" -Argument "run --project `"$projeto\LinkedinDicas.csproj`" --no-launch-profile" -WorkingDirectory $projeto
    $gatilho = New-ScheduledTaskTrigger -Daily -At $h
    $config = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 30)
    Register-ScheduledTask -TaskName $nome -Action $acao -Trigger $gatilho -Settings $config -Description "Gera rascunhos de posts IA x C# ($h)" -Force | Out-Null
    Write-Host "Tarefa criada: $nome"
}
