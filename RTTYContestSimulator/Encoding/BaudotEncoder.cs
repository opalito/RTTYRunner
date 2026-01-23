namespace RTTYContestSimulator.Encoding;

/// <summary>
/// Codificador Baudot ITA2 para RTTY.
/// Convierte texto ASCII a secuencia de bits Baudot con start/stop bits.
/// </summary>
public class BaudotEncoder
{
    // Tablas ITA2 - Letters (LTRS) y Figures (FIGS)
    private static readonly Dictionary<char, byte> LettersTable = new()
    {
        { '\0', 0b00000 }, // NUL
        { 'E', 0b00001 },
        { '\n', 0b00010 }, // LF
        { 'A', 0b00011 },
        { ' ', 0b00100 }, // Space
        { 'S', 0b00101 },
        { 'I', 0b00110 },
        { 'U', 0b00111 },
        { '\r', 0b01000 }, // CR
        { 'D', 0b01001 },
        { 'R', 0b01010 },
        { 'J', 0b01011 },
        { 'N', 0b01100 },
        { 'F', 0b01101 },
        { 'C', 0b01110 },
        { 'K', 0b01111 },
        { 'T', 0b10000 },
        { 'Z', 0b10001 },
        { 'L', 0b10010 },
        { 'W', 0b10011 },
        { 'H', 0b10100 },
        { 'Y', 0b10101 },
        { 'P', 0b10110 },
        { 'Q', 0b10111 },
        { 'O', 0b11000 },
        { 'B', 0b11001 },
        { 'G', 0b11010 },
        { 'M', 0b11100 },
        { 'X', 0b11101 },
        { 'V', 0b11110 },
    };

    private static readonly Dictionary<char, byte> FiguresTable = new()
    {
        { '\0', 0b00000 }, // NUL
        { '3', 0b00001 },
        { '\n', 0b00010 }, // LF
        { '-', 0b00011 },
        { ' ', 0b00100 }, // Space
        { '\'', 0b00101 }, // Apostrophe
        { '8', 0b00110 },
        { '7', 0b00111 },
        { '\r', 0b01000 }, // CR
        { '$', 0b01001 }, // WRU in some variants
        { '4', 0b01010 },
        { ',', 0b01100 },
        { '!', 0b01101 },
        { ':', 0b01110 },
        { '(', 0b01111 },
        { '5', 0b10000 },
        { '+', 0b10001 },
        { ')', 0b10010 },
        { '2', 0b10011 },
        { '#', 0b10100 },
        { '6', 0b10101 },
        { '0', 0b10110 },
        { '1', 0b10111 },
        { '9', 0b11000 },
        { '?', 0b11001 },
        { '&', 0b11010 },
        { '.', 0b11100 },
        { '/', 0b11101 },
        { ';', 0b11110 },
    };

    // Códigos de shift
    private const byte LTRS_SHIFT = 0b11111; // 31 - Shift to Letters
    private const byte FIGS_SHIFT = 0b11011; // 27 - Shift to Figures

    private bool _inFiguresMode = false;

    /// <summary>
    /// Codifica una cadena de texto a una lista de bytes Baudot (5 bits cada uno).
    /// Incluye los shifts LTRS/FIGS necesarios.
    /// </summary>
    public List<byte> EncodeText(string text)
    {
        var result = new List<byte>();
        _inFiguresMode = false;

        // Empezar siempre en modo letras
        result.Add(LTRS_SHIFT);

        foreach (char c in text.ToUpperInvariant())
        {
            var encoded = EncodeChar(c);
            result.AddRange(encoded);
        }

        return result;
    }

    private List<byte> EncodeChar(char c)
    {
        var result = new List<byte>();

        // Verificar si el carácter está en la tabla de letras
        if (LettersTable.TryGetValue(c, out byte letterCode))
        {
            // Space, CR, LF son comunes en ambos modos
            if (c == ' ' || c == '\r' || c == '\n' || c == '\0')
            {
                result.Add(letterCode);
            }
            else
            {
                if (_inFiguresMode)
                {
                    result.Add(LTRS_SHIFT);
                    _inFiguresMode = false;
                }
                result.Add(letterCode);
            }
        }
        // Verificar si está en la tabla de figuras
        else if (FiguresTable.TryGetValue(c, out byte figureCode))
        {
            if (!_inFiguresMode)
            {
                result.Add(FIGS_SHIFT);
                _inFiguresMode = true;
            }
            result.Add(figureCode);
        }
        // Carácter desconocido - enviar espacio
        else
        {
            result.Add(LettersTable[' ']);
        }

        return result;
    }

    /// <summary>
    /// Convierte un byte Baudot (5 bits) a una secuencia de bits para transmisión RTTY.
    /// Formato: 1 start bit (Space) + 5 data bits (LSB first) + 1.5 stop bits (Mark)
    /// </summary>
    /// <returns>Array de booleanos donde true=Mark, false=Space</returns>
    public static bool[] ToBits(byte baudotChar)
    {
        // Start bit (Space/0) + 5 data bits + 1.5 stop bits (Mark/1)
        // Representamos 1.5 stop bits como 2 bits para simplificar
        var bits = new bool[8]; // 1 + 5 + 2 = 8

        // Start bit - siempre Space (false)
        bits[0] = false;

        // 5 data bits - LSB first
        for (int i = 0; i < 5; i++)
        {
            bits[i + 1] = ((baudotChar >> i) & 1) == 1;
        }

        // Stop bits - siempre Mark (true)
        bits[6] = true;
        bits[7] = true;

        return bits;
    }

    /// <summary>
    /// Obtiene la duración en samples para cada tipo de bit.
    /// El stop bit es 1.5 veces la duración de un bit normal.
    /// </summary>
    public static int[] GetBitDurations(int samplesPerBit)
    {
        // Duración de bits: start=1, data=5x1, stop=1.5
        // Total = 7.5 bits por carácter
        int stopBitSamples = (int)(samplesPerBit * 1.5);

        return new int[]
        {
            samplesPerBit,     // Start bit
            samplesPerBit,     // Data bit 0
            samplesPerBit,     // Data bit 1
            samplesPerBit,     // Data bit 2
            samplesPerBit,     // Data bit 3
            samplesPerBit,     // Data bit 4
            stopBitSamples     // Stop bit (1.5)
        };
    }
}
