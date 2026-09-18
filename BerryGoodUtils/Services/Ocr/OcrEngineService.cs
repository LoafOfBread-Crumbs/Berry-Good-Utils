using System.IO;
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using Windows.Globalization;

namespace BerryGoodUtils.Services.Ocr;

public static class OcrEngineService
{
    public static async Task<string> ExtractTextAsync(string imagePath, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image not found.", imagePath);

        progress?.Report("Preparing image for OCR...");

        try
        {
            var engine = OcrEngine.TryCreateFromLanguage(new Language("en"))
                ?? OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine == null)
                throw new InvalidOperationException(
                    "Windows OCR is not available. Ensure an OCR language pack (English) is installed in Windows Settings > Time & language > Language & region.");

            progress?.Report("Reading text from image...");

            using var fileStream = File.OpenRead(imagePath);
            using var randomAccessStream = fileStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream).AsTask(cancellationToken);
            var bitmap = await decoder.GetSoftwareBitmapAsync().AsTask(cancellationToken);

            var result = await engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
            var text = result.Text;

            progress?.Report("Done.");
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Could not run Windows OCR on the selected image. Make sure the image is readable and that an English OCR language pack is installed.", ex);
        }
    }
}
