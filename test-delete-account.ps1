$ErrorActionPreference = "Stop"

$baseUrl = "http://localhost:5236/api"

# 1. Generate unique user details
$timestamp = (Get-Date).ToString("yyyyMMddHHmmss")
$email = "test.delete.$timestamp@example.com"
$password = "Test@12345"

Write-Host "============================="
Write-Host "TEST DELETE ACCOUNT FLOW"
Write-Host "============================="
Write-Host "Email: $email"

# 2. Register user
Write-Host "`n[1] Registering new user..."
$registerBody = @{
    Email = $email
    Password = $password
    FullName = "Test Delete User"
    PreferredLanguage = "vi"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    $token = $registerResponse.data.accessToken
    Write-Host "-> User registered successfully. Token length: $($token.Length)"
} catch {
    Write-Host "-> Failed to register: $_"
    throw
}

# 3. Request Delete Account
Write-Host "`n[2] Deleting account..."
$deleteBody = @{
    Password = $password
    ConfirmationPhrase = "XOA_TAI_KHOAN"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/auth/delete-account" -Method Post -Headers $headers -Body $deleteBody -ContentType "application/json"
    Write-Host "-> Account deleted successfully. Response message: $($deleteResponse.message)"
} catch {
    Write-Host "-> Failed to delete account: $_"
    $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
    $errorJson = $streamReader.ReadToEnd()
    Write-Host "Error Details:`n $errorJson"
    throw
}

# 4. Verify login fails (account is deleted)
Write-Host "`n[3] Verifying login fails with deleted account..."
$loginBody = @{
    EmailOrPhone = $email
    Password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "-> Warning: Login succeeded! Account was NOT deleted properly."
    exit 1
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> Login failed as expected (Status Code: $statusCode)."
    
    $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
    $errorJson = $streamReader.ReadToEnd()
    Write-Host "-> Error Message:`n $errorJson"
    
    Write-Host "`n============================="
    Write-Host "TEST PASSED"
    Write-Host "============================="
}
