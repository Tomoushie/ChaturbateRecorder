using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.ViewModels
{
    public sealed class EntreeVersion
    {
        public required string Titre { get; init; }
        public required IReadOnlyList<string> Changements { get; init; }
    }

    public sealed class ChangelogViewModel
    {
        public ObservableCollection<EntreeVersion> Versions { get; } = new();
        public string Titre { get; } = Localization.Get("changelog.title");
        public bool Vide => Versions.Count == 0;

        /// <summary>
        /// Les deux versions sont passées par l'appelant et non lues ici : c'est MainWindow qui sait ce que l'utilisateur avait déjà vu (UserSettings.LastSeenVersion) et où en est l'application. Le modèle de vue ne fait que mettre en forme.
        /// </summary>
        /// <param name="depuisVersion">Version depuis laquelle les changements sont récupérés</param>
        /// <param name="jusquaVersion">Version jusqu'à laquelle les changements sont récupérés</param>
        public ChangelogViewModel(string? depuisVersion, string jusquaVersion)
        {
            var anglais = Localization.Current == AppLanguage.English;
            var changements = Changelog.GetChangesSince(depuisVersion, jusquaVersion, anglais);

            foreach (var entree in changements)
            {
                var titre = Localization.Format("changelog.versionHeader", entree.Version);
                var changementsFormates = entree.Changes ?? new[] { Localization.Get("changelog.noDetails") };
                Versions.Add(new EntreeVersion { Titre = titre, Changements = changementsFormates });
            }
        }
    }
}