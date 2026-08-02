# Version PowerShell (historique)

Version originale du projet, écrite en PowerShell + Windows Forms, avant le
portage complet vers .NET 10 / C# natif (voir le reste du dépôt). Conservée
ici à titre d'archive/référence historique — **non maintenue**, tout le
développement actif se fait désormais côté C#.

## Contenu

- `Chaturbate_Record_GUI.ps1` — script principal (interface + logique
  d'enregistrement).
- `Lancer Record.ps1` — lanceur (masque la fenêtre PowerShell au démarrage).
- `config.json` — configuration par défaut (dossier de sortie, qualité...).
- `donate_qr.png` — QR code de don, identique à celui du projet C#.

## Utilisation

```powershell
cd legacy-powershell
.\Chaturbate_Record_GUI.ps1
```

Nécessite `yt-dlp.exe` et `ffmpeg.exe` (non inclus, voir le README principal
du dépôt pour où les obtenir).
