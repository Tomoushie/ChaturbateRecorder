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
    /// lieu. Deux ordres sont acceptables (décision du mainteneur) :
    /// alphabétique — celui qui est appliqué, par
    /// <c>Services/SupportersProvider.cs</c> — ou chronologique par date de
    /// don. Tout autre ordre laisserait croire à un classement.
    ///
    /// **PSEUDONYME, jamais nom + prénom** (règle du mainteneur) : le pseudo
    /// GitHub s'il existe, à défaut celui que la personne utilise. Un don
    /// PayPal transporte son état civil ; le publier serait une divulgation,
    /// pas un remerciement. Et n'inscrire personne sans son accord.
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
