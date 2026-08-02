[CmdletBinding()]
param(
    [string]$Url    # Pré-remplit le champ URL au démarrage (optionnel)
)

cmd /c chcp 65001 > $null
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ===============================
# CONFIGURATION GLOBALE
# ===============================
$Config = @{
    FFmpeg                 = "$PSScriptRoot\bin\ffmpeg.exe"
    YtDlp                  = "$PSScriptRoot\bin\yt-dlp.exe"
    CaptureDir             = "E:\Streamlink\videos"
    LogDir                 = "E:\Streamlink\logs"
    FavoritesFile          = "$PSScriptRoot\favorites.json"
    ThumbnailOffsetSeconds = 10
    DonateUrl              = "https://paypal.me/tomoushie"
    DonateQrPath           = "$PSScriptRoot\donate_qr.png"
}

$blacklist = @("example.com", "badhost.org")
$whitelist = @("chaturbate.com")

$YtDlpPath                     = $Config.YtDlp
$YtDlpExpectedSha256           = ""    # <-- A REMPLIR
$YtDlpRequireAuthenticode      = $false
$YtDlpExpectedSignerThumbprint = ""
$YtDlpExpectedSignerSubject    = ""

$FfmpegPath                     = $Config.FFmpeg
$FfmpegExpectedSha256           = ""   # <-- A REMPLIR
$FfmpegRequireAuthenticode      = $false
$FfmpegExpectedSignerThumbprint = ""
$FfmpegExpectedSignerSubject    = ""

# ==== PINNING CA LOCAL (binaires) — désactivé par défaut ====
# Si activé, yt-dlp.exe ET ffmpeg.exe doivent tous les deux correspondre au
# thumbprint/issuer ci-dessous, sinon le bouton Démarrer refusera de lancer.
$EnableCaPinning     = $false
$TrustedCaThumbprint = ""   # ex: "A1B2C3..."
$TrustedCaIssuer     = ""   # ex: "CN=Ma CA Racine, O=MonOrg"

# ==== PINNING TLS DU SERVEUR DISTANT — désactivé par défaut ====
# Si activé, vérifie le certificat présenté par le serveur AVANT de démarrer
# le job de téléchargement (en plus de la validation TLS native de yt-dlp).
$EnableTlsServerPinning   = $false
$ServerExpectedThumbprint = ""
$ServerExpectedIssuer     = ""

# Etat global
$script:CurrentJob        = $null
$script:LogFilePath       = $null
$script:LastLineIndex     = 0
$script:CurrentRoomName   = $null
$script:CurrentCaptureDir = $Config.CaptureDir
$script:Timer             = $null
$script:Favorites         = @()
$script:CurrentTheme      = "Clair"

$script:SessionLogFile = Join-Path $Config.LogDir "session-$(Get-Date -Format 'yyyy-MM-dd').log"

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARN", "ERROR")]
        [string]$Level = "INFO"
    )
    $ts   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    try {
        if (!(Test-Path (Split-Path $script:SessionLogFile -Parent))) {
            New-Item -ItemType Directory -Path (Split-Path $script:SessionLogFile -Parent) -Force | Out-Null
        }
        Add-Content -Path $script:SessionLogFile -Value $line -Encoding UTF8
    } catch { }
}

# =========================================================
# FONCTIONS DE SECURITE (binaires / URL / chemins)
# =========================================================

function Test-FileHash {
    param ([string]$FilePath, [string]$ExpectedHash, [string]$Algorithm = "SHA256")
    if (-not $ExpectedHash) {
        Write-Log "Aucun hash attendu fourni pour '$FilePath' : vérification refusée par sécurité." "ERROR"
        return $false
    }
    try {
        $actualHash = (Get-FileHash -Path $FilePath -Algorithm $Algorithm).Hash
        return ($actualHash.ToUpperInvariant() -eq $ExpectedHash.ToUpperInvariant())
    } catch {
        Write-Log "Erreur lors du calcul du hash du fichier : $_" "ERROR"
        return $false
    }
}

function Test-AuthenticodeSignature2 {
    param ($FilePath)
    try {
        $certInfo = Get-AuthenticodeSignature -FilePath $FilePath
        if ($certInfo.SignatureStatus -eq 'Valid') { return $true }
    } catch {
        Write-Log "Erreur lors de la vérification du certificat : $_" "ERROR"
    }
    return $false
}

