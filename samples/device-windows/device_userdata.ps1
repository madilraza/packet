#ps1_sysnative

Write-Host "Configure PowerShell"
Set-ExecutionPolicy RemoteSigned -Force
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "Install Chocolatey"
$env:chocolateyVersion = '0.10.15'
Set-ExecutionPolicy Bypass -Scope Process -Force; Invoke-WebRequest https://chocolatey.org/install.ps1 -UseBasicParsing | Invoke-Expression

Write-Host "Install OpenSSH"
netsh advfirewall firewall add rule name="OpenSSH-Install" dir=in localport=22 protocol=TCP action=block
choco install openssh -y --version 8.0.0.1 -params '"/SSHServerFeature"'
net stop sshd
netsh advfirewall firewall delete rule name="OpenSSH-Install"

Write-Host "Configure OpenSSH"
$sshd_config = "$($env:ProgramData)\ssh\sshd_config"
(Get-Content $sshd_config).Replace("Match Group administrators", "# Match Group administrators") | Set-Content $sshd_config
(Get-Content $sshd_config).Replace("AuthorizedKeysFile", "# AuthorizedKeysFile") | Set-Content $sshd_config
net start sshd

Write-Host "Configure Packet"
mkdir -Force C:/Users/Admin/.ssh
Invoke-WebRequest https://metadata.packet.net/2009-04-04/meta-data/public-keys -OutFile C:/Users/Admin/.ssh/authorized_keys
