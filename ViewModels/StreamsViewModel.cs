namespace ChaturbateRecorderApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using ChaturbateRecorderApp.Config;
    using ChaturbateRecorderApp.Services;
    using ChaturbateRecorderApp.UI;
    using SentinelGuard;

    public partial class StreamsViewModel : ObservableObject, IDisposable
    {
        private readonly RoomStore _store = new();
        public ObservableCollection<RoomCardViewModel> Rooms { get; } = new();

        /// <summary>
        /// Vrai quand aucun salon n'est connu — l'état du PREMIER LANCEMENT, et
        /// celui qu'on voit forcément avant d'avoir rien fait.
        ///
        /// L'écran n'en disait rien : la vue principale s'ouvrait entièrement
        /// blanche sous la barre d'ajout, ce qui se lit comme une application
        /// cassée plutôt que comme une liste à remplir. L'historique, lui, dit
        /// depuis toujours qu'il n'a pas d'enregistrement.
        /// </summary>
        public bool Vide => Rooms.Count == 0;
        [ObservableProperty]
        private string _newRoomUrl = "";
        [ObservableProperty]
        private string _addError = "";

        /// <summary>
        /// Un SEUL coordinateur pour toute la vue, et non un par carte : il
        /// tient la table des enregistrements en cours, qui est justement ce
        /// qui empêche d'ouvrir deux captures du même salon.
        /// </summary>
        private readonly RecordingCoordinator _enregistrement = new();

        /// <summary>
        /// Le panneau des journaux. Il s'abonne a Logger, qui est STATIQUE :
        /// c'est pour cela qu'il est libere par Dispose ci-dessous.
        /// </summary>
        public LogPanelViewModel Journaux { get; } = new();

        public StreamsViewModel()
        {
            // `Recharger` vide et reconstruit la liste ENTIERE a chaque ajout et
            // chaque retrait : sans cet abonnement, le message d'etat vide
            // resterait affiche par-dessus le premier salon ajoute.
            Rooms.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Vide));

            _store.Load();
            Recharger();

            // « Illimite » par defaut, jamais null : une liste deroulante vide
            // se lit comme un reglage manquant, et `DureeChoisie?.Minutes ?? 0`
            // donnerait le meme resultat sans que l'ecran le dise.
            DureeChoisie = Durees[0];

            // La FONCTION est relue à chaque tour de surveillance : un salon
            // dont on arme l'interrupteur entre deux tours est pris au suivant,
            // sans redémarrer quoi que ce soit.
            _surveillance = new MonitorService(() =>
                Rooms.Where(c => c.AutoRecord).Select(c => c.Url).ToList());

            // Les évènements arrivent déjà marshalés sur le fil d'interface :
            // le coordinateur s'en charge une fois pour tous ses abonnés.
            _enregistrement.EtatChange += (url, etat) =>
            {
                if (Trouver(url) is not { } carte) return;
                carte.State = RecordingLabels.EtatDeCarte(etat);
                carte.StateLabel = RecordingLabels.Libelle(etat);
                carte.IsRecording = _enregistrement.EnCours(url);
                // La progression n'a plus de sens une fois la capture finie, et
                // une barre laissée à 43 % se lit comme un enregistrement figé.
                if (!carte.IsRecording)
                {
                    carte.Progress = 0;
                    carte.Indeterminate = false;
                    // Le decompte rendait la place : on remet la plateforme et
                    // la date, sinon la carte garderait « 0 s » pour toujours.
                    carte.Detail = carte.DetailBase;
                }
                else
                {
                    // AU DEMARRAGE AUSSI, et pas seulement a la reception d'une
                    // progression : un direct n'en rend souvent aucune, et la
                    // barre restait alors vide pendant toute la capture.
                    carte.Indeterminate = RecordingLabels.BarreIndeterminee(true);
                }
            };

            _enregistrement.Progression += (url, pourcent) =>
            {
                if (Trouver(url) is not { } carte) return;
                carte.Progress = (int)pourcent;
                // Un direct n'a pas de fin connue : yt-dlp rend souvent 0 %.
                // Une barre indéterminée dit « ça travaille » sans mentir sur
                // une progression que personne ne peut calculer.
                carte.Indeterminate = RecordingLabels.BarreIndeterminee(true);
            };

            _enregistrement.ReconnexionProgrammee += (url, secondes, tentative, total) =>
            {
                if (Trouver(url) is not { } carte) return;

                // La carte reste EN CHARGE du salon : `IsRecording` n'est pas
                // remis à faux, parce que le bouton doit rester « Arrêter » —
                // c'est le seul moyen d'annuler une reconnexion en attente.
                carte.State = RoomRowState.Reconnecting;
                carte.StateLabel = Localization.Format("job.reconnectIn", secondes);
                carte.Indeterminate = true;
                carte.Progress = 0;
            };

            _enregistrement.Decompte += (url, restant) =>
            {
                if (Trouver(url) is not { } carte) return;
                carte.Detail = $"{carte.DetailBase} — {restant}";
            };

            // La surveillance est lancee EN DERNIER : elle peut declencher un
            // enregistrement des le premier tour, et les abonnements ci-dessus
            // doivent donc etre en place avant qu'elle ne parte.
            DemarrerLaSurveillance();
        }

        /// <summary>
        /// La surveillance. Elle ne sonde QUE les salons dont l'interrupteur
        /// « auto » est armé — c'est la règle du produit : une ligne n'est
        /// qu'un salon connu, et sonder tout le monde ferait interroger le site
        /// pour chaque favori, ce que la séparation favoris/surveillance
        /// évitait justement avant leur fusion.
        /// </summary>
        private readonly MonitorService _surveillance;

        /// <summary>Une duree proposee au demarrage d'un enregistrement.</summary>
        public sealed record OptionDuree(string Libelle, int Minutes)
        {
            /// <summary>
            /// Le ToString() genere d'un record liste tous ses membres — un
            /// format de DEBOGAGE, pas d'affichage. Sans cette redefinition la
            /// liste deroulante montrait « OptionDuree { Libelle = Illimite,
            /// Minutes = 0 } », defaut vu a la capture et invisible autrement.
            /// </summary>
            public override string ToString() => Libelle;
        }

        /// <summary>
        /// Les sept durees du WinForms, reprises telles quelles depuis
        /// <see cref="RecordingTimer.PresetMinutes"/> — la liste des valeurs et
        /// celle des libelles sont donc INDEXEES ENSEMBLE et ne peuvent pas
        /// diverger, ce qu'un second tableau ecrit a la main aurait permis.
        /// </summary>
        public IReadOnlyList<OptionDuree> Durees { get; } = new[]
        {
            new OptionDuree(Localization.Get("duration.unlimited"), 0),
            new OptionDuree(Localization.Get("duration.15min"), 15),
            new OptionDuree(Localization.Get("duration.30min"), 30),
            new OptionDuree(Localization.Get("duration.1h"), 60),
            new OptionDuree(Localization.Get("duration.2h"), 120),
            new OptionDuree(Localization.Get("duration.4h"), 240),
            new OptionDuree(Localization.Get("duration.8h"), 480),
        };

        /// <summary>
        /// Duree appliquee aux PROCHAINS demarrages. « Illimite » par defaut :
        /// borner une capture que personne n'a demande de borner serait le pire
        /// defaut possible pour un enregistreur.
        /// </summary>
        [ObservableProperty]
        private OptionDuree? _dureeChoisie;

        /// <summary>
        /// Branche la surveillance sur les cartes. Appelé par le constructeur,
        /// après que la liste est peuplée.
        /// </summary>
        private void DemarrerLaSurveillance()
        {
            _surveillance.IntervalleSecondes = SettingsManager.Load().WatchIntervalSeconds;

            _surveillance.StatutObtenu += (url, statut) =>
            {
                if (Trouver(url) is not { } carte) return;

                // `RoomStore.Resolve` porte une règle qu'il ne faut PAS
                // réécrire ici : l'ENREGISTREMENT PRIME SUR LE SONDAGE. Un
                // sondage peut échouer pendant que la capture reçoit des
                // données — afficher « hors ligne » sur un salon en cours
                // d'enregistrement serait le pire des contresens.
                carte.State = RoomStore.Resolve(
                    statut,
                    carte.IsRecording ? DownloadState.Running : null,
                    reconnexionPrevue: false);

                if (!carte.IsRecording)
                    carte.StateLabel = statut switch
                    {
                        RoomStatus.Online => Localization.Get("watch.state.online"),
                        RoomStatus.Offline => Localization.Get("watch.state.offline"),
                        RoomStatus.NotFound => Localization.Get("watch.state.notfound"),
                        _ => Localization.Get("watch.state.unknown"),
                    };

                // LE DÉCLENCHEMENT AUTOMATIQUE. Trois conditions, et les trois
                // comptent : l'interrupteur armé, le salon réellement en ligne
                // (jamais `Unknown`, qui veut dire « on n'a pas pu savoir »), et
                // aucune capture déjà en cours pour ce salon.
                if (carte.AutoRecord && statut == RoomStatus.Online && !carte.IsRecording)
                    BasculerCommand.Execute(carte);

                // La vignette premium se rafraichit a chaque tour ou le salon
                // est vu en ligne, en tache de fond (le `_ =`) : ce lambda
                // traite aussi les autres salons du meme tour de surveillance,
                // et l'appel enchaine yt-dlp puis ffmpeg -- plusieurs secondes
                // qu'il ne faut pas faire attendre au reste de la boucle.
                if (statut == RoomStatus.Online) _ = RafraichirApercuAsync(carte);
            };

            _surveillance.Demarrer();
        }

        /// <summary>
        /// Arrête la surveillance. Sans cet appel, la boucle survivrait à la
        /// fenêtre et continuerait de lancer un yt-dlp par salon armé, toutes
        /// les deux minutes, sans que rien ne l'affiche.
        /// </summary>
        public void Dispose()
        {
            // **LES CAPTURES D'ABORD.** Sans cet appel, fermer l'application
            // laissait ses yt-dlp EN VIE : ils continuaient d'ecrire dans leur
            // `.part` et plus personne n'etait la pour le renommer. Constate
            // apres le premier essai sur un vrai direct — quatre processus
            // orphelins alors qu'aucune fenetre n'etait ouverte.
            _enregistrement.ArreterTout();

            _surveillance.Dispose();
            Journaux.Dispose();
            foreach (var carte in Rooms) carte.Detach();
        }

        private RoomCardViewModel? Trouver(string url) =>
            Rooms.FirstOrDefault(c => string.Equals(c.Url, url, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Demande une vignette au composant premium pour ce salon et la pose
        /// sur la carte. Silencieuse en cas d'échec — pas de licence, pas de
        /// composant, flux qui ne rend rien — car c'est le cas de la
        /// quasi-totalité des utilisateurs, ce n'est pas une anomalie.
        /// </summary>
        private async Task RafraichirApercuAsync(RoomCardViewModel carte)
        {
            if (!App.Premium.IsLicensed) return;

            // Nom de fichier dérivé de l'URL normalisée, comme côté WinForms :
            // un nom de salon peut contenir n'importe quoi, y compris des
            // caractères interdits en chemin.
            var cle = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(RoomStore.Normalize(carte.Url))))[..16];
            var chemin = Path.Combine(AppConfig.PreviewDir, $"{cle}.jpg");

            var url = carte.Url;
            var ok = await Task.Run(() => App.Premium.TryCapturePreview(url, chemin)).ConfigureAwait(true);
            if (!ok || Trouver(url) is null) return;

            // Le chemin est DETERMINISTE (meme cle SHA-256 a chaque tour) :
            // reposer la MEME chaine ne leverait aucun PropertyChanged
            // (SetProperty compare par egalite), et l'image resterait celle
            // du tour precedent meme si le fichier vient d'etre reecrit sur
            // le disque. Le passage par null force les deux notifications.
            carte.CheminApercu = null;
            carte.CheminApercu = chemin;
        }

        /// <summary>
        /// Un seul bouton pour les deux actions : « Démarrer » quand rien ne
        /// tourne, « Arrêter » sinon. Deux boutons dont un toujours inactif
        /// prendraient la place de la carte sans rien apprendre.
        /// </summary>
        [RelayCommand]
        private void Basculer(RoomCardViewModel? carte)
        {
            if (carte is null) return;

            if (_enregistrement.EnCours(carte.Url))
            {
                _enregistrement.Arreter(carte.Url);
                return;
            }

            try
            {
                // La reconnexion suit le réglage persisté, comme en WinForms
                // (`UserSettings.AutoReconnectDefault`). Le minuteur reste à 0 :
                // le choix de durée par enregistrement n'a pas encore sa place
                // dans l'interface, et poser une valeur en dur couperait des
                // captures que personne n'a demandé de borner.
                _enregistrement.Demarrer(
                    carte.Url,
                    reconnexionAuto: SettingsManager.Load().AutoReconnectDefault,
                    minutesMinuteur: DureeChoisie?.Minutes ?? 0);
                carte.IsRecording = true;
            }
            catch (Exception ex)
            {
                // L'échec est dit SUR LA CARTE et non dans une boîte de dialogue :
                // c'est cette ligne-là qui n'a pas démarré, et une modale
                // obligerait à la refermer avant de pouvoir en lancer une autre.
                carte.State = RoomRowState.Failed;
                carte.StateLabel = ex.Message;
            }
        }

        public void Recharger()
        {
            foreach (var carte in Rooms.ToList())
            {
                carte.Detach();
            }
            Rooms.Clear();
            foreach (var entree in _store.Rooms)
            {
                Rooms.Add(Creer(entree));
            }
        }

        private static RoomCardViewModel Creer(RoomEntry entree)
        {
            var carte = new RoomCardViewModel(entree)
            {
                RoomName = Platforms.DisplayName(entree.Url),
                PlatformIconKey = CleDePictogramme(Platforms.Badge(Platforms.Detect(entree.Url)).Icon)
            };
            carte.DetailBase = Platforms.Badge(Platforms.Detect(entree.Url)).Label;
            carte.Detail = carte.DetailBase;
            return carte;
        }

        private static string CleDePictogramme(string icon)
        {
            return icon switch
            {
                "twitch" => "Icon.Twitch",
                "youtube" => "Icon.YouTube",
                "tiktok" => "Icon.TikTok",
                _ => "Icon.Camera"
            };
        }

        [RelayCommand]
        private void Ajouter()
        {
            // LES PROPRIETES, PAS LES CHAMPS, et ici ce n'etait pas cosmetique :
            // ecrire `_addError` court-circuitait la propriete generee, donc
            // AUCUNE notification n'etait levee — le message de refus du bac a
            // sable ne se serait JAMAIS affiche. L'analyseur du toolkit le
            // signalait (MVVMTK0034) ; l'avertissement disait vrai.
            if (string.IsNullOrWhiteSpace(NewRoomUrl))
            {
                return;
            }

            if (!UrlValidator.IsSafeUrl(NewRoomUrl, AppConfig.Whitelist, AppConfig.Blacklist, out var motif))
            {
                AddError = motif ?? Localization.Get("error.invalidUrl");
                return;
            }

            AddError = "";
            _store.Add(NewRoomUrl);
            _store.Save();
            Recharger();
            NewRoomUrl = "";
        }

        [RelayCommand]
        private void Retirer(RoomCardViewModel? carte)
        {
            if (carte == null)
            {
                return;
            }

            _store.Remove(carte.Url);
            _store.Save();
            Recharger();
        }

        /// <summary>
        /// Ouvre la vidéo en direct du salon (v2 premium) — clic sur la
        /// vignette. En tâche de fond comme <see cref="RafraichirApercuAsync"/> :
        /// résoudre le flux est un appel réseau bloquant, et App.Premium se
        /// charge lui-même de revenir sur le fil d'interface pour la fenêtre.
        /// </summary>
        [RelayCommand]
        private void VoirEnDirect(RoomCardViewModel? carte)
        {
            if (carte is null) return;
            _ = Task.Run(() => App.Premium.TryShowLiveWindow(carte.Url, carte.RoomName));
        }
    }
}