function Test-CACertificate {
    param (
        [string]$FilePath,
        [string]$ExpectedThumbprint,
        [string]$ExpectedIssuer
    )

    $chain = $null
    $cert  = $null

    try {
        if (-not (Test-AuthenticodeSignature2 -FilePath $FilePath)) {
            Write-Log "Signature Authenticode invalide ou absente pour $FilePath." "ERROR"
            return $false
        }

        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($FilePath)

        if ($ExpectedThumbprint) {
            $actualThumbprint = $cert.Thumbprint.ToUpperInvariant().Replace(" ", "")
            $expected = $ExpectedThumbprint.ToUpperInvariant().Replace(" ", "")
            if ($actualThumbprint -ne $expected) {
                Write-Log "Thumbprint CA invalide pour $FilePath. Attendu : $expected / Obtenu : $actualThumbprint" "ERROR"
                return $false
            }
        } else {
            Write-Log "Aucun thumbprint CA attendu fourni pour $FilePath : vérification refusée par sécurité." "ERROR"
            return $false
        }

        if ($ExpectedIssuer -and $cert.Issuer -ne $ExpectedIssuer) {
            Write-Log "Émetteur CA inattendu pour $FilePath. Attendu : $ExpectedIssuer / Obtenu : $($cert.Issuer)" "ERROR"
            return $false
        }

        $now = Get-Date
        if ($now -lt $cert.NotBefore -or $now -gt $cert.NotAfter) {
            Write-Log "Certificat CA hors période de validité pour $FilePath." "ERROR"
            return $false
        }

        $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
        $chain.ChainPolicy.RevocationMode      = [System.Security.Cryptography.X509Certificates.X509RevocationMode]::Online
        $chain.ChainPolicy.RevocationFlag      = [System.Security.Cryptography.X509Certificates.X509RevocationFlag]::EntireChain
        $chain.ChainPolicy.VerificationFlags   = [System.Security.Cryptography.X509Certificates.X509VerificationFlags]::NoFlag
        $chain.ChainPolicy.UrlRetrievalTimeout = New-TimeSpan -Seconds 15

        if (-not $chain.Build($cert)) {
            $errors = $chain.ChainStatus | ForEach-Object { "$($_.Status): $($_.StatusInformation)" }
            Write-Log "Échec de construction de la chaîne CA pour $FilePath :`n$($errors -join "`n")" "ERROR"
            return $false
        }

        foreach ($status in $chain.ChainStatus) {
            if ($status.Status -in @('Revoked','RevocationStatusUnknown','OfflineRevocation','UntrustedRoot','PartialChain','NotTimeValid','NotSignatureValid')) {
                Write-Log "Statut de chaîne CA problématique pour $FilePath : $($status.Status)" "ERROR"
                return $false
            }
        }

        return $true

    } catch {
        Write-Log "Erreur lors de la vérification du certificat CA pour $FilePath : $_" "ERROR"
        return $false
    } finally {
        if ($chain) { $chain.Dispose() }
        if ($cert)  { $cert.Dispose() }
    }
}

function Test-CertificateSAN {
    param (
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [string]$ExpectedHostName
    )

    $sanExtension = $Certificate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.17' }
    if (-not $sanExtension) {
        Write-Log "Aucune extension SAN (Subject Alternative Name) trouvée sur le certificat." "ERROR"
        return $false
    }

    # Format(0) renvoie une chaîne du type "DNS Name=chaturbate.com, DNS Name=*.chaturbate.com"
    $sanText = $sanExtension.Format($false)
    $sanEntries = $sanText -split '(,|;)' | Where-Object { $_ -match 'DNS Name=' } | ForEach-Object {
        ($_ -replace '.*DNS Name=', '').Trim()
    }

    if (-not $sanEntries -or $sanEntries.Count -eq 0) {
        Write-Log "Extension SAN présente mais aucune entrée DNS Name exploitable." "ERROR"
        return $false
    }

    foreach ($entry in $sanEntries) {
        if ($entry -eq $ExpectedHostName) {
            return $true
        }
        if ($entry.StartsWith('*.')) {
            $suffix = $entry.Substring(1)  # ".exemple.com"
            if ($ExpectedHostName.EndsWith($suffix) -and ($ExpectedHostName.Length -gt $suffix.Length)) {
                return $true
            }
        }
    }

    Write-Log "Le nom d'hôte '$ExpectedHostName' ne correspond à aucune entrée SAN ($($sanEntries -join ', '))." "ERROR"
    return $false
}

function Test-RemoteCertificate {
    param (
        [string]$HostName,
        [int]$Port = 443,
        [string]$ExpectedThumbprint,
        [string]$ExpectedIssuer
    )

    $tcpClient = $null
    $sslStream = $null
    $script:capturedChainOk = $true
    $script:capturedErrors  = @()

    $validationCallback = {
        param($sender, $certificate, $chain, $sslPolicyErrors)

        if ($sslPolicyErrors -ne [System.Net.Security.SslPolicyErrors]::None) {
            $script:capturedErrors += "Erreurs de politique SSL : $sslPolicyErrors"
            $script:capturedChainOk = $false
        }

        $verifyChain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
        $verifyChain.ChainPolicy.RevocationMode      = [System.Security.Cryptography.X509Certificates.X509RevocationMode]::Online
        $verifyChain.ChainPolicy.RevocationFlag      = [System.Security.Cryptography.X509Certificates.X509RevocationFlag]::EntireChain
        $verifyChain.ChainPolicy.UrlRetrievalTimeout = New-TimeSpan -Seconds 15

        $cert2 = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certificate)
        $built = $verifyChain.Build($cert2)

        if (-not $built) {
            foreach ($status in $verifyChain.ChainStatus) {
                $script:capturedErrors += "$($status.Status): $($status.StatusInformation)"
            }
            $script:capturedChainOk = $false
        }

        return $true
    }

    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient($HostName, $Port)
        $sslStream = New-Object System.Net.Security.SslStream(
            $tcpClient.GetStream(), $false, $validationCallback
        )
        $sslStream.AuthenticateAsClient($HostName)

        $remoteCert2 = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($sslStream.RemoteCertificate)

        if (-not $script:capturedChainOk) {
            Write-Log "Chaîne/révocation du certificat serveur invalide pour ${HostName}:`n$($script:capturedErrors -join "`n")" "ERROR"
            return $false
        }

        $now = Get-Date
        if ($now -lt $remoteCert2.NotBefore -or $now -gt $remoteCert2.NotAfter) {
            Write-Log "Certificat serveur hors période de validité pour $HostName." "ERROR"
            return $false
        }

        if ($ExpectedThumbprint) {
            $actual   = $remoteCert2.Thumbprint.ToUpperInvariant().Replace(" ", "")
            $expected = $ExpectedThumbprint.ToUpperInvariant().Replace(" ", "")
            if ($actual -ne $expected) {
                Write-Log "Thumbprint serveur inattendu pour $HostName. Attendu : $expected / Obtenu : $actual" "ERROR"
                return $false
            }
        }

        if ($ExpectedIssuer -and $remoteCert2.Issuer -ne $ExpectedIssuer) {
            Write-Log "Émetteur serveur inattendu pour $HostName. Attendu : $ExpectedIssuer / Obtenu : $($remoteCert2.Issuer)" "ERROR"
            return $false
        }

        # Vérification explicite du SAN, en plus de la validation de nom déjà effectuée
        # implicitement par SslStream.AuthenticateAsClient() via sslPolicyErrors.
        if (-not (Test-CertificateSAN -Certificate $remoteCert2 -ExpectedHostName $HostName)) {
            return $false
        }

        return $true

    } catch {
        Write-Log "Erreur lors de la vérification TLS de $HostName : $_" "ERROR"
        return $false
    } finally {
        if ($sslStream) { $sslStream.Dispose() }
        if ($tcpClient) { $tcpClient.Dispose() }
    }
}

