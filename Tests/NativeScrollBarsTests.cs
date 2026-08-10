using System.Windows.Forms;
using ChaturbateRecorderApp.UI;
using Xunit;

namespace ChaturbateRecorderApp.Tests
{
    /// <summary>
    /// Ascenseurs natifs en thème sombre (114.0).
    ///
    /// **Ce que ces tests peuvent et ne peuvent pas prouver** : un ascenseur
    /// est peint par Windows en zone NON CLIENTE, et <c>SetWindowTheme</c> ne
    /// renvoie rien d'exploitable — « la barre est grise » ne se vérifie qu'à
    /// la capture d'écran, ce qui a été fait. Ce qui se teste, en revanche,
    /// c'est la cause exacte du défaut signalé : le traitement n'était câblé
    /// que sur un seul type de contrôle, et personne ne s'en apercevait tant
    /// qu'on ne regardait pas les Favoris ou les Logs.
    /// </summary>
    public class NativeScrollBarsTests
    {
        [Fact]
        public void EveryControlThatCanShowAScrollBarIsHandled()
        {
            using var form = new Form();
            var favorites = new ListBox();                   // panneau Favoris, panneau Logs
            var legal = new TextBox { Multiline = true };    // note de légalité, remerciements
            var changelog = new RichTextBox();               // dialogue « Nouveautés »
            var content = new Panel { AutoScroll = true };   // fenêtre principale, liste des jobs
            var history = new ListView();                    // historique, surveillance
            form.Controls.AddRange(new Control[] { favorites, legal, changelog, content, history });

            ThemeManager.Apply(form, AppTheme.Dark);

            Assert.True(NativeScrollBars.IsWired(favorites), "ListBox : Favoris et Logs");
            Assert.True(NativeScrollBars.IsWired(legal), "TextBox multiligne : légalité, remerciements");
            Assert.True(NativeScrollBars.IsWired(changelog), "RichTextBox : dialogue Nouveautés");
            Assert.True(NativeScrollBars.IsWired(content), "Panel AutoScroll : fenêtre principale");
            Assert.True(NativeScrollBars.IsWired(history), "ListView : historique et surveillance");
        }

        /// <summary>
        /// Le traitement doit descendre l'arbre : les contrôles concernés sont
        /// tous imbriqués dans une carte, jamais posés directement sur la
        /// fenêtre.
        /// </summary>
        [Fact]
        public void NestedControlsAreReachedToo()
        {
            using var form = new Form();
            var card = new Panel();
            var inner = new Panel();
            var list = new ListBox();
            inner.Controls.Add(list);
            card.Controls.Add(inner);
            form.Controls.Add(card);

            ThemeManager.Apply(form, AppTheme.Light);

            Assert.True(NativeScrollBars.IsWired(list));
        }

        /// <summary>
        /// Un contrôle jamais affiché n'a pas de handle. Le premier passage
        /// serait alors perdu en silence, et l'ascenseur resterait clair
        /// jusqu'au prochain changement de thème — mode de panne d'autant plus
        /// vicieux qu'il ne concerne que le mode simple et les fenêtres pas
        /// encore ouvertes.
        /// </summary>
        [Fact]
        public void AControlWithoutAHandleIsStillRegisteredForLater()
        {
            var list = new ListBox();
            Assert.False(list.IsHandleCreated);

            NativeScrollBars.Apply(list, ThemeManager.GetPalette(AppTheme.Dark));

            Assert.True(NativeScrollBars.IsWired(list));
            list.Dispose();
        }

        /// <summary>
        /// Le seuil décide de la bascule pendant le fondu clair/sombre. Il
        /// vivait dans ThemedListView ; l'avoir déplacé ne doit pas l'avoir
        /// changé, sans quoi les ascenseurs des listes basculeraient à un autre
        /// moment que ceux du reste de la fenêtre.
        /// </summary>
        [Theory]
        [InlineData(AppTheme.Dark, true)]
        [InlineData(AppTheme.Light, false)]
        public void ThemeIsDeducedFromTheInputSurface(AppTheme theme, bool expectedDark)
        {
            Assert.Equal(expectedDark, NativeScrollBars.IsDark(ThemeManager.GetPalette(theme)));
        }
    }
}
