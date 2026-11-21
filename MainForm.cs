using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using Timer = System.Windows.Forms.Timer;

namespace SimpleCamera
{
    public partial class MainForm : Form
    {
        private FilterInfoCollection? _videoDevices;
        private VideoCaptureDevice? _videoSource;
        private string _saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        private bool _isCapturing = false;
        private Bitmap? _currentFrame;

        private DoubleBufferedPanel _videoPanel = null!;
        private ComboBox _cameraCombo = null!;
        private TextBox _folderTextBox = null!;
        private Button _captureButton = null!;
        private Label _statusLabel = null!;
        private Button _settingsButton = null!;
        private TableLayoutPanel _mainLayout = null!;
        private FlowLayoutPanel _buttonPanel = null!;

        private ThemeSettings _themeSettings = ThemeSettings.Load();
        private ColorScheme _colorScheme;

        public MainForm()
        {
            _colorScheme = _themeSettings.GetColorScheme();
            InitializeComponent();
            ApplyTheme();
            LoadVideoDevices();
            LoadSaveFolder();
        }

        private void InitializeComponent()
        {
            this.Text = "Simple Camera";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 550);
            this.FormClosing += MainForm_FormClosing;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Main layout
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Video preview takes remaining space
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Controls - auto height with wrapping
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Status bar

