using System.Runtime.InteropServices;
using RTTYContestSimulator.Audio;
using RTTYContestSimulator.Contest;
using RTTYContestSimulator.Localization;

namespace RTTYContestSimulator;

public partial class MainForm : Form
{
    // Para suspender el redibujado del RichTextBox
    private const int WM_SETREDRAW = 0x0B;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private readonly RttyGenerator _rttyGenerator;
    private readonly AudioPlayer _audioPlayer;
    private readonly ContestSimulator _contestSimulator;

    // Controles
    private TextBox _txtCallsign = null!;
    private NumericUpDown _numMarkFreq = null!;
    private NumericUpDown _numShift = null!;
    private NumericUpDown _numBaudRate = null!;
    private TrackBar _trkVolume = null!;
    private TrackBar _trkNoise = null!;
    private CheckBox _chkNoise = null!;
    private ComboBox _cboAudioDevice = null!;
    private ComboBox _cboLanguage = null!;
    private ComboBox _cboContestType = null!;
    private Button _btnStart = null!;
    private Button _btnStop = null!;
    private Button _btnTest = null!;
    private RichTextBox _txtLog = null!;
    private Label _lblStatus = null!;
    private Label _lblQsoCount = null!;
    private Label _lblSerial = null!;
    private Panel _pnlIndicator = null!;

    // Labels que necesitan actualización al cambiar idioma
    private Label _lblCallLabel = null!;
    private Label _lblDeviceLabel = null!;
    private Label _lblVolLabel = null!;
    private Label _lblLangLabel = null!;
    private Label _lblContestLabel = null!;
    private GroupBox _grpStation = null!;
    private GroupBox _grpRtty = null!;
    private GroupBox _grpAudio = null!;
    private GroupBox _grpLog = null!;

    // Para el progreso de caracteres
    private string _currentMessage = "";
    private int _currentMessageStart = 0;
    private const int MAX_LOG_LENGTH = 50000; // Limitar tamaño del log

    public MainForm()
    {
        _rttyGenerator = new RttyGenerator();
        _audioPlayer = new AudioPlayer(_rttyGenerator.SampleRate);
        _contestSimulator = new ContestSimulator(_rttyGenerator, _audioPlayer);

        InitializeComponent();
        SetupEvents();
        LoadAudioDevices();
    }

    private void InitializeComponent()
    {
        this.Text = "RTTY Runner by EC5W";
        this.Size = new Size(750, 620);
        this.MinimumSize = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9F);

        // Load icon from executable
        try
        {
            string exePath = Environment.ProcessPath ?? System.AppContext.BaseDirectory + "RTTYContestSimulator.exe";
            this.Icon = Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Panel superior - Configuración
        var configPanel = CreateConfigPanel();
        mainPanel.Controls.Add(configPanel, 0, 0);

        // Panel central - Log
        var logPanel = CreateLogPanel();
        mainPanel.Controls.Add(logPanel, 0, 1);

        // Panel inferior - Status
        var statusPanel = CreateStatusPanel();
        mainPanel.Controls.Add(statusPanel, 0, 2);

        this.Controls.Add(mainPanel);
    }

