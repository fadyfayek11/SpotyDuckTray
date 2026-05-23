using System.Globalization;
using System.Windows.Forms;

namespace SpotyDuckTray;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _duckLevelInput;
    private readonly NumericUpDown _attackMsInput;
    private readonly NumericUpDown _releaseMsInput;
    private readonly NumericUpDown _thresholdInput;
    private readonly NumericUpDown _pollIntervalInput;
    private readonly NumericUpDown _gamingDuckLevelInput;
    private readonly NumericUpDown _fadeDurationInput;
    private readonly ComboBox _presetInput;
    private readonly ComboBox _appRuleModeInput;
    private readonly CheckedListBox _appRulesList;
    private readonly TextBox _newAppRuleInput;
    private readonly CheckBox _duckingEnabledCheckbox;
    private readonly CheckBox _gamingModeCheckbox;
    private readonly CheckBox _startWithWindowsCheckbox;
    private readonly CheckBox _hotkeyEnabledCheckbox;
    private readonly ComboBox _hotkeyModifierInput;
    private readonly ComboBox _hotkeyKeyInput;
    private readonly Label _appRuleHintLabel;
    private readonly Button _applyPresetButton;
    private readonly Button _addAppButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    public AppSettings? SavedSettings { get; private set; }

    public SettingsForm(AppSettings settings, IReadOnlyCollection<string>? detectedApps = null)
    {
        Text = "Spoty Duck Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(640, 820);
        ClientSize = new Size(640, 820);

        var knownApps = AudioRuleEngine.MergeDetectedApps(
            (detectedApps ?? []).Select(static name => new AudioSessionInfo { ProcessName = name }),
            settings.AppRules);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var headerLabel = new Label
        {
            Text = "Configure ducking, presets, app rules, startup, and hotkey behavior.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoScroll = true
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _duckLevelInput = CreateDecimalInput(0.10m, 0.30m, 0.01m, (decimal)settings.DuckLevel);
        _attackMsInput = CreateIntegerInput(50, 2000, settings.AttackMs);
        _releaseMsInput = CreateIntegerInput(100, 5000, settings.ReleaseMs);
        _thresholdInput = CreateDecimalInput(0.001m, 0.10m, 0.001m, (decimal)settings.Threshold, 3);
        _pollIntervalInput = CreateIntegerInput(50, 500, settings.PollIntervalMs);
        _gamingDuckLevelInput = CreateDecimalInput(0.50m, 0.70m, 0.01m, (decimal)settings.GamingDuckLevel);
        _fadeDurationInput = CreateIntegerInput(0, 5000, settings.FadeDurationMs);

        _presetInput = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        _presetInput.Items.AddRange(Enum.GetValues<BuiltInPreset>().Cast<object>().ToArray());
        _presetInput.SelectedItem = settings.SelectedPreset;

        _applyPresetButton = new Button
        {
            Text = "Apply preset values",
            AutoSize = true
        };
        _applyPresetButton.Click += (_, _) => ApplySelectedPreset();

        _appRuleModeInput = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        _appRuleModeInput.Items.AddRange(Enum.GetValues<AppRuleMode>().Cast<object>().ToArray());
        _appRuleModeInput.SelectedItem = settings.AppRuleMode;
        _appRuleModeInput.SelectedIndexChanged += (_, _) => UpdateAppRuleHint();

        _appRulesList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            Height = 180,
            IntegralHeight = false
        };

        foreach (var appName in knownApps)
        {
            var existingRule = settings.AppRules.FirstOrDefault(rule => string.Equals(rule.ProcessName, appName, StringComparison.OrdinalIgnoreCase));
            var isEnabled = existingRule?.Enabled ?? settings.AppRuleMode == AppRuleMode.Blacklist;
            _appRulesList.Items.Add(appName, isEnabled);
        }

        _newAppRuleInput = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Add process name, for example: discord"
        };

        _hotkeyModifierInput = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        _hotkeyModifierInput.Items.AddRange(new object[]
        {
            HotkeyDisplayOption.Create("Ctrl + Shift", Keys.Control | Keys.Shift),
            HotkeyDisplayOption.Create("Ctrl + Alt", Keys.Control | Keys.Alt),
            HotkeyDisplayOption.Create("Alt + Shift", Keys.Alt | Keys.Shift),
            HotkeyDisplayOption.Create("Ctrl + Alt + Shift", Keys.Control | Keys.Alt | Keys.Shift),
            HotkeyDisplayOption.Create("Ctrl", Keys.Control),
            HotkeyDisplayOption.Create("Alt", Keys.Alt),
            HotkeyDisplayOption.Create("Shift", Keys.Shift)
        });
        SelectHotkeyModifier(settings.HotkeyModifiers);

        _hotkeyKeyInput = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        foreach (var key in GetHotkeyKeys())
        {
            _hotkeyKeyInput.Items.Add(key);
        }

        _hotkeyKeyInput.SelectedItem = settings.HotkeyKey;

        _addAppButton = new Button
        {
            Text = "Add",
            AutoSize = true
        };
        _addAppButton.Click += AddAppClicked;

        _duckingEnabledCheckbox = new CheckBox
        {
            Text = "Enable automatic ducking",
            Checked = settings.DuckingEnabled,
            AutoSize = true
        };

        _gamingModeCheckbox = new CheckBox
        {
            Text = "Use gaming mode duck level",
            Checked = settings.GamingModeEnabled,
            AutoSize = true
        };

        _startWithWindowsCheckbox = new CheckBox
        {
            Text = "Launch automatically when I sign in to Windows",
            Checked = settings.StartWithWindows,
            AutoSize = true
        };

        _hotkeyEnabledCheckbox = new CheckBox
        {
            Text = "Enable a global keyboard shortcut to pause or resume ducking",
            Checked = settings.HotkeyEnabled,
            AutoSize = true
        };
        _hotkeyEnabledCheckbox.CheckedChanged += (_, _) => UpdateHotkeyControls();

        _appRuleHintLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };

        var presetPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true
        };
        presetPanel.Controls.Add(CreatePropertyGrid(new (string Label, Control Control, string Hint)[]
        {
            ("Preset", _presetInput, "Built-in profiles quickly tune ducking for different creator and gamer scenarios.")
        }));
        presetPanel.Controls.Add(new Label
        {
            Text = "Choose a preset and apply it to load recommended values. You can still fine-tune settings before saving.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 0, 0, 8)
        });
        presetPanel.Controls.Add(_applyPresetButton);

        content.Controls.Add(CreateSectionPanel("Profiles and presets", new Control[]
        {
            presetPanel
        }));

        content.Controls.Add(CreateSectionPanel("Playback behavior", new Control[]
        {
            CreatePropertyGrid(new (string Label, Control Control, string Hint)[]
            {
                ("Duck level", _duckLevelInput, "Spotify volume while another app is speaking or playing audio."),
                ("Attack (ms)", _attackMsInput, "How long external audio must persist before ducking starts."),
                ("Release (ms)", _releaseMsInput, "How long silence must persist before volume is restored."),
                ("Threshold", _thresholdInput, "Minimum session peak to count as active external audio."),
                ("Polling interval (ms)", _pollIntervalInput, "How often the app checks system audio sessions."),
                ("Fade duration (ms)", _fadeDurationInput, "How long volume transitions take."),
                ("Gaming duck level", _gamingDuckLevelInput, "Alternative duck level used when gaming mode is enabled.")
            }),
            _duckingEnabledCheckbox,
            _gamingModeCheckbox
        }));

        var appRulePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true
        };
        appRulePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        appRulePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        appRulePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        appRulePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));

        appRulePanel.Controls.Add(CreatePropertyGrid(new (string Label, Control Control, string Hint)[]
        {
            ("App rule mode", _appRuleModeInput, "Choose whether all apps, only checked apps, or all except checked apps can trigger ducking.")
        }));
        appRulePanel.Controls.Add(_appRuleHintLabel);

        var addAppPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true
        };
        addAppPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        addAppPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        addAppPanel.Controls.Add(_newAppRuleInput, 0, 0);
        addAppPanel.Controls.Add(_addAppButton, 1, 0);

        appRulePanel.Controls.Add(addAppPanel);
        appRulePanel.Controls.Add(_appRulesList);

        content.Controls.Add(CreateSectionPanel("Per-app rules", new Control[]
        {
            appRulePanel
        }));

        content.Controls.Add(CreateSectionPanel("Startup", new Control[]
        {
            _startWithWindowsCheckbox
        }));

        content.Controls.Add(CreateSectionPanel("Global hotkey", new Control[]
        {
            _hotkeyEnabledCheckbox,
            CreatePropertyGrid(new (string Label, Control Control, string Hint)[]
            {
                ("Hotkey modifiers", _hotkeyModifierInput, "Modifier keys used for the pause or resume shortcut."),
                ("Hotkey key", _hotkeyKeyInput, "Main key used with the selected modifiers.")
            })
        }));

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _saveButton = new Button
        {
            Text = "Save",
            AutoSize = true
        };
        _saveButton.Click += SaveClicked;

        _cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(_cancelButton);

        root.Controls.Add(headerLabel, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(root);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        UpdateAppRuleHint();
        UpdateHotkeyControls();
    }

    private void AddAppClicked(object? sender, EventArgs e)
    {
        var processName = NormalizeProcessName(_newAppRuleInput.Text);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        for (var i = 0; i < _appRulesList.Items.Count; i++)
        {
            if (string.Equals(_appRulesList.Items[i]?.ToString(), processName, StringComparison.OrdinalIgnoreCase))
            {
                _appRulesList.SetItemChecked(i, true);
                _newAppRuleInput.Clear();
                return;
            }
        }

        _appRulesList.Items.Add(processName, true);
        _newAppRuleInput.Clear();
    }

    private void SaveClicked(object? sender, EventArgs e)
    {
        var appRules = new List<AppRule>();
        for (var i = 0; i < _appRulesList.Items.Count; i++)
        {
            var processName = NormalizeProcessName(_appRulesList.Items[i]?.ToString());
            if (string.IsNullOrWhiteSpace(processName))
            {
                continue;
            }

            appRules.Add(new AppRule
            {
                ProcessName = processName,
                Enabled = _appRulesList.GetItemChecked(i)
            });
        }

        var selectedModifier = _hotkeyModifierInput.SelectedItem as HotkeyDisplayOption;
        var selectedHotkeyKey = _hotkeyKeyInput.SelectedItem is Keys key ? key : Keys.D;
        var selectedPreset = _presetInput.SelectedItem is BuiltInPreset preset ? preset : BuiltInPreset.Custom;

        SavedSettings = AppSettings.Sanitize(new AppSettings
        {
            DuckLevel = DecimalToFloat(_duckLevelInput.Value),
            AttackMs = (int)_attackMsInput.Value,
            ReleaseMs = (int)_releaseMsInput.Value,
            Threshold = DecimalToFloat(_thresholdInput.Value),
            PollIntervalMs = (int)_pollIntervalInput.Value,
            DuckingEnabled = _duckingEnabledCheckbox.Checked,
            GamingModeEnabled = _gamingModeCheckbox.Checked,
            StartWithWindows = _startWithWindowsCheckbox.Checked,
            GamingDuckLevel = DecimalToFloat(_gamingDuckLevelInput.Value),
            FadeDurationMs = (int)_fadeDurationInput.Value,
            AppRuleMode = _appRuleModeInput.SelectedItem is AppRuleMode mode ? mode : AppRuleMode.AllExceptSpotify,
            AppRules = appRules,
            HotkeyEnabled = _hotkeyEnabledCheckbox.Checked,
            HotkeyModifiers = selectedModifier?.Value ?? (Keys.Control | Keys.Shift),
            HotkeyKey = selectedHotkeyKey,
            SelectedPreset = selectedPreset
        });

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ApplySelectedPreset()
    {
        var preset = _presetInput.SelectedItem is BuiltInPreset selectedPreset
            ? selectedPreset
            : BuiltInPreset.Custom;

        if (preset == BuiltInPreset.Custom)
        {
            return;
        }

        var presetSettings = CollectCurrentSettings().ApplyPreset(preset);

        _duckLevelInput.Value = (decimal)presetSettings.DuckLevel;
        _attackMsInput.Value = presetSettings.AttackMs;
        _releaseMsInput.Value = presetSettings.ReleaseMs;
        _thresholdInput.Value = (decimal)presetSettings.Threshold;
        _pollIntervalInput.Value = presetSettings.PollIntervalMs;
        _gamingDuckLevelInput.Value = (decimal)presetSettings.GamingDuckLevel;
        _fadeDurationInput.Value = presetSettings.FadeDurationMs;
        _gamingModeCheckbox.Checked = presetSettings.GamingModeEnabled;
        _appRuleModeInput.SelectedItem = presetSettings.AppRuleMode;
    }

    private AppSettings CollectCurrentSettings()
    {
        var appRules = new List<AppRule>();
        for (var i = 0; i < _appRulesList.Items.Count; i++)
        {
            var processName = NormalizeProcessName(_appRulesList.Items[i]?.ToString());
            if (string.IsNullOrWhiteSpace(processName))
            {
                continue;
            }

            appRules.Add(new AppRule
            {
                ProcessName = processName,
                Enabled = _appRulesList.GetItemChecked(i)
            });
        }

        var selectedModifier = _hotkeyModifierInput.SelectedItem as HotkeyDisplayOption;
        var selectedHotkeyKey = _hotkeyKeyInput.SelectedItem is Keys key ? key : Keys.D;

        return AppSettings.Sanitize(new AppSettings
        {
            DuckLevel = DecimalToFloat(_duckLevelInput.Value),
            AttackMs = (int)_attackMsInput.Value,
            ReleaseMs = (int)_releaseMsInput.Value,
            Threshold = DecimalToFloat(_thresholdInput.Value),
            PollIntervalMs = (int)_pollIntervalInput.Value,
            DuckingEnabled = _duckingEnabledCheckbox.Checked,
            GamingModeEnabled = _gamingModeCheckbox.Checked,
            StartWithWindows = _startWithWindowsCheckbox.Checked,
            GamingDuckLevel = DecimalToFloat(_gamingDuckLevelInput.Value),
            FadeDurationMs = (int)_fadeDurationInput.Value,
            AppRuleMode = _appRuleModeInput.SelectedItem is AppRuleMode mode ? mode : AppRuleMode.AllExceptSpotify,
            AppRules = appRules,
            HotkeyEnabled = _hotkeyEnabledCheckbox.Checked,
            HotkeyModifiers = selectedModifier?.Value ?? (Keys.Control | Keys.Shift),
            HotkeyKey = selectedHotkeyKey,
            SelectedPreset = _presetInput.SelectedItem is BuiltInPreset preset ? preset : BuiltInPreset.Custom
        });
    }

    private void UpdateHotkeyControls()
    {
        var enabled = _hotkeyEnabledCheckbox.Checked;
        _hotkeyModifierInput.Enabled = enabled;
        _hotkeyKeyInput.Enabled = enabled;
    }

    private void UpdateAppRuleHint()
    {
        var mode = _appRuleModeInput.SelectedItem is AppRuleMode selectedMode
            ? selectedMode
            : AppRuleMode.AllExceptSpotify;

        _appRuleHintLabel.Text = mode switch
        {
            AppRuleMode.AllExceptSpotify => "Any non-Spotify app with audible output can trigger ducking.",
            AppRuleMode.Whitelist => "Only checked apps can trigger ducking.",
            AppRuleMode.Blacklist => "All apps except checked apps can trigger ducking.",
            _ => string.Empty
        };
    }

    private void SelectHotkeyModifier(Keys modifiers)
    {
        foreach (var item in _hotkeyModifierInput.Items)
        {
            if (item is HotkeyDisplayOption option && option.Value == modifiers)
            {
                _hotkeyModifierInput.SelectedItem = item;
                return;
            }
        }

        _hotkeyModifierInput.SelectedIndex = 0;
    }

    private static IEnumerable<Keys> GetHotkeyKeys()
    {
        for (var key = Keys.A; key <= Keys.Z; key++)
        {
            yield return key;
        }

        for (var key = Keys.F1; key <= Keys.F12; key++)
        {
            yield return key;
        }
    }

    private static Control CreateSectionPanel(string title, IEnumerable<Control> controls)
    {
        var groupBox = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        foreach (var control in controls)
        {
            control.Margin = new Padding(0, 0, 0, 8);
            panel.Controls.Add(control);
        }

        groupBox.Controls.Add(panel);
        return groupBox;
    }

    private static Control CreatePropertyGrid((string Label, Control Control, string Hint)[] rows)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        for (var i = 0; i < rows.Length; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = rows[i].Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 8, 0)
            };

            var inputPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true
            };
            inputPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inputPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            rows[i].Control.Dock = DockStyle.Top;

            var hint = new Label
            {
                Text = rows[i].Hint,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 4, 0, 0)
            };

            inputPanel.Controls.Add(rows[i].Control);
            inputPanel.Controls.Add(hint);

            table.Controls.Add(label, 0, i);
            table.Controls.Add(inputPanel, 1, i);
        }

        return table;
    }

    private static NumericUpDown CreateIntegerInput(int minimum, int maximum, int value)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            DecimalPlaces = 0,
            ThousandsSeparator = false,
            Width = 120
        };
    }

    private static NumericUpDown CreateDecimalInput(decimal minimum, decimal maximum, decimal increment, decimal value, int decimalPlaces = 2)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            DecimalPlaces = decimalPlaces,
            Value = value,
            Width = 120
        };
    }

    private static float DecimalToFloat(decimal value)
    {
        return float.Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    private static string NormalizeProcessName(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private sealed class HotkeyDisplayOption
    {
        public string Text { get; private init; } = string.Empty;
        public Keys Value { get; private init; }

        public static HotkeyDisplayOption Create(string text, Keys value)
        {
            return new HotkeyDisplayOption
            {
                Text = text,
                Value = value
            };
        }

        public override string ToString()
        {
            return Text;
        }
    }
}

// Made with Bob
