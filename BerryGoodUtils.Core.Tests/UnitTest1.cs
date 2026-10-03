using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using BerryGoodUtils.Core.Documents;
using BerryGoodUtils.Core.Email;
using BerryGoodUtils.Core.Ocr;
using BerryGoodUtils.Core.Scheduling;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Core.Tests;

public class CoreTests
{
    [Fact]
    public void PartRequestHtmlEscapesUserContent()
    {
        var supplier = new Supplier { Name = "Parts <Co>", Email = "parts@example.com" };
        var items = new[] { new PartRequestItem { PartOrService = "Bolt & Nut", Quantity = 2 } };

        var html = PartRequestDocumentComposer.GenerateHtml(supplier, items, new CompanyInfo { CompanyName = "Berry & Co" });

        Assert.Contains("Parts &lt;Co&gt;", html);
        Assert.Contains("Bolt &amp; Nut", html);
        Assert.Contains("Berry &amp; Co", html);
    }

    [Fact]
    public void QuoteEmailUsesCustomerAndQuoteDetails()
    {
        var quote = new Quote
        {
            QuoteNumber = "Q-01001",
            CustomerName = "Josh",
            CustomerEmail = "josh@example.com",
            Items = new ObservableCollection<QuoteItem> { new() { PartOrService = "Work", Quantity = 2, UnitPrice = 10 } }
        };

        var message = EmailMessageFactory.ForQuote(quote, new CompanyInfo { CompanyName = "Berry Good" });

        Assert.Equal("josh@example.com", message.To);
        Assert.Contains("Q-01001", message.Subject);
        Assert.Contains("$20.00", message.HtmlContent);
        EmailMessageFactory.Validate(message);
    }

    [Fact]
    public void ValidationRejectsMissingRecipient()
    {
        var message = new EmailMessage { Subject = "Test" };

        Assert.Throws<FormatException>(() => EmailMessageFactory.Validate(message));
    }

    [Fact]
    public void PartRequestHtmlIncludesAttachmentSection()
    {
        var supplier = new Supplier { Name = "Supplier", Email = "supplier@example.com" };
        var items = new[] { new PartRequestItem { PartOrService = "Pump", Quantity = 1 } };
        var attachments = new[]
        {
            new EmailAttachment { ContentId = "abc123", FileName = "pump.jpg" },
            new EmailAttachment { ContentId = "def456", FileName = "filter.png" }
        };

        var html = PartRequestDocumentComposer.GenerateHtml(supplier, items, new CompanyInfo { CompanyName = "Berry" }, attachments);

        Assert.Contains("Attachments", html);
        Assert.Contains("cid:abc123", html);
        Assert.Contains("cid:def456", html);
        Assert.Contains("pump.jpg", html);
        Assert.Contains("filter.png", html);
    }

