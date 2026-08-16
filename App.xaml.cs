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

            // TODO: La couche de vues est en cours de portage. Ne PAS instancier de fenetre.
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _singleInstance?.ReleaseMutex();
            }
            catch (ApplicationException ex)
            {
                Logger.Log($"Erreur lors de la libération du mutex : {ex.Message}", LogLevel.ERROR);
            }
            finally
            {
                _singleInstance?.Dispose();
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReporter.HandleDispatcher(e.Exception);
            e.Handled = true;
            // TODO: c'est le pendant WPF de Application.ThreadException de WinForms, et que sans e.Handled=true l'application se terminerait.
        }
    }
}