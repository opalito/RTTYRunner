using RTTYContestSimulator.Encoding;

namespace RTTYContestSimulator.Audio;

/// <summary>
/// Generador de señal RTTY FSK.
/// Produce muestras de audio a 44100 Hz con modulación FSK.
/// </summary>
public class RttyGenerator
{
    public int SampleRate { get; set; } = 44100;
    public double BaudRate { get; set; } = 45.45;
    public double MarkFrequency { get; set; } = 2125.0;
    public double Shift { get; set; } = 170.0;
    public double Volume { get; set; } = 0.8;
    public double NoiseLevel { get; set; } = 0.0;

    public double SpaceFrequency => MarkFrequency + Shift;

    private readonly BaudotEncoder _encoder = new();
    private readonly Random _random = new();
    private double _phase = 0;

    // Estado para ruido HF realista
    private double _pinkNoiseState = 0;
    private double _qrmPhase1 = 0;
    private double _qrmPhase2 = 0;
    private double _qrmPhase3 = 0;
    private double _splatterPhase = 0;
    private int _splatterCounter = 0;
    private bool _splatterActive = false;
    private double _splatterFreq = 800;
    private int _crackCounter = 0;

    /// <summary>
    /// Número de samples por bit a la velocidad actual.
    /// </summary>
    public int SamplesPerBit => (int)(SampleRate / BaudRate);

    /// <summary>
    /// Genera muestras de audio RTTY para el texto dado.
    /// </summary>
    /// <param name="text">Texto a transmitir</param>
    /// <returns>Array de muestras de audio float (-1 a 1)</returns>
    public float[] GenerateAudio(string text)
    {
        var baudotBytes = _encoder.EncodeText(text);
        var samples = new List<float>();

        // Agregar un poco de mark tone al inicio (idle)
        samples.AddRange(GenerateTone(true, SamplesPerBit * 4));

        foreach (byte b in baudotBytes)
        {
            var charSamples = GenerateCharacter(b);
            samples.AddRange(charSamples);
        }

        // Agregar mark tone al final
        samples.AddRange(GenerateTone(true, SamplesPerBit * 2));

        // Añadir ruido si está configurado
        if (NoiseLevel > 0)
        {
            AddNoise(samples);
        }

        return samples.ToArray();
    }

    /// <summary>
    /// Genera las muestras de audio para un carácter Baudot completo.
    /// Incluye start bit, 5 data bits y stop bit.
    /// </summary>
    private List<float> GenerateCharacter(byte baudotChar)
    {
        var samples = new List<float>();

        // Start bit - Space
        samples.AddRange(GenerateTone(false, SamplesPerBit));

        // 5 data bits - LSB first
        for (int i = 0; i < 5; i++)
        {
            bool isMark = ((baudotChar >> i) & 1) == 1;
            samples.AddRange(GenerateTone(isMark, SamplesPerBit));
        }

        // Stop bit - Mark, 1.5 bits de duración
        int stopBitSamples = (int)(SamplesPerBit * 1.5);
        samples.AddRange(GenerateTone(true, stopBitSamples));

        return samples;
    }

    /// <summary>
    /// Genera un tono (Mark o Space) con transición de fase continua.
    /// </summary>
    private float[] GenerateTone(bool isMark, int numSamples)
    {
        var samples = new float[numSamples];
        double frequency = isMark ? MarkFrequency : SpaceFrequency;
        double phaseIncrement = 2.0 * Math.PI * frequency / SampleRate;

        for (int i = 0; i < numSamples; i++)
        {
            samples[i] = (float)(Math.Sin(_phase) * Volume);
            _phase += phaseIncrement;

            // Mantener la fase en rango para evitar pérdida de precisión
            if (_phase > 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }
        }

        return samples;
    }

    /// <summary>
    /// Genera silencio con ruido HF de fondo (para pausas entre transmisiones).
    /// </summary>
    public float[] GenerateSilence(int durationMs)
    {
        int numSamples = (int)(SampleRate * durationMs / 1000.0);
        var samples = new List<float>(new float[numSamples]);

        if (NoiseLevel > 0)
        {
            AddNoise(samples);
        }

        return samples.ToArray();
    }

    /// <summary>
    /// Genera solo tono idle (Mark continuo).
    /// </summary>
    public float[] GenerateIdleTone(int durationMs)
    {
        int numSamples = (int)(SampleRate * durationMs / 1000.0);
        var samples = GenerateTone(true, numSamples);

        if (NoiseLevel > 0)
        {
            var samplesList = samples.ToList();
            AddNoise(samplesList);
            return samplesList.ToArray();
        }

        return samples;
    }

    /// <summary>
    /// Añade ruido HF realista: atmosférico, QRM, splatters típicos de 40m.
    /// </summary>
    private void AddNoise(List<float> samples)
    {
        for (int i = 0; i < samples.Count; i++)
        {
            double totalNoise = 0;

            // 1. Ruido atmosférico (ruido rosa - más graves que agudos)
            totalNoise += GeneratePinkNoise() * 0.4;

            // 2. Ruido blanco de fondo (hiss del receptor)
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double whiteNoise = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            totalNoise += whiteNoise * 0.2;

            // 3. QRM - Otras estaciones SSB cercanas (tonos modulados)
            totalNoise += GenerateQRM() * 0.3;

            // 4. Splatters - Distorsión de estaciones SSB sobremoduladas
            totalNoise += GenerateSplatter() * 0.25;

            // 5. Cracks atmosféricos (impulsos aleatorios)
            totalNoise += GenerateAtmosphericCrack() * 0.15;

            // Aplicar nivel de ruido configurado
            samples[i] += (float)(totalNoise * NoiseLevel * Volume);

            // Clamp para evitar clipping
            samples[i] = Math.Clamp(samples[i], -1.0f, 1.0f);
        }
    }

