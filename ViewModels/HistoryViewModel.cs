using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.ViewModels
{
    /// <summary>Deux présentations de la MÊME liste (Premium II) — jamais deux sources de données.</summary>
    public enum ModeAffichageHistorique { Liste, Galerie }

    public enum TriHistorique { DatePlusRecente, DureeLaPlusLongue, Salon }

    public sealed partial class HistoryItemViewModel : ObservableObject
    {
        public HistoryItemViewModel(HistoryEntry entree)
        {
            Nom = entree.Nom;
            CheminComplet = entree.CheminComplet;
            CheminVignette = entree.CheminVignette;
            Salon = entree.Salon;
            Date = entree.Date;
            TailleLisible = HistoryService.FormaterTaille(entree.Taille);
            DateLisible = entree.Date.ToString("g");
        }

        public string Nom { get; }
        public string CheminComplet { get; }
        public string? CheminVignette { get; }

        /// <summary>Du sidecar écrit à la finalisation, ou reparsé en repli — voir <c>HistoryService.Salon</c>. Null : salon inconnu (nom déjà personnalisé, sans sidecar).</summary>
        public string? Salon { get; }

        /// <summary>
        /// Fixe dès la construction (jamais recalculé) : sert UNIQUEMENT à
        /// choisir entre le vrai nom et le repli <c>{ui:Str gallery.unknownRoom}</c>
        /// en XAML, qui lui reste une VRAIE liaison et survit donc à un
        /// changement de langue — un texte de repli lu une fois en C# ne le
        /// ferait pas (voir la mésaventure déjà payée sur la localisation).
        /// </summary>
        public bool SalonInconnu => Salon == null;

        public DateTime Date { get; }
        public string TailleLisible { get; }
        public string DateLisible { get; }

        /// <summary>Valeur brute pour le TRI (Galerie) — jamais affichée telle quelle, voir <see cref="DureeLisible"/>.</summary>
        internal TimeSpan? DureeBrute { get; private set; }

        /// <summary>Null tant que non résolue (Galerie) : <see cref="HistoryViewModel"/> la détecte en arrière-plan, jamais sur le chemin de chargement de la liste.</summary>
        [ObservableProperty] private string? _dureeLisible;

        public bool EstLicencie => App.Premium.IsLicensed;

        /// <summary>Empreinte d'INTÉGRITÉ (pas d'authenticité — rien à comparer pour un enregistrement personnel), calculée à la DEMANDE, jamais au chargement de la liste (fichiers potentiellement énormes).</summary>
        [ObservableProperty] private string? _empreinte;
        [ObservableProperty] private bool _calculEmpreinteEnCours;

        /// <summary>Pour n'afficher le bouton « Empreinte » qu'avant son calcul — <see cref="InverseBooleanToVisibilityConverter"/>, pas un nouveau convertisseur pour une seule liaison.</summary>
        public bool EmpreinteInconnue => Empreinte == null;

        partial void OnEmpreinteChanged(string? value) => OnPropertyChanged(nameof(EmpreinteInconnue));

        /// <summary>
        /// Date seule, ou date + durée si connue — un seul <c>TextBlock.Text</c>
        /// plutôt que deux <c>Run</c> dont l'un serait masqué : <c>Run</c> n'a
        /// pas de propriété <c>Visibility</c> (ce n'est pas un <c>UIElement</c>).
        /// </summary>
        public string DateEtDuree => DureeLisible != null ? $"{DateLisible} — {DureeLisible}" : DateLisible;

        partial void OnDureeLisibleChanged(string? value) => OnPropertyChanged(nameof(DateEtDuree));

        internal void DefinirDuree(TimeSpan? duree)
        {
            DureeBrute = duree;
            DureeLisible = duree is { } d ? FormaterDuree(d) : null;
        }

        private static string FormaterDuree(TimeSpan d) =>
            d.Hours > 0 ? $"{d.Hours}:{d.Minutes:00}:{d.Seconds:00}" : $"{d.Minutes:00}:{d.Seconds:00}";

        [RelayCommand]
        private async Task CalculerEmpreinteAsync()
        {
            if (!EstLicencie || CalculEmpreinteEnCours || Empreinte != null) return;

            CalculEmpreinteEnCours = true;
            try
            {
                Empreinte = await HistoryService.CalculerEmpreinteAsync(CheminComplet).ConfigureAwait(true);
            }
            finally
            {
                CalculEmpreinteEnCours = false;
            }
        }
    }

    public partial class HistoryViewModel : ObservableObject
    {
        public ObservableCollection<HistoryItemViewModel> Elements { get; } = new();
        [ObservableProperty] private bool _chargement;
        [ObservableProperty] private bool _vide;
        [ObservableProperty] private ModeAffichageHistorique _mode = ModeAffichageHistorique.Liste;
        [ObservableProperty] private string _filtreSalon = "";
        [ObservableProperty] private TriHistorique _tri = TriHistorique.DatePlusRecente;

        // La liste COMPLÈTE, jamais affichée directement : Elements n'en montre
        // que la vue filtrée/triée, recalculée à chaque changement de l'un des
        // deux réglages plutôt que de re-lire le dossier.
        private IReadOnlyList<HistoryItemViewModel> _tous = Array.Empty<HistoryItemViewModel>();

        public HistoryViewModel()
        {
            _ = RafraichirAsync();
        }

        [RelayCommand]
        private void AfficherEnListe() => Mode = ModeAffichageHistorique.Liste;

        [RelayCommand]
        private void AfficherEnGalerie() => Mode = ModeAffichageHistorique.Galerie;

        [RelayCommand]
        private void TrierParDate() => Tri = TriHistorique.DatePlusRecente;

        [RelayCommand]
        private void TrierParDuree() => Tri = TriHistorique.DureeLaPlusLongue;

        [RelayCommand]
        private void TrierParSalon() => Tri = TriHistorique.Salon;

        [RelayCommand]
        private async Task RafraichirAsync()
        {
            try
            {
                Chargement = true;
                var entrees = await HistoryService.ListerAsync();
                _tous = entrees.Select(e => new HistoryItemViewModel(e)).ToList();
                AppliquerTriEtFiltre();

                // EN ARRIÈRE-PLAN, jamais avant d'afficher la liste : sonder
                // jusqu'à 50 fichiers avec ffmpeg avant le premier rendu
                // ferait attendre l'utilisateur pour une donnée secondaire.
                _ = DetecterDureesEnArrierePlanAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Historique : rafraîchissement impossible — {ex.Message}", LogLevel.WARN);
            }
            finally
            {
                Chargement = false;
            }
        }

        // Propriétés dérivées, pour réutiliser BooleanToVisibilityConverter
        // (déjà employé pour Chargement/Vide) plutôt qu'écrire un nouveau
        // convertisseur pour une seule paire de valeurs d'énumération.
        public bool EstEnListe => Mode == ModeAffichageHistorique.Liste;
        public bool EstEnGalerie => Mode == ModeAffichageHistorique.Galerie;

        partial void OnModeChanged(ModeAffichageHistorique value)
        {
            OnPropertyChanged(nameof(EstEnListe));
            OnPropertyChanged(nameof(EstEnGalerie));
        }

        public bool TriParDate => Tri == TriHistorique.DatePlusRecente;
        public bool TriParDuree => Tri == TriHistorique.DureeLaPlusLongue;
        public bool TriParSalon => Tri == TriHistorique.Salon;

        partial void OnFiltreSalonChanged(string value) => AppliquerTriEtFiltre();

        partial void OnTriChanged(TriHistorique value)
        {
            AppliquerTriEtFiltre();
            OnPropertyChanged(nameof(TriParDate));
            OnPropertyChanged(nameof(TriParDuree));
            OnPropertyChanged(nameof(TriParSalon));
        }

        private void AppliquerTriEtFiltre()
        {
            IEnumerable<HistoryItemViewModel> vue = _tous;

            if (!string.IsNullOrWhiteSpace(FiltreSalon))
            {
                vue = vue.Where(e => e.Salon != null &&
                    e.Salon.Contains(FiltreSalon, StringComparison.OrdinalIgnoreCase));
            }

            vue = Tri switch
            {
                // Duree/Salon inconnus repoussés en fin de tri plutôt que
                // traités comme le minimum : un salon inconnu n'est pas "avant
                // l'alphabet", une durée pas encore détectée n'est pas "0 s".
                TriHistorique.DureeLaPlusLongue => vue
                    .OrderBy(e => e.DureeBrute == null)
                    .ThenByDescending(e => e.DureeBrute),
                TriHistorique.Salon => vue
                    .OrderBy(e => e.Salon == null)
                    .ThenBy(e => e.Salon, StringComparer.OrdinalIgnoreCase),
                _ => vue.OrderByDescending(e => e.Date),
            };

            Elements.Clear();
            foreach (var e in vue) Elements.Add(e);
            Vide = Elements.Count == 0;
        }

        /// <summary>
        /// Bornée à trois sondes ffmpeg concurrentes : cinquante à la fois
        /// (la borne de <c>HistoryService.Maximum</c>) saturerait la machine
        /// pour une donnée qui n'a jamais besoin d'être instantanée.
        /// </summary>
        private async Task DetecterDureesEnArrierePlanAsync()
        {
            using var limiteur = new System.Threading.SemaphoreSlim(3);

            var taches = _tous.Select(async item =>
            {
                await limiteur.WaitAsync().ConfigureAwait(true);
                try
                {
                    var duree = await CaptureFinalizer.DetecterDureeAsync(item.CheminComplet, AppConfig.FFmpegPath)
                        .ConfigureAwait(true);
                    item.DefinirDuree(duree);
                }
                finally
                {
                    limiteur.Release();
                }
            });

            await Task.WhenAll(taches).ConfigureAwait(true);

            // Un tri par durée déjà demandé pendant la détection doit refléter
            // les valeurs maintenant connues, pas rester figé sur "inconnu en dernier".
            if (Tri == TriHistorique.DureeLaPlusLongue) AppliquerTriEtFiltre();
        }

        [RelayCommand]
        private static void OuvrirDossier(string? chemin)
        {
            if (string.IsNullOrEmpty(chemin))
                return;
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{chemin}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture du dossier impossible — {ex.Message}", LogLevel.WARN);
            }
        }

        [RelayCommand]
        private static void OuvrirFichier(string? chemin)
        {
            if (string.IsNullOrEmpty(chemin) || !File.Exists(chemin))
                return;
            try
            {
                Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture du fichier impossible — {ex.Message}", LogLevel.WARN);
            }
        }
    }
}
