using SpareParts.Domain.VehicleProfile;

namespace SpareParts.Infrastructure.Services;

public sealed class VinDecodeService
{
    private readonly string? _apiKey;

    public VinDecodeService(string? apiKey = null)
    {
        _apiKey = apiKey;
    }

    public VinDecodeResult Decode(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return new VinDecodeResult { Success = false, ErrorMessage = "VIN is required." };

        vin = vin.Trim().ToUpperInvariant();

        if (vin.Length != 17)
            return new VinDecodeResult { Success = false, ErrorMessage = "VIN must be exactly 17 characters." };

        if (vin.Any(c => c == 'I' || c == 'O' || c == 'Q'))
            return new VinDecodeResult { Success = false, ErrorMessage = "VIN cannot contain letters I, O, or Q." };

        if (!vin.All(c => char.IsLetterOrDigit(c)))
            return new VinDecodeResult { Success = false, ErrorMessage = "VIN must contain only letters and digits." };

        var year = DecodeYear(vin[9]);
        var make = DecodeMakeFromWmi(vin[..3]);

        return new VinDecodeResult
        {
            Success = true,
            Make = make,
            Model = null,
            Year = year,
            Trim = null,
            EngineCode = null,
            FuelType = null
        };
    }

    private static int? DecodeYear(char yearChar)
    {
        return yearChar switch
        {
            'A' => 1980, 'B' => 1981, 'C' => 1982, 'D' => 1983, 'E' => 1984,
            'F' => 1985, 'G' => 1986, 'H' => 1987, 'J' => 1988, 'K' => 1989,
            'L' => 1990, 'M' => 1991, 'N' => 1992, 'P' => 1993, 'R' => 1994,
            'S' => 1995, 'T' => 1996, 'V' => 1997, 'W' => 1998, 'X' => 1999,
            'Y' => 2000,
            '1' => 2001, '2' => 2002, '3' => 2003, '4' => 2004, '5' => 2005,
            '6' => 2006, '7' => 2007, '8' => 2008, '9' => 2009,
            _ => null
        };
    }

    private static string? DecodeMakeFromWmi(string wmi)
    {
        return wmi.ToUpperInvariant() switch
        {
            // Toyota
            "JT1" or "JT2" or "JT3" or "JT4" or "JT6" or "JT7" or "JT8" => "Toyota",
            "4T1" or "4T3" or "4T4" or "5TD" or "5TE" or "5TF" or "5TJ" or "5TK" => "Toyota",
            // Honda
            "JHM" or "1HG" or "1HH" or "2HG" or "2HH" or "3HG" or "5FN" or "5J6" or "5J8" => "Honda",
            "19X" or "19U" or "19V" => "Honda",
            // Nissan
            "JN1" or "JN6" or "1N4" or "1N6" or "3N1" or "3N6" or "5N1" => "Nissan",
            // BMW
            "WBA" or "WBS" or "WBX" or "4US" or "5UX" or "5YM" => "BMW",
            // Mercedes
            "WDB" or "WDD" or "WDC" or "55S" or "4JG" => "Mercedes-Benz",
            // Hyundai
            "KMH" or "KM8" => "Hyundai",
            // Kia
            "KNA" or "KND" or "KNE" => "Kia",
            // Volkswagen
            "WVW" or "WV1" or "WV2" or "1VW" or "2V4" or "3VW" or "9BW" => "Volkswagen",
            // Ford
            "1FA" or "1FB" or "1FC" or "1FD" or "2FA" or "2FB" or "2FC" or "2FD" => "Ford",
            "3FA" or "3FB" or "4F4" => "Ford",
            // Chevrolet/GM
            "1G1" or "1G2" or "1G3" or "1G4" or "1G6" or "1GC" or "1GM" => "Chevrolet",
            "2G1" or "2G2" or "2G4" => "Chevrolet",
            // Mitsubishi
            "JA3" or "JA4" or "JA5" or "4A3" or "4A4" => "Mitsubishi",
            // Mazda
            "JM1" or "JM3" or "4F2" or "1YV" => "Mazda",
            // Subaru
            "JF1" or "JF2" or "4S3" or "4S4" => "Subaru",
            _ => null
        };
    }
}
