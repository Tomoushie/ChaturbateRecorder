using System;
using System.Threading;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace ChaturbateRecorderApp.Services
{
    /// <summary>
    /// Icône de zone de notification, et réveil de l'application par un second
    /// lancement.
    ///
    /// **C'est le seul endroit du projet qui utilise WinForms**, et il ne s'en
    /// sert que pour <c>NotifyIcon</c> : .NET n'offre aucune autre icône de
    /// zone de notification, et WPF n'en a pas. Aucune fenêtre WinForms n'est
    /// créée ici.
    ///
    /// **Pourquoi la fenêtre se MASQUE au lieu de se fermer** (19.0) : un
    /// enregistrement en cours doit survivre au clic sur la croix. Fermer
    /// l'application tuerait yt-dlp au milieu d'une capture, et personne
    /// n'attend cela d'un bouton « fermer la fenêtre ».
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        private readonly WinForms.NotifyIcon _icone;
        private readonly Window _fenetre;
        private EventWaitHandle? _evenementReveil;
        private Thread? _ecoute;

        /// <summary>
        /// Vrai quand l'utilisateur a demandé de QUITTER, par opposition à
        /// fermer la fenêtre. Sans ce drapeau, « Quitter » du menu ne ferait que
        /// masquer la fenêtre comme la croix, et l'application serait
        /// impossible à arrêter autrement que par le gestionnaire de tâches.
        /// </summary>
        private bool _fermetureDemandee;

        public TrayIcon(Window fenetre)
        {
            _fenetre = fenetre;

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Afficher", null, (s, e) => Afficher());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Quitter", null, (s, e) => Quitter());

            _icone = new WinForms.NotifyIcon
            {
                // L'icône de l'exécutable lui-même : pas de ressource
                // supplémentaire à embarquer ni à tenir synchronisée.
                Icon = ExtraireIcone(),
                Text = "Chaturbate Recorder",
                Visible = true,
                ContextMenuStrip = menu,
            };
            _icone.DoubleClick += (s, e) => Afficher();

            _fenetre.Closing += SurFermeture;
            EcouterLeSecondLancement();
        }

        private static System.Drawing.Icon ExtraireIcone()
        {
            try
            {
                var chemin = Environment.ProcessPath;
                if (chemin is not null)
                {
                    var icone = System.Drawing.Icon.ExtractAssociatedIcon(chemin);
                    if (icone is not null) return icone;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Icône de l'exécutable illisible : {ex.Message}", LogLevel.WARN);
            }

            // Sans icône, NotifyIcon n'affiche RIEN — pas d'erreur, juste une
            // zone de notification vide et un menu inaccessible. Le repli
            // système vaut mieux que l'invisibilité.
            return System.Drawing.SystemIcons.Application;
        }

        private void SurFermeture(object? expediteur, System.ComponentModel.CancelEventArgs e)
        {
            if (_fermetureDemandee) return;

            e.Cancel = true;
            _fenetre.Hide();
        }

        private void Afficher()
        {
            _fenetre.Show();
            // Une fenêtre réduite doit revenir à sa taille : `Show` seul la
            // laisse dans la barre des tâches, réduite, ce qui se lit comme un
            // clic sans effet.
            if (_fenetre.WindowState == WindowState.Minimized)
                _fenetre.WindowState = WindowState.Normal;
            _fenetre.Activate();
        }

        private void Quitter()
        {
            _fermetureDemandee = true;
            _fenetre.Close();
        }

        /// <summary>
        /// Écoute l'évènement nommé que signale un second lancement.
        ///
        /// **Cet évènement était DÉCLARÉ depuis le portage de `App.xaml.cs` et
        /// personne ne l'écoutait** : la seconde instance le signalait dans le
        /// vide, puis se terminait. Relancer l'exécutable ne faisait donc rien
        /// du tout — or c'est précisément le geste de quelqu'un qui cherche une
        /// fenêtre masquée dans la zone de notification.
        /// </summary>
        private void EcouterLeSecondLancement()
        {
            try
            {
                _evenementReveil = new EventWaitHandle(
                    false, EventResetMode.AutoReset, App.ShowWindowEventName);
            }
            catch (Exception ex)
            {
                // L'application reste parfaitement utilisable : seul le réveil
                // par second lancement est perdu, le mutex ayant déjà empêché
                // la seconde instance de s'ouvrir.
                Logger.Log($"Surveillance du second lancement indisponible : {ex.Message}", LogLevel.WARN);
                return;
            }

            // Fil d'ARRIÈRE-PLAN : il attend indéfiniment, et un fil de premier
            // plan empêcherait le processus de se terminer.
            _ecoute = new Thread(() =>
            {
                while (_evenementReveil.WaitOne())
                {
                    if (_fermetureDemandee) return;
                    _fenetre.Dispatcher.Invoke(Afficher);
                }
            })
            {
                IsBackground = true,
                Name = "Reveil-SecondLancement",
            };
            _ecoute.Start();
        }

        /// <summary>Affiche une notification système.</summary>
        public void Notifier(string titre, string message)
        {
            try
            {
                _icone.ShowBalloonTip(4000, titre, message, WinForms.ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"Notification impossible : {ex.Message}", LogLevel.WARN);
            }
        }

        public void Dispose()
        {
            _fenetre.Closing -= SurFermeture;

            // Masquer AVANT de libérer : une icône libérée sans être masquée
            // reste affichée dans la zone de notification jusqu'à ce que la
            // souris passe dessus. Défaut bien connu, et il donne l'impression
            // que l'application tourne encore.
            _icone.Visible = false;
            _icone.Dispose();

            _evenementReveil?.Set();   // débloque le fil d'écoute
            _evenementReveil?.Dispose();
            _evenementReveil = null;
        }
    }
}
