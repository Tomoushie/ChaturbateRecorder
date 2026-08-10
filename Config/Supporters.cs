using System;

namespace ChaturbateRecorderApp.Config
{
    /// <summary>
    /// Liste embarquée des personnes ayant soutenu le projet (104.0), socle
    /// hors ligne de la fenêtre "Remerciements".
    ///
    /// **Deux sources, volontairement** : celle-ci part avec la version et
    /// s'affiche donc sans connexion ; <c>docs/supporters.json</c>, servi par
    /// le site, permet d'ajouter quelqu'un sans attendre une release. Le
    /// service fusionne les deux (voir <c>Services/SupportersProvider.cs</c>).
    /// Le geste à faire lors d'un bump est de recopier ici ce que contient le
    /// JSON du site, pour que les deux ne divergent pas indéfiniment.
    ///
    /// **AUCUN MONTANT, jamais**, et pas davantage un ordre qui en tiendrait
    /// lieu : la liste est triée alphabétiquement à l'affichage, précisément
    /// pour qu'on ne puisse pas lire un classement là où il n'y en a pas.
    ///
    /// **Consentement** : n'inscrire ici un nom qu'avec l'accord de la
    /// personne, et un pseudonyme plutôt que son état civil. Un don PayPal
    /// transporte le nom réel du payeur ; le publier sans le lui avoir demandé
    /// serait une divulgation, pas un remerciement.
    /// </summary>
    public static class Supporters
    {
        /// <summary>
        /// Vide pour l'instant : personne n'a encore donné. La fenêtre affiche
        /// dans ce cas un état vide explicite plutôt qu'un cadre blanc.
        /// </summary>
        public static readonly string[] Embedded = Array.Empty<string>();
    }
}
