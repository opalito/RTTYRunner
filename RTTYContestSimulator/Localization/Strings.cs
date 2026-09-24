namespace RTTYContestSimulator.Localization;

public enum Language
{
    Spanish,
    English
}

public static class Strings
{
    public static Language CurrentLanguage { get; set; } = Language.Spanish;

    // Window
    public static string WindowTitle => CurrentLanguage == Language.Spanish
        ? "RTTY Runner by EC5W"
        : "RTTY Runner by EC5W";

    // Groups
    public static string StationGroup => CurrentLanguage == Language.Spanish
        ? "Estacion"
        : "Station";

    public static string RttyParamsGroup => CurrentLanguage == Language.Spanish
        ? "Parametros RTTY"
        : "RTTY Parameters";

    public static string AudioGroup => CurrentLanguage == Language.Spanish
        ? "Audio"
        : "Audio";

    public static string ContestGroup => CurrentLanguage == Language.Spanish
        ? "Concurso"
        : "Contest";

    // Labels
    public static string CallsignLabel => CurrentLanguage == Language.Spanish
        ? "Indicativo:"
        : "Callsign:";

    public static string MarkLabel => "Mark (Hz):";
    public static string ShiftLabel => "Shift (Hz):";
    public static string BaudLabel => CurrentLanguage == Language.Spanish
        ? "Baudios:"
        : "Baud Rate:";

    public static string SoundCardLabel => CurrentLanguage == Language.Spanish
        ? "Tarjeta de sonido:"
        : "Sound Card:";

    public static string VolumeLabel => CurrentLanguage == Language.Spanish
        ? "Volumen:"
        : "Volume:";

    public static string HfNoiseLabel => CurrentLanguage == Language.Spanish
        ? "Ruido HF:"
        : "HF Noise:";

    public static string LanguageLabel => CurrentLanguage == Language.Spanish
        ? "Idioma:"
        : "Language:";

    public static string ContestTypeLabel => CurrentLanguage == Language.Spanish
        ? "Tipo:"
        : "Type:";

    public static string TransmitSideLabel => CurrentLanguage == Language.Spanish
        ? "Transmitir:"
        : "Transmit:";

    public static string TransmitBoth => CurrentLanguage == Language.Spanish
        ? "Ambos lados"
        : "Both sides";

    public static string TransmitCallerOnly => CurrentLanguage == Language.Spanish
        ? "Solo llamada (CQ)"
        : "Caller only (CQ)";

    public static string TransmitRespondersOnly => CurrentLanguage == Language.Spanish
        ? "Solo respuestas"
        : "Responders only";

    // Buttons
    public static string StartButton => CurrentLanguage == Language.Spanish
        ? "Iniciar Run"
        : "Start Run";

    public static string StopButton => CurrentLanguage == Language.Spanish
        ? "Detener"
        : "Stop";

    public static string TestButton => "Test CQ";

    // Status
    public static string StatusStopped => CurrentLanguage == Language.Spanish
        ? "Detenido"
        : "Stopped";

    public static string StatusRunning => CurrentLanguage == Language.Spanish
        ? "En ejecucion - Run activo"
        : "Running - Active Run";

    public static string StatusTxCq => "TX: CQ";

    public static string StatusRxWaitingCall => CurrentLanguage == Language.Spanish
        ? "RX: Esperando llamada"
        : "RX: Waiting for call";

    public static string StatusTxReport => CurrentLanguage == Language.Spanish
        ? "TX: Enviando reporte"
        : "TX: Sending report";

    public static string StatusRxWaitingQsl => CurrentLanguage == Language.Spanish
        ? "RX: Esperando QSL"
        : "RX: Waiting for QSL";

    public static string StatusTxTu => "TX: TU";

    public static string StatusQsoCompleted => CurrentLanguage == Language.Spanish
        ? "QSO completado"
        : "QSO completed";

    public static string StatusIdle => "Idle";

    public static string QsoCompletedSeparator => CurrentLanguage == Language.Spanish
        ? "--- QSO completado ---"
        : "--- QSO completed ---";

    // Labels format
    public static string StatusFormat => CurrentLanguage == Language.Spanish
        ? "Estado: {0}"
        : "Status: {0}";

    public static string QsosFormat => "QSOs: {0}";
    public static string SerialFormat => "Serial: {0:D3}";

    // Log title
    public static string LogTitle => "RTTY TX Log";
}