            // Video preview panel
            _videoPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 10) // Space below preview
            };
            _videoPanel.Paint += VideoPanel_Paint;
            _mainLayout.Controls.Add(_videoPanel, 0, 0);

            // Controls panel - simple FlowLayoutPanel directly in TableLayoutPanel
            _buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.None,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 10, 0, 0) // Space from preview
            };
            _mainLayout.Controls.Add(_buttonPanel, 0, 1);

            // Camera selector
            var cameraLabel = new Label { Text = "Camera:", AutoSize = true, Margin = new Padding(0, 8, 5, 0) };
            _cameraCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Margin = new Padding(0, 5, 10, 0)
            };
            _cameraCombo.SelectedIndexChanged += CameraCombo_SelectedIndexChanged;

            // Camera settings button (opens webcam property pages)
            var cameraSettingsButton = new Button
            {
                Text = "🎥 Cam Settings",
                AutoSize = true,
                Margin = new Padding(0, 5, 10, 0),
                Padding = new Padding(10, 5, 10, 5)
            };
            cameraSettingsButton.Click += CameraSettingsButton_Click;

            // Folder selector
            var folderLabel = new Label { Text = "Save Folder:", AutoSize = true, Margin = new Padding(0, 8, 5, 0) };
            _folderTextBox = new TextBox
            {
                ReadOnly = true,
                Width = 220,
                Margin = new Padding(0, 5, 5, 0)
            };
            var folderButton = new Button
            {
                Text = "Browse...",
                AutoSize = true,
                Margin = new Padding(0, 5, 10, 0)
            };
            folderButton.Click += FolderButton_Click;

            // Settings button
            _settingsButton = new Button
            {
                Text = "⚙ Settings",
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 0),
                Padding = new Padding(10, 5, 10, 5)
            };
            _settingsButton.Click += SettingsButton_Click;

            // Capture button
            _captureButton = new Button
            {
                Text = "📷 Take Photo",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Margin = new Padding(10, 5, 0, 0),
                Padding = new Padding(15, 8, 15, 8)
            };
            _captureButton.FlatStyle = FlatStyle.Flat;
            _captureButton.FlatAppearance.BorderSize = 0;
            _captureButton.Click += CaptureButton_Click;
            _captureButton.Enabled = false;

            // Open folder button
            var openFolderButton = new Button
            {
                Text = "📁 Open Folder",
                AutoSize = true,
                Margin = new Padding(10, 5, 0, 0),
                Padding = new Padding(10, 8, 10, 8)
            };
            openFolderButton.Click += OpenFolderButton_Click;

            _buttonPanel.Controls.AddRange(new Control[] {
                cameraLabel, _cameraCombo,
                cameraSettingsButton,
                folderLabel, _folderTextBox, folderButton,
                _settingsButton,
                _captureButton, openFolderButton
            });

            // Status bar
            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ready - Select a camera to begin",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };
            _mainLayout.Controls.Add(_statusLabel, 0, 2);

            this.Controls.Add(_mainLayout);
        }

        private void ApplyTheme()
        {
            this.BackColor = _colorScheme.BackColor;
            this.ForeColor = _colorScheme.ForeColor;

            if (_mainLayout != null) _mainLayout.BackColor = _colorScheme.BackColor;
            if (_buttonPanel != null) _buttonPanel.BackColor = _colorScheme.BackColor;

            if (_videoPanel != null)
            {
                _videoPanel.BackColor = Color.Black;
                _videoPanel.BorderStyle = BorderStyle.FixedSingle;
            }

            ApplyControlTheme(_cameraCombo, isComboBox: true);
            ApplyControlTheme(_folderTextBox, isTextBox: true);
            ApplyControlTheme(_folderTextBox, isTextBox: true); // folderButton handled below

            // Apply to all buttons in buttonPanel
            if (_buttonPanel != null)
            {
                foreach (Control ctrl in _buttonPanel.Controls)
                {
                    if (ctrl is Button btn)
                    {
                        ApplyButtonTheme(btn, btn == _captureButton);
                    }
                    else if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = _colorScheme.ForeColor;
                    }
                    else if (ctrl is ComboBox cb)
                    {
                        ApplyControlTheme(cb, isComboBox: true);
                    }
                    else if (ctrl is TextBox tb)
                    {
                        ApplyControlTheme(tb, isTextBox: true);
                    }
                }
            }

            if (_statusLabel != null)
            {
                _statusLabel.BackColor = _colorScheme.StatusBack;
                _statusLabel.ForeColor = _colorScheme.ForeColor;
            }

            // Folder button
            foreach (Control ctrl in _buttonPanel.Controls)
            {
                if (ctrl is Button btn && btn.Text == "Browse...")
                {
                    ApplyButtonTheme(btn, false);
                }
                else if (ctrl is Button btn2 && btn2.Text == "📁 Open Folder")
                {
                    ApplyButtonTheme(btn2, false);
                }
                else if (ctrl is Button btn3 && btn3.Text == "⚙ Settings")
                {
                    ApplyButtonTheme(btn3, false);
                }
            }
        }

        private void ApplyControlTheme(Control ctrl, bool isComboBox = false, bool isTextBox = false)
        {
            if (isComboBox && ctrl is ComboBox cb)
            {
                cb.BackColor = _colorScheme.ComboBack;
                cb.ForeColor = _colorScheme.ComboFore;
                cb.FlatStyle = FlatStyle.Flat;
            }
            else if (isTextBox && ctrl is TextBox tb)
            {
                tb.BackColor = _colorScheme.TextBoxBack;
                tb.ForeColor = _colorScheme.TextBoxFore;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private void ApplyButtonTheme(Button btn, bool isAccent)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = _colorScheme.ButtonFore;

            if (isAccent)
            {
                btn.BackColor = _colorScheme.Accent;
            }
            else
            {
                btn.BackColor = _colorScheme.ButtonBack;
            }

            // Store original colors for hover effects
            btn.MouseEnter += (s, e) => {
                if (btn.Enabled)
                    btn.BackColor = isAccent ? _colorScheme.AccentHover : _colorScheme.ButtonHover;
            };
            btn.MouseLeave += (s, e) => {
                if (btn.Enabled)
                    btn.BackColor = isAccent ? _colorScheme.Accent : _colorScheme.ButtonBack;
            };
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_themeSettings, OnThemeChanged))
            {
                settingsForm.ShowDialog(this);
            }
        }

        private void CameraSettingsButton_Click(object? sender, EventArgs e)
        {
            if (_videoSource != null && _cameraCombo.SelectedIndex >= 0 && _cameraCombo.SelectedIndex < _videoDevices.Count)
            {
                try
                {
                    // Stop video source temporarily to show property page
                    bool wasRunning = _videoSource.IsRunning;
                    if (wasRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }

                    // Show the DirectShow property pages for the video capture device
                    var device = _videoDevices[_cameraCombo.SelectedIndex];
                    _videoSource.DisplayPropertyPage(this.Handle);

                    // Restart if it was running
                    if (wasRunning)
                    {
                        _videoSource.Start();
                    }
                }
                catch (Exception ex)
                {
                    _statusLabel.Text = $"Error opening camera settings: {ex.Message}";
                }
            }
        }

        private void OnThemeChanged(ColorScheme newScheme)
        {
            _colorScheme = newScheme;
            ApplyTheme();
        }

        private void LoadVideoDevices()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                _cameraCombo.Items.Clear();

                if (_videoDevices.Count == 0)
                {
                    _cameraCombo.Items.Add("No cameras found");
                    _cameraCombo.SelectedIndex = 0;
                    _captureButton.Enabled = false;
                    _statusLabel.Text = "No cameras detected";
                    return;
                }

                foreach (FilterInfo device in _videoDevices)
                {
                    _cameraCombo.Items.Add(device.Name);
                }

                _cameraCombo.SelectedIndex = 0;
                StartVideoSource(0);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error loading cameras: {ex.Message}";
                _cameraCombo.Items.Add("Error loading cameras");
                _cameraCombo.SelectedIndex = 0;
            }
        }

        private void CameraCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cameraCombo.SelectedIndex >= 0 && _cameraCombo.SelectedIndex < _videoDevices.Count)
            {
                StartVideoSource(_cameraCombo.SelectedIndex);
            }
        }

        private void StartVideoSource(int deviceIndex)
        {
            StopVideoSource();

            try
            {
                _videoSource = new VideoCaptureDevice(_videoDevices[deviceIndex].MonikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
                _isCapturing = true;
                _captureButton.Enabled = true;
                _statusLabel.Text = $"Camera: {_videoDevices[deviceIndex].Name} - Ready";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error starting camera: {ex.Message}";
                _captureButton.Enabled = false;
            }
        }

        private void StopVideoSource()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.WaitForStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource = null;
                _isCapturing = false;
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            _currentFrame?.Dispose();
            _currentFrame = (Bitmap)eventArgs.Frame.Clone();

            if (_videoPanel.IsHandleCreated)
            {
                _videoPanel.BeginInvoke(new Action(() => _videoPanel.Invalidate()));
            }
        }

        private void VideoPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_currentFrame != null)
            {
                var panel = sender as Panel;
                var panelRect = panel!.ClientRectangle;
                var frameRect = new Rectangle(0, 0, _currentFrame.Width, _currentFrame.Height);

                float scaleX = (float)panelRect.Width / frameRect.Width;
                float scaleY = (float)panelRect.Height / frameRect.Height;
                float scale = Math.Min(scaleX, scaleY);

                int drawWidth = (int)(frameRect.Width * scale);
                int drawHeight = (int)(frameRect.Height * scale);
                int drawX = (panelRect.Width - drawWidth) / 2;
                int drawY = (panelRect.Height - drawHeight) / 2;

                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(_currentFrame, drawX, drawY, drawWidth, drawHeight);
            }
        }

        private void LoadSaveFolder()
        {
            _folderTextBox.Text = _saveFolder;
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }
        }

        private void FolderButton_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to save photos",
                SelectedPath = _saveFolder,
                ShowNewFolderButton = true
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _saveFolder = dialog.SelectedPath;
                    _folderTextBox.Text = _saveFolder;
                    _statusLabel.Text = $"Save folder: {_saveFolder}";
                }
            }
        }

        private void CaptureButton_Click(object? sender, EventArgs e)
        {
            if (_currentFrame == null)
            {
                _statusLabel.Text = "No frame available to capture";
                return;
            }

            try
            {
                string fileName = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                string filePath = Path.Combine(_saveFolder, fileName);

                using (var bitmap = (Bitmap)_currentFrame.Clone())
                {
                    var encoder = ImageCodecInfo.GetImageEncoders()
                        .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    if (encoder != null)
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                        bitmap.Save(filePath, encoder, encoderParams);
                    }
                    else
                    {
                        bitmap.Save(filePath, ImageFormat.Jpeg);
                    }
                }

                _statusLabel.Text = $"Photo saved: {fileName}";

                _captureButton.BackColor = Color.FromArgb(0, 200, 100);
                _captureButton.Text = "✓ Saved!";
                var timer = new Timer { Interval = 1000 };
                timer.Tick += (s, args) => {
                    _captureButton.BackColor = _colorScheme.Accent;
                    _captureButton.Text = "📷 Take Photo";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error saving photo: {ex.Message}";
            }
        }

        private void OpenFolderButton_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _saveFolder);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error opening folder: {ex.Message}";
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopVideoSource();
            _currentFrame?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopVideoSource();
                _currentFrame?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Custom panel with double buffering to eliminate flicker
    internal class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
    }
}