function Test-TrustedBinary {
    param (
        [string]$FilePath, [string]$ExpectedSha256, [bool]$RequireAuthenticode = $true,
        [string]$ExpectedSignerThumbprint, [string]$ExpectedSignerSubject
    )
    if (-not (Test-Path -Path $FilePath)) {
        Write-Log "Binaire introuvable : $FilePath" "ERROR"
        return $false
    }
    if (-not (Test-FileHash -FilePath $FilePath -ExpectedHash $ExpectedSha256 -Algorithm 'SHA256')) {
        Write-Log "Hachage SHA256 invalide pour $FilePath" "ERROR"
        return $false
    }
    if (-not $RequireAuthenticode) { return $true }

    $chain = $null
    try {
        $sig = Get-AuthenticodeSignature -FilePath $FilePath
        if ($sig.SignatureStatus -ne 'Valid') {
            Write-Log "Signature Authenticode invalide pour $FilePath : $($sig.SignatureStatus)" "ERROR"
            return $false
        }
        $signerCert = $sig.SignerCertificate
        if (-not $signerCert) {
            Write-Log "Aucun certificat signataire trouvé pour $FilePath" "ERROR"
            return $false
        }
        $now = Get-Date
        if (-not $sig.TimeStamperCertificate -and ($now -lt $signerCert.NotBefore -or $now -gt $signerCert.NotAfter)) {
            Write-Log "Certificat signataire hors validité et aucun horodatage de confiance." "ERROR"
            return $false
        }
        $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
        $chain.ChainPolicy.RevocationMode      = [System.Security.Cryptography.X509Certificates.X509RevocationMode]::Online
        $chain.ChainPolicy.RevocationFlag      = [System.Security.Cryptography.X509Certificates.X509RevocationFlag]::EntireChain
        $chain.ChainPolicy.UrlRetrievalTimeout = New-TimeSpan -Seconds 15
        if (-not $chain.Build($signerCert)) {
            Write-Log "Échec de la chaîne de certification pour $FilePath" "ERROR"
            return $false
        }
        foreach ($status in $chain.ChainStatus) {
            if ($status.Status -in @('Revoked','RevocationStatusUnknown','OfflineRevocation','UntrustedRoot','PartialChain','NotTimeValid','NotSignatureValid')) {
                Write-Log "Statut de chaîne problématique pour $FilePath : $($status.Status)" "ERROR"
                return $false
            }
        }
        if ($ExpectedSignerThumbprint) {
            $actual = $signerCert.Thumbprint.ToUpperInvariant().Replace(" ", "")
            if ($actual -ne $ExpectedSignerThumbprint.ToUpperInvariant().Replace(" ", "")) {
                Write-Log "Thumbprint du signataire inattendu pour $FilePath" "ERROR"
                return $false
            }
        }
        if ($ExpectedSignerSubject -and $signerCert.Subject -ne $ExpectedSignerSubject) {
            Write-Log "Sujet du signataire inattendu pour $FilePath" "ERROR"
            return $false
        }
        return $true
    } catch {
        Write-Log "Erreur lors de la vérification du binaire $FilePath : $_" "ERROR"
        return $false
    } finally {
        if ($chain) { $chain.Dispose() }
    }
}

function Test-YtDlpBinary {
    return Test-TrustedBinary -FilePath $YtDlpPath -ExpectedSha256 $YtDlpExpectedSha256 `
        -RequireAuthenticode $YtDlpRequireAuthenticode `
        -ExpectedSignerThumbprint $YtDlpExpectedSignerThumbprint `
        -ExpectedSignerSubject $YtDlpExpectedSignerSubject
}

function Test-FfmpegBinary {
    return Test-TrustedBinary -FilePath $FfmpegPath -ExpectedSha256 $FfmpegExpectedSha256 `
        -RequireAuthenticode $FfmpegRequireAuthenticode `
        -ExpectedSignerThumbprint $FfmpegExpectedSignerThumbprint `
        -ExpectedSignerSubject $FfmpegExpectedSignerSubject
}

function Test-ValidDomainFormat {
    param ($domain)
    if ([string]::IsNullOrWhiteSpace($domain)) { return $false }
    $normalized = $domain.TrimEnd('.')
    if ($normalized.Length -eq 0 -or $normalized.Length -gt 253) { return $false }
    $labelPattern = '^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$'
    foreach ($label in $normalized.Split('.')) {
        if (-not ([System.Text.RegularExpressions.Regex]::IsMatch($label, $labelPattern))) { return $false }
    }
    return $true
}

function Test-DomainAgainstWhitelist {
    param ($domain, [string[]]$whitelist)
    if (-not (Test-ValidDomainFormat -domain $domain)) { return $false }
    $normalizedDomain = $domain.TrimEnd('.').ToLowerInvariant()
    foreach ($blocked in $blacklist) {
        $blockedNormalized = $blocked.TrimEnd('.').ToLowerInvariant()
        if ($normalizedDomain -eq $blockedNormalized -or $normalizedDomain.EndsWith(".$blockedNormalized")) { return $false }
    }
    if ($whitelist -and $whitelist.Length -gt 0) {
        $allowed = $false
        foreach ($allowedDomain in $whitelist) {
            $allowedNormalized = $allowedDomain.TrimEnd('.').ToLowerInvariant()
            if ($normalizedDomain -eq $allowedNormalized -or $normalizedDomain.EndsWith(".$allowedNormalized")) {
                $allowed = $true; break
            }
        }
        if (-not $allowed) { return $false }
    }
    return $true
}

