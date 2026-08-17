using System;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// La table de traduction vue comme une source de liaison.
    ///
    /// L'indexeur rend la chaîne, et <see cref="Localization.LanguageChanged"/>
    /// fait annoncer « Item[] » — le nom que WPF réserve aux indexeurs, et qui
    /// réévalue TOUTES les liaisons indexées d'un coup. C'est ce qui rend le
    /// changement de langue immédiat sans qu'aucune vue n'ait à s'en occuper :
    /// la promesse écrite dans les Réglages (« thème et langue s'appliquent
    /// immédiatement ») tient alors d'elle-même.
    /// </summary>
    public sealed class LocalizationSource : INotifyPropertyChanged
    {
        public static LocalizationSource Instance { get; } = new();

        private LocalizationSource()
        {
            Localization.LanguageChanged += () =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        /// <summary>
        /// Une clé inconnue rend la clé elle-même (contrat de
        /// <see cref="Localization.Get(string)"/>) : la faute se voit à l'écran
        /// au lieu de laisser un blanc, et rien ne lève.
        /// </summary>
        public string this[string cle] => Localization.Get(cle);

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// <c>Text="{ui:Str settings.captureFolder}"</c> dans le XAML.
    ///
    /// Rend une LIAISON et non la chaîne : une extension de balisage n'est
    /// évaluée qu'une fois, au chargement, et rendre le texte directement
    /// figerait la langue du démarrage. C'est précisément le piège qu'on vient
    /// de payer autrement — soixante et un libellés écrits en dur, dont aucun
    /// ne suivait la case « Français ».
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class StrExtension : MarkupExtension
    {
        public StrExtension() { }

        public StrExtension(string cle) => Cle = cle;

        /// <summary>Clé de la table, telle qu'écrite dans Localization.</summary>
        public string Cle { get; set; } = "";

        public override object ProvideValue(IServiceProvider fournisseur)
        {
            var liaison = new Binding
            {
                // Les crochets désignent l'indexeur. La clé porte des points
                // (« settings.captureFolder ») : ils sont ici littéraux, la
                // lecture de chemin ne les prend pas pour des sauts de
                // propriété tant qu'ils sont entre crochets.
                Path = new System.Windows.PropertyPath("[" + Cle + "]"),
                Source = LocalizationSource.Instance,
                Mode = BindingMode.OneWay,
            };
            return liaison.ProvideValue(fournisseur);
        }
    }
}
