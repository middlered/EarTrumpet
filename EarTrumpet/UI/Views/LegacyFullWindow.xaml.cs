using EarTrumpet.Extensions;
using EarTrumpet.Interop;
using EarTrumpet.UI.Controls;
using EarTrumpet.UI.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace EarTrumpet.UI.Views
{
    public partial class LegacyFullWindow : Window
    {
        private LegacyFullWindowViewModel ViewModel => (LegacyFullWindowViewModel)DataContext;
        private bool _hasAppliedLegacyDefaultSize;

        public LegacyFullWindow()
        {
            Trace.WriteLine("LegacyFullWindow .ctor");
            Closed += (_, __) =>
            {
                Trace.WriteLine("LegacyFullWindow Closed");

                if (DataContext is LegacyFullWindowViewModel viewModel)
                {
                    viewModel.AllDevices.CollectionChanged -= OnDevicesChanged;
                }
            };

            InitializeComponent();
            SourceInitialized += (sender, __) =>
            {
                this.Cloak();
                this.EnableRoundedCornersIfApplicable();

                if (App.Settings.FullMixerWindowPlacement != null)
                {
                    User32.SetWindowPlacement(new WindowInteropHelper((Window)sender).Handle, App.Settings.FullMixerWindowPlacement.Value);
                }
            };

            Closing += (sender, __) =>
            {
                if (User32.GetWindowPlacement(new WindowInteropHelper((Window)sender).Handle, out var placement))
                {
                    App.Settings.FullMixerWindowPlacement = placement;
                }
            };

            ContentRendered += (_, __) =>
            {
                ViewModel.AllDevices.CollectionChanged += OnDevicesChanged;
                OnDevicesChanged(null, null);
            };
        }

        private void OnDevicesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateLegacyWindowSize();
        }

        private void UpdateLegacyWindowSize()
        {
            const double legacyDefaultWidth = 600;

            if (ViewModel.AllDevices.Count == 0)
            {
                MaxWidth = double.PositiveInfinity;
                MinHeight = 0;
                MaxHeight = double.PositiveInfinity;
                SizeToContent = SizeToContent.WidthAndHeight;
                ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                SizeToContent = SizeToContent.Manual;
                Height = 360;
                MinHeight = 360;
                MaxHeight = 360;
                MinWidth = 480;
                MaxWidth = double.PositiveInfinity;
                if (!_hasAppliedLegacyDefaultSize)
                {
                    Width = legacyDefaultWidth;
                    _hasAppliedLegacyDefaultSize = true;
                }
                ResizeMode = ResizeMode.CanResize;
            }

            this.RemoveWindowStyle(User32.WS_MAXIMIZEBOX);
        }

        private void LegacyDeviceSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.ItemsSource = null;
                button.ContextMenu.Items.Clear();
                foreach (var device in ViewModel.LegacyDevices)
                {
                    button.ContextMenu.Items.Add(new MenuItem
                    {
                        Header = device.DisplayName,
                        Command = device.Select,
                        Icon = new ImageEx
                        {
                            Width = 16,
                            Height = 16,
                            SourceEx = device,
                            Stretch = Stretch.Uniform,
                        },
                        IsCheckable = true,
                        IsChecked = device.IsSelected,
                        Style = (Style)FindResource("LegacyDeviceMenuItemStyle"),
                    });
                }

                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void LegacyAppMuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IAppItemViewModel app)
            {
                ViewModel.ToggleLegacyAppMute(app);
                e.Handled = true;
            }
        }
    }
}
