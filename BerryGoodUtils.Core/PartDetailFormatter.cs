using System.Text;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Documents;

public static class PartDetailFormatter
{
    private static readonly (string Name, string Label)[] Fields =
    [
        (nameof(SavedPart.Manufacturer), "Manufacturer"),
        (nameof(SavedPart.EquipmentType), "Equipment Type"),
        (nameof(SavedPart.PartNumber), "Part Number"),
        (nameof(SavedPart.ModelNumber), "Model Number"),
        (nameof(SavedPart.SerialNumber), "Serial Number"),
        (nameof(SavedPart.ReferenceNumber), "Reference Number"),
        (nameof(SavedPart.AdditionalIdentifier), "Additional Identifier"),
        (nameof(SavedPart.Voltage), "Voltage"),
        (nameof(SavedPart.Amps), "Amps"),
        (nameof(SavedPart.Frequency), "Frequency"),
        (nameof(SavedPart.Phase), "Phase"),
        (nameof(SavedPart.Horsepower), "Horsepower"),
        (nameof(SavedPart.Kilowatts), "kW"),
        (nameof(SavedPart.IPRating), "IP Rating"),
        (nameof(SavedPart.Refrigerant), "Refrigerant"),
        (nameof(SavedPart.RefrigerantCharge), "Refrigerant Charge"),
        (nameof(SavedPart.MaxCellPressure), "Max Cell Pressure"),
        (nameof(SavedPart.MaxPressure), "Max Pressure"),
        (nameof(SavedPart.MaxHead), "Max Head"),
        (nameof(SavedPart.TargetOutput), "Target Output"),
        (nameof(SavedPart.GrossWeight), "Gross Weight"),
        (nameof(SavedPart.ApprovalNumber), "Approval Number"),
        (nameof(SavedPart.BuildDate), "Build Date"),
        (nameof(SavedPart.Barcode), "Barcode"),
        (nameof(SavedPart.CountryOfManufacture), "Country of Manufacture"),
        (nameof(SavedPart.Notes), "Notes")
    ];

    public static string GetFieldLabel(string fieldName)
    {
        return Fields.FirstOrDefault(f => f.Name == fieldName).Label ?? fieldName;
    }

    public static IEnumerable<(string Name, string Label)> GetFields() => Fields;

    public static string FormatHtml(SavedPart? part)
    {
        if (part == null)
            return string.Empty;

        var included = GetIncludedFields(part);
        if (!included.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<div style='font-size:12px;color:#555;line-height:1.4;'>");
        foreach (var (name, label) in included)
        {
            var value = GetValue(part, name);
            if (!string.IsNullOrWhiteSpace(value))
                sb.Append($"<div><strong>{Escape(label)}:</strong> {Escape(value)}</div>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    public static string FormatText(SavedPart? part)
    {
        if (part == null)
            return string.Empty;

        var included = GetIncludedFields(part);
        if (!included.Any())
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var (name, label) in included)
        {
            var value = GetValue(part, name);
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"  {label}: {value}");
        }
        return sb.ToString().TrimEnd();
    }

    public static void AutoIncludePopulatedFields(SavedPart part)
    {
        part.IncludedEmailFields ??= [];
        foreach (var (name, _) in Fields)
        {
            var value = GetValue(part, name);
            if (!string.IsNullOrWhiteSpace(value) && !part.IncludedEmailFields.Contains(name))
                part.IncludedEmailFields.Add(name);
        }
    }

    private static IEnumerable<(string Name, string Label)> GetIncludedFields(SavedPart part)
    {
        var included = part.IncludedEmailFields ?? [];
        return Fields.Where(f => included.Contains(f.Name));
    }

    private static string GetValue(SavedPart part, string propertyName)
    {
        return propertyName switch
        {
            nameof(SavedPart.Manufacturer) => part.Manufacturer,
            nameof(SavedPart.EquipmentType) => part.EquipmentType,
            nameof(SavedPart.PartNumber) => part.PartNumber,
            nameof(SavedPart.ModelNumber) => part.ModelNumber,
            nameof(SavedPart.SerialNumber) => part.SerialNumber,
            nameof(SavedPart.ReferenceNumber) => part.ReferenceNumber,
            nameof(SavedPart.AdditionalIdentifier) => part.AdditionalIdentifier,
            nameof(SavedPart.Voltage) => part.Voltage,
            nameof(SavedPart.Amps) => part.Amps,
            nameof(SavedPart.Frequency) => part.Frequency,
            nameof(SavedPart.Phase) => part.Phase,
            nameof(SavedPart.Horsepower) => part.Horsepower,
            nameof(SavedPart.Kilowatts) => part.Kilowatts,
            nameof(SavedPart.IPRating) => part.IPRating,
            nameof(SavedPart.Refrigerant) => part.Refrigerant,
            nameof(SavedPart.RefrigerantCharge) => part.RefrigerantCharge,
            nameof(SavedPart.MaxCellPressure) => part.MaxCellPressure,
            nameof(SavedPart.MaxPressure) => part.MaxPressure,
            nameof(SavedPart.MaxHead) => part.MaxHead,
            nameof(SavedPart.TargetOutput) => part.TargetOutput,
            nameof(SavedPart.GrossWeight) => part.GrossWeight,
            nameof(SavedPart.ApprovalNumber) => part.ApprovalNumber,
            nameof(SavedPart.BuildDate) => part.BuildDate,
            nameof(SavedPart.Barcode) => part.Barcode,
            nameof(SavedPart.CountryOfManufacture) => part.CountryOfManufacture,
            nameof(SavedPart.Notes) => part.Notes,
            _ => string.Empty
        };
    }

    private static string Escape(string text) => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
}
