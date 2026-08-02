using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Guide de démarrage pas-à-pas : affiché automatiquement au tout premier
    /// lancement (voir MainForm.ShowFirstRunDialogs) et réutilisable à tout
    /// moment via le bouton "Guide de démarrage".
    /// </summary>
    public class TutorialForm : Form
    {
        private static readonly (string Title, string Body)[] Steps =
        {
            ("Bienvenue",
                "Chaturbate Recorder enregistre des lives en local, avec des vérifications " +
                "de sécurité intégrées (hash des binaires, sandbox de chemins, ACL).\n\n" +
                "Ce guide rapide passe en revue les fonctionnalités principales — tu peux le " +
                "rouvrir à tout moment via le bouton \"Guide de démarrage\"."),

            ("Démarrer un enregistrement",
                "Colle l'URL Chaturbate dans le champ en haut, puis clique sur \"Démarrer\".\n\n" +
                "Chaque enregistrement tourne indépendamment : tu peux en lancer plusieurs en " +
                "même temps sans ouvrir plusieurs fenêtres. \"Tout arrêter\" stoppe tous les " +
                "enregistrements en cours d'un coup."),

            ("Qualité, codec et format",
                "Trois menus déroulants te laissent choisir :\n\n" +
                "•  la qualité source (meilleure / moyenne / minimale)\n" +
                "•  le codec de sortie (copie sans perte, ou réencodage H.264/H.265 fait " +
                "après coup, sans bloquer l'appli)\n" +
                "•  le conteneur (MP4, MKV — plus robuste en cas d'arrêt brutal —, ou MOV)"),

            ("Dossier de sauvegarde",
                "Le bouton \"Parcourir...\" te permet de choisir où sont enregistrées tes " +
                "vidéos.\n\nCe choix est mémorisé automatiquement pour les prochains " +
                "lancements."),

            ("Confidentialité",
                "Le champ \"Cookies\" permet d'importer une session déjà connectée depuis " +
                "ton navigateur (fichier cookies.txt), pour accéder au contenu réservé à un " +
                "compte.\n\n" +
                "Le champ \"Proxy\" route le trafic via un SOCKS5/HTTP de ton choix, pour " +
                "masquer ton IP réelle vis-à-vis du site distant."),

            ("Suivi des enregistrements",
                "Chaque enregistrement actif apparaît comme une ligne dans \"Enregistrements " +
                "en cours\", avec :\n\n" +
                "•  sa propre barre de progression\n" +
                "•  un bouton \"Ouvrir\" (accès direct à la page du stream)\n" +
                "•  un bouton Stop individuel (qui devient \"Retirer\" une fois terminé)"),

            ("Sécurité et mises à jour",
                "Avant chaque enregistrement, l'appli vérifie le hash de yt-dlp.exe et " +
                "ffmpeg.exe, et surveille l'emplacement d'exécution du programme.\n\n" +
                "Le bouton \"Rechercher une mise à jour\" en haut de la fenêtre vérifie " +
                "automatiquement les nouvelles versions publiées sur GitHub."),
        };

        private int _stepIndex;
        private Label titleLabel = null!;
        private Label bodyLabel = null!;
        private Label progressLabel = null!;
        private Button backButton = null!;
        private Button nextButton = null!;

        public TutorialForm()
        {
            InitializeComponent();
            RenderStep();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Guide de démarrage";
            ClientSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;

            titleLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(440, 28),
                Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            };

            bodyLabel = new Label
            {
                Location = new Point(20, 56),
                Size = new Size(440, 196),
                AutoSize = false,
            };

            progressLabel = new Label
            {
                Location = new Point(20, 270),
                AutoSize = true,
            };

            backButton = new Button { Text = "◀ Précédent", Location = new Point(220, 264), Size = new Size(110, 28) };
            nextButton = new Button { Text = "Suivant ▶", Location = new Point(336, 264), Size = new Size(124, 28) };

            backButton.Click += (s, e) => { if (_stepIndex > 0) { _stepIndex--; RenderStep(); } };
            nextButton.Click += (s, e) =>
            {
                if (_stepIndex < Steps.Length - 1) { _stepIndex++; RenderStep(); }
                else Close();
            };

            Controls.AddRange(new Control[] { titleLabel, bodyLabel, progressLabel, backButton, nextButton });

            ResumeLayout(false);
        }

        private void RenderStep()
        {
            var (title, body) = Steps[_stepIndex];
            titleLabel.Text = title;
            bodyLabel.Text = body;
            progressLabel.Text = $"Étape {_stepIndex + 1} / {Steps.Length}";
            backButton.Enabled = _stepIndex > 0;
            nextButton.Text = _stepIndex == Steps.Length - 1 ? "Terminer" : "Suivant ▶";
        }
    }
}
