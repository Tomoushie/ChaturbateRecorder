using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChaturbateRecorderApp.Config;
using ChaturbateRecorderApp.Services;
using ChaturbateRecorderApp.UI;

namespace ChaturbateRecorderApp.ViewModels
{
    public partial class ReportViewModel : ObservableObject
    {
        public sealed record OptionType(string Libelle, ReportKind Valeur) { public override string ToString() => Libelle; }

        public IReadOnlyList<OptionType> Types { get; } = new List<OptionType>
        {
            new OptionType(Localization.Get("report.kind.bug"), ReportKind.Bug),
            new OptionType(Localization.Get("report.kind.feature"), ReportKind.Feature),
            new OptionType(Localization.Get("report.kind.feedback"), ReportKind.Feedback)
        };

        [ObservableProperty]
        private OptionType? _typeChoisi;

        [ObservableProperty]
        private string _titre = "";

        [ObservableProperty]
        private string _corps = "";

        [ObservableProperty]
        private string _message = "";

        [ObservableProperty]
        private bool _envoiEnCours;

        [ObservableProperty]
        private bool _envoye;

        [ObservableProperty]
        private string _urlIssue = "";

        public string Contexte { get; } = ReportSender.BuildContext(false, Localization.Current);

        public bool RelaisDisponible { get; } = ReportSender.IsConfigured;

        public ReportViewModel()
        {
            Types = new List<OptionType>
            {
                new OptionType(Localization.Get("report.kind.bug"), ReportKind.Bug),
                new OptionType(Localization.Get("report.kind.feature"), ReportKind.Feature),
                new OptionType(Localization.Get("report.kind.feedback"), ReportKind.Feedback)
            };
            TypeChoisi = Types[0];
            Contexte = ReportSender.BuildContext(false, Localization.Current);
            if (!RelaisDisponible)
            {
                Message = Localization.Get("report.noRelay");
            }
        }

        [RelayCommand]
        private async Task EnvoyerAsync()
        {
            if (EnvoiEnCours || !RelaisDisponible)
            {
                return;
            }

            var motif = ReportSender.Validate(Titre, Corps);
            if (motif is not null)
            {
                Message = Localization.Get(motif);
                return;
            }

            EnvoiEnCours = true;
            Message = Localization.Get("report.sending");

            var version = typeof(ReportViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            try
            {
                var resultat = await ReportSender.SendAsync(TypeChoisi?.Valeur ?? ReportKind.Bug, Titre, Corps, version, Contexte);
                if (resultat.Success)
                {
                    Envoye = true;
                    UrlIssue = resultat.IssueUrl;
                    Message = Localization.Get("report.sent");
                }
                else
                {
                    Message = Localization.Get("report.error." + resultat.ErrorCode) ?? Localization.Get("report.error.network");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
                Message = Localization.Get("report.error.network");
            }
            finally
            {
                EnvoiEnCours = false;
            }
        }

        [RelayCommand]
        private void OuvrirIssue()
        {
            if (string.IsNullOrEmpty(UrlIssue))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(UrlIssue) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, LogLevel.WARN);
            }
        }
    }
}