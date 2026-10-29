using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;
using ChaturbateRecorderApp.ViewModels;

// System.Windows.Localization existe aussi : alias obligatoire (même piège
// que dans le reste du projet).
using Localization = ChaturbateRecorderApp.UI.Localization;

// UseWindowsForms est activé dans ce projet (pour NotifyIcon) : les noms
// CheckBox/TextBlock/... existent donc AUSSI dans System.Windows.Forms.
// Alias explicites ; Button et Brush sont déjà aliasés globalement dans le csproj.
using CheckBox = System.Windows.Controls.CheckBox;
using Grid = System.Windows.Controls.Grid;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using StackPanel = System.Windows.Controls.StackPanel;
using TextBlock = System.Windows.Controls.TextBlock;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Visibility = System.Windows.Visibility;
using Orientation = System.Windows.Controls.Orientation;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// v2.3 — choix des salons pour la grille multi-live (2 à 4).
    ///
    /// Construite en code plutôt qu'en XAML : c'est une liste dynamique de
    /// cases à cocher, et une page XAML n'apporterait ici que des clés de
    /// localisation à tenir pour un écran que l'on ouvre dix secondes.
    /// Les textes passent quand même par <see cref="Localization"/>.
    /// </summary>
    public sealed class MultiLivePickerWindow : Window
    {
        private const int Maximum = 4;
        private readonly List<CheckBox> _cases = new();
        private readonly TextBlock _erreur;
        private readonly Button _lancer;

        public MultiLivePickerWindow(IReadOnlyList<RoomCardViewModel> rooms)
        {
            Title = Localization.Get("multilive.title");
            Width = 520;
            Height = 480;
            MinWidth = 420;
            MinHeight = 360;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Find<Brush>("Brush.Bg");

            var racine = new Grid { Margin = new Thickness(16) };
            racine.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            racine.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            racine.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            racine.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            racine.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var aide = new TextBlock
            {
                Text = Localization.Get("multilive.help"),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Find<Brush>("Brush.FgMuted"),
                Margin = new Thickness(0, 0, 0, 10),
            };
            Grid.SetRow(aide, 0);
            racine.Children.Add(aide);

            var avertissement = new TextBlock
            {
                Text = Localization.Get("multilive.max4"),
                Foreground = Find<Brush>("Brush.FgMuted"),
                Margin = new Thickness(0, 0, 0, 8),
            };
            Grid.SetRow(avertissement, 1);
            racine.Children.Add(avertissement);

            var liste = new StackPanel();
            foreach (var room in rooms)
            {
                var caseRoom = new CheckBox
                {
                    Margin = new Thickness(0, 4, 0, 4),
                    Style = Find<Style>("Interrupteur"),
                    Tag = room,
                };
                caseRoom.Content = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(room.StateLabel)
                        ? room.RoomName
                        : $"{room.RoomName} — {room.StateLabel}",
                    Foreground = Find<Brush>("Brush.Fg"),
                };
                caseRoom.Checked += (_, _) => AjusterLimite();
                caseRoom.Unchecked += (_, _) => AjusterLimite();
                _cases.Add(caseRoom);
                liste.Children.Add(caseRoom);
            }

            if (rooms.Count == 0)
            {
                liste.Children.Add(new TextBlock
                {
                    Text = Localization.Get("streams.empty"),
                    Foreground = Find<Brush>("Brush.FgMuted"),
                });
            }

            var defilement = new ScrollViewer
            {
                Content = liste,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 8),
            };
            Grid.SetRow(defilement, 2);
            racine.Children.Add(defilement);

            _erreur = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = Find<Brush>("Brush.Danger"),
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 8),
            };
            Grid.SetRow(_erreur, 3);
            racine.Children.Add(_erreur);

            var boutons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var annuler = new Button
            {
                Content = Localization.Get("button.cancel"),
                Style = Find<Style>("Button.Secondary"),
                MinWidth = 96,
            };
            annuler.Click += (_, _) => Close();

            _lancer = new Button
            {
                Content = Localization.Get("multilive.launch"),
                Style = Find<Style>("Button.Primary"),
                MinWidth = 130,
                Margin = new Thickness(8, 0, 0, 0),
            };
            _lancer.Click += async (_, _) => await LancerAsync();

            boutons.Children.Add(annuler);
            boutons.Children.Add(_lancer);
            Grid.SetRow(boutons, 4);
            racine.Children.Add(boutons);

            Content = racine;

            if (!App.Premium.IsLicensed)
            {
                AfficherErreur(Localization.Get("multilive.needsLicense"));
                _lancer.IsEnabled = false;
            }

            AjusterLimite();
        }

        private void AjusterLimite()
        {
            var coches = _cases.Count(c => c.IsChecked == true);
            foreach (var c in _cases)
            {
                if (c.IsChecked != true) c.IsEnabled = coches < Maximum;
            }
        }

        private async Task LancerAsync()
        {
            var choisis = _cases.Where(c => c.IsChecked == true)
                                .Select(c => (RoomCardViewModel)c.Tag)
                                .Take(Maximum)
                                .ToList();

            if (choisis.Count == 0)
            {
                AfficherErreur(Localization.Get("multilive.selectAtLeastOne"));
                return;
            }

            _lancer.IsEnabled = false;
            _lancer.Content = Localization.Get("multilive.loading");

            var json = JsonSerializer.Serialize(choisis.Select(r => new { url = r.Url, titre = r.RoomName }));
            var cookies = SafeMode.IsEnabled(SafeComponent.Cookies) ? AppConfig.CookiesFilePath : "";
            var proxy = SafeMode.IsEnabled(SafeComponent.Proxy) ? AppConfig.ProxyUrl : "";

            var ok = await Task.Run(() => App.Premium.TryShowMultiLive(json, cookies, proxy, topmost: false))
                                .ConfigureAwait(true);

            if (ok)
            {
                Close();
                return;
            }

            _lancer.IsEnabled = true;
            _lancer.Content = Localization.Get("multilive.launch");
            AfficherErreur(Localization.Get("multilive.failed"));
        }

        private void AfficherErreur(string texte)
        {
            _erreur.Text = texte;
            _erreur.Visibility = Visibility.Visible;
        }

        private T Find<T>(string cle) where T : class =>
            TryFindResource(cle) as T ?? throw new InvalidOperationException($"Ressource absente : {cle}");
    }
}