function Test-SafePathSegment {
    param ($pathSegment)
    if ([string]::IsNullOrEmpty($pathSegment)) { return $false }
    if ($pathSegment -in @('../', '..', './', '.')) { return $false }
    if ([System.Text.RegularExpressions.Regex]::IsMatch($pathSegment, '[%\?&=#\s"''<>\\;:@\x00]')) { return $false }
    if (-not ([System.Text.RegularExpressions.Regex]::IsMatch($pathSegment, "^[a-zA-Z0-9_\-\.]+$"))) { return $false }
    if ($pathSegment.Length -gt 255) { return $false }
    return $true
}

function Test-SafeQueryString {
    param ($QueryString)
    if ([string]::IsNullOrEmpty($QueryString)) { return $true }
    if ($QueryString.Length -gt 512) { return $false }
    # '%' n'est autorisé que comme début d'une séquence d'échappement valide %XX (2 chiffres hexa).
    # Tout autre caractère doit appartenir à l'ensemble autorisé.
    if (-not ([System.Text.RegularExpressions.Regex]::IsMatch($QueryString, '^([a-zA-Z0-9_\-\.=&]|%[0-9A-Fa-f]{2})+$'))) { return $false }
    return $true
}

function Test-SafeUrl {
    param ([string]$UrlToTest, [string[]]$AllowedDomains)
    if ([string]::IsNullOrWhiteSpace($UrlToTest)) { Write-Log "URL vide." "ERROR"; return $false }
    try { $uri = [System.Uri]$UrlToTest } catch { Write-Log "URL malformée : $_" "ERROR"; return $false }

    $forbiddenSchemes = @('javascript', 'file', 'ftp', 'data', 'blob')
    if ($uri.Scheme.ToLowerInvariant() -in $forbiddenSchemes) {
        Write-Log "Schéma interdit : $($uri.Scheme)://" "ERROR"; return $false
    }
    if ($uri.Scheme.ToLowerInvariant() -ne 'https') {
        Write-Log "Seul le schéma https:// est autorisé." "ERROR"; return $false
    }
    if (-not [string]::IsNullOrEmpty($uri.UserInfo)) {
        Write-Log "Identifiants intégrés dans l'URL interdits." "ERROR"; return $false
    }
    # Interdiction explicite des cibles locales/loopback (défense en profondeur : la whitelist de
    # domaine ci-dessous rejette déjà ces valeurs puisqu'elles ne correspondent pas à chaturbate.com,
    # mais un blocage explicite protège aussi si la whitelist venait à être élargie par erreur).
    if ($uri.Host -in @("localhost", "127.0.0.1", "::1")) {
        Write-Log "Hôte local/loopback interdit : $($uri.Host)" "ERROR"; return $false
    }
    if (-not (Test-DomainAgainstWhitelist -domain $uri.Host -whitelist $AllowedDomains)) {
        Write-Log "Domaine non autorisé : $($uri.Host)" "ERROR"; return $false
    }
    $segments = $uri.AbsolutePath.Trim('/').Split('/') | Where-Object { $_ -ne '' }
    foreach ($seg in $segments) {
        if (-not (Test-SafePathSegment -pathSegment $seg)) {
            Write-Log "Segment de chemin invalide : '$seg'" "ERROR"; return $false
        }
    }
    $query = $uri.Query.TrimStart('?')
    if (-not (Test-SafeQueryString -QueryString $query)) {
        Write-Log "Query string invalide : '$query'" "ERROR"; return $false
    }
    return $true
}

function Test-ValidPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [switch]$MustExist
    )
    if ([string]::IsNullOrWhiteSpace($Path)) { Write-Log "Chemin vide." "ERROR"; return $false }
    if ($Path -match '^\\\\[^\?\.]') { Write-Log "Chemin UNC interdit : $Path" "ERROR"; return $false }
    if ($Path -match '^\\\\\?\\' -or $Path -match '^\\\\\.\\') { Write-Log "Chemin étendu interdit : $Path" "ERROR"; return $false }
    if ($Path -match '\\Device\\') { Write-Log "Chemin \Device\ interdit : $Path" "ERROR"; return $false }
    $driveLessPath = $Path -replace '^[a-zA-Z]:', ''
    if ($driveLessPath.Contains(':')) { Write-Log "Flux alternatif NTFS (ADS) interdit : $Path" "ERROR"; return $false }
    if ($Path -match '[\x00-\x1F]') { Write-Log "Caractères de contrôle interdits : $Path" "ERROR"; return $false }
    if ($Path -notmatch '^[a-zA-Z]:\\') { Write-Log "Chemin non absolu local : $Path" "ERROR"; return $false }

    $reservedNames = @('CON','PRN','AUX','NUL',
        'COM1','COM2','COM3','COM4','COM5','COM6','COM7','COM8','COM9',
        'LPT1','LPT2','LPT3','LPT4','LPT5','LPT6','LPT7','LPT8','LPT9')
    $segments = $Path -split '[\\/]' | Where-Object { $_ -ne '' -and $_ -notmatch '^[a-zA-Z]:$' }
    foreach ($seg in $segments) {
        $baseName = $seg.Split('.')[0].ToUpperInvariant()
        if ($baseName -in $reservedNames) { Write-Log "Nom réservé interdit : '$seg'" "ERROR"; return $false }
    }

    if (Test-Path -LiteralPath $Path) {
        try {
            $item = Get-Item -LiteralPath $Path -Force
            if ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                Write-Log "Symlink / reparse point interdit : $Path" "ERROR"; return $false
            }
        } catch { Write-Log "Erreur vérification chemin '$Path' : $_" "ERROR"; return $false }
    } elseif ($MustExist) {
        Write-Log "Chemin introuvable : $Path" "ERROR"; return $false
    }

    $current = Split-Path $Path -Parent
    while ($current -and (Test-Path -LiteralPath $current -ErrorAction SilentlyContinue)) {
        try {
            $parentItem = Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
            if ($parentItem -and ($parentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                Write-Log "Dossier parent symlink / reparse point : $current" "ERROR"; return $false
            }
        } catch { }
        $newCurrent = Split-Path $current -Parent
        if (-not $newCurrent -or $newCurrent -eq $current) { break }
        $current = $newCurrent
    }
    return $true
}

