using System.Text.RegularExpressions;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Ocr;

public static class PartNameplateParser
{
    public static SavedPart Parse(string ocrText)
    {
        var text = ocrText;
        var part = new SavedPart();

        part.EquipmentType = ExtractEquipmentType(text);
        part.Manufacturer = ExtractManufacturer(text);
        part.ModelNumber = ExtractModel(text);
        part.SerialNumber = ExtractSerial(text);
        part.PartNumber = ExtractPartNumber(text);
        part.ReferenceNumber = ExtractReferenceNumber(text);
        part.AdditionalIdentifier = ExtractAdditionalIdentifier(text);
        part.Voltage = ExtractVoltage(text);
        part.Amps = ExtractAmps(text);
        part.Frequency = ExtractFrequency(text);
        part.Phase = ExtractPhase(text);
        part.Horsepower = ExtractHorsepower(text);
        part.Kilowatts = ExtractKilowatts(text);
        part.IPRating = ExtractIPRating(text);
        part.Refrigerant = ExtractRefrigerant(text);
        part.RefrigerantCharge = ExtractRefrigerantCharge(text);
        part.MaxCellPressure = ExtractAfterLabel(text, ["Max Cell Pressure:", "Cell Pressure:"]);
        part.MaxPressure = ExtractAfterLabel(text, ["Max Pressure:", "Max Press:"]);
        part.MaxHead = ExtractAfterLabel(text, ["Max Head:", "Head:"]);
        part.TargetOutput = ExtractAfterLabel(text, ["Target Output:", "Output:", "Target:"]);
        part.GrossWeight = ExtractGrossWeight(text);
        part.ApprovalNumber = ExtractAfterLabel(text, ["Approval No:", "Approval Number:", "Approval:", "A/C No:"]);
        part.BuildDate = ExtractBuildDate(text);
        part.Barcode = ExtractBarcode(text);
        part.CountryOfManufacture = ExtractCountryOfManufacture(text);

        if (string.IsNullOrWhiteSpace(part.Description))
            part.Description = BuildDescription(text, part);

        return part;
    }

    private static string BuildDescription(string text, SavedPart part)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var equipmentLine = lines.FirstOrDefault(l =>
            Regex.IsMatch(l, @"\b(Pool\s*(?:Heat\s*)?Pump|Heat\s*Pump|Chlorinator|Pool\s*Heater|HTR|Heater|Filter|Pump)\b", RegexOptions.IgnoreCase));

        if (!string.IsNullOrWhiteSpace(equipmentLine))
        {
            var cleaned = Regex.Replace(equipmentLine, @"\b(Model|Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
            if (cleaned.Length > 3)
                return cleaned;
        }

        // Fall back to the equipment type (e.g. "Pool Heat Pump") so the description isn't just raw OCR noise.
        if (!string.IsNullOrWhiteSpace(part.EquipmentType))
            return part.EquipmentType;

        return string.Empty;
    }

    private static string ExtractEquipmentType(string text)
    {
        var match = Regex.Match(text, @"\b(Pool\s*(?:Heat\s*)?Pump|Heat\s*Pump|Chlorinator|Pool\s*Heater|HTR|Heater|Filter|Pump)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        return string.Empty;
    }

    private static string ExtractManufacturer(string text)
    {
        var madeBy = Regex.Match(text, @"\b(?:Manufactured|Made)\b.*\bby\b\s+([A-Za-z][A-Za-z0-9\s&]+?)(?:\s+in|\s+NSW|\s+VIC|\s+QLD|\s+WA|\s+SA|\s+TAS|\s+ACT|\s+NT|\s+\d|\s+by|$)", RegexOptions.IgnoreCase);
        if (madeBy.Success)
            return CleanValue(madeBy.Groups[1].Value);

        var candidates = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 1 && l.Length < 30 && l.Split(' ').Length <= 4)
            .Where(l => l.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '&' || c == '.' || c == '-'))
            .ToList();

        if (candidates.Count > 0)
            return candidates.First();

