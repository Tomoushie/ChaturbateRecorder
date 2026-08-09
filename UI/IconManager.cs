using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Svg;

namespace ChaturbateRecorderApp.UI
{
    /// <summary>
    /// Icônes vectorielles pour les boutons (3.1) : chaque glyphe est un SVG
    /// minimal défini en code (pas de fichiers externes à gérer/distribuer),
    /// rendu en Bitmap à la volée via la librairie Svg à la taille et à la
    /// couleur demandées — net à n'importe quelle résolution/DPI, et retintable
    /// selon le thème clair/sombre actif.
    /// </summary>
    public static class IconManager
    {
        private static readonly Dictionary<string, string> Templates = new()
        {
            ["play"] = Wrap("<polygon points=\"6,4 20,12 6,20\" fill=\"{COLOR}\"/>"),

            ["stop"] = Wrap("<rect x=\"5\" y=\"5\" width=\"14\" height=\"14\" rx=\"2\" fill=\"{COLOR}\"/>"),

            ["folder"] = Wrap(
                "<rect x=\"3\" y=\"5\" width=\"8\" height=\"4\" rx=\"1\" fill=\"{COLOR}\"/>" +
                "<rect x=\"3\" y=\"8\" width=\"18\" height=\"12\" rx=\"1.5\" fill=\"{COLOR}\"/>"),

            ["open"] = Wrap(
                "<polyline points=\"8,6 16,12 8,18\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"3\" " +
                "stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"),

            ["update"] = Wrap(
                "<polygon points=\"12,4 20,14 14,14 14,20 10,20 10,14 4,14\" fill=\"{COLOR}\"/>"),

            ["book"] = Wrap(
                "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"1.5\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>" +
                "<line x1=\"12\" y1=\"4\" x2=\"12\" y2=\"20\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>"),

            ["globe"] = Wrap(
                "<circle cx=\"12\" cy=\"12\" r=\"9\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>" +
                "<line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>" +
                "<ellipse cx=\"12\" cy=\"12\" rx=\"4\" ry=\"9\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>"),

            // Curseurs de réglage (19.0, bouton "Paramètres") : plus simple à
            // dessiner qu'un vrai engrenage avec des primitives de base, tout
            // aussi reconnaissable comme icône de préférences.
            ["sliders"] = Wrap(
                "<line x1=\"4\" y1=\"7\" x2=\"20\" y2=\"7\" stroke=\"{COLOR}\" stroke-width=\"2\"/>" +
                "<circle cx=\"15\" cy=\"7\" r=\"2.5\" fill=\"{COLOR}\"/>" +
                "<line x1=\"4\" y1=\"12\" x2=\"20\" y2=\"12\" stroke=\"{COLOR}\" stroke-width=\"2\"/>" +
                "<circle cx=\"9\" cy=\"12\" r=\"2.5\" fill=\"{COLOR}\"/>" +
                "<line x1=\"4\" y1=\"17\" x2=\"20\" y2=\"17\" stroke=\"{COLOR}\" stroke-width=\"2\"/>" +
                "<circle cx=\"17\" cy=\"17\" r=\"2.5\" fill=\"{COLOR}\"/>"),

            // Point d'exclamation (18.0, bouton "Signaler un bug").
            ["alert"] = Wrap(
                "<circle cx=\"12\" cy=\"12\" r=\"9\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.5\"/>" +
                "<line x1=\"12\" y1=\"7\" x2=\"12\" y2=\"13\" stroke=\"{COLOR}\" stroke-width=\"2\" stroke-linecap=\"round\"/>" +
                "<circle cx=\"12\" cy=\"16.5\" r=\"1.3\" fill=\"{COLOR}\"/>"),

            // Ligne de pouls/ECG (2.3, bouton "Diagnostic").
            ["pulse"] = Wrap(
                "<polyline points=\"3,12 8,12 10,6 14,18 16,12 21,12\" fill=\"none\" stroke=\"{COLOR}\" " +
                "stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"),

            // --- 103.0 : plateformes prises en charge ---
            //
            // Glyphes SIMPLIFIÉS et monochromes, pas les logos officiels. Deux
            // raisons : IconManager retint chaque icône à la couleur du thème,
            // ce que les chartes de marque de YouTube et Twitch interdisent
            // explicitement sur leur logo ; et un pictogramme sobre indiquant
            // la compatibilité relève de l'usage nominatif, admis, là où
            // reproduire une marque ne l'est pas.

            // Twitch : l'écran à encoche, avec ses deux barres.
            ["twitch"] = Wrap(
                "<path d=\"M5 3 H21 V15 L16.5 19.5 H12.5 L9 23 V19.5 H5 Z\" fill=\"none\" stroke=\"{COLOR}\" " +
                "stroke-width=\"1.8\" stroke-linejoin=\"round\"/>" +
                "<rect x=\"11\" y=\"7.5\" width=\"1.8\" height=\"6\" fill=\"{COLOR}\"/>" +
                "<rect x=\"15.5\" y=\"7.5\" width=\"1.8\" height=\"6\" fill=\"{COLOR}\"/>"),

            // YouTube : l'écran arrondi et sa flèche de lecture.
            ["youtube"] = Wrap(
                "<rect x=\"2.5\" y=\"5.5\" width=\"19\" height=\"13\" rx=\"3.5\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.8\"/>" +
                "<polygon points=\"10,9.5 16,12 10,14.5\" fill=\"{COLOR}\"/>"),

            // TikTok : la note de musique.
            ["tiktok"] = Wrap(
                "<path d=\"M14 3 V13.5 a3.5 3.5 0 1 1 -2.5 -3.36\" fill=\"none\" stroke=\"{COLOR}\" " +
                "stroke-width=\"1.8\" stroke-linecap=\"round\"/>" +
                "<path d=\"M14 3 c0.6 2.6 2.4 4.2 5 4.5\" fill=\"none\" stroke=\"{COLOR}\" " +
                "stroke-width=\"1.8\" stroke-linecap=\"round\"/>"),

            // Chaturbate n'a pas de marque réductible à un pictogramme simple :
            // une caméra générique dit « diffusion en direct » sans rien
            // emprunter à personne.
            ["camera"] = Wrap(
                "<rect x=\"2.5\" y=\"6\" width=\"13\" height=\"12\" rx=\"2\" fill=\"none\" stroke=\"{COLOR}\" stroke-width=\"1.8\"/>" +
                "<polygon points=\"17,10 21.5,7 21.5,17 17,14\" fill=\"{COLOR}\"/>"),

            // Cœur plein (80.0, bouton "Sponsoriser"). Deux arcs symétriques
            // fermés par une pointe en bas, dessinés à la courbe plutôt qu'avec
            // un caractère ❤ : le rendu reste net à toute taille et suit la
            // couleur du thème, comme les autres icônes.
            ["heart"] = Wrap(
                "<path d=\"M12 20.8 4.2 13a4.6 4.6 0 0 1 0-6.5 4.6 4.6 0 0 1 6.5 0l1.3 1.3 1.3-1.3a4.6 4.6 0 0 1 " +
                "6.5 0 4.6 4.6 0 0 1 0 6.5z\" fill=\"{COLOR}\"/>"),
        };

        private static string Wrap(string inner) =>
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">{inner}</svg>";

        public static Bitmap Render(string name, int size, Color color)
        {
            if (!Templates.TryGetValue(name, out var template))
                throw new ArgumentException($"Icône inconnue : '{name}'", nameof(name));

            var colorHex = ColorTranslator.ToHtml(color);
            var svgText = template.Replace("{COLOR}", colorHex);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));
            var document = SvgDocument.Open<SvgDocument>(stream);
            return document.Draw(size, size);
        }
    }
}