# =========================================================
# FAVORIS (persistance JSON)
# =========================================================
function Save-Favorites {
    try {
        $script:Favorites | ConvertTo-Json | Set-Content -Path $Config.FavoritesFile -Encoding UTF8
    } catch {
        Write-Log "Erreur lors de la sauvegarde des favoris : $_" "ERROR"
    }
}

function Load-Favorites {
    $script:Favorites = @()
    if (-not (Test-Path $Config.FavoritesFile)) { return }
    try {
        $raw = Get-Content -Path $Config.FavoritesFile -Raw -Encoding UTF8
        if ([string]::IsNullOrWhiteSpace($raw)) { return }
        $items = $raw | ConvertFrom-Json
        foreach ($item in $items) {
            # Chaque favori est revalidé : un fichier favoris.json corrompu ou modifié
            # à la main ne doit pas réintroduire une URL dangereuse.
            if ($item -and (Test-SafeUrl -UrlToTest $item.ToString() -AllowedDomains $whitelist)) {
                $script:Favorites += $item.ToString()
            }
        }
    } catch {
        Write-Log "Fichier de favoris illisible, ignoré : $_" "WARN"
    }
}

# Correction du bug signalé : $url doit être casté en [System.Uri] avant d'utiliser .Host
function Add-FavoriteUrl {
    param([string]$UrlToAdd)

    if (-not (Test-SafeUrl -UrlToTest $UrlToAdd -AllowedDomains $whitelist)) {
        return $false
    }
    $uri = [System.Uri]$UrlToAdd   # cast explicite -> plus de crash sur .Host
    Write-Log "Ajout favori pour l'hôte : $($uri.Host)"

    if ($script:Favorites -contains $UrlToAdd) {
        return $false
    }
    $script:Favorites += $UrlToAdd
    Save-Favorites
    return $true
}

function Remove-FavoriteUrl {
    param([string]$UrlToRemove)
    $script:Favorites = @($script:Favorites | Where-Object { $_ -ne $UrlToRemove })
    Save-Favorites
}

# =========================================================
# PREPARATION DOSSIERS
# =========================================================
foreach ($dir in @($Config.CaptureDir, $Config.LogDir)) {
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}
if (-not (Test-ValidPath -Path $Config.CaptureDir)) {
    [System.Windows.Forms.MessageBox]::Show("Dossier de capture invalide : $($Config.CaptureDir)", "Erreur", "OK", "Error") | Out-Null
    exit 1
}
if (-not (Test-ValidPath -Path $Config.LogDir)) {
    [System.Windows.Forms.MessageBox]::Show("Dossier de logs invalide : $($Config.LogDir)", "Erreur", "OK", "Error") | Out-Null
    exit 1
}

Load-Favorites

# =========================================================
# INTERFACE GRAPHIQUE
# =========================================================
$form                 = New-Object System.Windows.Forms.Form
$form.Text            = "Chaturbate Recorder"
$form.Size            = New-Object System.Drawing.Size(700, 850)
$form.StartPosition   = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox     = $false

# --- Sélecteur de thème ---
$themeLabel            = New-Object System.Windows.Forms.Label
$themeLabel.Text       = "Thème :"
$themeLabel.Location   = New-Object System.Drawing.Point(12, 12)
$themeLabel.AutoSize   = $true

$themeCombo                 = New-Object System.Windows.Forms.ComboBox
$themeCombo.Location        = New-Object System.Drawing.Point(70, 9)
$themeCombo.Size            = New-Object System.Drawing.Size(120, 24)
$themeCombo.DropDownStyle   = "DropDownList"
[void]$themeCombo.Items.Add("Clair")
[void]$themeCombo.Items.Add("Sombre")
$themeCombo.SelectedItem    = "Clair"

# --- GroupBox : Enregistrement ---
$grpRecord             = New-Object System.Windows.Forms.GroupBox
$grpRecord.Text        = "Enregistrement"
$grpRecord.Location    = New-Object System.Drawing.Point(12, 40)
$grpRecord.Size        = New-Object System.Drawing.Size(660, 110)

$urlLabel            = New-Object System.Windows.Forms.Label
$urlLabel.Text       = "URL Chaturbate :"
$urlLabel.Location   = New-Object System.Drawing.Point(12, 25)
$urlLabel.AutoSize   = $true

$urlTextBox          = New-Object System.Windows.Forms.TextBox
$urlTextBox.Location = New-Object System.Drawing.Point(12, 48)
$urlTextBox.Size     = New-Object System.Drawing.Size(420, 24)
if ($Url) { $urlTextBox.Text = $Url }

$startButton          = New-Object System.Windows.Forms.Button
$startButton.Text     = "Démarrer"
$startButton.Location = New-Object System.Drawing.Point(445, 46)
$startButton.Size     = New-Object System.Drawing.Size(95, 28)

$stopButton           = New-Object System.Windows.Forms.Button
$stopButton.Text      = "STOP"
$stopButton.Location  = New-Object System.Drawing.Point(548, 46)
$stopButton.Size      = New-Object System.Drawing.Size(95, 28)
$stopButton.Enabled   = $false

$addFavoriteButton          = New-Object System.Windows.Forms.Button
$addFavoriteButton.Text     = "+ Favori"
$addFavoriteButton.Location = New-Object System.Drawing.Point(445, 78)
$addFavoriteButton.Size     = New-Object System.Drawing.Size(198, 24)

$grpRecord.Controls.AddRange(@($urlLabel, $urlTextBox, $startButton, $stopButton, $addFavoriteButton))

