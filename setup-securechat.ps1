$base = "tests/SecureChat.Tests.Integration"

# =========================
# FOLDERS
# =========================

$folders = @(
"Api",
"SignalR"
)

foreach ($f in $folders)
{
    New-Item -ItemType Directory -Force -Path "$base/$f" | Out-Null
}

# =========================
# API TESTS
# =========================

$apiTests = @(
"AuthApiTests.cs",
"ChatApiTests.cs",
"GroupApiTests.cs"
)

foreach ($file in $apiTests)
{
    New-Item -ItemType File -Force -Path "$base/Api/$file" | Out-Null
}

# =========================
# SIGNALR TESTS
# =========================

New-Item -ItemType File -Force -Path "$base/SignalR/ChatHubTests.cs" | Out-Null

Write-Host "DONE: SecureChat.Tests.Integration structure created"