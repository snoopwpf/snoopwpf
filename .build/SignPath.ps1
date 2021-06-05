[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$true)]
    [string]$CIUserToken,
    [Parameter(Position=1,Mandatory=$true)]
    [string]$OrganizationId,
    [Parameter(Position=2,Mandatory=$true)]
    [string]$ProjectSlug,
    [Parameter(Position=3,Mandatory=$true)]
    [string]$SigningPolicySlug
)

Install-Module -Name SignPath -Scope CurrentUser -Force

#-InputArtifactPath "artifacts" `
Submit-SigningRequest `
  -CIUserToken $CIUserToken `
  -OrganizationId $OrganizationId `
  -ProjectSlug $ProjectSlug `
  -SigningPolicySlug $SigningPolicySlug #`
  #-OutputArtifactPath "artifacts/signed" `
  #-WaitForCompletion