    private Panel CreateConfigPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 240,
            MinimumSize = new Size(0, 240),
            Padding = new Padding(5)
        };

        // Grupo de configuración de estación y concurso
        _grpStation = new GroupBox
        {
            Text = Strings.StationGroup,
            Location = new Point(5, 5),
            Size = new Size(200, 115)
        };

        _lblCallLabel = new Label { Text = Strings.CallsignLabel, Location = new Point(10, 22), AutoSize = true };
        _txtCallsign = new TextBox
        {
            Text = "EA5XXX",
            Location = new Point(75, 19),
            Size = new Size(110, 23),
            CharacterCasing = CharacterCasing.Upper
        };

        _lblLangLabel = new Label { Text = Strings.LanguageLabel, Location = new Point(10, 52), AutoSize = true };
        _cboLanguage = new ComboBox
        {
            Location = new Point(75, 49),
            Size = new Size(110, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboLanguage.Items.AddRange(new object[] { "Español", "English" });
        _cboLanguage.SelectedIndex = 0;

        _lblContestLabel = new Label { Text = Strings.ContestTypeLabel, Location = new Point(10, 82), AutoSize = true };
        _cboContestType = new ComboBox
        {
            Location = new Point(75, 79),
            Size = new Size(110, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboContestType.Items.AddRange(new object[] { "CQ WPX", "CQ WW" });
        _cboContestType.SelectedIndex = 0;

        _grpStation.Controls.AddRange(new Control[] { _lblCallLabel, _txtCallsign, _lblLangLabel, _cboLanguage, _lblContestLabel, _cboContestType });

        // Grupo de configuración RTTY
        _grpRtty = new GroupBox
        {
            Text = Strings.RttyParamsGroup,
            Location = new Point(215, 5),
            Size = new Size(250, 165)
        };

        var lblMark = new Label { Text = Strings.MarkLabel, Location = new Point(10, 25), AutoSize = true };
        _numMarkFreq = new NumericUpDown
        {
            Location = new Point(100, 23),
            Size = new Size(80, 23),
            Minimum = 500,
            Maximum = 3000,
            Value = 2125,
            DecimalPlaces = 0
        };

        var lblShift = new Label { Text = Strings.ShiftLabel, Location = new Point(10, 55), AutoSize = true };
        _numShift = new NumericUpDown
        {
            Location = new Point(100, 53),
            Size = new Size(80, 23),
            Minimum = 100,
            Maximum = 500,
            Value = 170,
            DecimalPlaces = 0
        };

        var lblBaud = new Label { Text = Strings.BaudLabel, Location = new Point(10, 85), AutoSize = true };
        _numBaudRate = new NumericUpDown
        {
            Location = new Point(100, 83),
            Size = new Size(80, 23),
            Minimum = 30,
            Maximum = 100,
            Value = 45.45m,
            DecimalPlaces = 2
        };

        var lblSpaceVal = new Label
        {
            Text = $"Space: {2125 + 170} Hz",
            Location = new Point(10, 115),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        _numMarkFreq.ValueChanged += (s, e) => lblSpaceVal.Text = $"Space: {_numMarkFreq.Value + _numShift.Value} Hz";
        _numShift.ValueChanged += (s, e) => lblSpaceVal.Text = $"Space: {_numMarkFreq.Value + _numShift.Value} Hz";

        _grpRtty.Controls.AddRange(new Control[] { lblMark, _numMarkFreq, lblShift, _numShift, lblBaud, _numBaudRate, lblSpaceVal });

        // Grupo de audio
        _grpAudio = new GroupBox
        {
            Text = Strings.AudioGroup,
            Location = new Point(475, 5),
            Size = new Size(250, 185)
        };

        _lblDeviceLabel = new Label { Text = Strings.SoundCardLabel, Location = new Point(10, 22), AutoSize = true };
        _cboAudioDevice = new ComboBox
        {
            Location = new Point(10, 42),
            Size = new Size(230, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _lblVolLabel = new Label { Text = Strings.VolumeLabel, Location = new Point(10, 72), AutoSize = true };
        _trkVolume = new TrackBar
        {
            Location = new Point(10, 90),
            Size = new Size(230, 45),
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickFrequency = 10
        };

        _chkNoise = new CheckBox
        {
            Text = Strings.HfNoiseLabel,
            Location = new Point(10, 135),
            Size = new Size(80, 20)
        };

        _trkNoise = new TrackBar
        {
            Location = new Point(90, 130),
            Size = new Size(150, 45),
            Minimum = 0,
            Maximum = 100,
            Value = 30,
            TickFrequency = 10,
            Enabled = false
        };

        _chkNoise.CheckedChanged += (s, e) => _trkNoise.Enabled = _chkNoise.Checked;

        _grpAudio.Controls.AddRange(new Control[] { _lblDeviceLabel, _cboAudioDevice, _lblVolLabel, _trkVolume, _chkNoise, _trkNoise });

        // Botones de control - posicionados debajo de todos los GroupBoxes
        _btnStart = new Button
        {
            Text = Strings.StartButton,
            Location = new Point(10, 195),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        _btnStop = new Button
        {
            Text = Strings.StopButton,
            Location = new Point(105, 195),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };

        _btnTest = new Button
        {
            Text = Strings.TestButton,
            Location = new Point(215, 200),
            Size = new Size(100, 25),
            FlatStyle = FlatStyle.Flat
        };

        panel.Controls.AddRange(new Control[] { _grpStation, _grpRtty, _grpAudio, _btnStart, _btnStop, _btnTest });

        return panel;
    }

    private void UpdateLanguageUI()
    {
        _grpStation.Text = Strings.StationGroup;
        _grpRtty.Text = Strings.RttyParamsGroup;
        _grpAudio.Text = Strings.AudioGroup;
        _grpLog.Text = Strings.LogTitle;

        _lblCallLabel.Text = Strings.CallsignLabel;
        _lblLangLabel.Text = Strings.LanguageLabel;
        _lblContestLabel.Text = Strings.ContestTypeLabel;
        _lblDeviceLabel.Text = Strings.SoundCardLabel;
        _lblVolLabel.Text = Strings.VolumeLabel;
        _chkNoise.Text = Strings.HfNoiseLabel;

        _btnStart.Text = Strings.StartButton;
        _btnStop.Text = Strings.StopButton;
        _btnTest.Text = Strings.TestButton;

        UpdateStatus(Strings.StatusStopped);
    }

    private Panel CreateLogPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        _grpLog = new GroupBox
        {
            Text = Strings.LogTitle,
            Dock = DockStyle.Fill
        };

        _txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 11F),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(0, 255, 0),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };

        _grpLog.Controls.Add(_txtLog);
        panel.Controls.Add(_grpLog);

        return panel;
    }

    private Panel CreateStatusPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 50
        };

        _pnlIndicator = new Panel
        {
            Location = new Point(10, 15),
            Size = new Size(20, 20),
            BackColor = Color.Gray
        };

        _lblStatus = new Label
        {
            Text = string.Format(Strings.StatusFormat, Strings.StatusStopped),
            Location = new Point(40, 15),
            AutoSize = true
        };

        _lblQsoCount = new Label
        {
            Text = string.Format(Strings.QsosFormat, 0),
            Location = new Point(200, 15),
            AutoSize = true
        };

        _lblSerial = new Label
        {
            Text = "Serial: 001",
            Location = new Point(300, 15),
            AutoSize = true
        };

        panel.Controls.AddRange(new Control[] { _pnlIndicator, _lblStatus, _lblQsoCount, _lblSerial });

        return panel;
    }

    private void LoadAudioDevices()
    {
        var devices = AudioPlayer.GetOutputDevices();
        _cboAudioDevice.Items.Clear();
        foreach (var device in devices)
        {
            _cboAudioDevice.Items.Add(device);
        }

        if (_cboAudioDevice.Items.Count > 0)
        {
            _cboAudioDevice.SelectedIndex = 0;
        }
    }

    private void SetupEvents()
    {
        _btnStart.Click += BtnStart_Click;
        _btnStop.Click += BtnStop_Click;
        _btnTest.Click += BtnTest_Click;

        _txtCallsign.TextChanged += (s, e) => _contestSimulator.MyCallsign = _txtCallsign.Text;

        // Cambio de idioma
        _cboLanguage.SelectedIndexChanged += (s, e) =>
        {
            Strings.CurrentLanguage = _cboLanguage.SelectedIndex == 0 ? Language.Spanish : Language.English;
            UpdateLanguageUI();
        };

        // Cambio de tipo de concurso
        _cboContestType.SelectedIndexChanged += (s, e) =>
        {
            _contestSimulator.ContestType = _cboContestType.SelectedIndex == 0
                ? ContestType.CqWpxRtty
                : ContestType.CqWwRtty;
        };

        _numMarkFreq.ValueChanged += (s, e) => _rttyGenerator.MarkFrequency = (double)_numMarkFreq.Value;
        _numShift.ValueChanged += (s, e) => _rttyGenerator.Shift = (double)_numShift.Value;
        _numBaudRate.ValueChanged += (s, e) => _rttyGenerator.BaudRate = (double)_numBaudRate.Value;
        _trkVolume.ValueChanged += (s, e) => _rttyGenerator.Volume = _trkVolume.Value / 100.0;
        _trkNoise.ValueChanged += (s, e) => _rttyGenerator.NoiseLevel = _chkNoise.Checked ? _trkNoise.Value / 100.0 : 0;
        _chkNoise.CheckedChanged += (s, e) => _rttyGenerator.NoiseLevel = _chkNoise.Checked ? _trkNoise.Value / 100.0 : 0;

        _cboAudioDevice.SelectedIndexChanged += (s, e) =>
        {
            if (_cboAudioDevice.SelectedItem is AudioDeviceInfo device)
            {
                _audioPlayer.SetDevice(device.DeviceNumber);
            }
        };

        _contestSimulator.MessageStarted += ContestSimulator_MessageStarted;
        _contestSimulator.CharacterSent += ContestSimulator_CharacterSent;
        _contestSimulator.MessageCompleted += ContestSimulator_MessageCompleted;
        _contestSimulator.QsoCompleted += ContestSimulator_QsoCompleted;
        _contestSimulator.StateChanged += ContestSimulator_StateChanged;
        _contestSimulator.SimulationStopped += ContestSimulator_SimulationStopped;

        this.FormClosing += MainForm_FormClosing;
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        _contestSimulator.MyCallsign = _txtCallsign.Text;
        _contestSimulator.Start();

        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        _btnTest.Enabled = false;
        SetControlsEnabled(false);

        _pnlIndicator.BackColor = Color.Lime;
        UpdateStatus(Strings.StatusRunning);
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        _contestSimulator.Stop();

        _btnStart.Enabled = true;
        _btnStop.Enabled = false;
        _btnTest.Enabled = true;
        SetControlsEnabled(true);

        _pnlIndicator.BackColor = Color.Gray;
        UpdateStatus(Strings.StatusStopped);
    }

    private void BtnTest_Click(object? sender, EventArgs e)
    {
        string contestName = _contestSimulator.ContestType == ContestType.CqWpxRtty ? "WPX" : "WW";
        string testMsg = $"CQ CQ {contestName} DE {_txtCallsign.Text} {_txtCallsign.Text} K";
        _contestSimulator.SendTestMessage(testMsg);
    }

    private void SetControlsEnabled(bool enabled)
    {
        _txtCallsign.Enabled = enabled;
        _numMarkFreq.Enabled = enabled;
        _numShift.Enabled = enabled;
        _numBaudRate.Enabled = enabled;
        _cboAudioDevice.Enabled = enabled;
        _cboLanguage.Enabled = enabled;
        _cboContestType.Enabled = enabled;
    }

    private void ContestSimulator_MessageStarted(object? sender, MessageProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => StartMessageProgress(e.Message));
        }
        else
        {
            StartMessageProgress(e.Message);
        }
    }

    private void ContestSimulator_CharacterSent(object? sender, int charIndex)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateCharacterProgress(charIndex));
        }
        else
        {
            UpdateCharacterProgress(charIndex);
        }
    }

    private void ContestSimulator_MessageCompleted(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => CompleteMessageProgress());
        }
        else
        {
            CompleteMessageProgress();
        }
    }

    private void StartMessageProgress(string message)
    {
        _currentMessage = message;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        // Suspender redibujado
        SendMessage(_txtLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            // Limpiar log si es muy largo
            TrimLogIfNeeded();

            // Guardar posición donde empieza el mensaje (después del timestamp)
            _txtLog.AppendText($"[{timestamp}] ");
            _currentMessageStart = _txtLog.TextLength;

            // Mostrar mensaje completo en gris (pendiente)
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.SelectionColor = Color.Gray;
            _txtLog.AppendText(message);
            _txtLog.SelectionColor = Color.FromArgb(0, 255, 0);
            _txtLog.AppendText("\r\n");
        }
        finally
        {
            SendMessage(_txtLog.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            _txtLog.Invalidate();
            _txtLog.ScrollToCaret();
        }
    }

    private void TrimLogIfNeeded()
    {
        if (_txtLog.TextLength > MAX_LOG_LENGTH)
        {
            // Eliminar la primera mitad del log
            int removeLength = _txtLog.TextLength / 2;
            // Buscar el siguiente salto de línea después de removeLength
            int newStart = _txtLog.Text.IndexOf('\n', removeLength);
            if (newStart > 0)
            {
                _txtLog.Select(0, newStart + 1);
                _txtLog.SelectedText = "";
                _currentMessageStart = 0;
            }
        }
    }

    private void UpdateCharacterProgress(int charIndex)
    {
        if (charIndex >= _currentMessage.Length) return;

        // Colorear el carácter actual en verde brillante (enviado)
        int position = _currentMessageStart + charIndex;
        if (position < _txtLog.TextLength)
        {
            // Suspender redibujado para evitar parpadeo
            SendMessage(_txtLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                _txtLog.Select(position, 1);
                _txtLog.SelectionColor = Color.FromArgb(0, 255, 0);
                _txtLog.SelectionStart = _txtLog.TextLength;
            }
            finally
            {
                SendMessage(_txtLog.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                // Solo refrescar al final del mensaje o cada N caracteres
                if (charIndex == _currentMessage.Length - 1 || charIndex % 5 == 0)
                {
                    _txtLog.Invalidate();
                }
            }
        }
    }

    private void CompleteMessageProgress()
    {
        // Asegurar que todo el mensaje esté en verde
        if (_currentMessageStart > 0 && _currentMessageStart + _currentMessage.Length <= _txtLog.TextLength)
        {
            SendMessage(_txtLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                _txtLog.Select(_currentMessageStart, _currentMessage.Length);
                _txtLog.SelectionColor = Color.FromArgb(0, 255, 0);
                _txtLog.SelectionStart = _txtLog.TextLength;
            }
            finally
            {
                SendMessage(_txtLog.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                _txtLog.Invalidate();
            }
        }
    }

    private void ContestSimulator_QsoCompleted(object? sender, QsoRecord qso)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateQsoInfo(qso));
        }
        else
        {
            UpdateQsoInfo(qso);
        }
    }

    private void ContestSimulator_StateChanged(object? sender, QsoState state)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStateIndicator(state));
        }
        else
        {
            UpdateStateIndicator(state);
        }
    }

    private void ContestSimulator_SimulationStopped(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                _btnStart.Enabled = true;
                _btnStop.Enabled = false;
                _btnTest.Enabled = true;
                SetControlsEnabled(true);
                _pnlIndicator.BackColor = Color.Gray;
                UpdateStatus(Strings.StatusStopped);
            });
        }
    }

    private void AppendLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _txtLog.AppendText($"[{timestamp}] {message}\r\n");
        _txtLog.ScrollToCaret();
    }

    private void UpdateQsoInfo(QsoRecord qso)
    {
        _lblQsoCount.Text = string.Format(Strings.QsosFormat, _contestSimulator.QsoCount);
        _lblSerial.Text = string.Format(Strings.SerialFormat, _contestSimulator.CurrentSerial);

        // Línea separadora entre QSOs
        _txtLog.AppendText($"{Strings.QsoCompletedSeparator}\r\n");
    }

    private void UpdateStateIndicator(QsoState state)
    {
        _pnlIndicator.BackColor = state switch
        {
            QsoState.SendingCq or QsoState.SendingReport or QsoState.SendingTu => Color.Red,
            QsoState.WaitingForCall or QsoState.WaitingForQsl => Color.Yellow,
            QsoState.Completed => Color.Lime,
            _ => Color.Gray
        };

        string stateText = state switch
        {
            QsoState.SendingCq => Strings.StatusTxCq,
            QsoState.WaitingForCall => Strings.StatusRxWaitingCall,
            QsoState.SendingReport => Strings.StatusTxReport,
            QsoState.WaitingForQsl => Strings.StatusRxWaitingQsl,
            QsoState.SendingTu => Strings.StatusTxTu,
            QsoState.Completed => Strings.StatusQsoCompleted,
            _ => Strings.StatusIdle
        };

        UpdateStatus($"{Strings.StatusRunning} - {stateText}");
    }

    private void UpdateStatus(string status)
    {
        _lblStatus.Text = string.Format(Strings.StatusFormat, status);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _contestSimulator.Stop();
        _audioPlayer.Dispose();
    }
}
