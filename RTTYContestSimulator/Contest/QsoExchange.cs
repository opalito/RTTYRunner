namespace RTTYContestSimulator.Contest;

/// <summary>
/// Modelo de intercambio de QSO para el concurso WPX RTTY.
/// </summary>
public class QsoExchange
{
    public string Callsign { get; set; } = string.Empty;
    public string Rst { get; set; } = "599";
    public int SerialNumber { get; set; }

    public string FormattedSerial => SerialNumber.ToString("D3");

    public override string ToString()
    {
        return $"{Callsign} {Rst} {FormattedSerial}";
    }
}

/// <summary>
/// Estados posibles de un QSO en progreso.
/// </summary>
public enum QsoState
{
    Idle,           // Esperando para enviar CQ
    SendingCq,      // Enviando CQ
    WaitingForCall, // Esperando respuesta de DX
    SendingReport,  // Enviando reporte RST + serial
    WaitingForQsl,  // Esperando confirmación del DX
    SendingTu,      // Enviando TU y siguiente CQ
    Completed       // QSO completado
}

/// <summary>
/// Lado del QSO que se transmite por audio.
/// </summary>
public enum TransmitSide
{
    Both,           // Ambos lados (comportamiento por defecto)
    CallerOnly,     // Solo la estación que llama CQ (MyCallsign)
    RespondersOnly  // Solo las estaciones que contestan (DX)
}

/// <summary>
/// Representa un QSO completo con tiempos y mensajes.
/// </summary>
public class QsoRecord
{
    public DateTime Time { get; set; }
    public string MyCallsign { get; set; } = string.Empty;
    public string DxCallsign { get; set; } = string.Empty;
    public int MySerialSent { get; set; }
    public int DxSerialReceived { get; set; }
    public string? DxState { get; set; } // Para CQ WW RTTY (estado o zona)
    public ContestType ContestType { get; set; }
}
