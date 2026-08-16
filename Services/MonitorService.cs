using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.Services
{
    public sealed class MonitorService : IDisposable
    {
        private readonly CancellationTokenSource _arret = new();
        private Task? _boucle;
        public int IntervalleSecondes { get; set; } = 120;
        public event Action<string, RoomStatus>? StatutObtenu;
        private readonly Func<IReadOnlyList<string>> _urlsASonder;

        /// <summary>
        /// On recoit une FONCTION et non une liste : les salons surveilles changent pendant que la boucle tourne (ajout, retrait, interrupteur bascule). Capturer la liste au demarrage aurait fige la surveillance sur l'etat du premier tour.
        /// </summary>
        public MonitorService(Func<IReadOnlyList<string>> urlsASonder)
        {
            _urlsASonder = urlsASonder;
        }

        public void Demarrer()
        {
            if (_boucle != null) return;
            _boucle = Task.Run(BoucleAsync);
        }

        private async Task BoucleAsync()
        {
            while (!_arret.IsCancellationRequested)
            {
                foreach (var url in _urlsASonder())
                {
                    if (_arret.IsCancellationRequested) break;
                    try
                    {
                        var statut = await RoomStatusChecker.CheckAsync(
                            AppConfig.YtDlpPath, url,
                            SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : null,
                            SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : null,
                            45, _arret.Token);
                        Prevenir(url, statut);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        Logger.Log($"Sondage impossible pour {url} : {ex.Message}", LogLevel.WARN);
                    }
                }
                try { await Task.Delay(TimeSpan.FromSeconds(IntervalleSecondes), _arret.Token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// Les salons sont sondes L'UN APRES L'AUTRE et non de front : chaque sondage lance un processus yt-dlp, et vingt salons surveilles en parallele ouvriraient vingt processus a chaque tour.
        /// </summary>
        /// <param name="url">URL du salon</param>
        /// <param name="statut">Statut du salon</param>
        private void Prevenir(string url, RoomStatus statut)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                StatutObtenu?.Invoke(url, statut);
            }
            else
            {
                Application.Current?.Dispatcher?.Invoke(() => StatutObtenu?.Invoke(url, statut));
            }
        }

        public void Dispose()
        {
            _arret.Cancel();
            _arret.Dispose();
        }
    }
}