# --- GroupBox : Progression ---
$grpProgress          = New-Object System.Windows.Forms.GroupBox
$grpProgress.Text     = "Progression"
$grpProgress.Location = New-Object System.Drawing.Point(12, 158)
$grpProgress.Size     = New-Object System.Drawing.Size(660, 70)

$progressBar          = New-Object System.Windows.Forms.ProgressBar
$progressBar.Location = New-Object System.Drawing.Point(12, 22)
$progressBar.Size     = New-Object System.Drawing.Size(636, 22)
$progressBar.Minimum  = 0
$progressBar.Maximum  = 100

$statusLabel          = New-Object System.Windows.Forms.Label
$statusLabel.Text     = "Prêt."
$statusLabel.Location = New-Object System.Drawing.Point(12, 46)
$statusLabel.AutoSize = $true

$grpProgress.Controls.AddRange(@($progressBar, $statusLabel))

# --- GroupBox : Favoris ---
$grpFavorites          = New-Object System.Windows.Forms.GroupBox
$grpFavorites.Text     = "Favoris"
$grpFavorites.Location = New-Object System.Drawing.Point(12, 236)
$grpFavorites.Size     = New-Object System.Drawing.Size(660, 150)

$favoritesListBox            = New-Object System.Windows.Forms.ListBox
$favoritesListBox.Location   = New-Object System.Drawing.Point(12, 22)
$favoritesListBox.Size       = New-Object System.Drawing.Size(500, 118)
foreach ($fav in $script:Favorites) { [void]$favoritesListBox.Items.Add($fav) }

$loadFavoriteButton          = New-Object System.Windows.Forms.Button
$loadFavoriteButton.Text     = "Charger"
$loadFavoriteButton.Location = New-Object System.Drawing.Point(522, 22)
$loadFavoriteButton.Size     = New-Object System.Drawing.Size(120, 26)

$removeFavoriteButton          = New-Object System.Windows.Forms.Button
$removeFavoriteButton.Text     = "Supprimer favori"
$removeFavoriteButton.Location = New-Object System.Drawing.Point(522, 54)
$removeFavoriteButton.Size     = New-Object System.Drawing.Size(120, 26)

$grpFavorites.Controls.AddRange(@($favoritesListBox, $loadFavoriteButton, $removeFavoriteButton))

# --- GroupBox : Logs ---
$grpLogs          = New-Object System.Windows.Forms.GroupBox
$grpLogs.Text     = "Logs"
$grpLogs.Location = New-Object System.Drawing.Point(12, 502)
$grpLogs.Size     = New-Object System.Drawing.Size(660, 270)

$logListBox                     = New-Object System.Windows.Forms.ListBox
$logListBox.Location            = New-Object System.Drawing.Point(12, 22)
$logListBox.Size                = New-Object System.Drawing.Size(636, 236)
$logListBox.HorizontalScrollbar = $true

$grpLogs.Controls.Add($logListBox)

# --- GroupBox : Soutenir le projet (don) ---
$grpDonate          = New-Object System.Windows.Forms.GroupBox
$grpDonate.Text     = "Soutenir le projet"
$grpDonate.Location = New-Object System.Drawing.Point(12, 394)
$grpDonate.Size     = New-Object System.Drawing.Size(660, 100)

$donateButton          = New-Object System.Windows.Forms.Button
$donateButton.Text     = "Faire un don (PayPal)"
$donateButton.Location = New-Object System.Drawing.Point(12, 34)
$donateButton.Size     = New-Object System.Drawing.Size(220, 32)

$qrPictureBox             = New-Object System.Windows.Forms.PictureBox
$qrPictureBox.Location    = New-Object System.Drawing.Point(250, 12)
$qrPictureBox.Size        = New-Object System.Drawing.Size(76, 76)
$qrPictureBox.SizeMode    = "Zoom"
$qrPictureBox.BorderStyle = "FixedSingle"
if (Test-Path $Config.DonateQrPath) {
    try {
        $qrPictureBox.Image = [System.Drawing.Image]::FromFile($Config.DonateQrPath)
    } catch {
        Write-Log "Impossible de charger le QR code de don ($($Config.DonateQrPath)) : $_" "WARN"
    }
} else {
    Write-Log "QR code de don introuvable : $($Config.DonateQrPath)" "WARN"
}

$donateLabel             = New-Object System.Windows.Forms.Label
$donateLabel.Text        = "Scanne le QR code avec ton téléphone, ou clique sur le bouton."
$donateLabel.Location    = New-Object System.Drawing.Point(340, 40)
$donateLabel.AutoSize    = $false
$donateLabel.Size        = New-Object System.Drawing.Size(300, 40)

$grpDonate.Controls.AddRange(@($donateButton, $qrPictureBox, $donateLabel))

$form.Controls.AddRange(@($themeLabel, $themeCombo, $grpRecord, $grpProgress, $grpFavorites, $grpDonate, $grpLogs))

# =========================================================
# THEME
# =========================================================
function Apply-Theme {
    param([string]$ThemeName)

    if ($ThemeName -eq "Sombre") {
        $bgColor    = [System.Drawing.Color]::FromArgb(32, 32, 34)
        $panelColor = [System.Drawing.Color]::FromArgb(48, 48, 51)
        $fgColor    = [System.Drawing.Color]::WhiteSmoke
        $btnColor   = [System.Drawing.Color]::FromArgb(66, 66, 70)
    } else {
        $bgColor    = [System.Drawing.Color]::FromArgb(240, 240, 240)
        $panelColor = [System.Drawing.Color]::White
        $fgColor    = [System.Drawing.Color]::Black
        $btnColor   = [System.Drawing.Color]::FromArgb(225, 225, 225)
    }

    $form.BackColor = $bgColor
    $form.ForeColor = $fgColor

    function Set-ControlTheme {
        param($control)
        switch ($control.GetType().Name) {
            'GroupBox' {
                $control.ForeColor = $fgColor
                $control.BackColor = $bgColor
            }
            'Button' {
                $control.BackColor = $btnColor
                $control.ForeColor = $fgColor
                $control.FlatStyle = "Flat"
            }
            'TextBox' {
                $control.BackColor = $panelColor
                $control.ForeColor = $fgColor
            }
            'ListBox' {
                $control.BackColor = $panelColor
                $control.ForeColor = $fgColor
            }
            'ComboBox' {
                $control.BackColor = $panelColor
                $control.ForeColor = $fgColor
            }
            'Label' {
                $control.ForeColor = $fgColor
            }
            'PictureBox' {
                $control.BackColor = $panelColor
            }
        }
        foreach ($child in $control.Controls) {
            Set-ControlTheme -control $child
        }
    }

    foreach ($ctrl in $form.Controls) {
        Set-ControlTheme -control $ctrl
    }

    $script:CurrentTheme = $ThemeName
}

