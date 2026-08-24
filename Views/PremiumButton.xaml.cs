using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChaturbateRecorderApp.Views
{
    /// <summary>
    /// Bouton flottant « Passer à Premium » (120.0) : halo pourpre pulsé en
    /// continu, éclairs procéduraux au survol. Purement décoratif — toute la
    /// logique de licence/visibilité vit dans <c>MainWindow.xaml.cs</c>.
    /// </summary>
    public partial class PremiumButton : UserControl
    {
        private static readonly Random Rng = new();

        private readonly DispatcherTimer _minuteurEclairs;

        public PremiumButton()
        {
            InitializeComponent();

            DemarrerPulsation();

            // INTERVALLE ALÉATOIRE À CHAQUE TIC, pas fixe : un éclair qui
            // clignote à cadence parfaitement régulière a l'air mécanique.
            // Voir RegenererEclairs, qui reprogramme le prochain intervalle.
            _minuteurEclairs = new DispatcherTimer();
            _minuteurEclairs.Tick += (_, _) => RegenererEclairs();
        }

        /// <summary>Fondu lent de l'opacité du halo, boucle infinie — la « surbrillance animée » demandée, indépendante du survol.</summary>
        private void DemarrerPulsation()
        {
            var pulsation = new DoubleAnimation(0.45, 1.0, TimeSpan.FromMilliseconds(1400))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            Halo.BeginAnimation(OpacityProperty, pulsation);
        }

        private void Bouton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RegenererEclairs();
            _minuteurEclairs.Start();
        }

        private void Bouton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _minuteurEclairs.Stop();
            CanevasEclairs.Children.Clear();
        }

        /// <summary>
        /// Pose 2 à 4 éclairs neufs, chacun partant d'un point du CONTOUR du
        /// bouton vers un point plus loin à l'extérieur — jamais depuis le
        /// centre, qui ferait « exploser » le bouton plutôt que crépiter
        /// autour de lui. Chaque éclair s'efface tout seul (fondu), et le
        /// minuteur est reprogrammé à un intervalle aléatoire pour la
        /// prochaine salve : c'est ce qui évite le clignotement mécanique.
        /// </summary>
        private void RegenererEclairs()
        {
            CanevasEclairs.Children.Clear();

            var centre = new Point(ActualWidth / 2, ActualHeight / 2);
            // Rayon approximatif du bouton (moitié de sa largeur réelle, ou
            // une valeur de repli tant que le layout n'a pas encore mesuré).
            var rayon = Bouton.ActualWidth > 0 ? Bouton.ActualWidth / 2 : 60;

            var nombre = Rng.Next(2, 5);
            for (var i = 0; i < nombre; i++)
            {
                var angle = Rng.NextDouble() * Math.PI * 2;
                var depart = new Point(
                    centre.X + Math.Cos(angle) * rayon,
                    centre.Y + Math.Sin(angle) * rayon * 0.6); // ellipse, pas un cercle : le bouton est plus large que haut
                var distance = rayon * (0.7 + Rng.NextDouble() * 0.8);
                var arrivee = new Point(
                    centre.X + Math.Cos(angle) * (rayon + distance),
                    centre.Y + Math.Sin(angle) * (rayon + distance) * 0.6);

                var eclair = new Polyline
                {
                    Points = GenererTrajet(depart, arrivee, segments: 5, amplitude: 10),
                    Stroke = (Brush)FindResource("Premium.Eclair"),
                    StrokeThickness = 1.6,
                    StrokeLineJoin = PenLineJoin.Round,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.MediumPurple,
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 0.8,
                    },
                };
                CanevasEclairs.Children.Add(eclair);

                var fondu = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220 + Rng.Next(120)))
                { BeginTime = TimeSpan.FromMilliseconds(Rng.Next(80)) };
                eclair.BeginAnimation(OpacityProperty, fondu);
            }

            _minuteurEclairs.Interval = TimeSpan.FromMilliseconds(150 + Rng.Next(250));
        }

        /// <summary>
        /// Trajet en zigzag par déplacement latéral aléatoire, ATTÉNUÉ vers
        /// les deux extrémités (facteur sinus) : un éclair qui part bien DU
        /// bord du bouton et y revient proprement, au lieu de commencer par
        /// un angle brutal.
        /// </summary>
        private static PointCollection GenererTrajet(Point depart, Point arrivee, int segments, double amplitude)
        {
            var dx = arrivee.X - depart.X;
            var dy = arrivee.Y - depart.Y;
            var longueur = Math.Sqrt(dx * dx + dy * dy);
            var (px, py) = longueur > 0 ? (-dy / longueur, dx / longueur) : (0.0, 0.0);

            var points = new PointCollection { depart };
            for (var i = 1; i < segments; i++)
            {
                var t = (double)i / segments;
                var x = depart.X + dx * t;
                var y = depart.Y + dy * t;
                var decalage = (Rng.NextDouble() - 0.5) * amplitude * Math.Sin(Math.PI * t);
                points.Add(new Point(x + px * decalage, y + py * decalage));
            }
            points.Add(arrivee);
            return points;
        }

        private void Bouton_Click(object sender, RoutedEventArgs e)
        {
            new PremiumUpgradeWindow { Owner = Window.GetWindow(this) }.ShowDialog();
        }
    }
}