        return string.Empty;
    }

    private static string ExtractModel(string text)
    {
        // Prefer a model-like token near the word "Model".
        var nearModel = Regex.Match(text, @"(?:Model|MOD\.?|MOD(?:el)?)[:\s]+([A-Za-z0-9][A-Za-z0-9\s\-/]*?)(?=\s+(?:Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)|\r?\n|$)", RegexOptions.IgnoreCase);
        if (nearModel.Success)
        {
            var value = nearModel.Groups[1].Value.Trim();
            if (Regex.IsMatch(value, @"\d") && !Regex.IsMatch(value, @"\b(Pool|Pump|Heat|Thermal|Chlorinator|Heater|Filter|Equipment)\b", RegexOptions.IgnoreCase))
                return CleanValue(value);
        }

        // Fallback: find any token that looks like a model number, preferring one near "Model".
        var modelPattern = @"\b[A-Za-z]{1,6}\d{2,}[A-Za-z0-9\-/]*\b";
        var matches = Regex.Matches(text, modelPattern);
        var modelMatches = matches.Cast<Match>().Where(m => m.Value.Length >= 5 && m.Value.Any(char.IsDigit)).ToList();
        if (modelMatches.Count == 0)
            return string.Empty;

        var modelIndex = text.IndexOf("Model", StringComparison.OrdinalIgnoreCase);
        var nearest = modelIndex >= 0
            ? modelMatches.OrderBy(m => Math.Abs(m.Index - modelIndex)).First()
            : modelMatches.First();

        return nearest.Value;
    }

    private static string ExtractSerial(string text)
    {
        // Look for a number after a Serial No label.
        var match = Regex.Match(text, @"(?:Serial\s*(?:No\.?|Number)?|S/N)[:\s]*(\d[\d -]{5,15})\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return CleanValue(match.Groups[1].Value).Replace(" ", string.Empty);

        // Fallback: any 6-12 digit number that is not part of a date or kW/weight.
        var numbers = Regex.Matches(text, @"\b\d{6,12}\b").Cast<Match>().ToList();
        foreach (var number in numbers)
        {
            var surrounding = text.Substring(Math.Max(0, number.Index - 10), Math.Min(20, text.Length - Math.Max(0, number.Index - 10)));
            if (!surrounding.Contains("kW", StringComparison.OrdinalIgnoreCase) &&
                !surrounding.Contains("kg", StringComparison.OrdinalIgnoreCase) &&
                !surrounding.Contains("KG", StringComparison.OrdinalIgnoreCase) &&
                !IsDateLike(number.Value))
                return number.Value;
        }

        return string.Empty;
    }

    private static string ExtractPartNumber(string text)
    {
        var match = Regex.Match(text, @"(?:Part\s*(?:No\.?|Number)?|PNo\.?|Part\s*#)[:\s]+([A-Za-z0-9][A-Za-z0-9\s\-/]*?)(?=\s+(?:Model|Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)|\r?\n|$)", RegexOptions.IgnoreCase);
        if (match.Success)
            return CleanValue(match.Groups[1].Value);

        return string.Empty;
    }

    private static string ExtractReferenceNumber(string text)
    {
        var match = Regex.Match(text, @"(?:Reference\s*(?:No\.?|Number)?|Ref\.?)[:\s]+([A-Za-z0-9][A-Za-z0-9\s\-/]*?)(?=\s+(?:Model|Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)|\r?\n|$)", RegexOptions.IgnoreCase);
        if (match.Success)
            return CleanValue(match.Groups[1].Value);

        return string.Empty;
    }

    private static string ExtractAdditionalIdentifier(string text)
    {
        var match = Regex.Match(text, @"(?:Additional\s*Identifier|ID|Code)[:\s]+([A-Za-z0-9][A-Za-z0-9\s\-/]*?)(?=\s+(?:Model|Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)|\r?\n|$)", RegexOptions.IgnoreCase);
        if (match.Success)
            return CleanValue(match.Groups[1].Value);

        return string.Empty;
    }

    private static string ExtractAfterLabel(string text, string[] labels)
    {
        foreach (var label in labels)
        {
            var pattern = Regex.Escape(label.TrimEnd(':', ' ')) + @"[:\s]*(.+?)(?=\s+(?:Model|Serial|Voltage|Phase|RLA|FLA|Amps?|Refrigerant|Charge|Date|Gross|Weight|Max|Target|Barcode|Approval|Build|Country|Manufacture|Manufactured)|\r?\n|$)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return CleanValue(match.Groups[1].Value);
        }

        return string.Empty;
    }

    private static string ExtractVoltage(string text)
    {
        var match = Regex.Match(text, @"(?:Voltage(?:\s*\([^)]*\))?|Volts?)[:\s]*(\d{2,3}(?:\s*[~-]\s*\d{2,3})?)", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.Replace(" ", string.Empty) + "V";

        match = Regex.Match(text, @"Voltage(?:\s*\([^)]*\))?[:\s]*(?:\D*\d{5,})?\D*(\d{2,3})\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value + "V";

        match = Regex.Match(text, @"(\d{2,3}(?:\s*[~-]\s*\d{2,3})?)\s*[Vv]\b");
        if (match.Success)
            return match.Groups[1].Value.Replace(" ", string.Empty) + "V";

        return string.Empty;
    }

    private static string ExtractAmps(string text)
    {
        var match = Regex.Match(text, @"(?:RLA|FLA|Amps?)(?:\s*\([^)]*\))?[:\s]*(\d+(?:\.\d+)?)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value + "A";

        match = Regex.Match(text, @"\b(?:RLA|FLA|Amps?)[:\s]*(?:\D*\d{5,})?\D*(\d+(?:\.\d+)?)\s*[Aa]?\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value + "A";

        match = Regex.Match(text, @"\b(\d+(?:\.\d+)?)\s*[Aa]\b");
        if (match.Success && !match.Value.Contains("kW", StringComparison.OrdinalIgnoreCase))
            return match.Groups[1].Value + "A";

        // Fallback: nameplates often have exactly one decimal current value (e.g. 30.3).
        var decimalMatch = Regex.Match(text, @"\b(\d{1,2}\.\d{1,2})\b");
        if (decimalMatch.Success)
            return decimalMatch.Groups[1].Value + "A";

        return string.Empty;
    }

    private static string ExtractFrequency(string text)
    {
        var match = Regex.Match(text, @"(?:Freq(?:uency)?|Hz)[:\s]*(\d{2,3})\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value + "Hz";

        match = Regex.Match(text, @"\b(\d{2,3})\s*[Hh][Zz]\b");
        if (match.Success)
            return match.Groups[1].Value + "Hz";

        return string.Empty;
    }

    private static string ExtractPhase(string text)
    {
        var match = Regex.Match(text, @"Phase[:\s]*(\d|Single|Three)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(text, @"\b([13])\s*[Pp][Hh]\b");
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(text, @"\b(\d)\s*Phase\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        // OCR commonly misreads "1PH" as "IPH".
        if (text.Contains("IPH", StringComparison.OrdinalIgnoreCase))
            return "1";

        return string.Empty;
    }

    private static string ExtractHorsepower(string text)
    {
        var match = Regex.Match(text, @"\b(\d+(?:\.\d+)?)\s*[Hh][Pp]\b");
        if (match.Success)
            return match.Groups[1].Value + "HP";

        return string.Empty;
    }

    private static string ExtractKilowatts(string text)
    {
        var match = Regex.Match(text, @"\b(\d+(?:\.\d+)?)\s*[Kk][Ww]\b");
        if (match.Success)
            return match.Groups[1].Value + "kW";

        return string.Empty;
    }

    private static string ExtractIPRating(string text)
    {
        var match = Regex.Match(text, @"\bIP\s*(\d{1,2}[A-Za-z]?)\b");
        if (match.Success)
            return "IP" + match.Groups[1].Value;

        return string.Empty;
    }

    private static string ExtractRefrigerant(string text)
    {
        var match = Regex.Match(text, @"Refrigerant[:\s]*(R\d{2,4}[A-Za-z]?)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(text, @"\b(R\d{2,4}[A-Za-z]?)\b");
        if (match.Success)
            return match.Groups[1].Value;

        return string.Empty;
    }

    private static string ExtractRefrigerantCharge(string text)
    {
        var match = Regex.Match(text, @"(?:Refrigerant\s*)?Charge[:\s]*(\d+(?:\.\d+)?\s*(?:kg|g|oz|lb)?)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        return string.Empty;
    }

    private static string ExtractGrossWeight(string text)
    {
        var match = Regex.Match(text, @"(?:Gross\s*)?Weight[:\s]*(\d+(?:\.\d+)?\s*(?:KG|kg|g|lb|oz)?)\b", RegexOptions.IgnoreCase);
        if (match.Success && !match.Groups[1].Value.Contains("kg", StringComparison.OrdinalIgnoreCase))
            return match.Groups[1].Value;

        // Fallback: prefer the last number-with-unit that looks like a weight.
        var weightMatches = Regex.Matches(text, @"\b(\d+(?:\.\d+)?)\s*(?:KG|kg|g|lb|oz)\b", RegexOptions.IgnoreCase)
            .Cast<Match>().ToList();
        if (weightMatches.Count > 0)
        {
            var last = weightMatches.Last();
            return last.Groups[1].Value + " " + Regex.Match(last.Value, @"(KG|kg|g|lb|oz)", RegexOptions.IgnoreCase).Value.ToUpperInvariant();
        }

        return string.Empty;
    }

    private static string ExtractBuildDate(string text)
    {
        var match = Regex.Match(text, @"(?:Build\s*Date|Date\s*of\s*Manufacture|Manufacture\s*Date|Mfg\s*Date)[:\s]*(\d{1,2}\s+[A-Za-z]{3,}\s+\d{4})\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(text, @"\b(\d{1,2}\s+[A-Za-z]{3,}\s+\d{4})\b");
        if (match.Success)
            return match.Groups[1].Value;

        return string.Empty;
    }

    private static string ExtractBarcode(string text)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"^\s*(\d{8,20})\s*$");
            if (match.Success)
                return match.Groups[1].Value;
        }
        return string.Empty;
    }

    private static string ExtractCountryOfManufacture(string text)
    {
        var match = Regex.Match(text, @"(?:Made|Manufactured)\s+in\s+(.+?)(?:\s+by|\r?\n|$)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var raw = match.Groups[1].Value.Trim();
            var stateAbbreviations = new[] { "NSW", "VIC", "QLD", "WA", "SA", "TAS", "ACT", "NT" };
            foreach (var abbr in stateAbbreviations)
                raw = Regex.Replace(raw, $"\\b{abbr}\\b", string.Empty, RegexOptions.IgnoreCase);

            var parts = raw.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 1 && p.All(char.IsLetter))
                .ToList();

            if (parts.Count > 0)
                return parts.Last();
        }

        return string.Empty;
    }

    private static bool IsDateLike(string value)
    {
        return value.Length == 8 && Regex.IsMatch(value, @"^\d{8}$");
    }

    private static string CleanValue(string value)
    {
        return value.Trim([' ', ':', ';', '-', '=', '"', '\'', '(', ')', '[', ']', '{', '}', '\r', '\n', '\t']);
    }
}
