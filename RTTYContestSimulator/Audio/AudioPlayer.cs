using NAudio.Wave;

namespace RTTYContestSimulator.Audio;

/// <summary>
/// Reproductor de audio usando NAudio.
/// Maneja la reproducción de muestras de audio generadas por RttyGenerator.
/// </summary>
public class AudioPlayer : IDisposable
{
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _bufferedProvider;
    private readonly int _sampleRate;
    private bool _isPlaying;
    private bool _disposed;
    private int _deviceNumber = -1; // -1 = dispositivo por defecto

    public event EventHandler? PlaybackStopped;

    public bool IsPlaying => _isPlaying;
    public int DeviceNumber => _deviceNumber;

    public AudioPlayer(int sampleRate = 44100)
    {
        _sampleRate = sampleRate;
    }

    /// <summary>
    /// Obtiene la lista de dispositivos de salida de audio disponibles.
    /// </summary>
    public static List<AudioDeviceInfo> GetOutputDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        for (int i = -1; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo
            {
                DeviceNumber = i,
                Name = i == -1 ? "(Predeterminado) " + caps.ProductName : caps.ProductName,
                Channels = caps.Channels
            });
        }

        return devices;
    }

    /// <summary>
    /// Establece el dispositivo de salida a usar.
    /// </summary>
    public void SetDevice(int deviceNumber)
    {
        if (_deviceNumber != deviceNumber)
        {
            _deviceNumber = deviceNumber;

            // Si ya estaba inicializado, reinicializar con el nuevo dispositivo
            if (_waveOut != null)
            {
                bool wasPlaying = _isPlaying;
                Stop();
                _waveOut.Dispose();
                _waveOut = null;
                _bufferedProvider = null;

                if (wasPlaying)
                {
                    Initialize();
                }
            }
        }
    }

    /// <summary>
    /// Inicializa el dispositivo de audio.
    /// </summary>
    public void Initialize()
    {
        _waveOut = new WaveOutEvent
        {
            DeviceNumber = _deviceNumber,
            DesiredLatency = 100
        };

        // Mono, 32-bit float
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, 1);
        _bufferedProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = _sampleRate * 4 * 10, // 10 segundos de buffer
            DiscardOnBufferOverflow = true
        };

        _waveOut.Init(_bufferedProvider);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
    }

    /// <summary>
    /// Reproduce un array de muestras de audio.
    /// </summary>
    public void Play(float[] samples)
    {
        if (_waveOut == null || _bufferedProvider == null)
        {
            Initialize();
        }

        // Convertir float[] a byte[]
        byte[] buffer = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);

        _bufferedProvider!.AddSamples(buffer, 0, buffer.Length);

        if (!_isPlaying)
        {
            _waveOut!.Play();
            _isPlaying = true;
        }
    }

    /// <summary>
    /// Agrega muestras al buffer sin iniciar reproducción.
    /// </summary>
    public void AddSamples(float[] samples)
    {
        if (_bufferedProvider == null)
        {
            Initialize();
        }

        byte[] buffer = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);
        _bufferedProvider!.AddSamples(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// Inicia la reproducción si hay muestras en el buffer.
    /// </summary>
    public void Start()
    {
        if (_waveOut != null && !_isPlaying)
        {
            _waveOut.Play();
            _isPlaying = true;
        }
    }

    /// <summary>
    /// Detiene la reproducción.
    /// </summary>
    public void Stop()
    {
        if (_waveOut != null && _isPlaying)
        {
            _waveOut.Stop();
            _bufferedProvider?.ClearBuffer();
            _isPlaying = false;
        }
    }

    /// <summary>
    /// Pausa la reproducción.
    /// </summary>
    public void Pause()
    {
        _waveOut?.Pause();
        _isPlaying = false;
    }

    /// <summary>
    /// Obtiene los bytes pendientes en el buffer.
    /// </summary>
    public int BufferedBytes => _bufferedProvider?.BufferedBytes ?? 0;

    /// <summary>
    /// Obtiene la duración del audio en buffer (en milisegundos).
    /// </summary>
    public int BufferedDurationMs
    {
        get
        {
            if (_bufferedProvider == null) return 0;
            return (int)(_bufferedProvider.BufferedBytes / (float)(_sampleRate * sizeof(float)) * 1000);
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_bufferedProvider?.BufferedBytes == 0)
        {
            _isPlaying = false;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _bufferedProvider = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Información de un dispositivo de audio.
/// </summary>
public class AudioDeviceInfo
{
    public int DeviceNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Channels { get; set; }

    public override string ToString() => Name;
}