$themeCombo.Add_SelectedIndexChanged({
    Apply-Theme -ThemeName $themeCombo.SelectedItem.ToString()
})

# =========================================================
# TIMER : logs dynamiques + progression
# =========================================================
$script:Timer          = New-Object System.Windows.Forms.Timer
$script:Timer.Interval = 1000

$script:Timer.Add_Tick({
    if ($script:LogFilePath -and (Test-Path $script:LogFilePath)) {
        $allLines = @(Get-Content -Path $script:LogFilePath -ErrorAction SilentlyContinue)
        if ($allLines.Count -gt $script:LastLineIndex) {
            $newLines = $allLines[$script:LastLineIndex..($allLines.Count - 1)]
            foreach ($line in $newLines) {
                if ([string]::IsNullOrWhiteSpace($line)) { continue }
                $logListBox.Items.Add($line) | Out-Null
                if ($line -match '\[download\]\s+([\d\.]+)%') {
                    $pct = [double]$matches[1]
                    $progressBar.Value = [Math]::Min(100, [Math]::Max(0, [int][Math]::Round($pct)))
                    $statusLabel.Text  = "Enregistrement en cours... ($([Math]::Round($pct, 1))%)"
                }
            }
            if ($logListBox.Items.Count -gt 0) { $logListBox.TopIndex = $logListBox.Items.Count - 1 }
            $script:LastLineIndex = $allLines.Count
        }
    }

    if ($script:CurrentJob -and $script:CurrentJob.State -in @('Completed', 'Failed', 'Stopped')) {
        $finalState = $script:CurrentJob.State
        $script:Timer.Stop()
        Remove-Job -Job $script:CurrentJob -Force -ErrorAction SilentlyContinue
        $script:CurrentJob = $null

        $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Job terminé (état : $finalState).") | Out-Null

        $VideoFile = Get-ChildItem $script:CurrentCaptureDir -File -Filter "$($script:CurrentRoomName)-*.mp4" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1

        if ($VideoFile) {
            $Thumbnail = Join-Path $VideoFile.DirectoryName "$($VideoFile.BaseName).jpg"
            & $Config.FFmpeg -ss $Config.ThumbnailOffsetSeconds -i $VideoFile.FullName -frames:v 1 -q:v 2 $Thumbnail -y -loglevel error 2>$null
            if (Test-Path $Thumbnail) {
                $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Miniature créée : $Thumbnail") | Out-Null
            } else {
                $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Erreur création miniature.") | Out-Null
            }
        } else {
            $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Aucune vidéo MP4 trouvée.") | Out-Null
        }

        if ($logListBox.Items.Count -gt 0) { $logListBox.TopIndex = $logListBox.Items.Count - 1 }

        $statusLabel.Text    = "Terminé ($finalState)."
        $startButton.Enabled = $true
        $stopButton.Enabled  = $false
        $progressBar.Value   = if ($finalState -eq 'Completed') { 100 } else { 0 }
    }
})

