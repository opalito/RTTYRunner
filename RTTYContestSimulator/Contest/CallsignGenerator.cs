namespace RTTYContestSimulator.Contest;

public enum ContestType
{
    CqWpxRtty,
    CqWwRtty
}

/// <summary>
/// Generador de indicativos de radioaficionado realistas.
/// Incluye prefijos de diferentes países y regiones.
/// </summary>
public class CallsignGenerator
{
    private readonly Random _random = new();

    // Estados de USA por distrito de llamada
    private static readonly Dictionary<int, string[]> UsaStates = new()
    {
        { 1, new[] { "CT", "MA", "ME", "NH", "RI", "VT" } },
        { 2, new[] { "NJ", "NY" } },
        { 3, new[] { "DE", "MD", "PA" } },
        { 4, new[] { "AL", "FL", "GA", "KY", "NC", "SC", "TN", "VA" } },
        { 5, new[] { "AR", "LA", "MS", "NM", "OK", "TX" } },
        { 6, new[] { "CA" } },
        { 7, new[] { "AZ", "ID", "MT", "NV", "OR", "UT", "WA", "WY" } },
        { 8, new[] { "MI", "OH", "WV" } },
        { 9, new[] { "IL", "IN", "WI" } },
        { 0, new[] { "CO", "IA", "KS", "MN", "MO", "NE", "ND", "SD" } }
    };

    // Provincias de Canadá
    private static readonly Dictionary<string, string[]> CanadaProvinces = new()
    {
        { "VE1", new[] { "NS", "NB", "PE" } },
        { "VE2", new[] { "QC" } },
        { "VE3", new[] { "ON" } },
        { "VE4", new[] { "MB" } },
        { "VE5", new[] { "SK" } },
        { "VE6", new[] { "AB" } },
        { "VE7", new[] { "BC" } },
        { "VE8", new[] { "NT" } },
        { "VE9", new[] { "NB" } },
        { "VY1", new[] { "YT" } },
        { "VY2", new[] { "PE" } },
        { "VO1", new[] { "NL" } },
        { "VO2", new[] { "NL" } }
    };

    // Prefijos USA
    private readonly List<(string prefix, int weight)> _usaPrefixes = new()
    {
        ("W", 25), ("K", 25), ("N", 20),
        ("WA", 8), ("WB", 8), ("KC", 8), ("KD", 8),
        ("WD", 5), ("WN", 3), ("KE", 5), ("KF", 5),
        ("KG", 5), ("KI", 3), ("KJ", 3), ("KK", 3),
        ("W1", 4), ("W2", 4), ("W3", 4), ("W4", 4), ("W5", 4),
        ("W6", 4), ("W7", 4), ("W8", 4), ("W9", 4), ("W0", 4),
        ("K1", 4), ("K2", 4), ("K3", 4), ("K4", 4), ("K5", 4),
        ("K6", 4), ("K7", 4), ("K8", 4), ("K9", 4), ("K0", 4),
        ("N1", 3), ("N2", 3), ("N3", 3), ("N4", 3), ("N5", 3),
        ("N6", 3), ("N7", 3), ("N8", 3), ("N9", 3), ("N0", 3),
        ("AA", 2), ("AB", 2), ("AC", 2), ("AD", 2), ("AE", 2),
        ("AF", 2), ("AG", 2), ("AI", 2), ("AJ", 2), ("AK", 2)
    };

    // Prefijos Canadá
    private readonly List<(string prefix, int weight)> _canadaPrefixes = new()
    {
        ("VE1", 3), ("VE2", 4), ("VE3", 8), ("VE4", 2),
        ("VE5", 2), ("VE6", 3), ("VE7", 4), ("VE9", 2),
        ("VA2", 2), ("VA3", 4), ("VA6", 2), ("VA7", 2)
    };

    // Prefijos DX (resto del mundo)
    private readonly List<(string prefix, int weight)> _dxPrefixes = new()
    {
        // Europa
        ("DL", 10), ("DF", 5), ("DK", 5), ("DJ", 3),
        ("G", 8), ("M", 5), ("2E", 2),
        ("F", 8), ("F5", 3), ("F6", 3),
        ("I", 6), ("IK", 4), ("IZ", 3),
        ("EA", 8), ("EB", 3), ("EC", 2),
        ("CT", 4),
        ("PA", 5), ("PD", 3),
        ("ON", 4),
        ("HB9", 3),
        ("OE", 3),
        ("OK", 4), ("OL", 2),
        ("SP", 5), ("SQ", 3),
        ("OM", 3),
        ("HA", 4),
        ("YO", 3),
        ("LZ", 3),
        ("9A", 3),
        ("S5", 3),
        ("OZ", 3),
        ("SM", 4),
        ("LA", 3),
        ("OH", 4),
        ("ES", 2),
        ("YL", 2),
        ("LY", 2),
        ("UA", 5), ("RV", 3), ("RW", 3),

        // América del Sur
        ("PY", 5), ("PP", 3),
        ("LU", 4),
        ("CE", 3),
        ("CX", 2),
        ("HK", 3),
        ("YV", 2),

        // Asia
        ("JA", 6), ("JH", 4), ("JR", 3),
        ("HL", 3),
        ("BV", 2),
        ("VU", 3),

        // Oceanía
        ("VK", 4),
        ("ZL", 3),

        // África
        ("ZS", 3),
        ("CN", 2),

        // Caribe
        ("PJ2", 2), ("PJ4", 1),
        ("VP5", 1), ("8P", 1), ("V4", 1)
    };

