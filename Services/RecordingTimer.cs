using System;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Logique du minuteur d'enregistrement (87.0), isolée de l'interface pour
    /// être testable : conversion du choix de l'utilisateur en minutes, et mise
    /// en forme du temps restant affiché sur la ligne du job.
    /// </summary>
    public static class RecordingTimer
    {
        /// <summary>
        /// Durées proposées, dans l'ordre exact des entrées du menu déroulant.
        /// 0 = illimité (aucun minuteur), et c'est le choix par défaut : un
        /// minuteur actif à l'insu de l'utilisateur couperait un enregistrement
        /// sans raison apparente.
        /// </summary>
        public static readonly int[] PresetMinutes = { 0, 15, 30, 60, 120, 240, 480 };

        /// <summary>
        /// Minutes correspondant à l'index sélectionné dans le menu déroulant.
        /// Un index hors bornes (ou -1, ComboBox vide) rend 0 : en cas de doute
        /// on n'arrête rien plutôt que de couper un enregistrement.
        /// </summary>
        public static int MinutesForIndex(int index) =>
            index >= 0 && index < PresetMinutes.Length ? PresetMinutes[index] : 0;

        /// <summary>
        /// Met en forme un temps restant de façon lisible et compacte, la
        /// largeur d'affichage sur une ligne de job étant contrainte.
        /// Les unités (h/min/s) sont identiques en français et en anglais, la
        /// chaîne n'a donc pas besoin d'être traduite.
        /// </summary>
        /// <param name="remaining">
        /// Durée restante. Une valeur nulle ou négative rend "0 s" plutôt
        /// qu'une durée négative — l'arrêt est imminent, pas en retard.
        /// </param>
        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "0 s";

            // Arrondi au supérieur des SECONDES uniquement : il reste toujours
            // au moins "1 s" tant que l'échéance n'est pas atteinte, sinon la
            // dernière seconde s'afficherait "0 s" avant l'arrêt effectif.
            //
            // Les minutes et les heures sont ensuite tronquées, comme dans un
            // décompte classique : 90 s restantes donnent "1 min", pas "2 min".
            // La bascule en secondes sous la barre de la minute rend le dernier
            // palier précis, l'imprécision n'est donc jamais visible longtemps.
            var totalSeconds = (long)Math.Ceiling(remaining.TotalSeconds);

            if (totalSeconds < 60) return $"{totalSeconds} s";

            var totalMinutes = totalSeconds / 60;
            if (totalMinutes < 60) return $"{totalMinutes} min";

            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            return minutes == 0 ? $"{hours} h" : $"{hours} h {minutes} min";
        }
    }
}
