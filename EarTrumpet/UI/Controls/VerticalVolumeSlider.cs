using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EarTrumpet.UI.Helpers;

namespace EarTrumpet.UI.Controls
{
    public class VerticalVolumeSlider : Slider
    {
        public float PeakValue1
        {
            get { return (float)GetValue(PeakValue1Property); }
            set { SetValue(PeakValue1Property, value); }
        }
        public static readonly DependencyProperty PeakValue1Property = DependencyProperty.Register(
            "PeakValue1", typeof(float), typeof(VerticalVolumeSlider), new PropertyMetadata(0f, OnPeakValueChanged));

        public float PeakValue2
        {
            get { return (float)GetValue(PeakValue2Property); }
            set { SetValue(PeakValue2Property, value); }
        }
        public static readonly DependencyProperty PeakValue2Property = DependencyProperty.Register(
            "PeakValue2", typeof(float), typeof(VerticalVolumeSlider), new PropertyMetadata(0f, OnPeakValueChanged));

        public bool PlaySoundOnVolumeChange
        {
            get { return (bool)GetValue(PlaySoundOnVolumeChangeProperty); }
            set { SetValue(PlaySoundOnVolumeChangeProperty, value); }
        }
        public static readonly DependencyProperty PlaySoundOnVolumeChangeProperty = DependencyProperty.Register(
            "PlaySoundOnVolumeChange", typeof(bool), typeof(VerticalVolumeSlider), new PropertyMetadata(false));

        private Border _peakMeter1;
        private Border _peakMeter2;
        private Border _volumeFill;
        private Thumb _thumb;
        private TextBlock _dragToolTipText;
        private Popup _dragPopup;
        private bool _isDragging;
        private bool _isDragPopupTracking;
        private const double ToolTipOffset = 6;

        public VerticalVolumeSlider()
        {
            Orientation = Orientation.Vertical;
            Minimum = 0;
            Maximum = 100;
            PreviewMouseDown += OnMouseDown;
            PreviewTouchDown += OnTouchDown;
            MouseMove += OnMouseMove;
            TouchMove += OnTouchMove;
            MouseUp += OnMouseUp;
            TouchUp += OnTouchUp;
            LostMouseCapture += OnLostMouseCapture;
            LostTouchCapture += OnLostTouchCapture;
            MouseWheel += OnMouseWheel;
            Loaded += (_, __) => ScheduleSizeOrVolumeOrPeakValueChanged();
            IsVisibleChanged += (_, __) => ScheduleSizeOrVolumeOrPeakValueChanged();
            DataContextChanged += (_, __) => ScheduleSizeOrVolumeOrPeakValueChanged();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _peakMeter1 = (Border)GetTemplateChild("PeakMeter1");
            _peakMeter2 = (Border)GetTemplateChild("PeakMeter2");
            _volumeFill = (Border)GetTemplateChild("VolumeFill");
            _thumb = (Thumb)GetTemplateChild("SliderThumb");
            SizeOrVolumeOrPeakValueChanged();
            ScheduleSizeOrVolumeOrPeakValueChanged();
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var ret = base.ArrangeOverride(arrangeBounds);
            SizeOrVolumeOrPeakValueChanged();
            return ret;
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            SizeOrVolumeOrPeakValueChanged();
            if (_isDragging)
            {
                UpdateDragToolTip();
            }
        }

