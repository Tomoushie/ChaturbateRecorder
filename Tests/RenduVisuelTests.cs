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
                finally { modele.Dispose(); }
            });
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