    private readonly string _letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int _usaWeight;
    private int _canadaWeight;
    private int _dxWeight;

    // Configuración: 70% USA/Canada, 30% DX
    public int UsaCanadaPercentage { get; set; } = 70;

    public CallsignGenerator()
    {
        _usaWeight = _usaPrefixes.Sum(p => p.weight);
        _canadaWeight = _canadaPrefixes.Sum(p => p.weight);
        _dxWeight = _dxPrefixes.Sum(p => p.weight);
    }

    /// <summary>
    /// Información del indicativo generado.
    /// </summary>
    public class CallsignInfo
    {
        public string Callsign { get; set; } = "";
        public string? State { get; set; } // Para USA/Canada en CQ WW
        public bool IsUsaOrCanada { get; set; }
        public int District { get; set; }
    }

    /// <summary>
    /// Genera un indicativo aleatorio realista con información adicional.
    /// </summary>
    public CallsignInfo GenerateWithInfo()
    {
        bool isUsaCanada = _random.Next(100) < UsaCanadaPercentage;

        if (isUsaCanada)
        {
            // 85% USA, 15% Canadá dentro de USA/Canada
            if (_random.Next(100) < 85)
            {
                return GenerateUsaCallsign();
            }
            else
            {
                return GenerateCanadaCallsign();
            }
        }
        else
        {
            return GenerateDxCallsign();
        }
    }

    /// <summary>
    /// Genera un indicativo simple (compatibilidad).
    /// </summary>
    public string Generate()
    {
        return GenerateWithInfo().Callsign;
    }

    private CallsignInfo GenerateUsaCallsign()
    {
        string prefix = SelectRandomPrefix(_usaPrefixes, _usaWeight);
        int district;

        // Determinar distrito
        if (prefix.Length >= 2 && char.IsDigit(prefix[^1]))
        {
            district = prefix[^1] - '0';
        }
        else
        {
            district = _random.Next(0, 10);
        }

        string suffix = GenerateSuffix(prefix);
        string callsign;

        if (char.IsDigit(prefix[^1]))
        {
            callsign = prefix + suffix;
        }
        else
        {
            callsign = prefix + district + suffix;
        }

        // Seleccionar estado según distrito
        var states = UsaStates[district];
        string state = states[_random.Next(states.Length)];

        return new CallsignInfo
        {
            Callsign = callsign,
            State = state,
            IsUsaOrCanada = true,
            District = district
        };
    }

    private CallsignInfo GenerateCanadaCallsign()
    {
        string prefix = SelectRandomPrefix(_canadaPrefixes, _canadaWeight);
        string suffix = GenerateSuffix(prefix);
        string callsign = prefix + suffix;

        // Buscar provincia
        string? province = null;
        string prefixKey = prefix.Length >= 2 ? prefix[..3] : prefix;
        if (prefix.Length >= 3 && CanadaProvinces.ContainsKey(prefix[..3]))
        {
            var provinces = CanadaProvinces[prefix[..3]];
            province = provinces[_random.Next(provinces.Length)];
        }
        else if (prefix.StartsWith("VA"))
        {
            // VA3 -> ON, VA6 -> AB, etc.
            var veEquiv = "VE" + prefix[2];
            if (CanadaProvinces.ContainsKey(veEquiv))
            {
                var provinces = CanadaProvinces[veEquiv];
                province = provinces[_random.Next(provinces.Length)];
            }
        }

        return new CallsignInfo
        {
            Callsign = callsign,
            State = province ?? "ON",
            IsUsaOrCanada = true,
            District = 0
        };
    }

    private CallsignInfo GenerateDxCallsign()
    {
        string prefix = SelectRandomPrefix(_dxPrefixes, _dxWeight);
        string suffix = GenerateSuffix(prefix);
        string callsign;

        if (char.IsDigit(prefix[^1]))
        {
            callsign = prefix + suffix;
        }
        else
        {
            int district = _random.Next(0, 10);
            callsign = prefix + district + suffix;
        }

        return new CallsignInfo
        {
            Callsign = callsign,
            State = null,
            IsUsaOrCanada = false,
            District = 0
        };
    }

    private string SelectRandomPrefix(List<(string prefix, int weight)> prefixes, int totalWeight)
    {
        int roll = _random.Next(totalWeight);
        int cumulative = 0;

        foreach (var (prefix, weight) in prefixes)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return prefix;
            }
        }

        return prefixes[0].prefix;
    }

    private string GenerateSuffix(string prefix)
    {
        int length = _random.Next(100) switch
        {
            < 5 => 1,
            < 45 => 2,
            _ => 3
        };

        var suffix = new char[length];
        for (int i = 0; i < length; i++)
        {
            suffix[i] = _letters[_random.Next(_letters.Length)];
        }

        return new string(suffix);
    }

    /// <summary>
    /// Genera un número de serie aleatorio para la estación DX.
    /// </summary>
    public int GenerateSerialNumber()
    {
        return _random.Next(100) switch
        {
            < 20 => _random.Next(1, 100),
            < 50 => _random.Next(100, 500),
            < 80 => _random.Next(500, 2000),
            _ => _random.Next(2000, 5000)
        };
    }
}
