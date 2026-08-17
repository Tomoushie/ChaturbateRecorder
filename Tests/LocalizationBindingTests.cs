using System;
using System.Threading;
using System.Windows.Controls;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le pont entre le XAML et la table de traduction.
    ///
    /// Il existe parce que soixante et un libellés étaient écrits EN DUR dans
    /// neuf vues : ils ne suivaient pas la case « Français », alors que le
    /// texte des Réglages promet que la langue s'applique immédiatement. Seule
    /// la barre de navigation changeait, parce qu'elle seule passait par la
    /// table.
    ///
    /// Ce que ces tests éprouvent est le point qui pouvait casser sans bruit :
    /// **une clé de la table porte des points** (« settings.captureFolder »), et
    /// un chemin de liaison lit normalement le point comme un saut de propriété.
    /// Entre crochets il est littéral — mais si ce n'était pas le cas, la
    /// liaison échouerait EN SILENCE, WPF se contentant d'une trace de
    /// débogage, et les soixante et un libellés s'afficheraient vides.
    /// </summary>
    [Collection("Localization")]
    public class LocalizationBindingTests
    {
        /// <summary>
        /// WPF exige un cloisonnement STA pour ses éléments, alors que xunit
        /// exécute sur des fils du pool, qui sont MTA.
        /// </summary>
        private static void SurFilStandard(Action action)
        {
            Exception? echec = null;
            var fil = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { echec = ex; }
            });
            fil.SetApartmentState(ApartmentState.STA);
            fil.Start();
            fil.Join();
            if (echec is not null) throw echec;
        }

        [Fact]
        public void LaSourceRendLaChaineDeLaLangueCourante()
        {
            var precedente = Localization.Current;
            try
            {
                Localization.Current = AppLanguage.French;
                Assert.Equal("Enregistrer", LocalizationSource.Instance["nav.streams"]);

                Localization.Current = AppLanguage.English;
                Assert.Equal("Record", LocalizationSource.Instance["nav.streams"]);
            }
            finally { Localization.Current = precedente; }
        }

        /// <summary>
        /// Une clé inconnue rend la clé : la faute se VOIT à l'écran au lieu de
        /// laisser un blanc, et rien ne lève. C'est ce qui rend une faute de
        /// frappe dans le XAML repérable d'un coup d'œil.
        /// </summary>
        [Fact]
        public void UneCleInconnueRendLaCleElleMeme()
        {
            Assert.Equal("cle.qui.nexiste.pas", LocalizationSource.Instance["cle.qui.nexiste.pas"]);
        }

        /// <summary>
        /// LE test de ce fichier : une clé à points traverse le chemin de
        /// liaison, et le changement de langue repeint sans que la vue s'en
        /// occupe.
        /// </summary>
        [Fact]
        public void UneCleAPointsSeLieEtSuitLeChangementDeLangue()
        {
            SurFilStandard(() =>
            {
                var precedente = Localization.Current;
                try
                {
                    Localization.Current = AppLanguage.French;

                    // La liaison est celle que l'extension construit, prise a
                    // sa source : hors contexte XAML, `ProvideValue` rend
                    // l'objet `Binding` lui-meme, qu'on pose ici a la main.
                    var bloc = new TextBlock();
                    var liaison = (Binding)new StrExtension("nav.settings")
                        .ProvideValue(new FournisseurNu())!;
                    bloc.SetBinding(TextBlock.TextProperty, liaison);

                    Assert.Equal("Réglages", bloc.Text);

                    Localization.Current = AppLanguage.English;
                    Assert.Equal("Settings", bloc.Text);
                }
                finally { Localization.Current = precedente; }
            });
        }

        /// <summary>
        /// L'extension appelée hors d'un contexte XAML : `ProvideValue` doit
        /// rendre une liaison, PAS la chaîne. Rendre la chaîne figerait la
        /// langue du démarrage — le défaut même qu'on répare.
        /// </summary>
        [Fact]
        public void LExtensionRendUneLiaisonEtNonUneChaine()
        {
            var rendu = new StrExtension("nav.settings").ProvideValue(new FournisseurNu());
            Assert.IsNotType<string>(rendu);
        }

        /// <summary>
        /// Fournisseur de services vide : hors XAML, `Binding.ProvideValue` rend
        /// l'objet de liaison lui-même plutôt que de le poser sur une cible.
        /// </summary>
        private sealed class FournisseurNu : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }

    /// <summary>
    /// `Localization.Current` est un état STATIQUE, et xunit lance les classes
    /// en parallèle. Cette collection sérialise les deux classes qui le
    /// basculent — sans quoi chacune verrait les bascules de l'autre, de façon
    /// intermittente et donc pénible à diagnostiquer.
    /// </summary>
    [CollectionDefinition("Localization")]
    public class CollectionLocalisation { }
}
