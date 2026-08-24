using System;
using System.IO;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Identifiant d'installation (120.0) — GUID généré une fois, persisté à
    /// côté de l'exécutable, jamais recalculé depuis du matériel.
    ///
    /// **Volontairement PAS une empreinte matérielle (CPU/disque/MAC)** :
    /// évalué et écarté avec Tom (25-08). Une empreinte multi-facteurs
    /// verrouille davantage sur le papier, mais casse chez de VRAIS acheteurs
    /// à la moindre carte réseau changée, disque remplacé, ou VM — Windows
    /// randomise même l'adresse MAC Wi-Fi par défaut sur des versions
    /// récentes. Le but assumé n'est PAS l'inviolabilité (aucun contrôle
    /// entièrement côté client ne l'est jamais) mais la dissuasion du partage
    /// CASUAL, sans faire retomber le coût sur les gens qui ont payé.
    ///
    /// Écrit ICI, jamais côté `StreamRecorderPro` : cet identifiant doit
    /// exister même SANS le composant payant présent — c'est justement ce
    /// qu'un acheteur envoie AVANT de recevoir son fichier (voir la fenêtre
    /// Premium). `StreamRecorderPro.Licence` ne fait que LIRE le même fichier
    /// depuis le même `AppContext.BaseDirectory` (même processus, chargé par
    /// réflexion) — jamais de référence croisée entre les deux assemblys.
    /// </summary>
    internal static class MachineId
    {
        /// <summary>Même nom que `StreamRecorderPro.MachineId.FileName` — les deux DOIVENT rester synchronisés à la main, aucune référence croisée ne peut le garantir.</summary>
        private const string FileName = "machine-id.txt";

        private static string Chemin => Path.Combine(AppContext.BaseDirectory, FileName);

        private static string? _cache;

        /// <summary>
        /// Lit l'identifiant existant, ou en génère et persiste un nouveau au
        /// premier appel. Mémorisé en mémoire pour le reste du processus :
        /// inutile de retoucher le disque à chaque affichage de la fenêtre
        /// Premium ou du panneau Diagnostic.
        /// </summary>
        internal static string Obtenir()
        {
            if (_cache != null) return _cache;

            try
            {
                if (File.Exists(Chemin))
                {
                    var existant = File.ReadAllText(Chemin).Trim();
                    if (existant.Length > 0)
                    {
                        _cache = existant;
                        return existant;
                    }
                }

                var nouveau = Guid.NewGuid().ToString("N");
                File.WriteAllText(Chemin, nouveau);
                _cache = nouveau;
                return nouveau;
            }
            catch (Exception ex)
            {
                // Un dossier en lecture seule ne doit jamais empêcher l'app de
                // démarrer : la fenêtre Premium affichera simplement une
                // chaîne vide, et le panneau Diagnostic peut journaliser le
                // vrai motif si quelqu'un signale le problème.
                Logger.Log($"Identifiant d'installation illisible/inscriptible : {ex.Message}", LogLevel.WARN);
                return "";
            }
        }
    }
}