        private static void OnPeakValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VerticalVolumeSlider)d).SizeOrVolumeOrPeakValueChanged();
        }

        private void SizeOrVolumeOrPeakValueChanged()
        {
            var usableHeight = GetTrackHeight();
            var volumeScale = Value / 100f;

            if (_volumeFill != null)
            {
                _volumeFill.Height = Math.Max(0, usableHeight * volumeScale);
            }

            if (_peakMeter1 != null)
            {
                _peakMeter1.Height = Math.Max(0, usableHeight * PeakValue1 * volumeScale);
            }

            if (_peakMeter2 != null)
            {
                _peakMeter2.Height = Math.Max(0, usableHeight * PeakValue2 * volumeScale);
            }
        }

        private void ScheduleSizeOrVolumeOrPeakValueChanged()
        {
            Dispatcher.BeginInvoke((Action)SizeOrVolumeOrPeakValueChanged, DispatcherPriority.Loaded);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetPositionByControlPoint(e.GetPosition(this));
                CaptureMouse();
                _isDragging = true;
                ShowDragToolTip();
                e.Handled = true;
            }
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            SetPositionByControlPoint(e.GetTouchPoint(this).Position);
            CaptureTouch(e.TouchDevice);
            _isDragging = true;
            ShowDragToolTip();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                SetPositionByControlPoint(e.GetPosition(this));
                UpdateDragToolTip();
                e.Handled = true;
            }
        }

        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            if (AreAnyTouchesCaptured)
            {
                SetPositionByControlPoint(e.GetTouchPoint(this).Position);
                UpdateDragToolTip();
                e.Handled = true;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && IsMouseCaptured)
            {
                ReleaseMouseCapture();
                _isDragging = false;
                HideDragToolTip();
                PlayBeepSound();
                e.Handled = true;
            }
        }

        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            if (AreAnyTouchesCaptured)
            {
                ReleaseTouchCapture(e.TouchDevice);
                _isDragging = false;
                HideDragToolTip();
                PlayBeepSound();
                e.Handled = true;
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            HideDragToolTip();
        }

        private void OnLostTouchCapture(object sender, TouchEventArgs e)
        {
            _isDragging = false;
            HideDragToolTip();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Value = Bound(Value + (Math.Sign(e.Delta) * 2.0));
            e.Handled = true;
        }

        private void SetPositionByControlPoint(Point point)
        {
            if (ActualHeight <= 0)
            {
                return;
            }

            var trackMargin = GetTrackMargin();
            var usableHeight = Math.Max(1, ActualHeight - trackMargin.Top - trackMargin.Bottom);
            var percent = 1 - ((point.Y - trackMargin.Top) / usableHeight);
            Value = Bound((Maximum - Minimum) * percent);
        }

        private double Bound(double val)
        {
            return Math.Max(Minimum, Math.Min(Maximum, val));
        }

        private double GetThumbHeight()
        {
            if (_thumb?.ActualHeight > 0)
            {
                return _thumb.ActualHeight;
            }

            if (_thumb?.Height > 0)
            {
                return _thumb.Height;
            }

            return 8;
        }

        private double GetTrackHeight()
        {
            var margin = GetTrackMargin();
            return Math.Max(0, ActualHeight - margin.Top - margin.Bottom);
        }

        private Thickness GetTrackMargin()
        {
            if (_volumeFill != null)
            {
                return _volumeFill.Margin;
            }

            return new Thickness(0);
        }

        private void PlayBeepSound()
        {
            if (PlaySoundOnVolumeChange)
            {
                SystemSoundsHelper.PlayBeepSound.Execute(null);
            }
        }

        private void ShowDragToolTip()
        {
            if (_thumb == null)
            {
                return;
            }

            if (_dragPopup == null)
            {
                _dragToolTipText = new TextBlock
                {
                    Text = Math.Round(Value).ToString()
                };
                var toolTipBorder = new Border
                {
                    Padding = new Thickness(4),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Child = _dragToolTipText
                };
                toolTipBorder.Background = SystemColors.ControlLightLightBrush;
                toolTipBorder.BorderBrush = SystemColors.ControlDarkBrush;
                _dragToolTipText.Foreground = SystemColors.ControlTextBrush;
                _dragPopup = new Popup
                {
                    Placement = PlacementMode.Relative,
                    PlacementTarget = this,
                    StaysOpen = true,
                    AllowsTransparency = true,
                    Child = toolTipBorder
                };
            }

            UpdateDragToolTip();
            _dragPopup.IsOpen = true;
            StartDragPopupTracking();
        }

        private void UpdateDragToolTip()
        {
            if (_dragToolTipText != null)
            {
                _dragToolTipText.Text = Math.Round(Value).ToString();
                if (_dragPopup != null && _thumb != null)
                {
                    var toolTipElement = _dragPopup.Child as FrameworkElement;
                    if (toolTipElement != null)
                    {
                        toolTipElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    }

                    var thumbPoint = _thumb.TransformToAncestor(this).Transform(new Point(0, 0));
                    var toolTipSize = toolTipElement?.DesiredSize ?? new Size(0, 0);
                    _dragPopup.HorizontalOffset = thumbPoint.X - toolTipSize.Width - ToolTipOffset;
                    _dragPopup.VerticalOffset = thumbPoint.Y + (_thumb.ActualHeight - toolTipSize.Height) / 2;
                }
            }
        }

        private void HideDragToolTip()
        {
            if (_dragPopup != null)
            {
                _dragPopup.IsOpen = false;
            }

            StopDragPopupTracking();
        }

        private void StartDragPopupTracking()
        {
            if (_isDragPopupTracking)
            {
                return;
            }

            CompositionTarget.Rendering += OnCompositionTargetRendering;
            _isDragPopupTracking = true;
        }

        private void StopDragPopupTracking()
        {
            if (!_isDragPopupTracking)
            {
                return;
            }

            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            _isDragPopupTracking = false;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (_isDragging && _dragPopup?.IsOpen == true)
            {
                UpdateDragToolTip();
            }
        }
    }
}
