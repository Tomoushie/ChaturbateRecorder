using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Security;
using ChaturbateRecorderApp.Services;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Fenêtre de paramètres (19.0), séparée du formulaire principal : thème,
    /// langue, dossier de sauvegarde, cookies, proxy, reconnexion automatique
    /// par défaut. Modale — le formulaire principal édite le même état global
    /// (AppConfig/UserSettings), une seconde fenêtre non modale l'éditant en
    /// parallèle demanderait une synchronisation inutile pour ce cas d'usage.
    /// Qualité/codec/format restent dans MainForm : ce sont des choix par
    /// enregistrement, pas des préférences globales.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly UserSettings _settings;
        private readonly Action<AppTheme> _onThemeChanged;
        private readonly Action<AppLanguage> _onLanguageChanged;
        private readonly Action<string> _appendLog;
        private AppTheme _theme;
        private AppLanguage _language;

        private Label themeLabel = null!;
        private ComboBox themeCombo = null!;
        private Label languageLabel = null!;
        private ComboBox languageCombo = null!;
        private Label saveDirLabel = null!;
        private TextBox saveDirTextBox = null!;
        private Button browseDirButton = null!;
        private Label cookiesLabel = null!;
        private TextBox cookiesTextBox = null!;
        private Button browseCookiesButton = null!;
        private Label proxyLabel = null!;
        private TextBox proxyTextBox = null!;
        private CheckBox autoReconnectCheckbox = null!;
        private CheckBox autoUpdateCheckbox = null!;
        private Label watchIntervalLabel = null!;
        private ComboBox watchIntervalCombo = null!;
        // 29.0 / 2.2 : un interrupteur par composant, dans l'ordre de l'enum.
        private Label safeSectionLabel = null!;
        private readonly Dictionary<SafeComponent, CheckBox> _safeBoxes = new();
        private Button closeButton = null!;

        public SettingsForm(UserSettings settings, AppTheme theme, AppLanguage language,
            Action<AppTheme> onThemeChanged, Action<AppLanguage> onLanguageChanged, Action<string> appendLog)
        {
            _settings = settings;
            _theme = theme;
            _language = language;
            _onThemeChanged = onThemeChanged;
            _onLanguageChanged = onLanguageChanged;
            _appendLog = appendLog;

            InitializeComponent();
            ThemeManager.Apply(this, _theme);
            ApplyLanguage(_language);
        }

        private void ApplyLanguage(AppLanguage lang)
        {
            string L(string key) => Localization.Get(key, lang);

            Text = L("window.settings");
            themeLabel.Text = L("theme.label");
            languageLabel.Text = L("language.label");
            saveDirLabel.Text = L("label.saveDir");
            browseDirButton.Text = L("button.browse");
            cookiesLabel.Text = L("label.cookies");
            proxyLabel.Text = L("label.proxy");
            autoReconnectCheckbox.Text = L("checkbox.autoReconnect");
            autoUpdateCheckbox.Text = L("checkbox.autoUpdateCheck");
            watchIntervalLabel.Text = L("label.watchInterval");
            safeSectionLabel.Text = L("safe.section");
            foreach (var (component, box) in _safeBoxes)
                box.Text = L("safe.component." + component);
            closeButton.Text = L("button.close");

            var themeIndex = themeCombo.SelectedIndex;
            themeCombo.Items.Clear();
            themeCombo.Items.AddRange(new object[] { L("theme.light"), L("theme.dark") });
            themeCombo.SelectedIndex = themeIndex < 0 ? (_theme == AppTheme.Dark ? 1 : 0) : themeIndex;
        }

        private void OnThemeComboChanged(object? sender, EventArgs e)
        {
            var theme = themeCombo.SelectedIndex == 1 ? AppTheme.Dark : AppTheme.Light;
            if (theme == _theme) return;
            _theme = theme;
            ThemeManager.Apply(this, theme);
            _onThemeChanged(theme);
        }

        private void OnLanguageComboChanged(object? sender, EventArgs e)
        {
            var language = languageCombo.SelectedIndex == 1 ? AppLanguage.English : AppLanguage.French;
            if (language == _language) return;
            _language = language;
            ApplyLanguage(language);
            _onLanguageChanged(language);
        }

        private void OnBrowseCaptureDirClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choisis le dossier de sauvegarde des enregistrements",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(AppConfig.CaptureDir) ? AppConfig.CaptureDir : AppConfig.AppDir,
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var chosen = dialog.SelectedPath;
            if (!PathValidator.IsValidPath(chosen))
            {
                MessageBox.Show(this, Localization.Get("error.invalidFolderSandbox"),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppConfig.CaptureDir = chosen;
            Directory.CreateDirectory(AppConfig.CaptureDir);
            saveDirTextBox.Text = AppConfig.CaptureDir;

            _settings.CaptureDir = AppConfig.CaptureDir;
            SettingsManager.Save(_settings);

            _appendLog($"Dossier de sauvegarde changé : {AppConfig.CaptureDir}");
        }

        private void OnBrowseCookiesClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choisis un fichier cookies.txt (format Netscape, exporté depuis ton navigateur)",
                Filter = "Fichiers cookies (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            if (!PathValidator.IsValidPath(dialog.FileName, mustExist: true))
            {
                MessageBox.Show(this, Localization.Get("error.invalidFileSandbox"),
                    Localization.Get("dialog.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Controle du format AVANT d'enregistrer le reglage. yt-dlp delegue
            // la lecture a http.cookiejar de Python, bien plus strict que le
            // format ne le laisse croire : un fichier refuse fait echouer tous
            // les enregistrements, et rend la surveillance definitivement muette
            // — sans que rien ne designe les cookies. Constate le 2026-08-05 sur
            // un fichier reel, ou un simple diese manquant invalidait 5 cookies
            // sur 6, dont celui de session.
            var check = CookieFileValidator.Validate(File.ReadAllLines(dialog.FileName));
            if (!check.IsValid)
            {
                var detail = check.Problem switch
                {
                    CookieFileProblem.MissingNetscapeHeader => Localization.Get("cookies.problem.header"),
                    CookieFileProblem.Empty => Localization.Get("cookies.problem.empty"),
                    CookieFileProblem.HttpOnlyPrefixMissingHash => Localization.Format("cookies.problem.httpOnly", check.Line),
                    CookieFileProblem.TooFewFields => Localization.Format("cookies.problem.fields", check.Line),
                    _ => Localization.Format("cookies.problem.domain", check.Line),
                };
                MessageBox.Show(this,
                    Localization.Get("cookies.invalid.intro") + "\n\n" + detail,
                    Localization.Get("cookies.invalid.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppConfig.CookiesFilePath = dialog.FileName;
            cookiesTextBox.Text = AppConfig.CookiesFilePath;

            _settings.CookiesFilePath = AppConfig.CookiesFilePath;
            SettingsManager.Save(_settings);

            _appendLog($"Fichier cookies changé : {AppConfig.CookiesFilePath}");
        }

        private void OnProxyTextChanged(object? sender, EventArgs e)
        {
            AppConfig.ProxyUrl = proxyTextBox.Text.Trim();
            _settings.ProxyUrl = AppConfig.ProxyUrl;
            SettingsManager.Save(_settings);
        }

        private void OnAutoReconnectChanged(object? sender, EventArgs e)
        {
            _settings.AutoReconnectDefault = autoReconnectCheckbox.Checked;
            SettingsManager.Save(_settings);
        }

        /// <summary>
        /// 88.0 — le nouvel intervalle ne prend effet qu'au prochain demarrage
        /// de l'application : le Timer de surveillance vit dans MainForm et
        /// n'est pas rappele ici. C'est assume, le reglage n'a rien d'urgent.
        /// </summary>
        /// <summary>
        /// 29.0 / 2.2 — bascule manuelle d'un composant.
        ///
        /// Reactiver a la main efface aussi la desactivation AUTOMATIQUE : sans
        /// ca, l'utilisateur qui vient de reparer son ffmpeg cocherait la case
        /// sans effet, et conclurait a un bug. La verification sera refaite au
        /// prochain demarrage et redesactivera si le probleme persiste.
        /// </summary>
        private void OnSafeComponentChanged(SafeComponent component, bool enabled)
        {
            SafeMode.SetManual(component, !enabled);
            if (enabled) SafeMode.ClearAutomatic();

            _settings.DisabledComponents = SafeMode.ManualNames.ToList();
            SettingsManager.Save(_settings);
            _appendLog($"Safe Mode : {component} {(enabled ? "réactivé" : "désactivé")}.");
        }

        private void OnWatchIntervalChanged(object? sender, EventArgs e)
        {
            _settings.WatchIntervalSeconds = watchIntervalCombo.SelectedIndex switch
            {
                0 => 60,
                1 => 120,
                2 => 300,
                _ => 600,
            };
            SettingsManager.Save(_settings);
        }

        /// <summary>
        /// 79.0 — la recherche horaire lit ce réglage à chaque passage, donc
        /// la décoche prend effet sans redémarrer l'application. Le seul appel
        /// réseau que l'app fait d'elle-même : il doit rester débrayable.
        /// </summary>
        private void OnAutoUpdateChanged(object? sender, EventArgs e)
        {
            _settings.AutoUpdateCheck = autoUpdateCheckbox.Checked;
            SettingsManager.Save(_settings);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Hauteur portée de 330 à 382 par la case de recherche automatique
            // de mise à jour (79.0) : une ligne de plus au même pas vertical
            // (48 px de case + 12 px de gouttière) et le bouton Fermer décalé
            // d'autant.
            ClientSize = new Size(480, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10F);

            themeLabel = new Label { Text = "Thème :", Location = new Point(12, 15), AutoSize = true };
            themeCombo = new ComboBox
            {
                Location = new Point(90, 12),
                Size = new Size(110, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeCombo.SelectedIndexChanged += OnThemeComboChanged;

            languageLabel = new Label { Text = "Langue :", Location = new Point(230, 15), AutoSize = true };
            languageCombo = new ComboBox
            {
                Location = new Point(300, 12),
                Size = new Size(118, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Noms de langue non traduits : chaque langue s'affiche dans
            // sa propre langue, convention standard d'un sélecteur de langue.
            languageCombo.Items.AddRange(new object[] { "Français", "English" });
            languageCombo.SelectedIndex = _language == AppLanguage.English ? 1 : 0;
            languageCombo.SelectedIndexChanged += OnLanguageComboChanged;

            saveDirLabel = new Label { Text = "Dossier de sauvegarde :", Location = new Point(12, 55), AutoSize = true };
            saveDirTextBox = new TextBox
            {
                Location = new Point(12, 73),
                Size = new Size(320, 24),
                ReadOnly = true,
                Text = AppConfig.CaptureDir,
            };
            // Largeur 126 (pas 98) : valeur reprise telle quelle de l'ancien
            // bouton "Parcourir..." dans MainForm — plus étroit, le Padding
            // horizontal des boutons (8.4/9.2) tronque le texte avec l'icône.
            browseDirButton = new Button { Text = "Parcourir...", Location = new Point(342, 73), Size = new Size(126, 24) };
            browseDirButton.Image = IconManager.Render("folder", 16, Color.White);
            browseDirButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            // ImageAlign et TextAlign sur le même côté (Left) : les mettre sur des
            // côtés opposés (icône à gauche, texte à droite) avec TextImageRelation
            // actif amène WinForms à mal calculer la position verticale de l'image
            // (rognée en bas) sur certains DPI/échelles d'affichage.
            browseDirButton.ImageAlign = ContentAlignment.MiddleLeft;
            browseDirButton.TextAlign = ContentAlignment.MiddleLeft;
            browseDirButton.Click += OnBrowseCaptureDirClick;

            cookiesLabel = new Label { Text = "Cookies (optionnel) :", Location = new Point(12, 112), AutoSize = true };
            cookiesTextBox = new TextBox
            {
                Location = new Point(12, 130),
                Size = new Size(388, 24),
                ReadOnly = true,
                Text = AppConfig.CookiesFilePath
            };
            browseCookiesButton = new Button { Text = "...", Location = new Point(408, 130), Size = new Size(40, 24) };
            browseCookiesButton.Click += OnBrowseCookiesClick;

            proxyLabel = new Label { Text = "Proxy SOCKS5/HTTP (optionnel) :", Location = new Point(12, 169), AutoSize = true };
            proxyTextBox = new TextBox
            {
                Location = new Point(12, 187),
                Size = new Size(436, 24),
                Text = AppConfig.ProxyUrl,
                PlaceholderText = "ex: socks5://127.0.0.1:9050",
            };
            proxyTextBox.Leave += OnProxyTextChanged;

            // Hauteur 48 (pas 40) : le texte tient sur deux lignes complètes,
            // 40px coupait la seconde ligne ("...si inattendue") à mi-hauteur.
            autoReconnectCheckbox = new CheckBox
            {
                Text = "Reconnexion automatique si le live se termine de façon inattendue",
                Location = new Point(12, 226),
                Size = new Size(456, 48),
                Checked = _settings.AutoReconnectDefault,
            };
            autoReconnectCheckbox.CheckedChanged += OnAutoReconnectChanged;

            // Même gabarit que la case précédente (456x48) : le libellé français
            // tient sur une ligne mais la fenêtre est de largeur fixe, et la
            // traduction n'a pas à être plus courte que le contrôle.
            autoUpdateCheckbox = new CheckBox
            {
                Text = "Rechercher automatiquement les mises à jour (toutes les heures)",
                Location = new Point(12, 278),
                Size = new Size(456, 48),
                Checked = _settings.AutoUpdateCheck,
            };
            autoUpdateCheckbox.CheckedChanged += OnAutoUpdateChanged;

            // 88.0 : rythme de la surveillance. Les valeurs sont en secondes,
            // plancher a 60 — descendre plus bas multiplierait les requetes vers
            // le site sans rien apporter, un live ne demarre pas a la seconde.
            watchIntervalLabel = new Label { Text = "Verifier les salons surveilles toutes les :", Location = new Point(12, 336), AutoSize = true };
            watchIntervalCombo = new ComboBox
            {
                Location = new Point(330, 332),
                Size = new Size(138, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            watchIntervalCombo.Items.AddRange(new object[] { "1 minute", "2 minutes", "5 minutes", "10 minutes" });
            watchIntervalCombo.SelectedIndex = _settings.WatchIntervalSeconds switch
            {
                <= 60 => 0,
                <= 120 => 1,
                <= 300 => 2,
                _ => 3,
            };
            watchIntervalCombo.SelectedIndexChanged += OnWatchIntervalChanged;

            // 29.0 / 2.2 — Safe Mode. Deux colonnes pour tenir en trois rangees :
            // cinq cases empilees auraient ajoute 150 px a une fenetre deja haute.
            safeSectionLabel = new Label { Text = "Fonctionnalites (Safe Mode)", Location = new Point(12, 372), AutoSize = true };

            var components = new[]
            {
                SafeComponent.Ffmpeg, SafeComponent.Cookies, SafeComponent.Proxy,
                SafeComponent.MultiStream, SafeComponent.Watch,
            };
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var box = new CheckBox
                {
                    // Coche = actif. L'inverse (« desactiver X ») obligerait a
                    // lire une negation pour comprendre l'etat courant.
                    Checked = SafeMode.IsEnabled(component),
                    Location = new Point(12 + (i % 2) * 230, 396 + (i / 2) * 24),
                    Size = new Size(225, 22),
                };
                box.CheckedChanged += (s, e) => OnSafeComponentChanged(component, box.Checked);
                _safeBoxes[component] = box;
            }

            closeButton = new Button { Text = "Fermer", Location = new Point(368, 496), Size = new Size(100, 26) };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                themeLabel, themeCombo, languageLabel, languageCombo,
                saveDirLabel, saveDirTextBox, browseDirButton,
                cookiesLabel, cookiesTextBox, browseCookiesButton,
                proxyLabel, proxyTextBox,
                autoReconnectCheckbox, autoUpdateCheckbox,
                watchIntervalLabel, watchIntervalCombo,
                safeSectionLabel,
                closeButton,
            });
            foreach (var box in _safeBoxes.Values) Controls.Add(box);

            themeCombo.Items.AddRange(new object[] { "Clair", "Sombre" });
            themeCombo.SelectedIndex = _theme == AppTheme.Dark ? 1 : 0;

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
