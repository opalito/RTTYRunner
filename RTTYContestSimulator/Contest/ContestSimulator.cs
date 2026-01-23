using RTTYContestSimulator.Audio;

namespace RTTYContestSimulator.Contest;

/// <summary>
/// Simulador de run de concurso CQ WPX RTTY y CQ WW RTTY.
/// Genera una secuencia realista de QSOs con estaciones DX.
/// </summary>
public class ContestSimulator
{
    private readonly RttyGenerator _rttyGenerator;
    private readonly AudioPlayer _audioPlayer;
    private readonly CallsignGenerator _callsignGenerator;
    private readonly Random _random = new();

    private CancellationTokenSource? _cts;
    private Task? _simulationTask;

    public string MyCallsign { get; set; } = "EA5XXX";
    public string MyState { get; set; } = "DX"; // Para CQ WW si eres de USA/Canada
    public int CurrentSerial { get; private set; } = 1;
    public int QsoCount { get; private set; } = 0;
    public QsoState CurrentState { get; private set; } = QsoState.Idle;
    public bool IsRunning => _simulationTask != null && !_simulationTask.IsCompleted;
    public ContestType ContestType { get; set; } = ContestType.CqWpxRtty;

    // Configuración de tiempos (en ms)
    public int MinDelayBetweenQsos { get; set; } = 500;
    public int MaxDelayBetweenQsos { get; set; } = 2000;
    public int MinResponseDelay { get; set; } = 200;
    public int MaxResponseDelay { get; set; } = 800;

    // Eventos para la UI
    public event EventHandler<MessageProgressEventArgs>? MessageStarted;
    public event EventHandler<int>? CharacterSent;
    public event EventHandler? MessageCompleted;
    public event EventHandler<QsoRecord>? QsoCompleted;
    public event EventHandler<QsoState>? StateChanged;
    public event EventHandler? SimulationStopped;

    public ContestSimulator(RttyGenerator rttyGenerator, AudioPlayer audioPlayer)
    {
        _rttyGenerator = rttyGenerator;
        _audioPlayer = audioPlayer;
        _callsignGenerator = new CallsignGenerator();
    }

