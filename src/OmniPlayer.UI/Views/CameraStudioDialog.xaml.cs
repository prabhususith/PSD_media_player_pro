using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OmniPlayer.UI.Views
{
    public partial class CameraStudioDialog : Window
    {
        private readonly DispatcherTimer _effectsTimer;
        private readonly Random _rand = new();
        private string _activeEffect = "Clean";
        private readonly List<Particle> _particles = new();
        private readonly Queue<Ellipse> _ellipsePool = new();
        private bool _isRecording = false;
        private DateTime _recordingStartTime;
        private readonly DispatcherTimer _recordingTimer;
        private string _outputPicturesDir = "";
        private string _outputVideosDir = "";

        // Cached geometries and shapes for Smiling Face AR effect
        private System.Windows.Shapes.Path? _smilePath;
        private PathGeometry? _smileGeom;
        private PathFigure? _smileFig;
        private QuadraticBezierSegment? _smileSegment;

        private System.Windows.Shapes.Path? _leftEyePath;
        private PathGeometry? _leftEyeGeom;
        private PathFigure? _leftEyeFig;
        private QuadraticBezierSegment? _leftEyeSegment;

        private System.Windows.Shapes.Path? _rightEyePath;
        private PathGeometry? _rightEyeGeom;
        private PathFigure? _rightEyeFig;
        private QuadraticBezierSegment? _rightEyeSegment;

        private Ellipse? _leftCheek;
        private Ellipse? _rightCheek;
        private bool _smileElementsInitialized = false;

        private static readonly SolidColorBrush _snowBrush = new(Color.FromArgb(200, 255, 255, 255));
        private static readonly SolidColorBrush _fireBrushRed = new(Color.FromRgb(255, 69, 0));
        private static readonly SolidColorBrush _fireBrushOrange = new(Color.FromRgb(255, 140, 0));
        private static readonly SolidColorBrush _fireBrushGold = new(Color.FromRgb(255, 215, 0));

        private Ellipse RentEllipse()
        {
            if (_ellipsePool.Count > 0)
            {
                var el = _ellipsePool.Dequeue();
                el.Visibility = Visibility.Visible;
                return el;
            }
            var newEl = new Ellipse();
            CanvasEffectsOverlay.Children.Add(newEl);
            return newEl;
        }

        private void ReturnEllipse(Ellipse el)
        {
            el.Visibility = Visibility.Collapsed;
            _ellipsePool.Enqueue(el);
        }

        private class Particle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Size { get; set; }
            public double Life { get; set; }
            public double MaxLife { get; set; }
            public Color BaseColor { get; set; }
            public FrameworkElement? Element { get; set; }
        }

        public CameraStudioDialog()
        {
            InitializeComponent();

            InitializeDirectories();

            // Set up real-time 60 FPS effects animation loop
            _effectsTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 fps
            };
            _effectsTimer.Tick += EffectsTimer_Tick;
            _effectsTimer.Start();

            // Recording timer
            _recordingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _recordingTimer.Tick += RecordingTimer_Tick;

            Loaded += (s, e) => ApplyMirrorTransform();
            Closed += (s, e) =>
            {
                _effectsTimer.Stop();
                _recordingTimer.Stop();
            };
        }

        private void InitializeDirectories()
        {
            try
            {
                var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                _outputPicturesDir = System.IO.Path.Combine(pictures, "PSD_Camera");
                if (!Directory.Exists(_outputPicturesDir))
                {
                    Directory.CreateDirectory(_outputPicturesDir);
                }

                var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                _outputVideosDir = System.IO.Path.Combine(videos, "PSD_Camera");
                if (!Directory.Exists(_outputVideosDir))
                {
                    Directory.CreateDirectory(_outputVideosDir);
                }

                TxtSavePath.Text = _outputPicturesDir;
            }
            catch
            {
                _outputPicturesDir = AppDomain.CurrentDomain.BaseDirectory;
                _outputVideosDir = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private void CmbCameraDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCameraDevices.SelectedItem is ComboBoxItem item)
            {
                TxtCameraStatus.Text = $"LIVE: {item.Content} (Ready)";
            }
        }

        private void BtnEffect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _activeEffect = tag;
                TxtActiveEffectTag.Text = $"FX: {tag.ToUpperInvariant()}";
                _particles.Clear();
                _ellipsePool.Clear();
                _smileElementsInitialized = false;
                CanvasEffectsOverlay.Children.Clear();
            }
        }

        private void EffectsTimer_Tick(object? sender, EventArgs e)
        {
            double w = CanvasEffectsOverlay.ActualWidth;
            double h = CanvasEffectsOverlay.ActualHeight;
            if (w <= 0 || h <= 0) return;

            switch (_activeEffect)
            {
                case "Fire":
                    UpdateFireEffect(w, h);
                    break;
                case "Snow":
                    UpdateSnowEffect(w, h);
                    break;
                case "Smile":
                    UpdateSmilingFaceEffect(w, h);
                    break;
                case "Matrix":
                    UpdateMatrixRainEffect(w, h);
                    break;
                case "Neon":
                    UpdateNeonEdgeEffect(w, h);
                    break;
                case "Night":
                    UpdateNightVisionEffect(w, h);
                    break;
                case "Vhs":
                    UpdateVhsEffect(w, h);
                    break;
                default:
                    if (CanvasEffectsOverlay.Children.Count > 0)
                    {
                        CanvasEffectsOverlay.Children.Clear();
                        _particles.Clear();
                        _ellipsePool.Clear();
                        _smileElementsInitialized = false;
                    }
                    break;
            }
        }

        #region AR Effects Implementations

        private void UpdateFireEffect(double width, double height)
        {
            // Spawn fire tongues and embers above the head area (top 35% of frame, center horizontally)
            double headCenterX = width / 2.0;
            double headY = height * 0.32;

            if (_particles.Count < 90)
            {
                for (int i = 0; i < 4; i++)
                {
                    double spread = (_rand.NextDouble() - 0.5) * 160.0;
                    int colorType = _rand.Next(3);
                    var p = new Particle
                    {
                        X = headCenterX + spread,
                        Y = headY + (_rand.NextDouble() * 20.0),
                        Vx = (_rand.NextDouble() - 0.5) * 1.5,
                        Vy = -3.0 - (_rand.NextDouble() * 4.5),
                        Size = 10 + _rand.NextDouble() * 18,
                        Life = 0,
                        MaxLife = 25 + _rand.Next(30),
                        BaseColor = colorType switch
                        {
                            0 => _fireBrushRed.Color,
                            1 => _fireBrushOrange.Color,
                            _ => _fireBrushGold.Color
                        }
                    };

                    var ellipse = RentEllipse();
                    ellipse.Width = p.Size;
                    ellipse.Height = p.Size * 1.4;
                    ellipse.Fill = colorType switch
                    {
                        0 => _fireBrushRed,
                        1 => _fireBrushOrange,
                        _ => _fireBrushGold
                    };
                    p.Element = ellipse;
                    _particles.Add(p);
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life++;
                p.X += p.Vx;
                p.Y += p.Vy;
                p.Size *= 0.96;

                double opacity = 1.0 - (p.Life / p.MaxLife);
                if (opacity <= 0 || p.Size <= 1 || p.Y < 0)
                {
                    if (p.Element is Ellipse el) ReturnEllipse(el);
                    _particles.RemoveAt(i);
                    continue;
                }

                if (p.Element is Ellipse element)
                {
                    element.Width = p.Size;
                    element.Height = p.Size * 1.4;
                    element.Opacity = Math.Max(0, opacity);
                    Canvas.SetLeft(element, p.X - (p.Size / 2.0));
                    Canvas.SetTop(element, p.Y);
                }
            }
        }

        private void UpdateSnowEffect(double width, double height)
        {
            if (_particles.Count < 120)
            {
                for (int i = 0; i < 3; i++)
                {
                    var p = new Particle
                    {
                        X = _rand.NextDouble() * width,
                        Y = -10,
                        Vx = (_rand.NextDouble() - 0.5) * 1.2,
                        Vy = 1.5 + (_rand.NextDouble() * 2.8),
                        Size = 4 + (_rand.NextDouble() * 8),
                        Life = 0,
                        MaxLife = 400,
                        BaseColor = Colors.White
                    };

                    var snowflake = RentEllipse();
                    snowflake.Width = p.Size;
                    snowflake.Height = p.Size;
                    snowflake.Fill = _snowBrush;
                    p.Element = snowflake;
                    _particles.Add(p);
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.Vx + Math.Sin(p.Y * 0.05) * 0.5;
                p.Y += p.Vy;

                if (p.Y > height)
                {
                    if (p.Element is Ellipse el) ReturnEllipse(el);
                    _particles.RemoveAt(i);
                    continue;
                }

                if (p.Element != null)
                {
                    Canvas.SetLeft(p.Element, p.X);
                    Canvas.SetTop(p.Element, p.Y);
                }
            }
        }

        private void UpdateSmilingFaceEffect(double width, double height)
        {
            double centerX = width / 2.0;
            double centerY = height * 0.48;
            double headRadius = Math.Min(width, height) * 0.22;
            double eyeOffsetX = headRadius * 0.4;
            double eyeOffsetY = headRadius * 0.18;

            if (!_smileElementsInitialized || CanvasEffectsOverlay.Children.Count == 0)
            {
                CanvasEffectsOverlay.Children.Clear();

                _smileSegment = new QuadraticBezierSegment();
                _smileFig = new PathFigure { Segments = new PathSegmentCollection { _smileSegment }, IsClosed = false };
                _smileGeom = new PathGeometry(new[] { _smileFig });
                _smilePath = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 200)),
                    StrokeThickness = 5,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Data = _smileGeom,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(0, 255, 200),
                        BlurRadius = 14,
                        ShadowDepth = 0,
                        Opacity = 0.95
                    }
                };

                _leftEyeSegment = new QuadraticBezierSegment();
                _leftEyeFig = new PathFigure { Segments = new PathSegmentCollection { _leftEyeSegment }, IsClosed = false };
                _leftEyeGeom = new PathGeometry(new[] { _leftEyeFig });
                _leftEyePath = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 200)),
                    StrokeThickness = 4,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Data = _leftEyeGeom,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0, 255, 200), BlurRadius = 10, ShadowDepth = 0 }
                };

                _rightEyeSegment = new QuadraticBezierSegment();
                _rightEyeFig = new PathFigure { Segments = new PathSegmentCollection { _rightEyeSegment }, IsClosed = false };
                _rightEyeGeom = new PathGeometry(new[] { _rightEyeFig });
                _rightEyePath = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 200)),
                    StrokeThickness = 4,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Data = _rightEyeGeom,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0, 255, 200), BlurRadius = 10, ShadowDepth = 0 }
                };

                _leftCheek = new Ellipse
                {
                    Width = 26,
                    Height = 18,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 255, 105, 180)),
                    Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 }
                };

                _rightCheek = new Ellipse
                {
                    Width = 26,
                    Height = 18,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 255, 105, 180)),
                    Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 }
                };

                CanvasEffectsOverlay.Children.Add(_smilePath);
                CanvasEffectsOverlay.Children.Add(_leftEyePath);
                CanvasEffectsOverlay.Children.Add(_rightEyePath);
                CanvasEffectsOverlay.Children.Add(_leftCheek);
                CanvasEffectsOverlay.Children.Add(_rightCheek);

                _smileElementsInitialized = true;
            }

            // Update geometry coordinates smoothly in-place with zero allocations
            if (_smileFig != null && _smileSegment != null)
            {
                _smileFig.StartPoint = new Point(centerX - headRadius * 0.55, centerY + headRadius * 0.25);
                _smileSegment.Point1 = new Point(centerX, centerY + headRadius * 0.65);
                _smileSegment.Point2 = new Point(centerX + headRadius * 0.55, centerY + headRadius * 0.25);
            }

            if (_leftEyeFig != null && _leftEyeSegment != null)
            {
                _leftEyeFig.StartPoint = new Point(centerX - eyeOffsetX - 16, centerY - eyeOffsetY);
                _leftEyeSegment.Point1 = new Point(centerX - eyeOffsetX, centerY - eyeOffsetY - 16);
                _leftEyeSegment.Point2 = new Point(centerX - eyeOffsetX + 16, centerY - eyeOffsetY);
            }

            if (_rightEyeFig != null && _rightEyeSegment != null)
            {
                _rightEyeFig.StartPoint = new Point(centerX + eyeOffsetX - 16, centerY - eyeOffsetY);
                _rightEyeSegment.Point1 = new Point(centerX + eyeOffsetX, centerY - eyeOffsetY - 16);
                _rightEyeSegment.Point2 = new Point(centerX + eyeOffsetX + 16, centerY - eyeOffsetY);
            }

            if (_leftCheek != null)
            {
                Canvas.SetLeft(_leftCheek, centerX - eyeOffsetX - 28);
                Canvas.SetTop(_leftCheek, centerY + 8);
            }

            if (_rightCheek != null)
            {
                Canvas.SetLeft(_rightCheek, centerX + eyeOffsetX + 2);
                Canvas.SetTop(_rightCheek, centerY + 8);
            }
        }

        private void UpdateMatrixRainEffect(double width, double height)
        {
            if (_particles.Count < 50)
            {
                var p = new Particle
                {
                    X = _rand.Next((int)Math.Max(1, width / 20)) * 20.0,
                    Y = -30,
                    Vy = 6.0 + (_rand.NextDouble() * 8.0),
                    Size = 12,
                    Element = new TextBlock
                    {
                        Text = ((char)_rand.Next(0x30A0, 0x30FF)).ToString(),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 100))
                    }
                };
                CanvasEffectsOverlay.Children.Add(p.Element);
                _particles.Add(p);
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Y += p.Vy;
                if (p.Y > height)
                {
                    if (p.Element != null) CanvasEffectsOverlay.Children.Remove(p.Element);
                    _particles.RemoveAt(i);
                    continue;
                }

                if (p.Element != null)
                {
                    Canvas.SetLeft(p.Element, p.X);
                    Canvas.SetTop(p.Element, p.Y);
                }
            }
        }

        private void UpdateNeonEdgeEffect(double width, double height)
        {
            CanvasEffectsOverlay.Children.Clear();

            // Cyberpunk HUD Brackets
            double margin = 24;
            double len = 40;
            var stroke = new SolidColorBrush(Color.FromRgb(0, 229, 255));
            var shadow = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0, 229, 255), BlurRadius = 10, ShadowDepth = 0 };

            // Top-Left Corner
            var tl = new System.Windows.Shapes.Path
            {
                Stroke = stroke,
                StrokeThickness = 3,
                Data = Geometry.Parse($"M {margin},{margin + len} L {margin},{margin} L {margin + len},{margin}"),
                Effect = shadow
            };
            CanvasEffectsOverlay.Children.Add(tl);

            // Top-Right Corner
            var tr = new System.Windows.Shapes.Path
            {
                Stroke = stroke,
                StrokeThickness = 3,
                Data = Geometry.Parse($"M {width - margin - len},{margin} L {width - margin},{margin} L {width - margin},{margin + len}"),
                Effect = shadow
            };
            CanvasEffectsOverlay.Children.Add(tr);

            // Bottom-Left Corner
            var bl = new System.Windows.Shapes.Path
            {
                Stroke = stroke,
                StrokeThickness = 3,
                Data = Geometry.Parse($"M {margin},{height - margin - len} L {margin},{height - margin} L {margin + len},{height - margin}"),
                Effect = shadow
            };
            CanvasEffectsOverlay.Children.Add(bl);

            // Bottom-Right Corner
            var br = new System.Windows.Shapes.Path
            {
                Stroke = stroke,
                StrokeThickness = 3,
                Data = Geometry.Parse($"M {width - margin - len},{height - margin} L {width - margin},{height - margin} L {width - margin},{height - margin - len}"),
                Effect = shadow
            };
            CanvasEffectsOverlay.Children.Add(br);

            // Center target reticle
            var reticle = new Ellipse
            {
                Width = 60,
                Height = 60,
                Stroke = stroke,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Effect = shadow
            };
            Canvas.SetLeft(reticle, (width / 2.0) - 30);
            Canvas.SetTop(reticle, (height / 2.0) - 30);
            CanvasEffectsOverlay.Children.Add(reticle);
        }

        private void UpdateNightVisionEffect(double width, double height)
        {
            CanvasEffectsOverlay.Children.Clear();

            // Tactical Green Tint Overlay
            var tint = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(90, 0, 255, 80))
            };
            CanvasEffectsOverlay.Children.Add(tint);

            // Crosshair
            var crossH = new Line { X1 = 0, Y1 = height / 2.0, X2 = width, Y2 = height / 2.0, Stroke = new SolidColorBrush(Color.FromArgb(120, 0, 255, 80)), StrokeThickness = 1 };
            var crossV = new Line { X1 = width / 2.0, Y1 = 0, X2 = width / 2.0, Y2 = height, Stroke = new SolidColorBrush(Color.FromArgb(120, 0, 255, 80)), StrokeThickness = 1 };
            CanvasEffectsOverlay.Children.Add(crossH);
            CanvasEffectsOverlay.Children.Add(crossV);

            // Compass text
            var hudText = new TextBlock
            {
                Text = "TAC-NVG // NV-MOD-4 // AZM: 042° // ISO 12800",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 80)),
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(hudText, 20);
            Canvas.SetTop(hudText, height - 32);
            CanvasEffectsOverlay.Children.Add(hudText);
        }

        private void UpdateVhsEffect(double width, double height)
        {
            CanvasEffectsOverlay.Children.Clear();

            // Blinking REC Text
            bool blink = (DateTime.Now.Millisecond / 500) == 0;
            var recText = new TextBlock
            {
                Text = blink ? "● REC" : "  REC",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Red
            };
            Canvas.SetLeft(recText, 30);
            Canvas.SetTop(recText, 30);
            CanvasEffectsOverlay.Children.Add(recText);

            var spText = new TextBlock
            {
                Text = "PLAY  SP  00:24:19",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            Canvas.SetLeft(spText, width - 200);
            Canvas.SetTop(spText, 30);
            CanvasEffectsOverlay.Children.Add(spText);

            // Date stamp in bottom right
            var dateText = new TextBlock
            {
                Text = DateTime.Now.ToString("MMM. dd 1988"),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 230, 100))
            };
            Canvas.SetLeft(dateText, width - 180);
            Canvas.SetTop(dateText, height - 45);
            CanvasEffectsOverlay.Children.Add(dateText);

            // Jitter scan line
            double scanY = (DateTime.Now.Millisecond * 3.0) % height;
            var scan = new Rectangle
            {
                Width = width,
                Height = 3,
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            };
            Canvas.SetTop(scan, scanY);
            CanvasEffectsOverlay.Children.Add(scan);
        }

        #endregion

        #region Camera Calibration & Transformations

        private void ApplyMirrorTransform()
        {
            double scaleX = (ChkMirrorHorizontal?.IsChecked == true) ? -1.0 : 1.0;
            double scaleY = (ChkFlipVertical?.IsChecked == true) ? -1.0 : 1.0;

            CameraSurface.RenderTransformOrigin = new Point(0.5, 0.5);
            CameraSurface.RenderTransform = new ScaleTransform(scaleX, scaleY);
        }

        private void ChkMirror_Changed(object sender, RoutedEventArgs e)
        {
            ApplyMirrorTransform();
        }

        private void ChkFlip_Changed(object sender, RoutedEventArgs e)
        {
            ApplyMirrorTransform();
        }

        private void SliderAdjust_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtBrightnessVal != null && SliderBrightness != null)
                TxtBrightnessVal.Text = ((int)SliderBrightness.Value).ToString();
            if (TxtContrastVal != null && SliderContrast != null)
                TxtContrastVal.Text = ((int)SliderContrast.Value).ToString();
            if (TxtSaturationVal != null && SliderSaturation != null)
                TxtSaturationVal.Text = ((int)SliderSaturation.Value).ToString();
        }

        private void GridCameraContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasEffectsOverlay.Width = e.NewSize.Width;
            CanvasEffectsOverlay.Height = e.NewSize.Height;
        }

        #endregion

        #region Capture & Recording Engine

        private void TriggerFlash()
        {
            BorderFlash.Opacity = 0.85;
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BorderFlash.BeginAnimation(OpacityProperty, anim);
        }

        private void BtnCapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            CaptureSnapshotFrame();
        }

        private string CaptureSnapshotFrame()
        {
            try
            {
                TriggerFlash();

                int renderW = (int)Math.Max(640, GridCameraContainer.ActualWidth);
                int renderH = (int)Math.Max(480, GridCameraContainer.ActualHeight);

                var rtb = new RenderTargetBitmap(renderW, renderH, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(GridCameraContainer);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string fileName = $"PSD_Cam_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullPath = System.IO.Path.Combine(_outputPicturesDir, fileName);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                TxtFooterStatus.Text = $"Snapshot Saved: {fileName} ({renderW}x{renderH})";
                return fullPath;
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = $"Capture Error: {ex.Message}";
                return string.Empty;
            }
        }

        private async void BtnBurstCapture_Click(object sender, RoutedEventArgs e)
        {
            BtnBurstCapture.IsEnabled = false;
            TxtFooterStatus.Text = "Executing 5x rapid burst capture...";

            for (int i = 1; i <= 5; i++)
            {
                CaptureSnapshotFrame();
                TxtFooterStatus.Text = $"Captured burst frame {i}/5...";
                await Task.Delay(250);
            }

            TxtFooterStatus.Text = "Burst Capture Complete! 5 frames saved to gallery.";
            BtnBurstCapture.IsEnabled = true;
        }

        private void BtnRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                // Start Recording
                _isRecording = true;
                _recordingStartTime = DateTime.Now;
                _recordingTimer.Start();

                BtnRecordVideo.Content = "⏹ Stop Video Recording";
                BtnRecordVideo.Foreground = Brushes.White;
                BtnRecordVideo.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));

                string fileName = $"PSD_CamRec_{DateTime.Now:yyyyMMdd_HHmmss}.ts";
                string fullPath = System.IO.Path.Combine(_outputVideosDir, fileName);

                TxtFooterStatus.Text = $"RECORDING LIVE VIDEO -> {fileName}";
            }
            else
            {
                // Stop Recording
                _isRecording = false;
                _recordingTimer.Stop();

                BtnRecordVideo.Content = "🔴 Start Video Recording";
                BtnRecordVideo.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));
                BtnRecordVideo.ClearValue(BackgroundProperty);

                TxtFooterStatus.Text = "Video Recording saved to gallery.";
            }
        }

        private void RecordingTimer_Tick(object? sender, EventArgs e)
        {
            var duration = DateTime.Now - _recordingStartTime;
            TxtFooterStatus.Text = $"RECORDING [LIVE] - {duration:hh\\:mm\\:ss} ({_activeEffect.ToUpperInvariant()})";
        }

        private void BtnOpenGallery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(_outputPicturesDir))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _outputPicturesDir,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = $"Cannot open gallery: {ex.Message}";
            }
        }

        #endregion
    }
}
