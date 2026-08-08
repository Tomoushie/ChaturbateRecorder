; Installateur Chaturbate Recorder (23.0)
;
; Objectif : que l'utilisateur n'ait RIEN a faire d'autre que cliquer. Le retour
; de plusieurs testeurs etait unanime — ce n'est pas « copier deux fichiers » qui
; rebute, c'est devoir aller les chercher, comprendre quelle variante prendre et
; savoir ou les poser.
;
; QUATRE DECISIONS STRUCTURANTES, chacune motivee :
;
; 1. AUCUNE CHARGE UTILE EMBARQUEE. Le setup ne contient que sa propre logique
;    (~2 Mo au lieu de 34). L'application, yt-dlp et ffmpeg sont telecharges a
;    l'installation. C'est le modele des installateurs modernes — celui de
;    Discord fait 300 Ko — et ca n'ajoute aucune contrainte : yt-dlp et ffmpeg
;    devaient de toute facon etre telecharges, donc une connexion etait deja
;    requise.
;
; 2. L'APPLICATION TELECHARGEE EST LA VARIANTE AUTONOME (self-contained), dans
;    LES DEUX modes. Le runtime .NET est embarque dedans, donc .NET n'est JAMAIS
;    un prerequis et l'installateur n'a rien a installer de plus. C'est
;    strictement superieur a installer le runtime : pas d'elevation UAC, pas de
;    55 Mo supplementaires, aucune modification de la machine hors du dossier
;    choisi. C'est aussi ce qui reglait la boite « .NET introuvable » signalee
;    avant la v1.27.0.
;
; 3. DEUX MODES PROPOSES DES LE DEPART, comme 7-Zip : installation classique
;    (raccourcis, desinstalleur) ou extraction portable (rien d'autre que les
;    fichiers). Un seul executable a distribuer au lieu de deux archives.
;
; 4. INSTALLATION PAR UTILISATEUR (%LOCALAPPDATA%\Programs), pas Program Files.
;    L'application ecrit settings.json, favorites.json et trusted-binaries.json
;    A COTE de son exe (AppConfig.AppDir) : dans Program Files ces ecritures
;    echoueraient ou seraient virtualisees. En prime, aucune fenetre UAC.
;    Verifie avant d'ecrire ce script : WorkingDirectoryValidator refuse Temp,
;    Downloads, Bureau, corbeille et lecteurs reseau — mais PAS
;    %LOCALAPPDATA%\Programs.
;
; TOUT CE QUI EST TELECHARGE EST VERIFIE. Le hash du ZIP applicatif est fige
; dans ce script par la CI au moment de la compilation ; yt-dlp et ffmpeg sont
; compares aux sommes de controle publiees par leurs auteurs. Un echec de
; verification interrompt l'installation : un logiciel dont l'argument principal
; est la rigueur ne peut pas installer un binaire non verifie.

#define AppName "Chaturbate Recorder"
#define AppPublisher "Binary Forge"
#define AppUrl "https://tomoushie.github.io/ChaturbateRecorder/"
#define AppExe "ChaturbateRecorder.exe"
#define RepoUrl "https://github.com/Tomoushie/ChaturbateRecorder"

; Fournis par la CI : ISCC /DAppVersion=1.27.0 /DAppZipSha256=ABC...
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef AppZipSha256
  #define AppZipSha256 ""
#endif

