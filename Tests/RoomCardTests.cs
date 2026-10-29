using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using ChaturbateRecorderApp.ViewModels;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// La carte de salon : ce qu'elle MONTRE de son état, et ce qu'elle
    /// notifie.
    ///
    /// `RoomStore.ResolveState` — quel état découle de quel sondage — est déjà
    /// éprouvé ailleurs. Ce qui suit reprend juste après : l'état est décidé,
    /// reste à savoir si l'écran l'apprend. Un affichage figé sur l'état
    /// précédent ne fait échouer aucun build et ne lève aucune exception ; il
    /// se voit uniquement sur un vrai bureau, c'est-à-dire par le mainteneur.
    /// </summary>
    public class RoomCardTests
    {
        private static RoomCardViewModel Carte(string url = "https://chaturbate.com/salon/") =>
            new(new RoomEntry { Url = url, AutoRecord = false, AddedUtc = DateTime.UtcNow });

        private static IReadOnlyList<RoomRowState> TousLesEtats =>
            Enum.GetValues<RoomRowState>();

        // --- Ce que la carte montre ---------------------------------------

        /// <summary>
        /// Seuls les états qui ont une progression MESURABLE ouvrent la carte.
        /// « Terminé » et « échec » n'ont rien à montrer : les garder hauts
        /// gaspille la place au moment même où on parcourt sa liste.
        /// </summary>
        [Theory]
        [InlineData(RoomRowState.Recording, true)]
        [InlineData(RoomRowState.Reconnecting, true)]
        [InlineData(RoomRowState.Idle, false)]
        [InlineData(RoomRowState.Unknown, false)]
        [InlineData(RoomRowState.Live, false)]
        [InlineData(RoomRowState.Finished, false)]
        [InlineData(RoomRowState.Failed, false)]
        [InlineData(RoomRowState.NotFound, false)]
        public void SeulsLesEtatsAProgressionOuvrentLaCarte(RoomRowState etat, bool ouverte)
        {
            Assert.Equal(ouverte, RoomCardVisuals.IsExpanded(etat));
            Assert.Equal(
                ouverte ? RoomCardVisuals.ExpandedHeight : RoomCardVisuals.CompactHeight,
                RoomCardVisuals.HeightFor(etat));
        }

        /// <summary>
        /// « Inexistant » est un échec DÉFINITIF, pas une panne passagère : sans
        /// la couleur d'alerte, une faute de frappe dans l'URL se confondrait
        /// avec un salon simplement hors ligne, et on l'attendrait indéfiniment.
        /// </summary>
        [Theory]
        [InlineData(RoomRowState.Live, ThemeManager.BrushSuccess)]
        [InlineData(RoomRowState.Recording, ThemeManager.BrushAccent)]
        [InlineData(RoomRowState.Reconnecting, ThemeManager.BrushWarning)]
        [InlineData(RoomRowState.Failed, ThemeManager.BrushDanger)]
        [InlineData(RoomRowState.NotFound, ThemeManager.BrushDanger)]
        [InlineData(RoomRowState.Idle, ThemeManager.BrushFgMuted)]
        [InlineData(RoomRowState.Unknown, ThemeManager.BrushFgMuted)]
        [InlineData(RoomRowState.Finished, ThemeManager.BrushFgMuted)]
        public void ChaqueEtatALaCouleurQuiLeDit(RoomRowState etat, string cleAttendue)
        {
            Assert.Equal(cleAttendue, RoomCardVisuals.StateBrushKey(etat));
        }

        /// <summary>
        /// Le filet contre l'ajout d'un état : le `switch` de `StateBrushKey`
        /// se termine par un `_ =>` qui rattrape TOUT en silence. Ajouter un
        /// neuvième état sans lui donner sa couleur ne casserait donc rien — il
        /// s'afficherait simplement gris, ce qu'aucun test nominal ne verrait.
        /// Ici, le compte fait foi.
        /// </summary>
        [Fact]
        public void AucunEtatNAEteAjouteSansCouleurNiHauteur()
        {
            Assert.Equal(8, TousLesEtats.Count);
        }

        /// <summary>
        /// Toute clé rendue doit être une clé DÉCLARÉE du thème. Une chaîne
        /// inventée ne se verrait ni au build ni au test — WPF rendrait la
        /// carte sans son pinceau, ou lèverait à l'affichage.
        /// </summary>
        [Fact]
        public void ToutesLesClesRenduesExistentDansLeTheme()
        {
            var declarees = typeof(ThemeManager)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("Brush"))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToHashSet(StringComparer.Ordinal);

            Assert.All(TousLesEtats, etat =>
                Assert.Contains(RoomCardVisuals.StateBrushKey(etat), declarees));
        }

        // --- Ce que la carte notifie --------------------------------------

        /// <summary>
        /// Trois propriétés DÉRIVENT de `State` sans champ à elles. Poser
        /// l'état doit donc notifier les quatre : WPF ne relit que ce qu'on lui
        /// annonce, et un oubli laisserait la carte à sa couleur et à sa
        /// hauteur précédentes — exactement l'oubli commis en 97.0, où deux
        /// couleurs ajoutées à la palette n'avaient pas été ajoutées au fondu.
        /// </summary>
        [Fact]
        public void PoserLEtatNotifieToutCeQuiEnDerive()
        {
            var carte = Carte();
            var notifiees = new List<string>();
            carte.PropertyChanged += (s, e) => notifiees.Add(e.PropertyName ?? "");

            carte.State = RoomRowState.Recording;

            Assert.Contains(nameof(RoomCardViewModel.State), notifiees);
            Assert.Contains(nameof(RoomCardViewModel.StateBrushKey), notifiees);
            Assert.Contains(nameof(RoomCardViewModel.IsExpanded), notifiees);
            Assert.Contains(nameof(RoomCardViewModel.CardHeight), notifiees);
            carte.Detach();
        }

        [Fact]
        public void ReposerLeMemeEtatNeNotifieRien()
        {
            var carte = Carte();
            carte.State = RoomRowState.Live;

            var notifiees = new List<string>();
            carte.PropertyChanged += (s, e) => notifiees.Add(e.PropertyName ?? "");
            carte.State = RoomRowState.Live;

            Assert.Empty(notifiees);
            carte.Detach();
        }

        [Fact]
        public void LesTroisDeriveesSuiventLEtat()
        {
            var carte = Carte();
            foreach (var etat in TousLesEtats)
            {
                carte.State = etat;
                Assert.Equal(RoomCardVisuals.StateBrushKey(etat), carte.StateBrushKey);
                Assert.Equal(RoomCardVisuals.IsExpanded(etat), carte.IsExpanded);
                Assert.Equal(RoomCardVisuals.HeightFor(etat), carte.CardHeight);
            }
            carte.Detach();
        }

        /// <summary>
        /// L'interrupteur « auto » écrit DANS l'entrée, qui est ce que le disque
        /// enregistre. Sans cette écriture, basculer l'interrupteur agirait
        /// jusqu'à la fermeture puis serait perdu — une surveillance armée qui
        /// ne repart pas au démarrage suivant.
        /// </summary>
        [Fact]
        public void LInterrupteurAutoEcritDansLEntree()
        {
            var entree = new RoomEntry { Url = "https://chaturbate.com/s/", AutoRecord = false, AddedUtc = DateTime.UtcNow };
            var carte = new RoomCardViewModel(entree);

            carte.AutoRecord = true;
            Assert.True(entree.AutoRecord);

            carte.AutoRecord = false;
            Assert.False(entree.AutoRecord);
            carte.Detach();
        }

        [Fact]
        public void LaCarteNaitAvecLEtatDeSonEntree()
        {
            var entree = new RoomEntry { Url = "https://chaturbate.com/s/", AutoRecord = true, AddedUtc = DateTime.UtcNow };
            var carte = new RoomCardViewModel(entree);

            Assert.True(carte.AutoRecord);
            Assert.Equal(entree.Url, carte.Url);
            Assert.Equal(RoomRowState.Idle, carte.State);
            carte.Detach();
        }

        /// <summary>
        /// Les trois champs que le compilateur signalait en CS8618 : la carte
        /// est construite AVANT que la plateforme soit résolue et l'état
        /// libellé. Ils doivent donc naître vides et non null — une liaison sur
        /// null passe encore, mais toute lecture de longueur ou de préfixe
        /// lèverait, et le premier rafraîchissement lit `DetailBase`.
        /// </summary>
        [Fact]
        public void LesTextesDeLaCarteNaissentVidesEtNonNuls()
        {
            var carte = Carte();

            Assert.NotNull(carte.Detail);
            Assert.NotNull(carte.StateLabel);
            Assert.NotNull(carte.PlatformIconKey);
            Assert.NotNull(carte.DetailBase);
            carte.Detach();
        }

        // --- La barre de progression ---------------------------------------

        /// <summary>
        /// **LE DÉFAUT TROUVÉ SUR UN VRAI DIRECT**, le premier jamais capturé
        /// par l'application WPF.
        ///
        /// DEUX mauvaises réponses ont précédé celle-ci, vues toutes les deux
        /// sur un vrai direct. D'abord l'indétermination n'était posée qu'à la
        /// réception d'une progression : la barre restait VIDE. Puis, une fois
        /// posée au démarrage mais conditionnée au pourcentage, elle restait
        /// PLEINE — yt-dlp rend 100 % à chaque fragment d'un direct.
        ///
        /// La règle juste est celle du WinForms, et elle est plus simple que
        /// mes deux tentatives : la barre défile tant que la capture tourne, le
        /// pourcentage ne servant qu'à la pose finale.
        /// </summary>
        [Theory]
        [InlineData(true, true)]    // une capture tourne : la barre defile, point
        [InlineData(false, false)]  // rien ne tourne : pas d'animation trompeuse
        public void LaBarreDefileTantQueLaCaptureTourne(bool enCours, bool attendu)
        {
            Assert.Equal(attendu, RecordingLabels.BarreIndeterminee(enCours));
        }

        // --- La fuite ------------------------------------------------------

        /// <summary>
        /// Chaque carte s'abonne à `ThemeManager.Applied`, qui est STATIQUE :
        /// il survit à la carte, à la liste et à la fenêtre. `Recharger` reconstruit
        /// la liste ENTIÈRE à chaque ajout et à chaque retrait — sans `Detach`,
        /// dix ajouts laisseraient dix listes de cartes mortes abonnées à vie,
        /// toutes repeintes à chaque changement de thème.
        ///
        /// Rien ne le rendrait visible : le programme fonctionnerait, en
        /// gardant simplement de plus en plus de mémoire.
        ///
        /// Ce test compte un état GLOBAL, et xunit lance les classes de test en
        /// parallèle : il ne tient que parce que `RoomCardViewModel` et
        /// `WindowChrome` sont les deux seuls abonnés, et que le second exige
        /// une `Window`. Une future classe de test qui construit une carte le
        /// rendrait intermittent — la sérialiser alors, pas retirer le test.
        /// </summary>
        [Fact]
        public void DetachRendLAbonnementAuTheme()
        {
            var avant = AbonnesAuTheme();

            var cartes = Enumerable.Range(0, 5)
                .Select(i => Carte($"https://chaturbate.com/salon{i}/"))
                .ToList();
            Assert.Equal(avant + 5, AbonnesAuTheme());

            foreach (var c in cartes) c.Detach();
            Assert.Equal(avant, AbonnesAuTheme());
        }

        /// <summary>
        /// Nombre d'abonnés à l'évènement statique `ThemeManager.Applied`. Lu
        /// par réflexion sur le CHAMP qui porte l'évènement : un évènement
        /// « champ » n'expose aucun moyen de compter ses abonnés, et le seul
        /// autre moyen de constater la fuite serait de mesurer la mémoire.
        /// </summary>
        private static int AbonnesAuTheme()
        {
            var champ = typeof(ThemeManager).GetField(
                "Applied", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(champ);
            return (champ!.GetValue(null) as Action)?.GetInvocationList().Length ?? 0;
        }
    }
}
