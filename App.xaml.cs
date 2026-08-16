using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using SentinelGuard;

// WPF apporte son propre `System.Windows.Localization` (attributs de
// localisation XAML), homonyme de la table de chaînes du projet : sans cet
// alias, chaque appel est ambigu (CS0104). Piège absent en WinForms.
using Localization = ChaturbateRecorderApp.UI.Localization;

namespace ChaturbateRecorderApp
{
    public partial class App : System.Windows.Application
    {
        internal const string SingleInstanceMutexName = @"Local\ChaturbateRecorder.SingleInstance";
        internal const string ShowWindowEventName = @"Local\ChaturbateRecorder.ShowWindow";

        private Mutex? _singleInstance;

        /// <summary>
        /// Vrai seulement si CETTE instance a obtenu le mutex.
        ///
        /// Sans ce drapeau, la sortie plantait pour de bon — trace relevée dans
        /// un vrai rapport de crash : une seconde instance construit le Mutex,
        /// ne le POSSÈDE pas, et `ReleaseMutex` sur un mutex non détenu lève.
        /// Le `catch (ApplicationException)` d'origine ne suffisait pas : selon
        /// le moment où le SafeHandle est libéré, c'est une
        /// `ObjectDisposedException` qui remonte, et elle traversait le filet
        /// pour ressortir dans `Application.DoShutdown`.
        /// </summary>
        private bool _detientLeMutex;

        public App()
        {
            this.Startup += Application_Startup;
            this.Exit += Application_Exit;
            this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Localization.Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase) ? AppLanguage.French : AppLanguage.English;

            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir, out var locationReason))
            {
                Logger.Log($"Emplacement d'execution refuse : {locationReason}", LogLevel.ERROR);
                System.Windows.MessageBox.Show(Localization.Get("error.unauthorizedLocation"), Localization.Get("error.unauthorizedLocation.title"), MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            _singleInstance = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
            _detientLeMutex = isFirstInstance;
            if (!isFirstInstance)
            {
                try
                {
                    if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var showEvent))
                    {
                        showEvent.Set();
                        showEvent.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Instance deja lancee, reveil impossible : {ex.Message}", LogLevel.WARN);
                }
                Shutdown(0);
                return;
            }

            CrashReporter.Install();

            // Sans ce branchement, CrashReporter ecrit le rapport et n'affiche
            // RIEN : l'utilisateur voit l'application disparaitre.
            Views.CrashWindow.Brancher();

            // Thème initial posé SANS animation : il n'y a rien à faire fondre
            // au premier affichage, et un fondu depuis les valeurs de départ du
            // dictionnaire se verrait à l'ouverture de la fenêtre.
            //
            // Clair en dur, comme le WinForms (`MainForm._currentTheme`) : il
            // n'existe AUCUN réglage de thème dans `UserSettings`, donc le choix
            // fait dans les Paramètres ne survit pas à la fermeture. Défaut
            // hérité, reproduit tel quel ici pour ne pas mélanger un correctif
            // de comportement avec un portage.
            ThemeManager.Apply(AppTheme.Light, animate: false);

            // Le thème est posé AVANT la fenêtre : construire la fenêtre
            // d'abord la ferait apparaître avec les couleurs de départ du
            // dictionnaire, puis se repeindre — visible à l'ouverture.
            new Views.MainWindow().Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                // Ne relâcher QUE si on le détient : voir _detientLeMutex.
                if (_detientLeMutex) _singleInstance?.ReleaseMutex();
            }
            catch (Exception ex)
            {
                // Filet large et non `ApplicationException` seule : la sortie du
                // processus ne doit jamais échouer sur son propre ménage. Le
                // pire cas ici est un mutex que Windows libère de toute façon à
                // la mort du processus.
                Logger.Log($"Erreur lors de la libération du mutex : {ex.Message}", LogLevel.ERROR);
            }
            finally
            {
                _singleInstance?.Dispose();
                _singleInstance = null;
            }
        }

        /// <summary>
        /// Pendant WPF de `Application.ThreadException` (WinForms) : c'est le
        /// filet du thread d'interface. `e.Handled = true` est ce qui permet à
        /// l'application de SURVIVRE à l'exception — sans lui, elle se termine
        /// une fois le gestionnaire rendu.
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReporter.HandleDispatcher(e.Exception);
            e.Handled = true;
        }
    }
}