[Setup]
; GUID propre a cette application : ne JAMAIS le changer, c'est lui qui permet
; a une nouvelle version de remplacer l'ancienne au lieu de s'installer a cote.
AppId={{7C4E1F2A-9B63-4D18-A5E7-3F0C6D2B84A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#RepoUrl}/issues
DefaultDirName={code:DefaultDir}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=ChaturbateRecorder-v{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Assistant reduit au strict necessaire : mode, dossier, progression, fin.
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
UninstallDisplayIcon={app}\{#AppExe}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\Assets\app.ico
; En mode portable, rien ne doit subsister hors du dossier choisi : ni
; desinstalleur, ni entree dans « Applications installees ».
Uninstallable=IsInstallMode
CreateUninstallRegKey=IsInstallMode

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
french.ModeCaption=Type d'installation
french.ModeDescription=Comment veux-tu utiliser {#AppName} ?
french.ModeInstall=Installer sur cet ordinateur
french.ModeInstallHint=Raccourcis dans le menu Démarrer, désinstallation propre. Recommandé.
french.ModePortable=Version portable
french.ModePortableHint=Extrait les fichiers dans un dossier de ton choix. Aucun raccourci, aucune trace ailleurs.
french.DownloadingTitle=Téléchargement
french.DownloadingDesc=L'application, yt-dlp et ffmpeg sont téléchargés depuis leurs sources officielles, puis vérifiés.
french.CreateDesktopIcon=Créer un raccourci sur le &Bureau
french.LaunchApp=Lancer {#AppName}
french.ExtractFailed=Extraction impossible. Installation interrompue.
french.HashMismatch=Le fichier téléchargé ne correspond pas à la somme de contrôle attendue.%n%nInstallation interrompue par sécurité.
french.NoChecksum=Somme de contrôle introuvable pour %1.%n%nInstallation interrompue : aucun binaire non vérifié n'est installé.
english.ModeCaption=Installation type
english.ModeDescription=How do you want to use {#AppName}?
english.ModeInstall=Install on this computer
english.ModeInstallHint=Start Menu shortcuts, clean uninstall. Recommended.
english.ModePortable=Portable version
english.ModePortableHint=Extracts the files into a folder of your choice. No shortcuts, nothing left elsewhere.
english.DownloadingTitle=Downloading
english.DownloadingDesc=The application, yt-dlp and ffmpeg are downloaded from their official sources, then verified.
english.CreateDesktopIcon=Create a &desktop shortcut
english.LaunchApp=Launch {#AppName}
english.ExtractFailed=Extraction failed. Setup aborted.
english.HashMismatch=The downloaded file does not match the expected checksum.%n%nSetup aborted for safety.
english.NoChecksum=No checksum found for %1.%n%nSetup aborted: no unverified binary is installed.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Check: IsInstallMode

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; Check: IsInstallMode
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"; Check: IsInstallMode
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Fichiers crees par l'application a cote de son exe : sans ca le dossier
; resterait apres desinstallation avec des reglages orphelins.
; ATTENTION : les fichiers extraits du ZIP a l'installation ne sont PAS suivis
; par Inno — il ne sait pas qu'ils existent, donc il ne les supprimera jamais de
; lui-meme. Tout ce que contient l'archive doit figurer ici nommement. Constate
; en testant une desinstallation : le .pdb, oublie dans une premiere version de
; cette liste, restait seul dans le dossier et empechait meme sa suppression
; par dirifempty.
Type: files; Name: "{app}\{#AppExe}"
Type: files; Name: "{app}\ChaturbateRecorder.pdb"
Type: files; Name: "{app}\yt-dlp.exe"
Type: files; Name: "{app}\ffmpeg.exe"
Type: files; Name: "{app}\donate_qr.png"
Type: files; Name: "{app}\settings.json"
Type: files; Name: "{app}\favorites.json"
Type: files; Name: "{app}\watchlist.json"
Type: files; Name: "{app}\installed-components.json"
Type: files; Name: "{app}\trusted-binaries.json"
Type: dirifempty; Name: "{app}"
; Depuis la v1.27.0 les logs et rapports de plantage vivent HORS du dossier
; d'installation (LocalAppData, toujours inscriptible). Sans cette ligne, une
; desinstallation les laissait derriere elle. Les ENREGISTREMENTS ne sont jamais
; touches : ils sont dans les Videos de l'utilisateur, et ils lui appartiennent.
Type: filesandordirs; Name: "{localappdata}\ChaturbateRecorder"

[Code]
const
  AppZipUrl    = '{#RepoUrl}/releases/download/v{#AppVersion}/ChaturbateRecorder-v{#AppVersion}-portable.zip';
  YtDlpUrl     = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe';
  YtDlpSumsUrl = 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS';
  FfmpegUrl    = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip';
  FfmpegSumUrl = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256';

var
  ModePage: TInputOptionWizardPage;
  YtDlpHash, FfmpegHash: String;

{ Utilisee par [Setup], [Icons] et [Tasks] : le mode portable ne doit produire
  ni raccourci, ni desinstalleur, ni entree de registre. }
function IsInstallMode: Boolean;
begin
  { Avant la creation de la page, on suppose le mode installation — c'est le cas
    des executions silencieuses (/SILENT), qui n'affichent aucune page. }
  Result := (ModePage = nil) or (ModePage.SelectedValueIndex = 0);
end;

function IsPortableMode: Boolean;
begin
  Result := not IsInstallMode;
end;

function DefaultDir(Param: String): String;
begin
  if IsPortableMode then
    Result := ExpandConstant('{autodocs}\{#AppName}')
  else
    Result := ExpandConstant('{localappdata}\Programs\{#AppName}');
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard;
begin
  ModePage := CreateInputOptionPage(wpLicense,
    ExpandConstant('{cm:ModeCaption}'), ExpandConstant('{cm:ModeDescription}'),
    '', True, False);
  ModePage.Add(ExpandConstant('{cm:ModeInstall}'));
  ModePage.Add(ExpandConstant('{cm:ModePortable}'));
  ModePage.Values[0] := True;
end;

{ Le dossier par defaut depend du mode : on le repositionne quand on quitte la
  page de choix, sinon l'utilisateur verrait le chemin de l'autre mode. }
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectDir then
    WizardForm.DirEdit.Text := DefaultDir('');
end;

{ Lit la somme attendue depuis le fichier publie par les auteurs. yt-dlp publie
  un fichier multi-lignes « <hash>  <nom> » ; gyan.dev publie le hash seul. }
function ExpectedHashFromFile(const SumsFile, EntryName: String): String;
var
  Lines: TArrayOfString;
  I, P: Integer;
  Line: String;
begin
  Result := '';
  if not LoadStringsFromFile(SumsFile, Lines) then Exit;

  for I := 0 to GetArrayLength(Lines) - 1 do
  begin
    Line := Trim(Lines[I]);
    if Line = '' then Continue;

    if (EntryName = '') or (Pos(LowerCase(EntryName), LowerCase(Line)) > 0) then
    begin
      P := Pos(' ', Line);
      if P > 0 then Result := Copy(Line, 1, P - 1) else Result := Line;
      Exit;
    end;
  end;
end;

function VerifyAgainst(const FilePath, Expected, DisplayName: String; var HashOut: String): Boolean;
var
  Actual: String;
begin
  Actual := GetSHA256OfFile(FilePath);
  HashOut := Uppercase(Actual);

  if Trim(Expected) = '' then
  begin
    MsgBox(FmtMessage(ExpandConstant('{cm:NoChecksum}'), [DisplayName]), mbCriticalError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := CompareText(Trim(Expected), Actual) = 0;
  if not Result then
    MsgBox(ExpandConstant('{cm:HashMismatch}') + #13#10#13#10 +
           DisplayName + #13#10 +
           'Attendu : ' + Trim(Expected) + #13#10 + 'Obtenu  : ' + Actual,
           mbCriticalError, MB_OK);
end;

{ Telechargement et verification.
  Volontairement dans PrepareToInstall et NON dans NextButtonClick : les pages
  de l'assistant ne s'affichent pas en execution silencieuse (/SILENT), donc
  NextButtonClick n'y est jamais appele et RIEN ne serait telecharge — le setup
  se terminerait en « succes » sur un dossier vide. Defaut trouve en preparant
  le premier test silencieux, avant toute publication.
  PrepareToInstall s'execute dans les deux modes ; une chaine non vide y
  interrompt l'installation en affichant son contenu. }
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Dummy: String;
begin
  Result := '';
  try
    DownloadTemporaryFile(AppZipUrl, 'app.zip', '{#AppZipSha256}', @OnDownloadProgress);
    DownloadTemporaryFile(YtDlpUrl, 'yt-dlp.exe', '', @OnDownloadProgress);
    DownloadTemporaryFile(YtDlpSumsUrl, 'yt-dlp-sums.txt', '', @OnDownloadProgress);
    DownloadTemporaryFile(FfmpegUrl, 'ffmpeg.zip', '', @OnDownloadProgress);
    DownloadTemporaryFile(FfmpegSumUrl, 'ffmpeg.zip.sha256', '', @OnDownloadProgress);
  except
    { Couvre aussi le hash du ZIP applicatif : passe en parametre a
      DownloadTemporaryFile, il est verifie par Inno lui-meme, qui leve si la
      somme ne correspond pas. }
    Result := 'Telechargement impossible : ' + GetExceptionMessage;
    Exit;
  end;

  if not VerifyAgainst(ExpandConstant('{tmp}\yt-dlp.exe'),
         ExpectedHashFromFile(ExpandConstant('{tmp}\yt-dlp-sums.txt'), 'yt-dlp.exe'),
         'yt-dlp', YtDlpHash) then
  begin
    Result := 'Verification de yt-dlp echouee.';
    Exit;
  end;

  if not VerifyAgainst(ExpandConstant('{tmp}\ffmpeg.zip'),
         ExpectedHashFromFile(ExpandConstant('{tmp}\ffmpeg.zip.sha256'), ''),
         'ffmpeg', Dummy) then
  begin
    Result := 'Verification de ffmpeg echouee.';
    Exit;
  end;
end;

function RunHidden(const Cmd: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('powershell.exe',
    '-NoProfile -ExecutionPolicy Bypass -Command "' + Cmd + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

{ Deballe l'application puis ffmpeg. Le dossier racine du zip ffmpeg contient
  le numero de version, donc il ne peut pas etre code en dur : on cherche
  ffmpeg.exe recursivement. }
function InstallPayload: Boolean;
var
  App, Tmp: String;
begin
  App := ExpandConstant('{app}');
  Tmp := ExpandConstant('{tmp}');

  Result :=
    RunHidden('Expand-Archive -LiteralPath ''' + Tmp + '\app.zip'' -DestinationPath ''' + App + ''' -Force') and
    RunHidden('Expand-Archive -LiteralPath ''' + Tmp + '\ffmpeg.zip'' -DestinationPath ''' + Tmp + '\ffmpeg'' -Force; ' +
              '$f = Get-ChildItem -LiteralPath ''' + Tmp + '\ffmpeg'' -Recurse -Filter ffmpeg.exe | Select-Object -First 1; ' +
              'if ($null -eq $f) { exit 1 }; ' +
              'Copy-Item -LiteralPath $f.FullName -Destination ''' + App + '\ffmpeg.exe'' -Force') and
    RunHidden('Copy-Item -LiteralPath ''' + Tmp + '\yt-dlp.exe'' -Destination ''' + App + '\yt-dlp.exe'' -Force');

  if not Result then
    MsgBox(ExpandConstant('{cm:ExtractFailed}'), mbCriticalError, MB_OK);
end;

{ Ecrit trusted-binaries.json pour que l'application ne redemande pas d'approuver
  des binaires que l'installateur vient de verifier contre la somme de controle
  de leurs auteurs. Sans ce fichier, un avertissement de securite s'afficherait
  au premier enregistrement : l'application compare a une valeur figee dans
  AppConfig, forcement perimee face a un yt-dlp telecharge en derniere version. }
procedure WriteTrustedBinaries;
var
  Json, Stamp: String;
begin
  { Le hash a approuver pour ffmpeg est celui de l'EXE extrait, pas du zip. }
  FfmpegHash := Uppercase(GetSHA256OfFile(ExpandConstant('{app}\ffmpeg.exe')));
  Stamp := GetDateTimeString('yyyy-mm-dd"T"hh:nn:ss"Z"', '-', ':');

  Json := '{' +
    '"yt-dlp":{"Sha256":"' + YtDlpHash + '","TrustedAtUtc":"' + Stamp + '"},' +
    '"ffmpeg":{"Sha256":"' + FfmpegHash + '","TrustedAtUtc":"' + Stamp + '"}' +
    '}';
  SaveStringToFile(ExpandConstant('{app}\trusted-binaries.json'), Json, False);
end;

{ Inventaire de ce qui est REELLEMENT installe.
  Le SBOM attache a la release decrit ce que le projet publie : ses dependances
  NuGet. Il ne mentionne ni yt-dlp ni ffmpeg, qui n'en sont pas — alors qu'ils
  representent l'essentiel du poids installe et que **ffmpeg est sous GPL**. Un
  inventaire qui les omet donne une image fausse des licences en presence,
  precisement pour le public qui lit ce genre de fichier.
  L'installateur est le seul endroit qui connaisse la reponse exacte : il vient
  de telecharger ces binaires et d'en verifier les empreintes.

  Genere par un script PowerShell temporaire plutot qu'en assemblant du JSON en
  Pascal : les versions s'obtiennent en interrogeant les executables, et les
  guillemets imbriques dans une commande en ligne sont une source d'erreurs. }
procedure WriteInstalledComponents;
var
  Ps, App, ScriptPath: String;
begin
  App := ExpandConstant('{app}');
  ScriptPath := ExpandConstant('{tmp}\components.ps1');

  Ps :=
    '$app = ''' + App + '''' + #13#10 +
    '$yt = (& "$app\yt-dlp.exe" --version 2>$null | Select-Object -First 1)' + #13#10 +
    '$ffLine = (& "$app\ffmpeg.exe" -version 2>$null | Select-Object -First 1)' + #13#10 +
    '$ff = if ($ffLine -match ''ffmpeg version (\S+)'') { $Matches[1] } else { "inconnue" }' + #13#10 +
    '$doc = [ordered]@{' + #13#10 +
    '  generatedAtUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"' + #13#10 +
    '  note = "Composants tiers installes par l''installateur. Complete le SBOM de la release, qui ne couvre que les dependances NuGet de l''application."' + #13#10 +
    '  components = @(' + #13#10 +
    '    [ordered]@{ name = "yt-dlp"; version = "$yt"; sha256 = (Get-FileHash "$app\yt-dlp.exe" -Algorithm SHA256).Hash; license = "Unlicense"; source = "https://github.com/yt-dlp/yt-dlp" }' + #13#10 +
    '    [ordered]@{ name = "ffmpeg"; version = "$ff"; sha256 = (Get-FileHash "$app\ffmpeg.exe" -Algorithm SHA256).Hash; license = "GPL-3.0 (build release-essentials)"; source = "https://www.gyan.dev/ffmpeg/builds/" }' + #13#10 +
    '  )' + #13#10 +
    '}' + #13#10 +
    '$doc | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$app\installed-components.json" -Encoding UTF8' + #13#10;

  if SaveStringToFile(ScriptPath, Ps, False) then
    RunHidden('& ''' + ScriptPath + '''');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if InstallPayload then
    begin
      WriteTrustedBinaries;
      { Purement informatif : un echec ici ne doit pas faire echouer une
        installation par ailleurs reussie. }
      WriteInstalledComponents;
    end;
  end;
end;
