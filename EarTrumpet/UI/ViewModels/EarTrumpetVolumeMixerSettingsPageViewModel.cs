namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetVolumeMixerSettingsPageViewModel : SettingsPageViewModel
    {
        public bool UseLegacyVolumeMixer
        {
            get => _settings.UseLegacyVolumeMixer;
            set
            {
                if (_settings.UseLegacyVolumeMixer != value)
                {
                    _settings.UseLegacyVolumeMixer = value;
                    RaisePropertyChanged(nameof(UseLegacyVolumeMixer));
                    RaisePropertyChanged(nameof(VolumeMixerModeText));
                }
            }
        }

        public string VolumeMixerModeText => UseLegacyVolumeMixer ?
            Properties.Resources.VolumeMixerLegacyModeText :
            Properties.Resources.VolumeMixerModernModeText;

        private readonly AppSettings _settings;

        public EarTrumpetVolumeMixerSettingsPageViewModel(AppSettings settings) : base(null)
        {
            _settings = settings;
            Title = Properties.Resources.VolumeMixerSettingsPageText;
            Glyph = "\xE9E9";
        }
    }
}
