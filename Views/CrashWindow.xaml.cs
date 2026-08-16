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
        /// Vrai pendant qu'une fenêtre de plantage est à l'écran.
        ///
        /// **SANS CE GARDE-FOU, LE RAPPORTEUR DE PLANTAGE TUE LE PROCESSUS.**
        /// Mesuré : si l'affichage de cette fenêtre lève — bureau indisponible,
        /// ressource manquante, interface trop abîmée — l'exception ne remonte
        /// PAS dans le `try` ci-dessous. WPF la route par
        /// `Dispatcher.CatchException`, donc vers
        /// `Application_DispatcherUnhandledException`, donc vers
        /// `CrashReporter.HandleDispatcher`, qui redemande d'afficher la
        /// fenêtre. La récursion est infinie et se termine en STACK OVERFLOW —
        /// qui ne se rattrape pas : le processus meurt sans écrire le moindre
        /// rapport, c'est-à-dire que le mécanisme censé signaler les plantages
        /// en provoque un pire.
        /// </summary>
        private static bool _affichageEnCours;

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
                if (_affichageEnCours)
                {
                    // Le rapport est déjà écrit sur disque par CrashReporter :
                    // renoncer à l'afficher ne perd rien d'autre que la fenêtre.
                    Debug.WriteLine($"Plantage pendant l'affichage d'un plantage, ignoré : {ex.Message}");
                    return;
                }
                var afficher = new Action(() =>
                {
                    _affichageEnCours = true;
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
                    finally
                    {
                        _affichageEnCours = false;
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
