using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ChaturbateRecorderApp.UI
{
    public enum AppTheme { Light, Dark }

    /// <summary>
    /// Niveau de lecture d'un libellé (39.0). Sans lui, tous les textes de la
    /// fenêtre avaient exactement le même poids : rien ne distinguait un
    /// intitulé de champ (« Qualité source : ») de la donnée qu'il annonce, et
    /// l'œil n'avait aucun repère pour parcourir l'écran.
    /// </summary>
    public enum TextRole
    {
        /// <summary>Contenu : couleur de texte pleine.</summary>
        Body,
        /// <summary>Intitulé, légende, unité — atténué d'un cran.</summary>
        Caption,
    }

    /// <summary>
    /// Applique un thème clair/sombre en parcourant récursivement tous les
    /// contrôles du formulaire.
    ///
    /// **Modèle de surfaces (39.0)** : la fenêtre est un fond (<c>Bg</c>) sur
    /// lequel sont posées des cartes (<c>Card</c>), qui portent elles-mêmes des
    /// zones de saisie et des listes (<c>Input</c>). Auparavant les cartes
    /// prenaient la couleur du fond : elles n'existaient visuellement que par un
    /// liseré de 1 px, d'où un écran entièrement plat. La récursion transporte
    /// donc la surface courante, ce qui permet à un panneau imbriqué de prendre
    /// la couleur de la carte qui le contient plutôt que celle de la fenêtre.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Jeu de couleurs d'un thème — exposé séparément de <see cref="Apply"/>
        /// pour permettre à MainForm d'interpoler entre deux palettes lors d'une
        /// transition animée clair/sombre (9.2), sans dupliquer les valeurs.
        /// </summary>
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
            // 97.0 — deux couleurs d'ÉTAT, ajoutées pour les cartes de salon.
            // Elles ne servent jamais de décor : Success dit « en ligne »,
            // Warning dit « reconnexion en cours ». Une couleur qui ne porte pas
            // d'information rend illisibles celles qui en portent.
            Color Success,
            Color Warning);

        public static Palette GetPalette(AppTheme theme) => theme == AppTheme.Dark
            ? new Palette(
                Bg: Color.FromArgb(0x1B, 0x1B, 0x1B),
                Card: Color.FromArgb(0x26, 0x26, 0x26),
                Input: Color.FromArgb(0x2B, 0x2B, 0x2B),
                Fg: Color.FromArgb(0xE8, 0xE8, 0xE8),
                FgMuted: Color.FromArgb(0xA8, 0xA8, 0xA8),
                Accent: Color.FromArgb(0x3A, 0x96, 0xDD),
                // Texte SOMBRE sur l'accent en thème sombre, comme Windows 11
                // lui-même : l'accent y est un bleu clair, sur lequel du blanc
                // ne donne qu'un contraste de 3,2 — sous le seuil de lisibilité
                // (mesuré par ThemeHierarchyTests, qui a trouvé le défaut).
                AccentFg: Color.FromArgb(0x10, 0x14, 0x18),
                Neutral: Color.FromArgb(0x33, 0x33, 0x33),
                // Rouge nettement éclairci pour le fond sombre : posé en TEXTE
                // sur le gris des boutons secondaires, un rouge franc ne
                // dépasse pas 3,5 de contraste (mesuré). Il faut monter jusque
                // dans les tons saumon pour repasser le seuil de 4,5.
                Danger: Color.FromArgb(0xFF, 0xA7, 0x9A),
                Border: Color.FromArgb(0x3D, 0x3D, 0x3D),
                Shadow: Color.FromArgb(90, 0, 0, 0),
                // Choisies par MESURE du contraste sur la surface de carte
                // (#262626) et non a l'oeil : 6,83 et 7,51, bien au-dessus du
                // seuil WCAG de 4,5.
                Success: Color.FromArgb(0x4C, 0xC3, 0x8A),
                Warning: Color.FromArgb(0xF0, 0xA8, 0x4E))
            : new Palette(
                Bg: Color.FromArgb(0xEF, 0xEF, 0xEF),
                Card: Color.FromArgb(0xFB, 0xFB, 0xFB),
                Input: Color.White,
                Fg: Color.FromArgb(0x1A, 0x1A, 0x1A),
                FgMuted: Color.FromArgb(0x5D, 0x5D, 0x5D),
                Accent: Color.FromArgb(0x00, 0x78, 0xD4),
                AccentFg: Color.White,
                Neutral: Color.White,
                // Assombri par rapport au rouge « erreur » de Windows
                // (#C42B1C) : ce rouge-ci sert de TEXTE, et il reste lisible
                // une fois le fond du bouton teinté au survol et à l'appui.
                Danger: Color.FromArgb(0xB0, 0x1F, 0x12),
                Border: Color.FromArgb(0xE0, 0xE0, 0xE0),
                Shadow: Color.FromArgb(24, 0, 0, 0),
                // Nettement assombries par rapport au theme sombre : sur une
                // carte quasi blanche (#FBFBFB), un vert ou un orange vifs
                // tombent sous 4 de contraste. Mesure : 5,12 et 5,24.
                Success: Color.FromArgb(0x0F, 0x7B, 0x4F),
                Warning: Color.FromArgb(0x9A, 0x5B, 0x00));

        public static void Apply(Control root, AppTheme theme) => ApplyPalette(root, GetPalette(theme));

        public static void ApplyPalette(Control root, Palette p)
        {
            // La surface de départ est celle du parent quand on ne repeint
            // qu'un sous-arbre (BuildJobRow applique le thème à une ligne déjà
            // posée dans une carte) : sans ça, la ligne reprendrait le fond de
            // la fenêtre et trancherait sur la carte qui la porte.
            var surface = root.Parent != null && IsInsideCard(root) ? p.Card : p.Bg;
            if (root is not RoundedGroupPanel)
            {
                root.BackColor = surface;
                root.ForeColor = p.Fg;
            }
            ApplyRecursive(root, p, surface);
        }

        private static bool IsInsideCard(Control control)
        {
            for (var c = control.Parent; c != null; c = c.Parent)
                if (c is RoundedGroupPanel) return true;
            return false;
        }

        /// <summary>
        /// Interpole deux palettes (9.2) : sert de base à MainForm pour animer
        /// la transition clair/sombre au lieu d'un saut instantané de couleurs.
        /// </summary>
        public static Palette LerpPalette(Palette a, Palette b, float t) => new(
            Lerp(a.Bg, b.Bg, t), Lerp(a.Card, b.Card, t), Lerp(a.Input, b.Input, t),
            Lerp(a.Fg, b.Fg, t), Lerp(a.FgMuted, b.FgMuted, t),
            Lerp(a.Accent, b.Accent, t), Lerp(a.AccentFg, b.AccentFg, t),
            Lerp(a.Neutral, b.Neutral, t), Lerp(a.Danger, b.Danger, t),
            Lerp(a.Border, b.Border, t), Lerp(a.Shadow, b.Shadow, t),
            // Interpolées comme les autres : oubliées ici, les pastilles d'état
            // sauteraient d'une couleur à l'autre au milieu du fondu clair/sombre,
            // seules immobiles pendant que tout le reste glisse.
            Lerp(a.Success, b.Success, t), Lerp(a.Warning, b.Warning, t));

        // --- Rôles de texte -------------------------------------------------

        private static readonly ConditionalWeakTable<Control, object> _textRoles = new();

        /// <summary>
        /// Marque un libellé comme secondaire. Stocké à côté du contrôle plutôt
        /// que dans sa couleur : une couleur posée au point d'appel serait
        /// écrasée au premier changement de thème, et un thème ne peut pas
        /// deviner qu'un texte donné est une légende.
        /// </summary>
        public static void SetTextRole(Control control, TextRole role)
        {
            _textRoles.Remove(control);
            _textRoles.Add(control, role);
        }

        internal static TextRole GetTextRole(Control control) =>
            _textRoles.TryGetValue(control, out var role) && role is TextRole r ? r : TextRole.Body;

        // --- Couleurs de bouton par rôle ------------------------------------

        /// <summary>
        /// Traduit un rôle en jeu de couleurs. Isolé (et internal plutôt que
        /// private) pour être vérifiable par les tests sans instancier de
        /// fenêtre : c'est ici que se joue la hiérarchie visuelle de 39.0.
        ///
        /// <see cref="ButtonRole.Danger"/> ne remplit PAS le bouton de rouge :
        /// « Stop », « Tout arrêter », « Supprimer favori » et « Ne plus
        /// surveiller » sont visibles en même temps, et quatre aplats rouges
        /// crieraient à l'écran en permanence. Le rouge ne porte donc que le
        /// texte, la bordure et la teinte de survol.
        /// </summary>
        internal static (Color Fill, Color Hover, Color Pressed, Color Border, Color Fg)
            ResolveButtonColors(ButtonRole role, Palette p) => role switch
        {
            // Survol et appui s'éloignent de la couleur du TEXTE, ils ne
            // s'éclaircissent pas systématiquement : sur l'accent clair du
            // thème sombre, éclaircir encore effacerait le libellé. Éloigner
            // garantit que le contraste ne peut que s'améliorer d'un état à
            // l'autre — c'est aussi ce que fait Windows 11 (accent assombri au
            // survol en thème clair, éclairci en thème sombre).
            ButtonRole.Primary => (p.Accent,
                                   ShiftAwayFrom(p.Accent, p.AccentFg, 14),
                                   ShiftAwayFrom(p.Accent, p.AccentFg, 28),
                                   Color.Empty, p.AccentFg),
            ButtonRole.Danger => (p.Neutral, Lerp(p.Neutral, p.Danger, 0.10f), Lerp(p.Neutral, p.Danger, 0.18f),
                                  Lerp(p.Border, p.Danger, 0.45f), p.Danger),
            _ => (p.Neutral, Lerp(p.Neutral, p.Fg, 0.06f), Lerp(p.Neutral, p.Fg, 0.12f), p.Border, p.Fg),
        };

        private static void ApplyRecursive(Control control, Palette p, Color surface)
        {
            // Surface transmise aux enfants : une carte devient la surface de
            // tout ce qu'elle contient, y compris à travers les panneaux
            // intermédiaires (advancedOptionsPanel, jobsListPanel, lignes de job).
            var childSurface = surface;

            switch (control)
            {
                case RoundedGroupPanel rgp:
                    rgp.ForeColor = p.Fg;
                    rgp.BackColor = p.Card;
                    rgp.TitleColor = p.Fg;
                    rgp.BorderColor = p.Border;
                    rgp.ShadowColor = p.Shadow;
                    rgp.SurfaceColor = surface;
                    rgp.FrameColor = p.Border;
                    childSurface = p.Card;
                    rgp.Invalidate();
                    break;

                // 97.0 — la carte de salon se dessine entièrement elle-même :
                // il suffit de lui passer la palette. On ne touche PAS à son
                // BackColor, qu'elle lit sur son PARENT pour peindre autour de
                // ses coins arrondis — le lui écraser dessinerait un rectangle
                // plein aux quatre angles.
                //
                // childSurface passe à Card : les boutons d'action posés dessus
                // doivent se croire sur une carte, pas sur le fond de fenêtre.
                //
                // AVANT `case Panel` : RoomCard en dérive, et c'est le cas
                // dérivé qui doit gagner — même règle que ThemedButton/Button
                // juste en dessous. Placé après, le compilateur le refuse comme
                // inaccessible, ce qui est la bonne nouvelle de l'affaire.
                case RoomCard rc:
                    rc.Palette = p;
                    childSurface = p.Card;
                    rc.Invalidate();
                    break;

                // ThemedButton avant Button : il en dérive, et c'est le cas
                // dérivé qui doit gagner.
                case ThemedButton tb:
                {
                    var (fill, hover, pressed, border, fg) = ResolveButtonColors(tb.Role, p);
                    tb.SurfaceColor = surface;
                    tb.DisabledFillColor = Lerp(surface, p.Fg, 0.06f);
                    tb.DisabledForeColor = p.FgMuted;
                    tb.SetColors(fill, hover, pressed, border, fg);
                    break;
                }

                case Panel pnl:
                    // Panneaux génériques (contentPanel, advancedOptionsPanel,
                    // lignes de job) : ils prennent la surface qui les porte,
                    // sans quoi leur fond système gris resterait visible et une
                    // ligne d'enregistrement trancherait sur sa carte.
                    pnl.BackColor = surface;
                    // Les panneaux AutoScroll (contentPanel, jobsListPanel) ont
                    // de VRAIS ascenseurs Windows : celui de la fenêtre
                    // principale restait blanc sur toute sa hauteur (114.0).
                    NativeScrollBars.Apply(pnl, p);
                    break;

                case Button b:
                    // Filet pour un éventuel bouton natif restant : au moins la
                    // couleur d'accent, sans les états animés du ThemedButton.
                    b.ForeColor = p.AccentFg;
                    b.BackColor = p.Accent;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    break;

                case TextBox tb2:
                    tb2.BackColor = p.Input;
                    tb2.ForeColor = p.Fg;
                    tb2.BorderStyle = BorderStyle.None;
                    InputFrame.Attach(tb2, p.Border);
                    // Multiligne uniquement en pratique (note de légalité,
                    // liste des remerciements) : un TextBox d'une ligne n'a pas
                    // d'ascenseur, l'appel est alors sans effet.
                    NativeScrollBars.Apply(tb2, p);
                    break;

                // RichTextBox ne dérive pas de TextBox (tous deux dérivent de
                // TextBoxBase) : sans ce cas, le corps du dialogue "Nouveautés"
                // resterait blanc sur blanc en thème sombre.
                case RichTextBox rtb:
                    rtb.BackColor = p.Input;
                    rtb.ForeColor = p.Fg;
                    rtb.BorderStyle = BorderStyle.None;
                    NativeScrollBars.Apply(rtb, p);
                    break;

                case ListBox lb:
                    lb.BackColor = p.Input;
                    lb.ForeColor = p.Fg;
                    lb.BorderStyle = BorderStyle.None;
                    InputFrame.Attach(lb, p.Border);
                    // Favoris et Logs : les deux ascenseurs blancs les plus
                    // visibles de la fenêtre, celui des Logs étant en plus
                    // HORIZONTAL (114.0).
                    NativeScrollBars.Apply(lb, p);
                    break;

                case ListView lv:
                    lv.BackColor = p.Input;
                    lv.ForeColor = p.Fg;
                    lv.BorderStyle = BorderStyle.None;
                    ThemedListView.Attach(lv, p);
                    InputFrame.Attach(lv, p.Border);
                    break;

                // ThemedComboBox avant ComboBox, même raison que pour les
                // boutons : le cas dérivé doit gagner.
                case ThemedComboBox tcb:
                    tcb.BackColor = p.Input;
                    tcb.ForeColor = p.Fg;
                    tcb.BorderColor = p.Border;
                    tcb.ArrowColor = p.FgMuted;
                    tcb.SurfaceColor = surface;
                    tcb.SelectionColor = Lerp(p.Input, p.Accent, 0.25f);
                    tcb.Invalidate();
                    break;

                case ComboBox cb:
                    cb.BackColor = p.Input;
                    cb.ForeColor = p.Fg;
                    cb.FlatStyle = FlatStyle.Flat;
                    break;

                case CheckBox cbx:
                    cbx.ForeColor = p.Fg;
                    cbx.BackColor = surface;
                    break;

                case PictureBox pb:
                    pb.BackColor = p.Input;
                    pb.BorderStyle = BorderStyle.None;
                    InputFrame.Attach(pb, p.Border);
                    break;

                // Couleurs dérivées de la palette plutôt qu'ajoutées à Palette :
                // un curseur d'ascenseur n'est ni du texte ni un fond, c'est un
                // intermédiaire entre les deux, et l'interpolation le donne
                // juste dans les deux thèmes sans deux champs de plus à animer
                // pendant la transition clair/sombre.
                // 97.0 — la barre de navigation prend la couleur de CARTE et
                // non celle du fond : c'est ce décalage d'un cran qui la
                // détache de la zone de contenu, sans bordure épaisse.
                case SideBar sb:
                    sb.SurfaceColor = p.Card;
                    sb.TextColor = p.Fg;
                    sb.MutedColor = p.FgMuted;
                    sb.AccentColor = p.Accent;
                    sb.BorderColor = p.Border;
                    sb.Invalidate();
                    break;

                case ThemedScrollBar tsb:
                    tsb.TrackColor = p.Input;
                    tsb.ThumbColor = Lerp(p.Input, p.Fg, 0.35f);
                    tsb.ThumbHoverColor = Lerp(p.Input, p.Fg, 0.55f);
                    tsb.Invalidate();
                    break;

                // Même raisonnement pour la barre de progression, à une réserve
                // près : seules la piste et la bordure suivent le thème.
                // BarColor encode l'état du job (en cours / terminé / erreur /
                // arrêté) et n'appartient donc pas à la palette — la réappliquer
                // ici repeindrait en bleu une barre passée au rouge ou au vert.
                case ThemedProgressBar tpb:
                    tpb.TrackColor = Lerp(surface, p.Fg, 0.10f);
                    tpb.BorderColor = p.Border;
                    break;

                // 103.0 — les pictogrammes de plateforme suivent la couleur des
                // textes secondaires : ils informent, ils ne réclament pas
                // l'attention.
                case PlatformStrip strip:
                    strip.BackColor = surface;
                    strip.IconColor = p.FgMuted;
                    break;

                case Label lbl:
                    lbl.ForeColor = GetTextRole(lbl) == TextRole.Caption ? p.FgMuted : p.Fg;
                    lbl.BackColor = surface;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyRecursive(child, p, childSurface);
        }

        internal static Color Lerp(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));

        /// <summary>
        /// Décale <paramref name="color"/> dans la direction opposée à
        /// <paramref name="text"/> : plus sombre sous un texte clair, plus
        /// claire sous un texte sombre. Le contraste ne peut donc qu'augmenter.
        /// </summary>
        private static Color ShiftAwayFrom(Color color, Color text, int amount) =>
            text.R + text.G + text.B > 384 ? Darken(color, amount) : Lighten(color, amount);

        private static Color Lighten(Color c, int amount) => Color.FromArgb(
            Math.Min(255, c.R + amount), Math.Min(255, c.G + amount), Math.Min(255, c.B + amount));

        private static Color Darken(Color c, int amount) => Color.FromArgb(
            Math.Max(0, c.R - amount), Math.Max(0, c.G - amount), Math.Max(0, c.B - amount));
    }
}