    /// <summary>
    /// Genera ruido rosa (1/f) - más energía en frecuencias bajas.
    /// </summary>
    private double GeneratePinkNoise()
    {
        // Filtro simple para aproximar ruido rosa
        double white = _random.NextDouble() * 2 - 1;
        _pinkNoiseState = 0.99 * _pinkNoiseState + 0.01 * white;
        return _pinkNoiseState * 3.0 + white * 0.3;
    }

    /// <summary>
    /// Genera QRM - otras estaciones SSB cercanas.
    /// Simula voces distorsionadas con múltiples tonos modulados.
    /// </summary>
    private double GenerateQRM()
    {
        double qrm = 0;

        // Estación 1: voz grave ~300-800 Hz
        double modulation1 = 0.5 + 0.5 * Math.Sin(_qrmPhase1 * 0.0001);
        qrm += Math.Sin(_qrmPhase1) * modulation1 * 0.4;
        _qrmPhase1 += 2.0 * Math.PI * (350 + 100 * Math.Sin(_qrmPhase1 * 0.00003)) / SampleRate;

        // Estación 2: voz media ~500-1200 Hz (más lejana)
        double modulation2 = 0.3 + 0.4 * Math.Sin(_qrmPhase2 * 0.00015);
        qrm += Math.Sin(_qrmPhase2) * modulation2 * 0.25;
        _qrmPhase2 += 2.0 * Math.PI * (700 + 200 * Math.Sin(_qrmPhase2 * 0.00005)) / SampleRate;

        // Estación 3: intermitente (aparece y desaparece)
        if (Math.Sin(_qrmPhase3 * 0.00002) > 0.3)
        {
            qrm += Math.Sin(_qrmPhase3) * 0.3;
        }
        _qrmPhase3 += 2.0 * Math.PI * 1100 / SampleRate;

        // Mantener fases en rango
        if (_qrmPhase1 > 1000) _qrmPhase1 -= 1000;
        if (_qrmPhase2 > 1000) _qrmPhase2 -= 1000;
        if (_qrmPhase3 > 1000) _qrmPhase3 -= 1000;

        return qrm;
    }

    /// <summary>
    /// Genera splatters - distorsión de SSB sobremodulada.
    /// Son ráfagas cortas de ruido con armónicos.
    /// </summary>
    private double GenerateSplatter()
    {
        _splatterCounter++;

        // Activar splatter aleatoriamente cada ~0.5-2 segundos
        if (!_splatterActive && _random.NextDouble() < 0.00005)
        {
            _splatterActive = true;
            _splatterCounter = 0;
            _splatterFreq = 600 + _random.NextDouble() * 800; // 600-1400 Hz
        }

        // Duración del splatter: 50-200ms
        int splatterDuration = (int)(SampleRate * (0.05 + _random.NextDouble() * 0.15));

        if (_splatterActive)
        {
            if (_splatterCounter > splatterDuration)
            {
                _splatterActive = false;
                return 0;
            }

            // Envelope del splatter (ataque rápido, decay lento)
            double envelope = Math.Exp(-_splatterCounter * 3.0 / splatterDuration);

            // Splatter: fundamental + armónicos con distorsión
            double splatter = Math.Sin(_splatterPhase) * 0.5;
            splatter += Math.Sin(_splatterPhase * 2.1) * 0.3; // 2do armónico
            splatter += Math.Sin(_splatterPhase * 3.2) * 0.2; // 3er armónico
            splatter += (_random.NextDouble() * 2 - 1) * 0.3; // Ruido de distorsión

            _splatterPhase += 2.0 * Math.PI * _splatterFreq / SampleRate;
            if (_splatterPhase > 2.0 * Math.PI) _splatterPhase -= 2.0 * Math.PI;

            return splatter * envelope;
        }

        return 0;
    }

    /// <summary>
    /// Genera cracks atmosféricos - impulsos cortos y fuertes.
    /// </summary>
    private double GenerateAtmosphericCrack()
    {
        _crackCounter++;

        // Crack aleatorio cada ~1-5 segundos
        if (_random.NextDouble() < 0.00002)
        {
            _crackCounter = 0;
        }

        // Duración del crack: 5-20ms
        int crackDuration = (int)(SampleRate * 0.01);

        if (_crackCounter < crackDuration)
        {
            // Impulso con decay exponencial
            double envelope = Math.Exp(-_crackCounter * 5.0 / crackDuration);
            double crack = (_random.NextDouble() * 2 - 1) * envelope * 2.0;
            return crack;
        }

        return 0;
    }

    /// <summary>
    /// Reinicia la fase del oscilador.
    /// </summary>
    public void Reset()
    {
        _phase = 0;
    }

    /// <summary>
    /// Calcula la duración en milisegundos de un texto dado.
    /// </summary>
    public int CalculateDurationMs(string text)
    {
        var baudotBytes = _encoder.EncodeText(text);
        // Cada carácter Baudot = 7.5 bits
        double totalBits = baudotBytes.Count * 7.5 + 6; // +6 para idle tones
        double durationSeconds = totalBits / BaudRate;
        return (int)(durationSeconds * 1000);
    }
}
