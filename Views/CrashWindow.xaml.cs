using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Fenêtre affichée après une exception non gérée.
    ///
    /// Elle est branchée sur <see cref="CrashReporter.ShowCrashDialog"/>, le
    /// point d'accroche posé lors du portage du service : celui-ci écrit le
    /// rapport et journalise sans rien connaître de l'interface. Tant que
    /// personne ne remplissait ce point, un plantage s'écrivait sur disque et
    /// n'affichait RIEN — l'utilisateur voyait l'application disparaître.
    /// </summary>
    public partial class CrashWindow : Window
    {
        private readonly string? _fichierRapport;

        public CrashWindow(Exception ex, string? fichierRapport, bool fatale)
        {
            InitializeComponent();
            _fichierRapport = fichierRapport;

            LigneFichier.Text = fichierRapport is null
                ? "Le rapport n'a pas pu être écrit sur le disque."
                : $"Rapport enregistré : {fichierRapport}";

            Details.Text = ex.ToString();

            // Une exception FATALE ne se survit pas : proposer « Redémarrer »
            // aurait du sens, mais « Fermer » laisserait croire que
            // l'application continue alors qu'elle est déjà condamnée. On
            // n'offre donc le redémarrage que dans ce cas-là.
            BoutonRedemarrer.Visibility = fatale ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Pose le point d'accroche. Appelé une fois au démarrage.
        ///
        /// L'affichage est marshalé : une exception non gérée peut venir de
        /// n'importe quel fil — c'est même le cas le plus courant, le filet
        /// `AppDomain` couvrant le pool de threads et les tâches non observées.
        /// Construire une fenêtre WPF hors du fil d'interface lève.
        /// </summary>
        public static void Brancher()
        {
            CrashReporter.ShowCrashDialog = (ex, fichier, fatale) =>
            {
                var afficher = new Action(() =>
                {
                    try
                    {
                        new CrashWindow(ex, fichier, fatale).ShowDialog();
                    }
                    catch (Exception affichageEx)
                    {
                        // Si même l'affichage échoue — interface dans un état
                        // trop abîmé pour créer une fenêtre — on ne bloque pas
                        // plus longtemps la fermeture ou la poursuite.
                        Debug.WriteLine($"Fenêtre de plantage indisponible : {affichageEx.Message}");
                    }
                });

                if (Application.Current?.Dispatcher is { } d && !d.CheckAccess()) d.Invoke(afficher);
                else afficher();
            };
        }

        private void OuvrirDossier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Le dossier des rapports, pas celui du fichier sélectionné :
                // s'il n'a pas pu être écrit, on ouvre quand même l'endroit où
                // il aurait dû aller.
                var dossier = _fichierRapport is not null
                    ? Path.GetDirectoryName(_fichierRapport)
                    : Path.Combine(AppConfig.LogDir, "crashes");

                if (string.IsNullOrEmpty(dossier)) return;
                Directory.CreateDirectory(dossier);
                Process.Start(new ProcessStartInfo(dossier) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log($"Ouverture du dossier des rapports impossible : {ex.Message}", LogLevel.WARN);
            }
        }

        private void Redemarrer_Click(object sender, RoutedEventArgs e) =>
            CrashReporter.RestartApplication();

        private void Fermer_Click(object sender, RoutedEventArgs e) => Close();
    }
}