# =========================================================
# EVENEMENTS
# =========================================================
$startButton.Add_Click({
    $urlInput = $urlTextBox.Text.Trim()

    if (-not (Test-SafeUrl -UrlToTest $urlInput -AllowedDomains $whitelist)) {
        [System.Windows.Forms.MessageBox]::Show("URL refusée par la validation de sécurité (voir log de session).", "Erreur", "OK", "Error") | Out-Null
        return
    }
    if (-not (Test-YtDlpBinary)) {
        [System.Windows.Forms.MessageBox]::Show("Échec vérification yt-dlp.exe. Renseigne `$YtDlpExpectedSha256.", "Erreur", "OK", "Error") | Out-Null
        return
    }
    if (-not (Test-FfmpegBinary)) {
        [System.Windows.Forms.MessageBox]::Show("Échec vérification ffmpeg.exe. Renseigne `$FfmpegExpectedSha256.", "Erreur", "OK", "Error") | Out-Null
        return
    }

    # --- Pinning CA local pour les binaires (optionnel, voir $EnableCaPinning) ---
    if ($EnableCaPinning) {
        if (-not (Test-CACertificate -FilePath $YtDlpPath -ExpectedThumbprint $TrustedCaThumbprint -ExpectedIssuer $TrustedCaIssuer)) {
            [System.Windows.Forms.MessageBox]::Show("Échec du pinning CA pour yt-dlp.exe.", "Erreur", "OK", "Error") | Out-Null
            return
        }
        if (-not (Test-CACertificate -FilePath $FfmpegPath -ExpectedThumbprint $TrustedCaThumbprint -ExpectedIssuer $TrustedCaIssuer)) {
            [System.Windows.Forms.MessageBox]::Show("Échec du pinning CA pour ffmpeg.exe.", "Erreur", "OK", "Error") | Out-Null
            return
        }
        Write-Log "CA pinning activé et validé pour yt-dlp.exe et ffmpeg.exe."
    } else {
        Write-Log "CA pinning désactivé (binaires non signés)."
    }

    # --- Sandbox dossier : re-validation défensive du dossier de sortie avant chaque lancement ---
    # (protège aussi contre un remplacement du dossier par un lien symbolique entre le démarrage
    #  du script et le clic sur Démarrer)
    if (-not (Test-ValidPath -Path $Config.CaptureDir)) {
        [System.Windows.Forms.MessageBox]::Show("Dossier de sortie invalide ou interdit par la sandbox : $($Config.CaptureDir)", "Erreur", "OK", "Error") | Out-Null
        return
    }

    $UriObj   = [System.Uri]$urlInput

    # --- Pinning TLS du serveur distant (optionnel, voir $EnableTlsServerPinning) ---
    if ($EnableTlsServerPinning) {
        if (-not (Test-RemoteCertificate -HostName $UriObj.Host -Port 443 -ExpectedThumbprint $ServerExpectedThumbprint -ExpectedIssuer $ServerExpectedIssuer)) {
            [System.Windows.Forms.MessageBox]::Show("Échec de la vérification TLS du serveur distant ($($UriObj.Host)).", "Erreur", "OK", "Error") | Out-Null
            return
        }
        Write-Log "TLS server pinning activé et validé pour $($UriObj.Host)."
    } else {
        Write-Log "TLS server pinning désactivé (validation TLS native uniquement)."
    }

    $RoomName = ($UriObj.AbsolutePath.Trim('/').Split('/') | Where-Object { $_ -ne '' } | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($RoomName)) { $RoomName = "Chaturbate" }

    $Time                     = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    $script:LogFilePath       = Join-Path $Config.LogDir "$RoomName-$Time.log"
    $script:LastLineIndex     = 0
    $script:CurrentRoomName   = $RoomName
    $script:CurrentCaptureDir = $Config.CaptureDir
    $OutputPath               = Join-Path $Config.CaptureDir "$RoomName-$Time.%(ext)s"

    if (-not (Test-ValidPath -Path $script:LogFilePath)) {
        [System.Windows.Forms.MessageBox]::Show("Chemin de log invalide.", "Erreur", "OK", "Error") | Out-Null
        return
    }

    New-Item -ItemType File -Path $script:LogFilePath -Force | Out-Null

    $logListBox.Items.Clear()
    $progressBar.Value = 0
    $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Démarrage de l'enregistrement pour '$RoomName'...") | Out-Null

    $jobScript = {
        param($YtDlpExe, $FfmpegExe, $TargetUrl, $OutputTemplate, $LogPath)
        $YtArgs = @(
            "--newline"
            "--retries", "infinite"
            "--fragment-retries", "infinite"
            "--retry-sleep", "5"
            "--socket-timeout", "30"
            "--wait-for-video", "30"
            "--hls-use-mpegts"
            "--remux-video", "mp4"
            "--ffmpeg-location", $FfmpegExe
            "-o", $OutputTemplate
            "--progress"
            $TargetUrl
        )
        try {
            & $YtDlpExe @YtArgs *>> $LogPath
        } catch {
            Add-Content -Path $LogPath -Value "[JOB-ERROR] $_"
        }
    }

    try {
        $script:CurrentJob = Start-Job -ScriptBlock $jobScript -ArgumentList $Config.YtDlp, $Config.FFmpeg, $urlInput, $OutputPath, $script:LogFilePath
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Impossible de démarrer le job : $_", "Erreur", "OK", "Error") | Out-Null
        return
    }

    $startButton.Enabled = $false
    $stopButton.Enabled  = $true
    $statusLabel.Text    = "Enregistrement en cours..."
    $script:Timer.Start()
})

$stopButton.Add_Click({
    if ($script:CurrentJob) {
        Stop-Job -Job $script:CurrentJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:CurrentJob -Force -ErrorAction SilentlyContinue
        $script:CurrentJob = $null
    }
    $script:Timer.Stop()
    $logListBox.Items.Add("[$(Get-Date -Format 'HH:mm:ss')] Téléchargement interrompu.") | Out-Null
    if ($logListBox.Items.Count -gt 0) { $logListBox.TopIndex = $logListBox.Items.Count - 1 }
    $statusLabel.Text    = "Arrêté."
    $startButton.Enabled = $true
    $stopButton.Enabled  = $false
    $progressBar.Value   = 0
})

$addFavoriteButton.Add_Click({
    $urlToAdd = $urlTextBox.Text.Trim()
    if (-not (Add-FavoriteUrl -UrlToAdd $urlToAdd)) {
        [System.Windows.Forms.MessageBox]::Show("URL invalide ou déjà présente dans les favoris.", "Info", "OK", "Information") | Out-Null
        return
    }
    $favoritesListBox.Items.Add($urlToAdd) | Out-Null
})

$removeFavoriteButton.Add_Click({
    $selected = $favoritesListBox.SelectedItem
    if (-not $selected) { return }
    Remove-FavoriteUrl -UrlToRemove $selected.ToString()
    $favoritesListBox.Items.Remove($selected)
})

$loadFavoriteButton.Add_Click({
    if ($favoritesListBox.SelectedItem) {
        $urlTextBox.Text = $favoritesListBox.SelectedItem.ToString()
    }
})

$favoritesListBox.Add_DoubleClick({
    if ($favoritesListBox.SelectedItem) {
        $urlTextBox.Text = $favoritesListBox.SelectedItem.ToString()
    }
})

$donateButton.Add_Click({
    try {
        Start-Process $Config.DonateUrl
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Impossible d'ouvrir le lien de don : $_", "Erreur", "OK", "Error") | Out-Null
    }
})

$form.Add_FormClosing({
    if ($script:Timer) { $script:Timer.Stop() }
    if ($script:CurrentJob) {
        Stop-Job -Job $script:CurrentJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:CurrentJob -Force -ErrorAction SilentlyContinue
    }
})

Apply-Theme -ThemeName "Clair"

[void]$form.ShowDialog()