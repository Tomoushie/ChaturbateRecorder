using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Les classes de test qui touchent un état de PROCESSUS, sérialisées.
    ///
    /// xunit lance les classes en parallèle, ce qui est bon marché et souhaitable
    /// — sauf pour trois singletons que ce projet ne peut pas éviter :
    ///
    /// - **`Application.Current`**, et surtout son `Dispatcher` : il appartient au
    ///   FIL qui l'a créé. Deux classes rendant du WPF sur deux fils STA
    ///   différents se disputent le même objet, et le symptôme n'est pas une
    ///   erreur mais un BLOCAGE — la suite ne rend jamais la main. Constaté :
    ///   `RenduVisuelTests` et `LocalizationBindingTests` passaient chacune
    ///   isolément et figeaient le processus une fois réunies.
    /// - **`Localization.Current`**, que deux classes basculent.
    /// - **`AppConfig.DataDir`**, que les tests d'emplacement déplacent.
    ///
    /// Une seule collection pour les quatre classes plutôt qu'une par état :
    /// elles se recoupent (le rendu lit la langue, qui lit les réglages), et
    /// trois collections distinctes laisseraient justement passer les
    /// combinaisons qui posent problème.
    /// </summary>
    [CollectionDefinition("EtatDeProcessus")]
    public class CollectionEtatDeProcessus { }
}
