using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace SimpleCamera
{
    public enum ThemeMode
    {
        Light,
        Dark,
        Custom
    }

    public class ThemeSettings
    {
        public ThemeMode Mode { get; set; } = ThemeMode.Dark;
        public float HueShift { get; set; } = 0f; // -180 to 180
        public float Saturation { get; set; } = 1f; // 0 to 2
        public bool AutoStart { get; set; } = false;

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SimpleCamera",
            "settings.json");

        public static ThemeSettings Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<ThemeSettings>(json) ?? new ThemeSettings();
                }
            }
            catch { }
            return new ThemeSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public ColorScheme GetColorScheme()
        {
            return Mode switch
            {
                ThemeMode.Light => ColorScheme.Light(),
                ThemeMode.Dark => ColorScheme.Dark(),
                ThemeMode.Custom => ColorScheme.Custom(HueShift, Saturation),
                _ => ColorScheme.Dark()
            };
        }
    }

    public class ColorScheme
    {
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Color PanelBack { get; set; }
        public Color PanelBorder { get; set; }
        public Color ButtonBack { get; set; }
        public Color ButtonFore { get; set; }
        public Color ButtonHover { get; set; }
        public Color ButtonPressed { get; set; }
        public Color ComboBack { get; set; }
        public Color ComboFore { get; set; }
        public Color TextBoxBack { get; set; }
        public Color TextBoxFore { get; set; }
        public Color TextBoxBorder { get; set; }
        public Color StatusBack { get; set; }
        public Color Accent { get; set; }
        public Color AccentHover { get; set; }

        public static ColorScheme Light()
        {
            return new ColorScheme
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(30, 30, 30),
                PanelBack = Color.White,
                PanelBorder = Color.FromArgb(180, 180, 180),
                ButtonBack = Color.FromArgb(0, 120, 215),
                ButtonFore = Color.White,
                ButtonHover = Color.FromArgb(0, 100, 180),
                ButtonPressed = Color.FromArgb(0, 80, 150),
                ComboBack = Color.White,
                ComboFore = Color.FromArgb(30, 30, 30),
                TextBoxBack = Color.White,
                TextBoxFore = Color.FromArgb(30, 30, 30),
                TextBoxBorder = Color.FromArgb(180, 180, 180),
                StatusBack = Color.FromArgb(230, 230, 230),
                Accent = Color.FromArgb(0, 120, 215),
                AccentHover = Color.FromArgb(0, 100, 180)
            };
        }

        public static ColorScheme Dark()
        {
            return new ColorScheme
            {
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(220, 220, 220),
                PanelBack = Color.FromArgb(45, 45, 45),
                PanelBorder = Color.FromArgb(80, 80, 80),
                ButtonBack = Color.FromArgb(0, 120, 215),
                ButtonFore = Color.White,
                ButtonHover = Color.FromArgb(20, 140, 235),
                ButtonPressed = Color.FromArgb(0, 100, 180),
                ComboBack = Color.FromArgb(45, 45, 45),
                ComboFore = Color.FromArgb(220, 220, 220),
                TextBoxBack = Color.FromArgb(45, 45, 45),
                TextBoxFore = Color.FromArgb(220, 220, 220),
                TextBoxBorder = Color.FromArgb(80, 80, 80),
                StatusBack = Color.FromArgb(25, 25, 25),
                Accent = Color.FromArgb(0, 120, 215),
                AccentHover = Color.FromArgb(20, 140, 235)
            };
        }

        public static ColorScheme Custom(float hueShift, float saturation)
        {
            // Base accent color (blue) - apply hue shift and saturation
            var baseHue = 210f / 360f; // Blue in 0-1 range
            var hue = (baseHue + hueShift / 360f) % 1f;
            if (hue < 0) hue += 1f;

            var accent = HsvToRgb(hue, Math.Clamp(saturation, 0f, 1f), 0.85f);
            var accentHover = HsvToRgb(hue, Math.Clamp(saturation * 0.8f, 0f, 1f), 0.95f);
            var accentPressed = HsvToRgb(hue, Math.Clamp(saturation, 0f, 1f), 0.7f);

            return new ColorScheme
            {
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(220, 220, 220),
                PanelBack = Color.FromArgb(45, 45, 45),
                PanelBorder = Color.FromArgb(80, 80, 80),
                ButtonBack = accent,
                ButtonFore = Color.White,
                ButtonHover = accentHover,
                ButtonPressed = accentPressed,
                ComboBack = Color.FromArgb(45, 45, 45),
                ComboFore = Color.FromArgb(220, 220, 220),
                TextBoxBack = Color.FromArgb(45, 45, 45),
                TextBoxFore = Color.FromArgb(220, 220, 220),
                TextBoxBorder = Color.FromArgb(80, 80, 80),
                StatusBack = Color.FromArgb(25, 25, 25),
                Accent = accent,
                AccentHover = accentHover
            };
        }

        private static Color HsvToRgb(float h, float s, float v)
        {
            int hi = (int)(h * 6);
            float f = h * 6 - hi;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            float r, g, b;
            switch (hi % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
                default: r = 0; g = 0; b = 0; break;
            }

            return Color.FromArgb(
                (int)(r * 255),
                (int)(g * 255),
                (int)(b * 255));
        }
    }
}