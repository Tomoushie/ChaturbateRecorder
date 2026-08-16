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

        // PAS DE CONSTRUCTEUR QUI S'ABONNE. Il en existait un, qui faisait
        // `this.Startup += Application_Startup;` et les deux autres — alors que
        // App.xaml les declare DEJA en attributs (`Startup="..."`). Les trois
        // gestionnaires s'executaient donc DEUX FOIS.
        //
        // Consequence, fatale et invisible jusqu'a ce qu'un vrai bureau
        // l'expose : au second passage, `Application_Startup` construisait un
        // Mutex que le PREMIER passage du MEME processus detenait deja. Il en
        // concluait « une autre instance tourne », appelait Shutdown(0), et
        // fermait l'application une seconde apres son ouverture.
        //
        // Cela explique aussi retroactivement l'ObjectDisposedException sur
        // ReleaseMutex traitee plus haut par `_detientLeMutex` : Application_Exit
        // passait deux fois lui aussi. Ce drapeau reste — il est correct — mais
        // il soignait le symptome, pas la cause.
        //
        // Trouve par le JOURNAL DE DEMARRAGE, pas par raisonnement : deux
        // lignes « Demarrage » a 2 ms d'intervalle dans le meme processus.

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // JOURNAL DE DEMARRAGE, permanent et non un harnais. Sans lui, une
            // application qui s'arrete pendant son demarrage ne laisse RIEN :
            // ni exception, ni trace, et le seul symptome est « elle se ferme
            // toute seule ». Chaque sortie anticipee ci-dessous se journalise
            // donc, et le demarrage reussi aussi.
            Logger.Log($"Demarrage — v{typeof(App).Assembly.GetName().Version?.ToString(3)}, " +
                       $"repertoire {AppConfig.AppDir}");
            Localization.Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase) ? AppLanguage.French : AppLanguage.English;

            if (!WorkingDirectoryValidator.IsAuthorizedLocation(AppConfig.AppDir, out var locationReason))
            {
                Logger.Log($"ARRET : emplacement d'execution refuse — {locationReason}", LogLevel.ERROR);
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
                Logger.Log("ARRET : une autre instance tient deja le verrou, "
                           + "l'evenement de reveil lui a ete signale.");
                Shutdown(0);
                return;
            }

            // MODE D'ARRET EXPLICITE, et c'est le mode correct pour une
            // application a zone de notification : la fenetre principale se
            // MASQUE au lieu de se fermer, donc « arreter quand la derniere
            // fenetre se ferme » ne veut plus rien dire ici.
            //
            // C'est aussi la cause la plus probable du defaut signale — « elle
            // demarre puis se ferme une seconde apres » : avec
            // OnLastWindowClose, une modale de premier lancement qui se referme
            // peut etre comptee comme la derniere fenetre et arreter
            // l'application. Le symptome n'apparait QUE sur un vrai bureau,
            // ou ces dialogues s'affichent reellement.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            CrashReporter.Install();

            // Sans ce branchement, CrashReporter ecrit le rapport et n'affiche
            // RIEN : l'utilisateur voit l'application disparaitre.
            Views.CrashWindow.Brancher();

            // Theme initial pose SANS animation : il n'y a rien a faire
            // fondre au premier affichage, et un fondu depuis les valeurs de
            // depart du dictionnaire se verrait a l'ouverture de la fenetre.
            //
            // Il est desormais LU depuis les reglages : le WinForms repartait
            // en clair a chaque lancement, faute d'un champ ou le ranger.
            var themeChoisi = string.Equals(SettingsManager.Load().Theme, "dark",
                StringComparison.OrdinalIgnoreCase) ? AppTheme.Dark : AppTheme.Light;
            ThemeManager.Apply(themeChoisi, animate: false);

            // Le thème est posé AVANT la fenêtre : construire la fenêtre
            // d'abord la ferait apparaître avec les couleurs de départ du
            // dictionnaire, puis se repeindre — visible à l'ouverture.
            var fenetre = new Views.MainWindow();
            fenetre.Show();

            // Ces deux traces encadrent l'affichage : si la seconde manque, la
            // fenetre principale a echoue a s'ouvrir ; si les deux sont la et
            // que l'application disparait quand meme, la cause est APRES le
            // demarrage — un dialogue de premier lancement, ou la fermeture.
            Logger.Log($"Fenetre principale affichee (theme {ThemeManager.Current}, "
                       + $"mode d'arret {ShutdownMode}).");
            Exit += (s2, e2) => Logger.Log($"Sortie de l'application, code {e2.ApplicationExitCode}.");
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