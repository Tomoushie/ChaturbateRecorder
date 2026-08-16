using System;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;

namespace ChaturbateRecorderApp.UI
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public enum TextRole
    {
        /// <summary>Contenu, couleur de texte pleine</summary>
        Body,
        /// <summary>Intitulé, légende, unité, atténué d'un cran</summary>
        Caption
    }

    public enum ButtonRole
    {
        /// <summary>Action principale de la zone, fond d'accent plein</summary>
        Primary,
        /// <summary>Tout le reste, fond neutre, bordure fine</summary>
        Secondary,
        /// <summary>Interrompt ou supprime, accent rouge en aplat léger</summary>
        Danger
    }

    public static class ThemeManager
    {
        public readonly record struct Palette(
            Color Bg,
            Color Card,
            Color Input,
            Color Fg,
            Color FgMuted,
            Color Accent,
            Color AccentFg,
            Color Neutral,
            Color Danger,
            Color Border,
            Color Shadow,
            Color Success,
            Color Warning
        );

        public static Palette GetPalette(AppTheme theme) => theme switch
        {
            AppTheme.Dark => new Palette(
                Bg: Color.FromRgb(0x1B, 0x1B, 0x1B),
                Card: Color.FromRgb(0x26, 0x26, 0x26),
                Input: Color.FromRgb(0x2B, 0x2B, 0x2B),
                Fg: Color.FromRgb(0xE8, 0xE8, 0xE8),
                FgMuted: Color.FromRgb(0xA8, 0xA8, 0xA8),
                Accent: Color.FromRgb(0x3A, 0x96, 0xDD),
                AccentFg: Color.FromRgb(0x10, 0x14, 0x18),
                Neutral: Color.FromRgb(0x33, 0x33, 0x33),
                Danger: Color.FromRgb(0xFF, 0xA7, 0x9A),
                Border: Color.FromRgb(0x3D, 0x3D, 0x3D),
                Shadow: Color.FromArgb(90, 0, 0, 0),
                Success: Color.FromRgb(0x4C, 0xC3, 0x8A),
                Warning: Color.FromRgb(0xF0, 0xA8, 0x4E)
            ),
            _ => new Palette(
                Bg: Color.FromRgb(0xEF, 0xEF, 0xEF),
                Card: Color.FromRgb(0xFB, 0xFB, 0xFB),
                Input: Color.FromRgb(0xFF, 0xFF, 0xFF),
                Fg: Color.FromRgb(0x1A, 0x1A, 0x1A),
                FgMuted: Color.FromRgb(0x5D, 0x5D, 0x5D),
                Accent: Color.FromRgb(0x00, 0x78, 0xD4),
                AccentFg: Color.FromRgb(0xFF, 0xFF, 0xFF),
                Neutral: Color.FromRgb(0xFF, 0xFF, 0xFF),
                Danger: Color.FromRgb(0xB0, 0x1F, 0x12),
                Border: Color.FromRgb(0xE0, 0xE0, 0xE0),
                Shadow: Color.FromArgb(24, 0, 0, 0),
                Success: Color.FromRgb(0x0F, 0x7B, 0x4F),
                Warning: Color.FromRgb(0x9A, 0x5B, 0x00)
            )
        };

        public const string BrushBg = "Brush.Bg";
        public const string BrushCard = "Brush.Card";
        public const string BrushInput = "Brush.Input";
        public const string BrushFg = "Brush.Fg";
        public const string BrushFgMuted = "Brush.FgMuted";
        public const string BrushAccent = "Brush.Accent";
        public const string BrushAccentFg = "Brush.AccentFg";
        public const string BrushNeutral = "Brush.Neutral";
        public const string BrushDanger = "Brush.Danger";
        public const string BrushBorder = "Brush.Border";
        public const string BrushShadow = "Brush.Shadow";
        public const string BrushSuccess = "Brush.Success";
        public const string BrushWarning = "Brush.Warning";

        public static AppTheme Current { get; private set; } = AppTheme.Light;

        /// <summary>Durée du fondu clair/sombre, reprise telle quelle du WinForms (9.2).</summary>
        private const int TransitionMs = 220;

        private static DispatcherTimer? _transition;

        /// <summary>
        /// Applique un thème, avec ou sans fondu.
        ///
        /// **LES PINCEAUX SONT REMPLACÉS, JAMAIS MODIFIÉS, et ce n'est pas un
        /// choix de style — c'est la seule chose qui marche.** Un
        /// `ResourceDictionary` d'application SCELLE les Freezable qu'on lui
        /// confie : mesuré sur cinq chemins, un pinceau lu du dictionnaire, un
        /// clone modifiable qu'on y repose, un pinceau neuf, et jusqu'à un
        /// dictionnaire construit en code puis fusionné ressortent TOUS
        /// `IsFrozen = true`. Un pinceau gelé n'est ni animable ni assignable :
        /// `BeginAnimation` lève, et une affectation directe ne ferait rien.
        ///
        /// Le premier essai animait la couleur des pinceaux et compilait sans
        /// une alerte ; à l'exécution le passage en thème sombre ne changeait
        /// pas un octet. Seul le lancement pouvait le dire.
        ///
        /// D'où l'interpolation par pas, qui est exactement ce que faisait
        /// `LerpPalette` en WinForms : à chaque image on repose treize (plus
        /// quinze) pinceaux neufs dans le dictionnaire, et les `DynamicResource`
        /// se réévaluent d'eux-mêmes. Le coût est négligeable — ~15 images de 28
        /// affectations — et le rendu est le même qu'avant la migration.
        /// </summary>
        public static void Apply(AppTheme theme, bool animate = true)
        {
            var depart = GetPalette(Current);
            var arrivee = GetPalette(theme);
            Current = theme;

            // Une transition déjà en cours est abandonnée : sans ça, deux
            // bascules rapprochées feraient courir deux interpolations vers des
            // cibles différentes, et la dernière image gagnerait au hasard.
            _transition?.Stop();
            _transition = null;

            if (!animate || Application.Current is null)
            {
                SetPalette(arrivee);
                return;
            }

            var chrono = Stopwatch.StartNew();
            var timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(15),
            };
            timer.Tick += (s, e) =>
            {
                var t = Math.Min(1f, (float)chrono.ElapsedMilliseconds / TransitionMs);
                SetPalette(LerpPalette(depart, arrivee, t));

                if (t >= 1f)
                {
                    timer.Stop();
                    if (ReferenceEquals(_transition, timer)) _transition = null;
                    // Dernière pose exacte : l'interpolation à t=1 passe par des
                    // arrondis en octet, et un composant pourrait finir à une
                    // unité de la valeur de la palette.
                    SetPalette(arrivee);
                }
            };
            _transition = timer;
            timer.Start();
        }

        /// <summary>
        /// Interpole deux palettes, composante par composante. Portage direct de
        /// <c>ThemeManager.LerpPalette</c> du WinForms — y compris son invariant :
        /// les treize couleurs doivent TOUTES y figurer. Success et Warning
        /// avaient été oubliées à leur ajout (97.0), et elles sautaient alors au
        /// milieu du fondu au lieu de le suivre.
        /// </summary>
        public static Palette LerpPalette(Palette a, Palette b, float t) => new(
            Lerp(a.Bg, b.Bg, t),
            Lerp(a.Card, b.Card, t),
            Lerp(a.Input, b.Input, t),
            Lerp(a.Fg, b.Fg, t),
            Lerp(a.FgMuted, b.FgMuted, t),
            Lerp(a.Accent, b.Accent, t),
            Lerp(a.AccentFg, b.AccentFg, t),
            Lerp(a.Neutral, b.Neutral, t),
            Lerp(a.Danger, b.Danger, t),
            Lerp(a.Border, b.Border, t),
            Lerp(a.Shadow, b.Shadow, t),
            Lerp(a.Success, b.Success, t),
            Lerp(a.Warning, b.Warning, t));

        /// <summary>Pose une palette complète, sans transition.</summary>
        private static void SetPalette(Palette p)
        {
            SetBrush(BrushBg, p.Bg);
            SetBrush(BrushCard, p.Card);
            SetBrush(BrushInput, p.Input);
            SetBrush(BrushFg, p.Fg);
            SetBrush(BrushFgMuted, p.FgMuted);
            SetBrush(BrushAccent, p.Accent);
            SetBrush(BrushAccentFg, p.AccentFg);
            SetBrush(BrushNeutral, p.Neutral);
            SetBrush(BrushDanger, p.Danger);
            SetBrush(BrushBorder, p.Border);
            SetBrush(BrushShadow, p.Shadow);
            SetBrush(BrushSuccess, p.Success);
            SetBrush(BrushWarning, p.Warning);

            PublishButtonBrushes(p);
        }

        /// <summary>
        /// Remplace l'entrée du dictionnaire par un pinceau neuf, déjà gelé.
        ///
        /// Le gel est ici DEMANDÉ et non subi : un Freezable gelé se partage
        /// entre threads et se rend sans copie, ce qui est exactement l'usage
        /// d'une ressource. Rien ne mutera plus ce pinceau — la prochaine image
        /// en apportera un autre.
        ///
        /// `Application.Current` nul (tests, outil en ligne de commande) : on ne
        /// fait rien plutôt que de lever. Un thème n'a pas de sens sans
        /// application, et une exception y serait une panne sans cause visible.
        /// </summary>
        private static void SetBrush(string cle, Color couleur)
        {
            var ressources = Application.Current?.Resources;
            if (ressources is null) return;

            var pinceau = new SolidColorBrush(couleur);
            pinceau.Freeze();
            ressources[cle] = pinceau;
        }

        /// <summary>
        /// Publie les couleurs de bouton DÉRIVÉES, pour les trois rôles.
        ///
        /// Elles ne sont pas écrites en XAML, et c'est le point important :
        /// survol, appui et bordure ne sont pas des teintes choisies, ce sont
        /// des FONCTIONS de la palette (<see cref="ResolveButtonColors"/>) —
        /// « s'éloigner de la couleur du texte de 14 puis 28 », « tirer la
        /// bordure de 45 % vers le rouge ». Recopiées à la main dans un style,
        /// elles seraient justes une fois puis fausses au premier ajustement de
        /// palette, sans que rien ne le signale. Le style XAML ne référence donc
        /// que les clés ci-dessous, et la règle reste dans le C# testable.
        /// </summary>
        private static void PublishButtonBrushes(Palette palette)
        {
            foreach (var role in new[] { ButtonRole.Primary, ButtonRole.Secondary, ButtonRole.Danger })
            {
                var (fill, hover, pressed, border, fg) = ResolveButtonColors(role, palette);
                var prefixe = $"Brush.Btn.{role}.";
                SetBrush(prefixe + "Fill", fill);
                SetBrush(prefixe + "Hover", hover);
                SetBrush(prefixe + "Pressed", pressed);
                SetBrush(prefixe + "Border", border);
                SetBrush(prefixe + "Fg", fg);
            }
        }

        internal static (Color Fill, Color Hover, Color Pressed, Color Border, Color Fg) ResolveButtonColors(ButtonRole role, Palette p) => role switch
        {
            ButtonRole.Primary => (p.Accent, ShiftAwayFrom(p.Accent, p.AccentFg, 14), ShiftAwayFrom(p.Accent, p.AccentFg, 28), Colors.Transparent, p.AccentFg),
            ButtonRole.Danger => (p.Neutral, Lerp(p.Neutral, p.Danger, 0.10f), Lerp(p.Neutral, p.Danger, 0.18f), Lerp(p.Border, p.Danger, 0.45f), p.Danger),
            _ => (p.Neutral, Lerp(p.Neutral, p.Fg, 0.06f), Lerp(p.Neutral, p.Fg, 0.12f), p.Border, p.Fg)
        };

        internal static Color Lerp(Color a, Color b, float t) => Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t)
        );

        private static Color ShiftAwayFrom(Color color, Color text, int amount) => text.R + text.G + text.B > 384 ? Darken(color, amount) : Lighten(color, amount);

        private static Color Lighten(Color c, int amount) => Color.FromRgb(
            (byte)Math.Min(255, c.R + amount),
            (byte)Math.Min(255, c.G + amount),
            (byte)Math.Min(255, c.B + amount)
        );

        private static Color Darken(Color c, int amount) => Color.FromRgb(
            (byte)Math.Max(0, c.R - amount),
            (byte)Math.Max(0, c.G - amount),
            (byte)Math.Max(0, c.B - amount)
        );
    }
}