    /// <summary>
    /// Inicia la simulación del run de concurso.
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _simulationTask = Task.Run(() => RunSimulation(_cts.Token));
    }

    /// <summary>
    /// Detiene la simulación.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _audioPlayer.Stop();
        SetState(QsoState.Idle);
        SimulationStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reinicia los contadores.
    /// </summary>
    public void Reset()
    {
        Stop();
        CurrentSerial = 1;
        QsoCount = 0;
    }

    private async Task RunSimulation(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await SimulateQso(ct);

                // Pausa entre QSOs con ruido de fondo
                int delay = _random.Next(MinDelayBetweenQsos, MaxDelayBetweenQsos);
                await PlayNoiseDelay(delay, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación normal
        }
        finally
        {
            SetState(QsoState.Idle);
            SimulationStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task SimulateQso(CancellationToken ct)
    {
        // Generar estación DX con información completa
        var dxInfo = _callsignGenerator.GenerateWithInfo();
        string dxCall = dxInfo.Callsign;
        int dxSerial = _callsignGenerator.GenerateSerialNumber();

        // Determinar mensajes de CQ según concurso
        string contestName = ContestType == ContestType.CqWpxRtty ? "WPX" : "WW";

        // 1. Enviar CQ
        SetState(QsoState.SendingCq);
        string cqMessage = $" CQ CQ {contestName} DE {MyCallsign} {MyCallsign} K ";
        await SendMessageCharByChar(cqMessage, ct);

        // 2. Simular respuesta del DX
        int responseDelay = _random.Next(MinResponseDelay, MaxResponseDelay);
        await PlayNoiseDelay(responseDelay, ct);
        SetState(QsoState.WaitingForCall);

        string dxResponse = $" {MyCallsign} DE {dxCall} {dxCall} ";
        await SendMessageCharByChar(dxResponse, ct);

        // 3. Enviar reporte (formato depende del concurso)
        responseDelay = _random.Next(MinResponseDelay, MaxResponseDelay);
        await PlayNoiseDelay(responseDelay, ct);
        SetState(QsoState.SendingReport);

        string myReport;
        if (ContestType == ContestType.CqWpxRtty)
        {
            // CQ WPX: 599 + Serial
            myReport = $" {dxCall} 599 {CurrentSerial:D3} {CurrentSerial:D3} K ";
        }
        else
        {
            // CQ WW: 599 + CQ Zone (o State si USA/Canada)
            // Asumimos que el operador es DX (zona CQ)
            myReport = $" {dxCall} 599 599 14 14 K "; // Zona 14 = Europa occidental
        }
        await SendMessageCharByChar(myReport, ct);

        // 4. Recibir confirmación del DX
        responseDelay = _random.Next(MinResponseDelay, MaxResponseDelay);
        await PlayNoiseDelay(responseDelay, ct);
        SetState(QsoState.WaitingForQsl);

        string dxQsl;
        if (ContestType == ContestType.CqWpxRtty)
        {
            // CQ WPX: 599 + Serial
            dxQsl = $" 599 {dxSerial:D3} {dxSerial:D3} TU ";
        }
        else
        {
            // CQ WW: 599 + Serial + State/Zone
            if (dxInfo.IsUsaOrCanada && dxInfo.State != null)
            {
                // USA/Canada envían estado
                dxQsl = $" 599 {dxSerial:D3} {dxInfo.State} TU ";
            }
            else
            {
                // DX envía zona CQ
                int cqZone = GetCqZone(dxCall);
                dxQsl = $" 599 {dxSerial:D3} {cqZone:D2} TU ";
            }
        }
        await SendMessageCharByChar(dxQsl, ct);

        // 5. Enviar TU y siguiente CQ
        responseDelay = _random.Next(MinResponseDelay, MaxResponseDelay);
        await PlayNoiseDelay(responseDelay, ct);
        SetState(QsoState.SendingTu);

        string tuMessage = $" TU {MyCallsign} CQ ";
        await SendMessageCharByChar(tuMessage, ct);

        // Registrar QSO completado
        var qsoRecord = new QsoRecord
        {
            Time = DateTime.Now,
            MyCallsign = MyCallsign,
            DxCallsign = dxCall,
            MySerialSent = CurrentSerial,
            DxSerialReceived = dxSerial,
            DxState = dxInfo.State,
            ContestType = ContestType
        };

        CurrentSerial++;
        QsoCount++;
        SetState(QsoState.Completed);

        QsoCompleted?.Invoke(this, qsoRecord);
    }

    /// <summary>
    /// Obtiene la zona CQ aproximada basada en el prefijo.
    /// </summary>
    private int GetCqZone(string callsign)
    {
        if (callsign.StartsWith("W") || callsign.StartsWith("K") || callsign.StartsWith("N") || callsign.StartsWith("A"))
            return _random.Next(3, 6); // USA: zonas 3, 4, 5
        if (callsign.StartsWith("VE") || callsign.StartsWith("VA") || callsign.StartsWith("VY") || callsign.StartsWith("VO"))
            return _random.Next(1, 5); // Canadá: zonas 1-4
        if (callsign.StartsWith("JA") || callsign.StartsWith("JH") || callsign.StartsWith("JR"))
            return 25; // Japón
        if (callsign.StartsWith("VK"))
            return _random.Next(29, 31); // Australia
        if (callsign.StartsWith("PY") || callsign.StartsWith("PP"))
            return 11; // Brasil
        if (callsign.StartsWith("LU"))
            return 13; // Argentina
        if (callsign.StartsWith("UA") || callsign.StartsWith("R"))
            return _random.Next(16, 20); // Rusia
        if (callsign.StartsWith("DL") || callsign.StartsWith("D"))
            return 14; // Alemania

        // Europa occidental por defecto
        return 14;
    }

    /// <summary>
    /// Espera el tiempo indicado mientras reproduce ruido de fondo.
    /// </summary>
    private async Task PlayNoiseDelay(int delayMs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Solo generar ruido si está habilitado
        if (_rttyGenerator.NoiseLevel > 0)
        {
            // Generar ruido de fondo para el período de espera
            var noiseSamples = _rttyGenerator.GenerateSilence(delayMs);
            _audioPlayer.Play(noiseSamples);

            // Esperar a que termine
            await Task.Delay(delayMs + 50, ct);

            // Esperar a que el buffer se vacíe
            while (_audioPlayer.BufferedBytes > 0 && !ct.IsCancellationRequested)
            {
                await Task.Delay(30, ct);
            }
        }
        else
        {
            // Sin ruido, solo esperar
            await Task.Delay(delayMs, ct);
        }
    }

    private async Task SendMessageCharByChar(string message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Notificar UI que empieza el mensaje
        MessageStarted?.Invoke(this, new MessageProgressEventArgs(message));

        // Calcular duración por carácter (aproximada)
        int charDurationMs = (int)(7.5 * 1000 / _rttyGenerator.BaudRate); // 7.5 bits per char

        // Generar y reproducir audio completo
        var samples = _rttyGenerator.GenerateAudio(message);
        _audioPlayer.Play(samples);

        // Notificar progreso carácter por carácter
        for (int i = 0; i < message.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            CharacterSent?.Invoke(this, i);
            await Task.Delay(charDurationMs, ct);
        }

        // Esperar a que el buffer se vacíe
        while (_audioPlayer.BufferedBytes > 0 && !ct.IsCancellationRequested)
        {
            await Task.Delay(30, ct);
        }

        MessageCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(QsoState newState)
    {
        CurrentState = newState;
        StateChanged?.Invoke(this, newState);
    }

    /// <summary>
    /// Genera un solo mensaje de prueba (para testing).
    /// </summary>
    public async void SendTestMessage(string message)
    {
        string paddedMessage = $" {message} ";
        MessageStarted?.Invoke(this, new MessageProgressEventArgs(paddedMessage));

        int charDurationMs = (int)(7.5 * 1000 / _rttyGenerator.BaudRate);
        var samples = _rttyGenerator.GenerateAudio(paddedMessage);
        _audioPlayer.Play(samples);

        // Progreso carácter por carácter
        for (int i = 0; i < paddedMessage.Length; i++)
        {
            CharacterSent?.Invoke(this, i);
            await Task.Delay(charDurationMs);
        }

        MessageCompleted?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Argumentos para el evento de inicio de mensaje.
/// </summary>
public class MessageProgressEventArgs : EventArgs
{
    public string Message { get; }

    public MessageProgressEventArgs(string message)
    {
        Message = message;
    }
}
