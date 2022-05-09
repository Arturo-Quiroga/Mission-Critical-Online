param
(
  # Load Test Id
  [Parameter(Mandatory=$true)]
  [string] $loadTestId,
  
  # Load Test data plane endpoint
  [Parameter(Mandatory=$true)]
  [string] $apiEndpoint,

  # Load Test data plane api version
  [Parameter(Mandatory=$true)]
  [string] $apiVersion
)

if (!$loadTestId) {
  throw "ERROR - Parameter loadTestId is required and cannot be empty."
}

. ./common.ps1

$urlRoot = "https://" + $apiEndpoint + "/loadtests/" + "$loadTestId"

az rest --url $urlRoot `
  --method DELETE `
  --skip-authorization-header `
  --headers "$accessTokenHeader" `
  --url-parameters testId="$loadTestId" api-version="$apiVersion" `
  $verbose