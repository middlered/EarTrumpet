using EarTrumpet.UI.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    public class LegacyFullWindowViewModel : BindableBase, IPopupHostViewModel
    {
        public static readonly int SmallDeviceCountLimit = 3;

        public ObservableCollection<DeviceViewModel> AllDevices => _mainViewModel.AllDevices;
        public DeviceViewModel DefaultDevice => _mainViewModel.Default;
        public string Title => DefaultDevice != null ?
            $"{Properties.Resources.FullWindowTitleText} - {DefaultDevice.DisplayName}" :
            Properties.Resources.FullWindowTitleText;
        public string DefaultDeviceShortName => GetShortDeviceName(DefaultDevice?.DisplayName);
        public bool HasMultipleDevices => AllDevices.Count > 1;
        public bool IsDeviceListOpen
        {
            get => _isDeviceListOpen;
            set
            {
                if (_isDeviceListOpen != value)
                {
                    _isDeviceListOpen = value;
                    RaisePropertyChanged(nameof(IsDeviceListOpen));
                }
            }
        }
        public ObservableCollection<LegacyDeviceItemViewModel> LegacyDevices { get; } = new ObservableCollection<LegacyDeviceItemViewModel>();
        public ModalDialogViewModel Dialog { get; }
        public ICommand ToggleDeviceList { get; }
        public ICommand ToggleLegacyMasterMute { get; }
        public ICommand DisplaySettingsChanged { get; }
        public bool IsManyDevicesMode => AllDevices.Count > SmallDeviceCountLimit;

        private readonly DeviceCollectionViewModel _mainViewModel;
        private WindowViewState _state;
        private bool _isDeviceListOpen;

        public LegacyFullWindowViewModel(DeviceCollectionViewModel mainViewModel)
        {
            Dialog = new ModalDialogViewModel();
            _mainViewModel = mainViewModel;
            _mainViewModel.OnFullWindowOpened();
            _mainViewModel.AllDevices.CollectionChanged += OnDevicesChanged;
            _mainViewModel.DefaultChanged += OnDefaultDeviceChanged;

            RebuildLegacyDevices();
            ToggleDeviceList = new RelayCommand(() => IsDeviceListOpen = !IsDeviceListOpen);
            ToggleLegacyMasterMute = new RelayCommand(ToggleLegacyMasterMuteImpl);
            DisplaySettingsChanged = new RelayCommand(() => Dialog.IsVisible = false);
        }

        private void OnDevicesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildLegacyDevices();
            RaisePropertyChanged(nameof(IsManyDevicesMode));
            RaisePropertyChanged(nameof(HasMultipleDevices));
        }

        private void OnDefaultDeviceChanged(object sender, DeviceViewModel e)
        {
            Dialog.IsVisible = false;
            IsDeviceListOpen = false;
            RebuildLegacyDevices();
            RaisePropertyChanged(nameof(DefaultDevice));
            RaisePropertyChanged(nameof(DefaultDeviceShortName));
            RaisePropertyChanged(nameof(Title));
        }

        public void OpenPopup(object vm, FrameworkElement container)
        {
            Dialog.IsVisible = false;

            if (vm is IAppItemViewModel)
            {
                Dialog.Focused = new FocusedAppItemViewModel(_mainViewModel, (IAppItemViewModel)vm);
            }
            else
            {
                var deviceViewModel = new FocusedDeviceViewModel(_mainViewModel, (DeviceViewModel)vm);
                if (deviceViewModel.IsApplicable)
                {
                    Dialog.Focused = deviceViewModel;
                }
            }

            if (Dialog.Focused != null)
            {
                Dialog.Focused.RequestClose += () => Dialog.IsVisible = false;
                Dialog.Source = container;
                Dialog.IsVisible = true;
            }
        }

        public void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch (_state)
            {
                case WindowViewState.Open:
                    _state = WindowViewState.Closing;
                    e.Cancel = true;

                    Dialog.IsVisible = false;
                    _mainViewModel.DefaultChanged -= OnDefaultDeviceChanged;
                    _mainViewModel.AllDevices.CollectionChanged -= OnDevicesChanged;
                    _mainViewModel.OnFullWindowClosed();

                    var window = (Window)sender;
                    WindowAnimationLibrary.BeginWindowExitAnimation(window, () =>
                    {
                        _state = WindowViewState.CloseReady;
                        window.Close();
                    });
                    break;
                case WindowViewState.Closing:
                    // Ignore any requests while playing the close animation.
                    e.Cancel = true;
                    break;
                case WindowViewState.CloseReady:
                    // Accept the close.
                    break;
            }
        }

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (Dialog.IsVisible)
                {
                    Dialog.IsVisible = false;
                }
                else
                {
                    ((Window)sender).Close();
                }
            }
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dialog.IsVisible = false;
        }

        public void OnLocationChanged(object sender, EventArgs e)
        {
            Dialog.IsVisible = false;
        }

        public void OnLightDismissBorderPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Dialog.IsVisible = false;
            e.Handled = true;
        }

        private void RebuildLegacyDevices()
        {
            LegacyDevices.Clear();

            foreach (var device in AllDevices)
            {
                LegacyDevices.Add(new LegacyDeviceItemViewModel(this, device));
            }
        }

        private void SelectLegacyDevice(DeviceViewModel device)
        {
            if (device != null && device.Id != DefaultDevice?.Id)
            {
                device.MakeDefaultDevice();
            }
            else
            {
                IsDeviceListOpen = false;
            }
        }

        private void ToggleLegacyMasterMuteImpl()
        {
            if (DefaultDevice == null)
            {
                return;
            }

            var isMuted = !DefaultDevice.IsMuted;
            DefaultDevice.IsMuted = isMuted;

            foreach (var app in DefaultDevice.Apps)
            {
                SetLegacyAppMuted(app, isMuted);
            }
        }

        public void ToggleLegacyAppMute(IAppItemViewModel app)
        {
            if (app == null || DefaultDevice == null)
            {
                return;
            }

            var isMuted = !app.IsMuted;
            SetLegacyAppMuted(app, isMuted);

            if (!isMuted && DefaultDevice.IsMuted)
            {
                DefaultDevice.IsMuted = false;
            }
        }

        private static void SetLegacyAppMuted(IAppItemViewModel app, bool isMuted)
        {
            app.IsMuted = isMuted;

            if (app.ChildApps != null)
            {
                foreach (var childApp in app.ChildApps)
                {
                    SetLegacyAppMuted(childApp, isMuted);
                }
            }
        }

        private static string GetShortDeviceName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            var parenIndex = displayName.IndexOf(" (", StringComparison.Ordinal);
            if (parenIndex < 0)
            {
                parenIndex = displayName.IndexOf("（", StringComparison.Ordinal);
            }
            if (parenIndex < 0)
            {
                parenIndex = displayName.IndexOf("(", StringComparison.Ordinal);
            }

            if (parenIndex > 0)
            {
                return displayName.Substring(0, parenIndex);
            }

            return displayName;
        }

        public class LegacyDeviceItemViewModel : IAppIconSource
        {
            private readonly LegacyFullWindowViewModel _parent;
            private readonly DeviceViewModel _device;

            public string DisplayName => _device.DisplayName;
            public string ShortName => GetShortDeviceName(_device.DisplayName);
            public string IconPath => _device.IconPath;
            public bool IsDesktopApp => _device.IsDesktopApp;
            public bool IsSelected => _device.Id == _parent.DefaultDevice?.Id;
            public ICommand Select { get; }

            public LegacyDeviceItemViewModel(LegacyFullWindowViewModel parent, DeviceViewModel device)
            {
                _parent = parent;
                _device = device;
                Select = new RelayCommand(() => _parent.SelectLegacyDevice(_device));
            }
        }
    }
}
