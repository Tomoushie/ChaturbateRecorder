using QRCoder;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Génère un QR EPC/SEPA (« Girocode », norme EPC069-12 v2) pour un
    /// virement bancaire — scannable par la plupart des applications
    /// bancaires européennes, qui pré-remplissent bénéficiaire, IBAN, BIC et
    /// communication automatiquement plutôt que de faire recopier l'IBAN à
    /// la main (120.0, bouton Premium).
    ///
    /// **Montant volontairement OMIS** (ligne vide, valide dans le standard) :
    /// le prix réel vit sur la page itch.io (<see cref="Config.AppConfig.ProUrl"/>),
    /// le dupliquer ici l'exposerait à diverger silencieusement d'un futur
    /// changement de prix — ce QR ne fait que désigner le compte, pas figer
    /// une somme.
    /// </summary>
    internal static class EpcQrGenerator
    {
        /// <summary>
        /// PNG en mémoire, prêt pour un <c>BitmapImage</c> WPF (via
        /// <c>MemoryStream</c>, voir le point d'usage dans
        /// <c>Views/PremiumUpgradeWindow.xaml.cs</c>).
        ///
        /// <paramref name="bic"/> peut être vide : un virement SEPA identifie
        /// déjà la banque par l'IBAN seul depuis 2016, le BIC n'est plus
        /// obligatoire au sein de l'UE/EEE — c'est le cas du premier des deux
        /// comptes du mainteneur, qui n'a donné que l'IBAN.
        /// </summary>
        internal static byte[] GenererPng(string beneficiaire, string iban, string? bic, string communication)
        {
            // Ordre et nombre de lignes IMPOSÉS par la norme (EPC069-12 v2) :
            // service/version/jeu de caractères/identification SCT/BIC/nom/
            // IBAN/montant/motif/communication structurée/communication libre.
            // Une ligne manquante ou déplacée rend un QR qui scanne mais que
            // l'appli bancaire du payeur refuse de comprendre.
            var lignes = new[]
            {
                "BCD",
                "002",
                "1", // jeu de caractères : 1 = UTF-8
                "SCT",
                bic ?? "",
                Tronquer(beneficiaire, 70),
                iban.Replace(" ", ""),
                "", // montant : volontairement ouvert, voir le commentaire de classe
                "", // motif (code ISO), non utilisé
                "", // communication STRUCTURÉE (référence bancaire), non utilisée
                Tronquer(communication, 140),
            };
            var payload = string.Join("\n", lignes);

            using var generateur = new QRCodeGenerator();
            // ECC de niveau M : niveau recommandé par la spécification EPC
            // pour un Girocode, ni le plus dense (L) ni le plus lourd (H).
            using var donnees = generateur.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var png = new PngByteQRCode(donnees);
            return png.GetGraphic(8);
        }

        private static string Tronquer(string valeur, int max) =>
            valeur.Length <= max ? valeur : valeur[..max];
    }
}
