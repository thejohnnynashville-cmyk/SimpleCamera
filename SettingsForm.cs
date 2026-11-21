using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleCamera
{
    public class SettingsForm : Form
    {
        private readonly ThemeSettings _settings;
        private readonly Action<ColorScheme> _onThemeChanged;

        private ComboBox _themeCombo = null!;
        private TrackBar _hueTrackBar = null!;
        private TrackBar _satTrackBar = null!;
        private Label _hueLabel = null!;
        private Label _satLabel = null!;
        private Panel _previewPanel = null!;
        private CheckBox _autoStartCheck = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;
        private Button _shortcutButton = null!;

        public SettingsForm(ThemeSettings settings, Action<ColorScheme> onThemeChanged)
        {
            _settings = settings;
            _onThemeChanged = onThemeChanged;
            InitializeComponent();
            LoadSettings();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(520, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Theme
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));  // Hue
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));  // Saturation
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Preview
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Buttons

            // Theme selection
            var themeContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 10)
            };
            themeContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            themeContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var themeLabel = new Label
            {
                Text = "Theme:",
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };

            _themeCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                MinimumSize = new Size(200, 0)
            };
            _themeCombo.Items.AddRange(new[] { "Dark", "Light", "Custom" });
            _themeCombo.SelectedIndexChanged += (s, e) => OnThemeChanged();

            themeContainer.Controls.Add(themeLabel, 0, 0);
            themeContainer.Controls.Add(_themeCombo, 1, 0);
            layout.Controls.Add(themeContainer, 0, 0);

            // Hue shift
            var huePanel = CreateSliderPanel("Hue Shift:", -180, 180, out _hueTrackBar, out _hueLabel);
            _hueTrackBar.ValueChanged += (s, e) => OnHueSatChanged();
            layout.Controls.Add(huePanel, 0, 1);

            // Saturation
            var satPanel = CreateSliderPanel("Saturation:", 0, 200, out _satTrackBar, out _satLabel);
            _satTrackBar.ValueChanged += (s, e) => OnHueSatChanged();
            layout.Controls.Add(satPanel, 0, 2);

            // Preview
            var previewContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 10)
            };
            previewContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            previewContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var previewLabel = new Label
            {
                Text = "Preview:",
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };

            _previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                MinimumSize = new Size(300, 100)
            };

            previewContainer.Controls.Add(previewLabel, 0, 0);
            previewContainer.Controls.Add(_previewPanel, 1, 0);
            layout.Controls.Add(previewContainer, 0, 3);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(15, 5, 15, 5) };
            _okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(15, 5, 15, 5) };
            _okButton.Click += OkButton_Click;

            _shortcutButton = new Button { Text = "Create Desktop Shortcut", AutoSize = true, Padding = new Padding(15, 5, 15, 5) };
            _shortcutButton.Click += ShortcutButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { _cancelButton, _okButton, _shortcutButton });
            layout.Controls.Add(buttonPanel, 0, 5);

            this.Controls.Add(layout);
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private Panel CreateSliderPanel(string labelText, int min, int max, out TrackBar trackBar, out Label valueLabel)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 10)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

            var label = new Label
            {
                Text = labelText,
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };

            trackBar = new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = min,
                Maximum = max,
                TickFrequency = Math.Max(1, (max - min) / 10),
                AutoSize = false
            };

            valueLabel = new Label
            {
                Anchor = AnchorStyles.Left,
                AutoSize = false,
                Width = 60,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(10, 0, 0, 0)
            };

            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(trackBar, 1, 0);
            panel.Controls.Add(valueLabel, 2, 0);

            return panel;
        }

        private void LoadSettings()
        {
            _themeCombo.SelectedIndex = (int)_settings.Mode;
            _hueTrackBar.Value = (int)Math.Round(_settings.HueShift);
            _satTrackBar.Value = (int)Math.Round(_settings.Saturation * 100);
            _autoStartCheck?.Dispose(); // Not used in this layout
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            _hueLabel.Text = $"{_hueTrackBar.Value}°";
            _satLabel.Text = $"{_satTrackBar.Value / 100f:F2}x";
        }

        private void OnThemeChanged()
        {
            _settings.Mode = (ThemeMode)_themeCombo.SelectedIndex;
            _hueTrackBar.Enabled = _settings.Mode == ThemeMode.Custom;
            _satTrackBar.Enabled = _settings.Mode == ThemeMode.Custom;
            UpdatePreview();
        }

        private void OnHueSatChanged()
        {
            _settings.HueShift = _hueTrackBar.Value;
            _settings.Saturation = _satTrackBar.Value / 100f;
            UpdateLabels();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var scheme = _settings.GetColorScheme();
            _previewPanel.BackColor = scheme.PanelBack;
            
            // Draw a mini preview
            using (var g = _previewPanel.CreateGraphics())
            {
                g.Clear(scheme.PanelBack);
                using (var brush = new SolidBrush(scheme.Accent))
                {
                    g.FillRectangle(brush, 10, 10, 80, 30);
                }
                using (var pen = new Pen(scheme.PanelBorder))
                {
                    g.DrawRectangle(pen, 0, 0, _previewPanel.Width - 1, _previewPanel.Height - 1);
                }
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            _settings.Save();
            _onThemeChanged(_settings.GetColorScheme());
        }

        private void ShortcutButton_Click(object? sender, EventArgs e)
        {
            CreateDesktopShortcut();
        }

        private void CreateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "Simple Camera.lnk");
                string exePath = Application.ExecutablePath;

                // Use COM to create shortcut
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.Description = "Simple Camera - Webcam photo capture";
                    shortcut.Save();
                }

                MessageBox.Show(this, "Desktop shortcut created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to create shortcut: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}