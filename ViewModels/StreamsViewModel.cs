namespace ChaturbateRecorderApp.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
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

        public StreamsViewModel()
        {
            _store.Load();
            Recharger();

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
                if (!carte.IsRecording) { carte.Progress = 0; carte.Indeterminate = false; }
            };

            _enregistrement.Progression += (url, pourcent) =>
            {
                if (Trouver(url) is not { } carte) return;
                carte.Progress = (int)pourcent;
                // Un direct n'a pas de fin connue : yt-dlp rend souvent 0 %.
                // Une barre indéterminée dit « ça travaille » sans mentir sur
                // une progression que personne ne peut calculer.
                carte.Indeterminate = pourcent <= 0;
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
            _surveillance.Dispose();
            foreach (var carte in Rooms) carte.Detach();
        }

        private RoomCardViewModel? Trouver(string url) =>
            Rooms.FirstOrDefault(c => string.Equals(c.Url, url, StringComparison.OrdinalIgnoreCase));

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
                _enregistrement.Demarrer(carte.Url);
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
            if (string.IsNullOrWhiteSpace(_newRoomUrl))
            {
                return;
            }

            if (!UrlValidator.IsSafeUrl(_newRoomUrl, AppConfig.Whitelist, AppConfig.Blacklist, out var motif))
            {
                _addError = motif ?? Localization.Get("error.invalidUrl");
                return;
            }

            _addError = "";
            _store.Add(_newRoomUrl);
            _store.Save();
            Recharger();
            _newRoomUrl = "";
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
    }
}