    [Fact]
    public void BuildHtmlBodyConvertsCidToDataUri()
    {
        var tempFile = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(tempFile, [137, 80, 78, 71, 13, 10, 26, 10]);
        try
        {
            var attachment = new EmailAttachment
            {
                ContentId = "img1",
                FileName = "test.png",
                FilePath = tempFile,
                ContentType = "image/png"
            };
            var message = new EmailMessage
            {
                To = "a@example.com",
                Subject = "Test",
                Introduction = "Hello",
                HtmlContent = $"<html><body><img src=\"cid:img1\"/></body></html>",
                Attachments = [attachment]
            };

            var html = EmailMessageFactory.BuildHtmlBody(message);

            Assert.Contains("data:image/png;base64,iVBORw0KGgo=", html);
            Assert.DoesNotContain("cid:img1", html);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExistingAppDataJsonShapeDeserializes()
    {
        const string json = """
            {"Company":{"CompanyName":"Example"},"SavedParts":[],"Customers":[],"Suppliers":[],"NextQuoteNumber":1234}
            """;

        var data = JsonSerializer.Deserialize<AppData>(json);

        Assert.NotNull(data);
        Assert.Equal("Example", data.Company.CompanyName);
        Assert.Equal(1234, data.NextQuoteNumber);
        Assert.Empty(data.Schedules);
    }

    [Fact]
    public void ExistingScheduleJsonDeserializesWithEmptyVisitComments()
    {
        const string json = """
            {"CustomerId":"customer-1","Title":"Service","StartDateTime":"2026-01-05T08:00:00"}
            """;

        var schedule = JsonSerializer.Deserialize<CustomerSchedule>(json);

        Assert.NotNull(schedule);
        Assert.Empty(schedule.VisitComments);
    }

    [Fact]
    public void VisitPhotosRoundTripWithScheduleData()
    {
        var schedule = new CustomerSchedule
        {
            VisitComments =
            [
                new VisitComment
                {
                    OccurrenceDateTime = new DateTime(2026, 10, 3, 9, 0, 0),
                    Comments = "Completed",
                    Photos = [new VisitPhoto { FileName = "pool.jpg", DriveFileId = "drive-1", LocalPath = "photos/pool.jpg" }]
                }
            ]
        };

        var roundTrip = JsonSerializer.Deserialize<CustomerSchedule>(JsonSerializer.Serialize(schedule));

        Assert.NotNull(roundTrip);
        var photo = Assert.Single(Assert.Single(roundTrip.VisitComments).Photos);
        Assert.Equal("pool.jpg", photo.FileName);
        Assert.Equal("drive-1", photo.DriveFileId);
    }

    [Fact]
    public void CalendarDescriptionRoundTripsMultilineCommentsAndPreservesText()
    {
        const string description = "Service details\r\nPhone: 123";
        var result = CalendarDescriptionFormatter.Build(description, "Cleaned filter\nNeeds a new seal");

        Assert.Contains("Service details\nPhone: 123", result);
        Assert.Equal("Cleaned filter\nNeeds a new seal", CalendarDescriptionFormatter.ParseComments(result));
        Assert.Equal("Service details\nPhone: 123", CalendarDescriptionFormatter.RemoveCommentsSection(result));
    }

    [Fact]
    public void CalendarDescriptionBuildReplacesExistingCommentsBlock()
    {
        var first = CalendarDescriptionFormatter.Build("Appointment", "Old comment");
        var updated = CalendarDescriptionFormatter.Build(first, "New comment");

        Assert.Equal("New comment", CalendarDescriptionFormatter.ParseComments(updated));
        Assert.DoesNotContain("Old comment", updated);
        Assert.Equal(1, updated.Split(CalendarDescriptionFormatter.StartMarker).Length - 1);
    }

    [Fact]
    public void CalendarDescriptionHandlesEmptyAndUnmarkedDescriptions()
    {
        var result = CalendarDescriptionFormatter.Build(null, null);

        Assert.Equal(string.Empty, CalendarDescriptionFormatter.ParseComments(result));
        Assert.Equal(string.Empty, CalendarDescriptionFormatter.ParseComments("Ordinary calendar text"));
        Assert.Equal("Ordinary calendar text", CalendarDescriptionFormatter.RemoveCommentsSection("Ordinary calendar text"));
    }

    [Fact]
    public void CalendarConflictResolutionUsesLatestModification()
    {
        var local = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

        Assert.True(CalendarDescriptionFormatter.ShouldImport(local, local.AddMinutes(1)));
        Assert.False(CalendarDescriptionFormatter.ShouldImport(local, local.AddMinutes(-1)));
        Assert.False(CalendarDescriptionFormatter.ShouldImport(local, null));
    }

    [Fact]
    public void SavedPartSerializesExtraFields()
    {
        var part = new SavedPart
        {
            Name = "Pump",
            PartNumber = "P-100",
            ModelNumber = "MX-2024",
            SerialNumber = "SN12345",
            ReferenceNumber = "REF-ABC",
            AdditionalIdentifier = "Aisle 4",
            DefaultPrice = 199.99m,
            Notes = "Note text"
        };

        var json = JsonSerializer.Serialize(part);
        var roundTrip = JsonSerializer.Deserialize<SavedPart>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal(part.PartNumber, roundTrip.PartNumber);
        Assert.Equal(part.SerialNumber, roundTrip.SerialNumber);
        Assert.Equal(part.DefaultPrice, roundTrip.DefaultPrice);
    }

    [Fact]
    public void QuoteEmailIncludesReferenceImageAttachments()
    {
        var tempFile = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(tempFile, [137, 80, 78, 71, 13, 10, 26, 10]);
        try
        {
            var attachment = new EmailAttachment
            {
                ContentId = "img1",
                FileName = "test.png",
                FilePath = tempFile,
                ContentType = "image/png"
            };
            var quote = new Quote
            {
                QuoteNumber = "Q-01001",
                CustomerName = "Josh",
                CustomerEmail = "josh@example.com",
                Items =
                [
                    new QuoteItem { PartOrService = "Work", Quantity = 2, UnitPrice = 10, IncludeReferenceImage = true }
                ]
            };

            var message = EmailMessageFactory.ForQuote(quote, new CompanyInfo { CompanyName = "Berry Good" }, [attachment]);

            Assert.Single(message.Attachments);
            Assert.Contains("Attachments", message.HtmlContent);
            Assert.Contains("cid:img1", message.HtmlContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void PartRequestEmailRendersIncludedPartDetails()
    {
        var parts = new[]
        {
            new SavedPart
            {
                Name = "Pump",
                Manufacturer = "Rheem",
                ModelNumber = "RTHP026-1P",
                Voltage = "240",
                IncludedEmailFields = [nameof(SavedPart.Manufacturer), nameof(SavedPart.ModelNumber), nameof(SavedPart.Voltage)]
            }
        };
        var items = new[] { new PartRequestItem { PartOrService = "Pump", Quantity = 1 } };

        var html = PartRequestDocumentComposer.GenerateHtml(
            new Supplier { Name = "Supplier", Email = "supplier@example.com" },
            items,
            new CompanyInfo { CompanyName = "Berry" },
            savedParts: parts);

        Assert.Contains("Manufacturer", html);
        Assert.Contains("Rheem", html);
        Assert.Contains("Model Number", html);
        Assert.Contains("RTHP026-1P", html);
        Assert.Contains("Voltage", html);
        Assert.Contains("240", html);
    }

    [Fact]
    public void PartDetailFormatterAutoIncludesPopulatedFields()
    {
        var part = new SavedPart { Name = "Test", SerialNumber = "SN123", Amps = "10" };
        PartDetailFormatter.AutoIncludePopulatedFields(part);

        Assert.Contains(nameof(SavedPart.SerialNumber), part.IncludedEmailFields);
        Assert.Contains(nameof(SavedPart.Amps), part.IncludedEmailFields);
        Assert.DoesNotContain(nameof(SavedPart.Manufacturer), part.IncludedEmailFields);
    }

    [Fact]
    public void NameplateParserExtractsCommonFields()
    {
        var ocrText = """
            Rheem
            THERMAL
            RHEEM THERMAL POOL HTR 26kW 1PH R407C
            Model: RTHP026-1P
            Serial No: 4100085800
            Voltage (V): 240
            Phase: 1
            RLA (A): 30.3
            Refrigerant: R407C
            Refrigerant Charge: 1.8kg
            Date of Manufacture: 12 Sep 2017
            Gross Weight: 140.000 KG
            Manufactured in Revesby NSW Australia
            """;

        var part = PartNameplateParser.Parse(ocrText);

        Assert.Contains("Rheem", part.Manufacturer);
        Assert.Equal("RTHP026-1P", part.ModelNumber);
        Assert.Equal("4100085800", part.SerialNumber);
        Assert.Equal("240V", part.Voltage);
        Assert.Equal("1", part.Phase);
        Assert.Equal("30.3A", part.Amps);
        Assert.Equal("R407C", part.Refrigerant);
        Assert.Equal("1.8kg", part.RefrigerantCharge);
        Assert.Equal("12 Sep 2017", part.BuildDate);
        Assert.Equal("140.000 KG", part.GrossWeight);
        Assert.Contains("Australia", part.CountryOfManufacture);
    }

    [Fact]
    public void NameplateParserExtractsPhaseAndFrequencyFromStandaloneValues()
    {
        var ocrText = """
            VIRON EQ25 CHLORINATOR
            Volts: 230-240V
            Freq: 50 Hz
            Amps: 10A
            Phase: 1
            Target Output: 25 g/h
            Made in Australia by FLUIDRA
            """;

        var part = PartNameplateParser.Parse(ocrText);

        Assert.Equal("230-240V", part.Voltage);
        Assert.Equal("50Hz", part.Frequency);
        Assert.Equal("10A", part.Amps);
        Assert.Equal("1", part.Phase);
        Assert.Equal("25 g/h", part.TargetOutput);
    }

    [Fact]
    public void RRuleBuilderReturnsNullForNonRecurringSchedules()
    {
        var schedule = new CustomerSchedule { RecurrenceType = ScheduleRecurrenceType.None };
        Assert.Null(RRuleBuilder.Build(schedule));
    }

    [Fact]
    public void RRuleBuilderReturnsNullForSpecificDates()
    {
        var schedule = new CustomerSchedule { RecurrenceType = ScheduleRecurrenceType.SpecificDates };
        Assert.Null(RRuleBuilder.Build(schedule));
    }

    [Theory]
    [InlineData(ScheduleRecurrenceType.Weekly, "RRULE:FREQ=WEEKLY")]
    [InlineData(ScheduleRecurrenceType.BiWeekly, "RRULE:FREQ=WEEKLY;INTERVAL=2")]
    [InlineData(ScheduleRecurrenceType.Monthly, "RRULE:FREQ=MONTHLY")]
    [InlineData(ScheduleRecurrenceType.Yearly, "RRULE:FREQ=YEARLY")]
    public void RRuleBuilderGeneratesPresetRules(ScheduleRecurrenceType type, string expected)
    {
        var schedule = new CustomerSchedule { RecurrenceType = type };
        Assert.Equal(expected, RRuleBuilder.Build(schedule));
    }

    [Theory]
    [InlineData(CustomIntervalUnit.Days, 3, "RRULE:FREQ=DAILY;INTERVAL=3")]
    [InlineData(CustomIntervalUnit.Weeks, 2, "RRULE:FREQ=WEEKLY;INTERVAL=2")]
    [InlineData(CustomIntervalUnit.Months, 6, "RRULE:FREQ=MONTHLY;INTERVAL=6")]
    [InlineData(CustomIntervalUnit.Years, 5, "RRULE:FREQ=YEARLY;INTERVAL=5")]
    public void RRuleBuilderGeneratesCustomIntervalRules(CustomIntervalUnit unit, int value, string expected)
    {
        var schedule = new CustomerSchedule
        {
            RecurrenceType = ScheduleRecurrenceType.CustomInterval,
            CustomIntervalUnit = unit,
            CustomIntervalValue = value
        };
        Assert.Equal(expected, RRuleBuilder.Build(schedule));
    }

    [Fact]
    public void RRuleBuilderIgnoresIntervalOfOne()
    {
        var schedule = new CustomerSchedule
        {
            RecurrenceType = ScheduleRecurrenceType.CustomInterval,
            CustomIntervalUnit = CustomIntervalUnit.Days,
            CustomIntervalValue = 1
        };
        Assert.Equal("RRULE:FREQ=DAILY", RRuleBuilder.Build(schedule));
    }

    [Fact]
    public void OccurrenceCalculatorExpandsWeeklySchedule()
    {
        var schedule = new CustomerSchedule
        {
            StartDateTime = new DateTime(2026, 1, 5),
            RecurrenceType = ScheduleRecurrenceType.Weekly
        };
        var occurrences = ScheduleOccurrenceCalculator.GetOccurrences(schedule, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        Assert.Equal(4, occurrences.Count);
        Assert.Contains(new DateTime(2026, 1, 5), occurrences);
        Assert.Contains(new DateTime(2026, 1, 12), occurrences);
        Assert.Contains(new DateTime(2026, 1, 19), occurrences);
        Assert.Contains(new DateTime(2026, 1, 26), occurrences);
    }

    [Fact]
    public void OccurrenceCalculatorExpandsBiWeeklySchedule()
    {
        var schedule = new CustomerSchedule
        {
            StartDateTime = new DateTime(2026, 1, 5),
            RecurrenceType = ScheduleRecurrenceType.BiWeekly
        };
        var occurrences = ScheduleOccurrenceCalculator.GetOccurrences(schedule, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28));
        Assert.Equal(4, occurrences.Count);
        Assert.Contains(new DateTime(2026, 1, 5), occurrences);
        Assert.Contains(new DateTime(2026, 1, 19), occurrences);
        Assert.Contains(new DateTime(2026, 2, 2), occurrences);
        Assert.Contains(new DateTime(2026, 2, 16), occurrences);
    }

    [Fact]
    public void OccurrenceCalculatorReturnsSpecificDatesOnlyInRange()
    {
        var schedule = new CustomerSchedule
        {
            RecurrenceType = ScheduleRecurrenceType.SpecificDates,
            SpecificDates =
            [
                new DateTime(2026, 1, 5),
                new DateTime(2026, 2, 15),
                new DateTime(2026, 6, 1)
            ]
        };
        var occurrences = ScheduleOccurrenceCalculator.GetOccurrences(schedule, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
        Assert.Equal(2, occurrences.Count);
        Assert.Contains(new DateTime(2026, 1, 5), occurrences);
        Assert.Contains(new DateTime(2026, 2, 15), occurrences);
    }

    [Fact]
    public void OccurrenceCalculatorReturnsSingleOccurrenceForNone()
    {
        var schedule = new CustomerSchedule
        {
            StartDateTime = new DateTime(2026, 1, 10),
            RecurrenceType = ScheduleRecurrenceType.None
        };
        var occurrences = ScheduleOccurrenceCalculator.GetOccurrences(schedule, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        Assert.Single(occurrences);
        Assert.Equal(new DateTime(2026, 1, 10), occurrences[0]);
    }

    [Fact]
    public void NextOccurrenceReturnsEarliestFutureSpecificDate()
    {
        var schedule = new CustomerSchedule
        {
            RecurrenceType = ScheduleRecurrenceType.SpecificDates,
            SpecificDates =
            [
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 15),
                new DateTime(2026, 1, 10)
            ]
        };
        var next = ScheduleOccurrenceCalculator.GetNextOccurrence(schedule, new DateTime(2026, 1, 9));
        Assert.Equal(new DateTime(2026, 1, 10), next);
    }
}
