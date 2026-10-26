using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ChaturbateRecorderApp.UI;
using ChaturbateRecorderApp.ViewModels;
using ChaturbateRecorderApp.Views;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Le rendu, mesuré AU PIXEL sur une vue réellement dessinée.
    ///
    /// **Relever la valeur d'une RESSOURCE ne prouve rien sur ce qui est
    /// affiché** — c'est la leçon la plus chère de ce portage, et elle vient
    /// d'être repayée : les gabarits de bouton et de champ déclaraient bien
    /// leurs pinceaux de bordure, et pourtant aucune bordure n'était dessinée,
    /// parce que le `Border` du gabarit ne reprenait pas la moitié manquante.
    /// Un test qui lit les ressources aurait été vert. C'est une capture d'écran
    /// du mainteneur qui l'a vu.
    ///
    /// Ce fichier ferme ce trou : il dessine les vues pour de vrai, échantillonne
    /// les pixels, et dépose les PNG pour qu'on puisse les regarder.
    ///
    /// Contraintes du rendu hors application, toutes mesurées auparavant :
    /// une `Window` non affichée ne rend RIEN (capture blanche) — on dessine donc
    /// la vue dans un `Border` autonome ; et le thème est posé SANS animation,
    /// sinon la capture attrape un état transitoire du fondu.
    /// </summary>
    [Collection("EtatDeProcessus")]
    public class RenduVisuelTests
    {
        private static readonly string DossierSortie = Path.Combine(
            Path.GetTempPath(), "cbr-rendu");

        public static IEnumerable<object[]> Themes =>
            new[] { new object[] { AppTheme.Light }, new object[] { AppTheme.Dark } };

        // --- Ce que le rendu doit montrer ---------------------------------

        /// <summary>
        /// LE test de non-régression du défaut trouvé sur capture : un bouton
        /// secondaire doit se DÉTACHER de la carte qui le porte.
        ///
        /// On échantillonne le bord du bouton et le fond de la carte à côté. Si
        /// les deux sont identiques, le bouton n'a ni fond ni bordure visibles —
        /// c'est-à-dire qu'il s'affiche en texte nu, exactement ce que montrait
        /// la capture pour « Parcourir... » et « Diagnostic... ».
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void UnBoutonSecondaireSeDetacheDeSaCarte(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var (vue, image, bitmap) = Rendre(theme, () => new SettingsView
                {
                    DataContext = new SettingsViewModel(),
                });

                Enregistrer(bitmap, $"reglages-{theme}".ToLowerInvariant());

                var bouton = Descendants<Button>(vue)
                    .First(b => ReferenceEquals(b.Style, vue.TryFindResource("Button.Secondary")));

                var zone = Zone(bouton, vue);
                var milieu = (int)(zone.Y + zone.Height / 2);
                var surLaCarte = image.Pixel((int)zone.X - 12, milieu);

                // **ON MESURE LA BORDURE, PAS L'INTERIEUR**, et la premiere
                // version de ce test s'y est trompee : en theme CLAIR le fond du
                // bouton secondaire est celui de la carte. Seule la bordure le
                // detache — d'ou un bouton litteralement invisible quand elle
                // manquait, et d'ou l'inutilite d'echantillonner son interieur.
                //
                // On balaie les trois premiers pixels : le trait fait un pixel et
                // l'anticrenelage du coin arrondi en deplace le rendu exact.
                var bord = Enumerable.Range(0, 3)
                    .Select(dx => image.Pixel((int)zone.X + dx, milieu))
                    .ToList();

                Assert.True(bord.Any(p => p != surLaCarte),
                    $"{theme} — le bouton secondaire est INDISCERNABLE de sa carte : " +
                    $"ses trois premiers pixels valent {string.Join(", ", bord)} et la " +
                    $"carte {surLaCarte}. Ni fond ni bordure, donc du texte nu. " +
                    $"Voir le PNG dans {DossierSortie}.");
            });
        }

        /// <summary>
        /// Même mesure pour un champ de saisie : son gabarit posait la brosse
        /// sans jamais poser l'épaisseur, dont le défaut est 0. Aucun cadre
        /// n'était donc dessiné, nulle part.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void UnChampDeSaisieAUnCadreVisible(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var (vue, image, bitmap) = Rendre(theme, () => new SettingsView
                {
                    DataContext = new SettingsViewModel(),
                });

                var champ = Descendants<TextBox>(vue).First();
                var zone = Zone(champ, vue);

                // Le cadre lui-meme, sur le bord gauche du champ.
                var surLeCadre = image.Pixel((int)zone.X, (int)(zone.Y + zone.Height / 2));
                // L'interieur du champ, quelques pixels plus loin.
                var dedans = image.Pixel((int)zone.X + 6, (int)(zone.Y + zone.Height / 2));

                Assert.True(surLeCadre != dedans,
                    $"{theme} — le champ de saisie n'a AUCUN cadre : son bord " +
                    $"({surLeCadre}) est de la meme couleur que son interieur. " +
                    $"Voir le PNG dans {DossierSortie}.");
            });
        }

        /// <summary>
        /// Garde-fou du harnais lui-même : une capture entièrement uniforme
        /// signifie que rien n'a été dessiné. Sans lui, les deux tests
        /// ci-dessus pourraient échouer pour la mauvaise raison — ou pire,
        /// passer sur une image vide si l'échantillonnage tombait sur deux
        /// teintes de bruit.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LaCaptureNEstPasVide(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var (_, image, bitmap) = Rendre(theme, () => new SupportView
                {
                    DataContext = new SupportViewModel(),
                });

                Enregistrer(bitmap, $"soutenir-{theme}".ToLowerInvariant());

                var distinctes = image.CouleursDistinctes();
                Assert.True(distinctes > 8,
                    $"{theme} — la capture ne porte que {distinctes} couleur(s) : " +
                    "rien n'a ete dessine. Une Window non affichee rend blanc, " +
                    "d'ou le rendu dans un Border autonome.");
            });
        }

        /// <summary>
        /// L'écran principal quand aucun salon n'est connu — l'état du PREMIER
        /// LANCEMENT, donc le premier qu'un inconnu voit.
        ///
        /// Il s'ouvrait entièrement blanc sous la barre d'ajout, ce qui se lit
        /// comme une application cassée. Le test vérifie qu'il porte désormais
        /// du texte : on compte les pixels qui ne sont ni le fond ni la carte
        /// dans la moitié basse, là où la liste vide laissait le vide.
        ///
        /// Aucun salon RÉEL n'entre ici : le modèle lit le dossier de données du
        /// binaire de test, qui n'en a aucun. Une capture publiée a déjà montré
        /// de vrais noms de salons dans ce projet.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LEcranPrincipalDitQuIlEstVide(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                // LE DOSSIER DE DONNEES EST DEPLACE le temps du rendu. Sans
                // cela le modele lit la VRAIE liste de salons de la machine —
                // ce qui est arrive des que le mainteneur a lance
                // l'application — et la capture porterait de vrais noms. La
                // garde ci-dessous l'avait attrape ; mieux vaut ne pas
                // dependre d'elle.
                var dataDirInitial = ChaturbateRecorderApp.Config.AppConfig.DataDir;
                var vierge = Path.Combine(Path.GetTempPath(), "cbr-vide-" + Guid.NewGuid().ToString("N"));
                ChaturbateRecorderApp.Config.AppConfig.DataDir = vierge;

                var modele = new StreamsViewModel();
                try
                {
                    var (vue, image, bitmap) = Rendre(theme, () => new StreamsView { DataContext = modele });
                    Enregistrer(bitmap, $"enregistrer-vide-{theme}".ToLowerInvariant());

                    Assert.True(modele.Vide,
                        "Le modele voit des salons : la capture montrerait de VRAIS noms.");

                    var texte = Descendants<System.Windows.Controls.TextBlock>(vue)
                        .Any(t => t.Text == ChaturbateRecorderApp.UI.Localization.Get("streams.empty")
                                  && t.Visibility == Visibility.Visible);

                    Assert.True(texte,
                        $"{theme} — l'ecran principal ne dit PAS qu'il est vide. " +
                        $"Voir le PNG dans {DossierSortie}.");
                }
                finally
                {
                    modele.Dispose();
                    ChaturbateRecorderApp.Config.AppConfig.DataDir = dataDirInitial;
                    try { if (Directory.Exists(vierge)) Directory.Delete(vierge, true); }
                    catch { /* dossier temporaire */ }
                }
            });
        }

        /// <summary>
        /// LA VIGNETTE PREMIUM (StreamRecorderPro, 24-08) NE DOIT RIEN CASSER,
        /// AVEC OU SANS ELLE. `RoomCard.xaml` redéclare localement les deux
        /// convertisseurs qu'elle utilise — même piège déjà payé ici que pour
        /// `IconTemplateConverter` : un `StaticResource` ne voit que ce qui est
        /// fusionné DANS le même dictionnaire, jamais ce que fusionne un
        /// parent. Une clé manquante ne lève qu'à l'exécution, jamais à la
        /// compilation — c'est exactement ce que ce test ferait échouer.
        ///
        /// Deux cartes FICTIVES : sans vignette (le cas de l'immense majorité
        /// des utilisateurs, qui n'ont pas le composant payé) et avec, pour
        /// éprouver les deux branches de la visibilité — colonne effacée dans
        /// le premier cas, cadre visible dans le second.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LaVignettePremiumNeCassePasLeRenduAvecOuSansElle(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var avecVignette = new RoomCardViewModel(new ChaturbateRecorderApp.Services.RoomEntry
                {
                    Url = "https://chaturbate.com/salon-fictif-avec-vignette",
                    AddedUtc = DateTime.UtcNow,
                })
                {
                    RoomName = "Salon fictif (avec vignette)",
                    PlatformIconKey = "Icon.Camera",
                };
                var sansVignette = new RoomCardViewModel(new ChaturbateRecorderApp.Services.RoomEntry
                {
                    Url = "https://chaturbate.com/salon-fictif-sans-vignette",
                    AddedUtc = DateTime.UtcNow,
                })
                {
                    RoomName = "Salon fictif (sans vignette)",
                    PlatformIconKey = "Icon.Camera",
                };

                // Peu importe que ce soit un JPEG valide : PathToThumbnailConverter
                // a déjà ses propres tests côté historique. Ce qui est éprouvé ici,
                // c'est que le CHEMIN existe et que la colonne se montre — pas ce
                // que l'image décode.
                var image = Path.Combine(Path.GetTempPath(), "cbr-vignette-" + Guid.NewGuid().ToString("N") + ".jpg");
                File.WriteAllBytes(image, new byte[] { 1, 2, 3 });
                try
                {
                    avecVignette.CheminApercu = image;

                    var (vue, _, bitmap) = Rendre(theme, () =>
                    {
                        var conteneur = new ItemsControl
                        {
                            ItemTemplate = (System.Windows.DataTemplate)Application.Current.Resources["RoomCard.Template"],
                        };
                        conteneur.Items.Add(avecVignette);
                        conteneur.Items.Add(sansVignette);
                        return conteneur;
                    });
                    Enregistrer(bitmap, $"vignette-premium-{theme}".ToLowerInvariant());

                    // Chercher le BOUTON, pas le Border qu'il habille : un
                    // Control COLLAPSED n'applique jamais son ControlTemplate
                    // (ApplyTemplate part de Measure, sauté pour Collapsed),
                    // donc le Border et l'Image qu'il contient n'existent tout
                    // simplement PAS dans l'arbre visuel pour la carte sans
                    // vignette — piège découvert en écrivant ce test même.
                    // Le Button, lui, est un enfant DIRECT de la grille (posé
                    // par XAML, pas par un template) : toujours présent.
                    var boutons = Descendants<Button>(vue)
                        .Where(b => Equals(b.ToolTip, "Voir le direct"))
                        .ToList();
                    Assert.Equal(2, boutons.Count);
                    Assert.Contains(boutons, b => b.Visibility == Visibility.Visible);
                    Assert.Contains(boutons, b => b.Visibility == Visibility.Collapsed);
                }
                finally
                {
                    avecVignette.Detach();
                    sansVignette.Detach();
                    try { File.Delete(image); } catch { /* dossier temporaire */ }
                }
            });
        }

        /// <summary>
        /// LE TEXTE DES DIALOGUES EST-IL LISIBLE ? Mesuré, pas jugé à l'œil.
        ///
        /// Sur les captures en thème SOMBRE du mainteneur, les libellés des
        /// trois dialogues paraissaient délavés là où ceux des Réglages sont
        /// francs. Leur XAML demande pourtant `Brush.Fg` — donc soit l'œil se
        /// trompe, soit quelque chose atténue le rendu. Un relevé de ressource
        /// ne peut pas trancher : il faut le PIXEL RÉELLEMENT DESSINÉ.
        ///
        /// Le seuil est celui que ce projet s'est déjà donné : WCAG 4,5, celui
        /// qui avait recalé deux couleurs du thème clair à 3,97 et 4,17.
        ///
        /// Une `Window` non affichée ne rend RIEN : on lui prend son `Content`,
        /// on le détache, et on le dessine dans un `Border` autonome.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LeTexteDesDialoguesEstLisible(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var plaintes = new List<string>();

                foreach (var (nom, fabrique) in new (string, Func<Window>)[]
                {
                    ("signalement", () => new ReportWindow()),
                    ("legalite", () => new LegalWindow()),
                    ("guide", () => new TutorialWindow()),
                    ("diagnostic", () => new DiagnosticWindow()),
                    ("planification", () => new ScheduleWindow("Salon fictif", false, -1, -1)),
                    // 120.0 — ajoutée au même filet que les autres dialogues :
                    // gratuit (PNG + contraste WCAG des libellés), et c'est
                    // exactement le genre d'écran (texte sur carte, sur bouton)
                    // où le défaut déjà payé une fois (bouton secondaire
                    // indiscernable) pourrait se répéter.
                    ("premium", () => new PremiumUpgradeWindow()),
                })
                {
                    // LA FENETRE SE CONSTRUIT DANS LA FABRIQUE, donc APRES que
                    // `Rendre` a pose les ressources. La construire avant fait
                    // lever son XAML sur « Impossible de trouver la ressource
                    // Text.Caption » : rien ne resout un `StaticResource` tant
                    // que le dictionnaire d'application n'existe pas.
                    FrameworkElement? contenu = null;
                    var (_, image, bitmap) = Rendre(theme, () =>
                    {
                        var fenetre = fabrique();
                        contenu = (FrameworkElement)fenetre.Content;
                        // Detache : une Window non affichee ne rend RIEN.
                        fenetre.Content = null;
                        return contenu;
                    });
                    Enregistrer(bitmap, $"{nom}-{theme}".ToLowerInvariant());

                    foreach (var bloc in Descendants<System.Windows.Controls.TextBlock>(contenu!)
                                 .Where(t => t.Visibility == Visibility.Visible
                                             && !string.IsNullOrWhiteSpace(t.Text)
                                             && t.Foreground is SolidColorBrush))
                    {
                        // LE FOND SUR LEQUEL LE TEXTE REPOSE, et non celui de la
                        // fenetre : la premiere version de ce test comparait tout
                        // a `Brush.Bg` et accusait « Envoyer » et « Fermer » d'un
                        // contraste de 1,07 — ces libelles sont poses sur le bleu
                        // de LEUR BOUTON. Une mesure prise contre le mauvais fond
                        // ne vaut pas mieux qu'un coup d'oeil.
                        var fond = FondEffectif(bloc);
                        if (fond is not { } surface) continue;

                        var couleur = ((SolidColorBrush)bloc.Foreground).Color;
                        var contraste = Contraste(couleur, surface);
                        if (contraste < 4.5)
                            plaintes.Add($"{nom} : {contraste:F2} pour « {Court(bloc.Text)} »");
                    }
                }

                Assert.True(plaintes.Count == 0,
                    $"{theme} — texte sous le seuil WCAG 4,5 :\n  " +
                    string.Join("\n  ", plaintes.Distinct()));
            });
        }

        /// <summary>
        /// Remonte l'arbre visuel jusqu'au premier ancêtre qui peint réellement
        /// un fond. Rend null si aucun n'en peint — le texte est alors sur le
        /// fond de la fenêtre, déjà couvert par les tests de palette.
        ///
        /// Un `Border` de gabarit de bouton compte : c'est bien lui que
        /// l'utilisateur voit derrière le libellé.
        /// </summary>
        private static Color? FondEffectif(DependencyObject depart)
        {
            for (var n = VisualTreeHelper.GetParent(depart); n is not null;
                 n = VisualTreeHelper.GetParent(n))
            {
                var pinceau = n switch
                {
                    Border b => b.Background,
                    System.Windows.Controls.Panel p => p.Background,
                    System.Windows.Controls.Control c => c.Background,
                    _ => null,
                };
                if (pinceau is SolidColorBrush s && s.Color.A > 0) return s.Color;
            }
            return null;
        }

        private static string Court(string t) =>
            t.Length <= 40 ? t.Replace("\n", " ") : t.Substring(0, 40).Replace("\n", " ") + "...";

        /// <summary>Contraste WCAG 2.x, même formule que ThemeWpfTests.</summary>
        private static double Contraste(Color a, Color b)
        {
            static double Canal(int v)
            {
                var c = v / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            static double Lum(Color c) => 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
            var (l1, l2) = (Lum(a), Lum(b));
            if (l1 < l2) (l1, l2) = (l2, l1);
            return (l1 + 0.05) / (l2 + 0.05);
        }

        /// <summary>
        /// Les deux thèmes doivent donner des images DIFFÉRENTES. C'est le
        /// contrôle qui manquait quand un relevé de ressources annonçait le
        /// thème appliqué alors que la moitié de l'écran ne bougeait pas.
        /// </summary>
        [Fact]
        public void LesDeuxThemesNeDonnentPasLaMemeImage()
        {
            SurFilStandard(() =>
            {
                var clair = Rendre(AppTheme.Light, () => new SettingsView { DataContext = new SettingsViewModel() }).Image;
                var sombre = Rendre(AppTheme.Dark, () => new SettingsView { DataContext = new SettingsViewModel() }).Image;

                Assert.NotEqual(clair.Pixel(4, 4), sombre.Pixel(4, 4));
            });
        }

        /// <summary>
        /// La barre de progression, TROISIÈME reprise du même défaut (voir le
        /// commentaire du gabarit dans Natifs.xaml) : trouvée par Tom contre
        /// un vrai direct, invisible ici sans lui. Ce test ne peut pas prouver
        /// que "Defilement" BOUGE (aucun Dispatcher qui tourne pendant un
        /// rendu hors écran, même limite que LiveOverlay) — seulement que le
        /// bon élément est VISIBLE dans le bon mode, ce qui aurait suffi à
        /// attraper l'absence totale de logique indéterminée d'avant ce
        /// correctif (les deux étaient alors invisibles OU l'ancien visible
        /// dans les deux modes).
        /// </summary>
        [Fact]
        public void LaBarreIndetermineeMontreLeDefilementPasLIndicateurDeValeur()
        {
            SurFilStandard(() =>
            {
                var (vue, _, bitmap) = Rendre(AppTheme.Dark, () => new System.Windows.Controls.ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 100, // exactement le cas réel : yt-dlp rend 100 % à chaque fragment
                    IsIndeterminate = true,
                });
                Enregistrer(bitmap, "barre-indeterminee");

                var indicateur = Descendants<Border>(vue).Single(b => b.Name == "PART_Indicator");
                var defilement = Descendants<Border>(vue).Single(b => b.Name == "Defilement");

                Assert.Equal(Visibility.Collapsed, indicateur.Visibility);
                Assert.Equal(Visibility.Visible, defilement.Visibility);

                // Le PNG ci-dessus capture l'instant t=0 : "Defilement" y
                // part hors-champ (X=-70) par construction (il doit ENTRER
                // par la gauche), donc invisible sur cette seule image sans
                // que ce soit un défaut. On force ici un point milieu de la
                // trajectoire pour vérifier au moins que le morceau se
                // dessine correctement une fois dans le cadre -- ce
                // qu'aucun Dispatcher ne fait avancer tout seul hors écran.
                var decalage = (TranslateTransform)defilement.RenderTransform;
                decalage.BeginAnimation(TranslateTransform.XProperty, null); // détache le Storyboard, sinon la valeur forcée est aussitôt reprise
                decalage.X = 200;
                var racineMilieu = (Border)vue.Parent;
                racineMilieu.UpdateLayout();
                var bitmapMilieu = new RenderTargetBitmap(Largeur, Hauteur, 96, 96, PixelFormats.Pbgra32);
                bitmapMilieu.Render(racineMilieu);
                Enregistrer(bitmapMilieu, "barre-indeterminee-mi-parcours");
            });
        }

        [Fact]
        public void LaBarreDeterminaeMontreToujoursLIndicateurDeValeur()
        {
            SurFilStandard(() =>
            {
                var (vue, _, bitmap) = Rendre(AppTheme.Dark, () => new System.Windows.Controls.ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 42,
                    IsIndeterminate = false,
                });
                Enregistrer(bitmap, "barre-determinee-42");

                var indicateur = Descendants<Border>(vue).Single(b => b.Name == "PART_Indicator");
                var defilement = Descendants<Border>(vue).Single(b => b.Name == "Defilement");

                Assert.Equal(Visibility.Visible, indicateur.Visibility);
                Assert.Equal(Visibility.Collapsed, defilement.Visibility);
            });
        }

        /// <summary>
        /// LA GALERIE (Premium II), au pixel — jamais tentée jusqu'ici.
        ///
        /// **Salons FICTIFS, comme pour "Enregistrer"** : `AppConfig.CaptureDir`
        /// est redirigé vers un dossier jetable AVANT toute construction du
        /// ViewModel, jamais le vrai dossier de capture de l'utilisateur, qui
        /// contient de vrais enregistrements.
        ///
        /// **`RafraichirCommand` est asynchrone** : l'attendre par
        /// `.GetAwaiter().GetResult()` depuis CE fil STA aurait fait un
        /// blocage mutuel, exactement le risque documenté pour
        /// `SurFilStandard` — la continuation de la tâche doit repasser par le
        /// Dispatcher de ce même fil, qui ne tourne plus pendant qu'il attend.
        /// `PousserJusqua` pompe la file de répartition en dessous plutôt que
        /// de bloquer dessus.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LaGalerieAfficheLesCartesEnGrille(AppTheme theme)
        {
            var dossier = Path.Combine(Path.GetTempPath(), "cbr-galerie-rendu-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dossier);
            var captureDirInitial = ChaturbateRecorderApp.Config.AppConfig.CaptureDir;

            try
            {
                CreerCaptureFictive(dossier, "salon-fictif-un-2026-08-17_20-15-35.mp4", "Salon fictif un", avecVignette: true);
                CreerCaptureFictive(dossier, "salon-fictif-deux-2026-08-10_10-00-00.mp4", "Salon fictif deux", avecVignette: false);

                ChaturbateRecorderApp.Config.AppConfig.CaptureDir = dossier;

                SurFilStandard(() =>
                {
                    var vm = new HistoryViewModel(); // lance RafraichirAsync en arrière-plan, voir le commentaire de classe
                    PousserJusqua(() => !vm.Chargement);
                    vm.AfficherEnGalerieCommand.Execute(null);

                    var (vue, _, bitmap) = Rendre(theme, () => new HistoryView { DataContext = vm });
                    Enregistrer(bitmap, $"galerie-{theme}".ToLowerInvariant());

                    Assert.Equal(2, vm.Elements.Count);
                    Assert.Contains(Descendants<System.Windows.Controls.WrapPanel>(vue), _ => true);

                    // FORCÉ, pas déclenché : IsMouseOver ne s'active jamais
                    // dans un rendu hors écran (pas de souris réelle). On
                    // éprouve ainsi la MISE EN PAGE du contenu révélé (tient-il
                    // dans 200 px sans se chevaucher ?), pas le déclencheur
                    // lui-même — déjà validé par la seule compilation
                    // (MC4011 aurait échoué si DataTemplate.Triggers ne
                    // pouvait pas cibler "Survol").
                    var survols = Descendants<Border>(vue).Where(b => b.Name == "Survol").ToList();
                    Assert.Equal(2, survols.Count);
                    foreach (var survol in survols) survol.Opacity = 1;

                    // "vue" est l'enfant de la Border racine posée par Rendre :
                    // la re-rendre reproduit le même cadrage que la capture
                    // normale, avec le survol désormais forcé visible.
                    var racine = (Border)vue.Parent;
                    racine.UpdateLayout();
                    var bitmapSurvol = new RenderTargetBitmap(Largeur, Hauteur, 96, 96, PixelFormats.Pbgra32);
                    bitmapSurvol.Render(racine);
                    Enregistrer(bitmapSurvol, $"galerie-survol-{theme}".ToLowerInvariant());
                });
            }
            finally
            {
                ChaturbateRecorderApp.Config.AppConfig.CaptureDir = captureDirInitial;
                try { Directory.Delete(dossier, recursive: true); } catch { /* dossier temporaire */ }
            }
        }

        private static void CreerCaptureFictive(string dossier, string nomFichier, string salon, bool avecVignette)
        {
            var video = Path.Combine(dossier, nomFichier);
            File.WriteAllBytes(video, new byte[] { 1, 2, 3 });
            File.WriteAllText(
                Path.Combine(dossier, Path.GetFileNameWithoutExtension(nomFichier) + ChaturbateRecorderApp.Services.CaptureFinalizer.ExtensionSidecarSalon),
                salon);

            // Peu importe que ce soit un JPEG valide, même raison que
            // LaVignettePremiumNeCassePasLeRenduAvecOuSansElle : on éprouve que
            // la colonne se montre, pas ce que l'image décode.
            if (avecVignette)
            {
                File.WriteAllBytes(Path.Combine(dossier, Path.GetFileNameWithoutExtension(nomFichier) + ".jpg"), new byte[] { 1, 2, 3 });
            }
        }

        /// <summary>
        /// Pompe la file de répartition de CE fil jusqu'à ce que
        /// <paramref name="pret"/> rende vrai, plutôt que de bloquer dessus :
        /// une tâche qui capture le contexte de synchronisation courant (tout
        /// `await` sans `ConfigureAwait(false)`) a besoin que CE Dispatcher
        /// continue de tourner pour reprendre après elle. `GetAwaiter().GetResult()`
        /// depuis ce même fil s'y opposerait — blocage mutuel.
        /// </summary>
        private static void PousserJusqua(Func<bool> pret, int timeoutMs = 5000)
        {
            var chrono = System.Diagnostics.Stopwatch.StartNew();
            var frame = new System.Windows.Threading.DispatcherFrame();

            var minuteur = new System.Windows.Threading.DispatcherTimer(
                TimeSpan.FromMilliseconds(20),
                System.Windows.Threading.DispatcherPriority.Background,
                (_, _) =>
                {
                    if (pret() || chrono.ElapsedMilliseconds > timeoutMs) frame.Continue = false;
                },
                System.Windows.Threading.Dispatcher.CurrentDispatcher);

            System.Windows.Threading.Dispatcher.PushFrame(frame);
            minuteur.Stop();
        }

        // --- Le harnais ----------------------------------------------------

        private const int Largeur = 900;
        private const int Hauteur = 700;

        private sealed record Capture(FrameworkElement Vue, Image Image, RenderTargetBitmap Bitmap);

        private sealed class Image
        {
            private readonly byte[] _pixels;
            private readonly int _stride;
            public int Largeur { get; }
            public int Hauteur { get; }

            public Image(RenderTargetBitmap bitmap)
            {
                Largeur = bitmap.PixelWidth;
                Hauteur = bitmap.PixelHeight;
                _stride = Largeur * 4;
                _pixels = new byte[_stride * Hauteur];
                bitmap.CopyPixels(_pixels, _stride, 0);
            }

            /// <summary>Couleur au format "#RRGGBB". Hors cadre rend "".</summary>
            public string Pixel(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Largeur || y >= Hauteur) return "";
                var i = y * _stride + x * 4;
                return $"#{_pixels[i + 2]:X2}{_pixels[i + 1]:X2}{_pixels[i]:X2}";
            }

            public int CouleursDistinctes()
            {
                var vues = new HashSet<int>();
                for (var i = 0; i + 3 < _pixels.Length; i += 4 * 7) // un pixel sur sept, assez pour compter
                    vues.Add(BitConverter.ToInt32(_pixels, i));
                return vues.Count;
            }
        }

        /// <summary>
        /// La zone blanche signalée par Tom : « Réglages » puis basculer
        /// « Thème sombre » — persistante, jamais un flash, et indépendante
        /// de la taille de fenêtre ou d'un enregistrement en cours.
        ///
        /// **Ce que <c>LesDeuxThemesNeDonnentPasLaMemeImage</c> ne peut pas
        /// attraper** : elle crée une vue FRAÎCHE par thème. Le scénario réel
        /// est différent — la MÊME fenêtre, le MÊME `SettingsView`, dont le
        /// thème change plusieurs fois sous ses pieds pendant qu'elle reste
        /// affichée. Ce test rejoue exactement ça : sombre → clair → sombre,
        /// sur une SEULE instance, et vérifie que le DERNIER rendu est bien
        /// sombre — pas resté clair.
        /// </summary>
        [Fact]
        public void BasculerDeuxFoisDeSuiteRedonneBienLeThemeSombre()
        {
            SurFilStandard(() =>
            {
                PreparerApplication();

                var vue = new SettingsView { DataContext = new SettingsViewModel() };
                var racine = new Border { Child = vue, Width = Largeur, Height = Hauteur };

                Image RendreMaintenant(AppTheme theme)
                {
                    ThemeManager.Apply(theme, animate: false);
                    racine.Background = (Brush)Application.Current.Resources[ThemeManager.BrushBg];
                    racine.Measure(new Size(Largeur, Hauteur));
                    racine.Arrange(new Rect(0, 0, Largeur, Hauteur));
                    racine.UpdateLayout();
                    var bitmap = new RenderTargetBitmap(Largeur, Hauteur, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(racine);
                    return new Image(bitmap);
                }

                var sombre1 = RendreMaintenant(AppTheme.Dark);
                var clair = RendreMaintenant(AppTheme.Light);
                var sombre2 = RendreMaintenant(AppTheme.Dark);

                // Le coin de la fenêtre (le fond, pas un contrôle particulier) :
                // sombre1 et sombre2 doivent se RESSEMBLER, clair doit DIFFÉRER
                // des deux.
                Assert.NotEqual(sombre1.Pixel(4, 4), clair.Pixel(4, 4));
                Assert.Equal(sombre1.Pixel(4, 4), sombre2.Pixel(4, 4));
            });
        }

        /// <summary>
        /// Sanity-check du bouton flottant Premium (120.0) : rendu réel non
        /// vide, dans les deux thèmes — le halo utilise des couleurs FIGÉES
        /// (pas de DynamicResource), donc rien ne garantissait qu'il survive
        /// à un changement de thème sans un rendu réel pour le prouver.
        ///
        /// **Ne prouve PAS** les éclairs au survol : ce harnais ne peut pas
        /// simuler une vraie souris (voir le commentaire de classe) — cette
        /// part-là reste à l'œil du mainteneur.
        /// </summary>
        [Theory]
        [MemberData(nameof(Themes))]
        public void LeBoutonPremiumEstVisible(AppTheme theme)
        {
            SurFilStandard(() =>
            {
                var (_, image, bitmap) = Rendre(theme, () => new PremiumButton());
                Enregistrer(bitmap, $"bouton-premium-{theme}".ToLowerInvariant());

                // Pixel() rend une chaîne "#RRGGBB", pas une Color : comparer
                // aux mêmes chaînes que le reste du fichier.
                var fond = theme == AppTheme.Dark ? "#1B1B1B" : "#EFEFEF";
                var pixels = new[]
                {
                    image.Pixel(450, 350), // centre du canevas 900x700, où le bouton doit se trouver
                    image.Pixel(430, 350),
                    image.Pixel(470, 350),
                };
                Assert.True(pixels.Any(p => p != fond),
                    $"{theme} — rien de dessiné au centre du canevas, le bouton semble absent.");
            });
        }

        private static Capture Rendre(AppTheme theme, Func<FrameworkElement> fabrique)
        {
            PreparerApplication();

            // SANS ANIMATION : le fondu clair/sombre ferait capturer un etat
            // transitoire, defaut deja paye cote WinForms.
            ThemeManager.Apply(theme, animate: false);

            var vue = fabrique();

            // Une Window non affichee ne rend RIEN. On dessine donc la vue dans
            // un Border autonome, qui porte le fond du theme.
            var racine = new Border
            {
                Background = (Brush)Application.Current.Resources[ThemeManager.BrushBg],
                Child = vue,
                Width = Largeur,
                Height = Hauteur,
            };

            racine.Measure(new Size(Largeur, Hauteur));
            racine.Arrange(new Rect(0, 0, Largeur, Hauteur));
            racine.UpdateLayout();

            var bitmap = new RenderTargetBitmap(Largeur, Hauteur, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(racine);

            return new Capture(vue, new Image(bitmap), bitmap);
        }

        private static Application? _application;

        /// <summary>
        /// Une `Application` avec les MÊMES ressources que la vraie.
        ///
        /// La liste est LUE DANS `App.xaml` plutôt que recopiée : l'ordre de
        /// fusion des dictionnaires décide de ce qu'un `StaticResource` voit —
        /// ce projet a déjà planté pour l'avoir eu faux — et une seconde liste à
        /// tenir à jour finirait par diverger sans que rien ne le dise.
        /// </summary>
        private static void PreparerApplication()
        {
            if (_application is not null) return;
            _application = Application.Current ?? new Application();

            var appXaml = Remonter("App.xaml");
            var doc = XDocument.Load(appXaml);
            XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

            foreach (var d in doc.Descendants(p + "ResourceDictionary")
                                 .Where(e => e.Attribute("Source") is not null))
            {
                var source = d.Attribute("Source")!.Value;
                _application.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/ChaturbateRecorder;component/{source}", UriKind.Absolute),
                });
            }

            // Les converters sont des classes C#, donc declares en elements et
            // non par Source. Meme lecture, meme fichier.
            foreach (var e in doc.Descendants().Where(e => e.Attribute(x + "Key") is not null
                                                          && e.Name != p + "ResourceDictionary"))
            {
                var cle = e.Attribute(x + "Key")!.Value;
                var type = Type.GetType($"ChaturbateRecorderApp.Converters.{e.Name.LocalName}, ChaturbateRecorder")
                           ?? Type.GetType($"System.Windows.Controls.{e.Name.LocalName}, PresentationFramework");
                if (type is null) continue;
                _application.Resources[cle] = Activator.CreateInstance(type);
            }
        }

        private static string Remonter(string nom)
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null)
            {
                var candidat = Path.Combine(d.FullName, nom);
                if (File.Exists(candidat)) return candidat;
                d = d.Parent;
            }
            throw new FileNotFoundException($"{nom} introuvable depuis {AppContext.BaseDirectory}");
        }

        private static Rect Zone(FrameworkElement element, FrameworkElement racine)
        {
            var coin = element.TransformToAncestor(racine).Transform(new Point(0, 0));
            return new Rect(coin.X, coin.Y, element.ActualWidth, element.ActualHeight);
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject racine) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(racine); i++)
            {
                var enfant = VisualTreeHelper.GetChild(racine, i);
                if (enfant is T trouve) yield return trouve;
                foreach (var petit in Descendants<T>(enfant)) yield return petit;
            }
        }

        /// <summary>
        /// Le PNG est déposé pour être REGARDÉ : aucun test ne remplace un coup
        /// d'œil, et la plupart des défauts visuels de ce portage ont été vus
        /// par un humain devant un écran, pas par une assertion.
        /// </summary>
        private static void Enregistrer(RenderTargetBitmap bitmap, string nom)
        {
            Directory.CreateDirectory(DossierSortie);
            var encodeur = new PngBitmapEncoder();
            encodeur.Frames.Add(BitmapFrame.Create(bitmap));
            using var flux = File.Create(Path.Combine(DossierSortie, nom + ".png"));
            encodeur.Save(flux);
        }

        private static System.Windows.Threading.Dispatcher? _repartiteur;
        private static readonly object _verrou = new();

        /// <summary>
        /// UN SEUL fil STA, persistant, pour TOUS ces tests.
        ///
        /// Un fil neuf par test paraissait plus propre et **faisait planter le
        /// processus hôte** : l'`Application` et ses ressources appartiennent au
        /// fil qui les a créées, et les réutiliser depuis un autre fil n'est pas
        /// une erreur rattrapable. Le symptôme n'était d'ailleurs pas une
        /// exception mais « Plantage du processus hôte de test », avec une partie
        /// de la suite jamais exécutée — donc un vert partiel et trompeur.
        ///
        /// Le fil porte une vraie boucle de répartition : `Dispatcher.Invoke`
        /// depuis xunit y dépose le travail et attend son retour. En arrière-plan
        /// (`IsBackground`), sans quoi il tiendrait le processus ouvert à la fin
        /// de la suite.
        /// </summary>
        private static void SurFilStandard(Action action)
        {
            lock (_verrou)
            {
                if (_repartiteur is null)
                {
                    var pret = new ManualResetEventSlim();
                    var fil = new Thread(() =>
                    {
                        _repartiteur = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        pret.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    })
                    { IsBackground = true, Name = "rendu-wpf" };
                    fil.SetApartmentState(ApartmentState.STA);
                    fil.Start();
                    pret.Wait();
                }
            }

            Exception? echec = null;
            _repartiteur!.Invoke(() =>
            {
                try { action(); }
                catch (Exception ex) { echec = ex; }
            });
            if (echec is not null) throw echec;
        }
    }

}
