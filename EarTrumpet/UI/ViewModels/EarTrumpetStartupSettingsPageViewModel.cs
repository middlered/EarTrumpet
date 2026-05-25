namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetStartupSettingsPageViewModel : SettingsPageViewModel
    {
        public bool RunAtStartup
        {
            get => _settings.RunAtStartup;
            set => _settings.RunAtStartup = value;
        }

        private readonly AppSettings _settings;

        public EarTrumpetStartupSettingsPageViewModel(AppSettings settings) : base(null)
        {
            _settings = settings;
            Title = Properties.Resources.StartupSettingsPageText;
            Glyph = "\xE7E8";
        }
    }
}
