using SkiaSharp;

namespace cos.Services
{
    public class CaptchaService
    {
        private readonly ILogger<CaptchaService> _logger;
        public CaptchaService(ILogger<CaptchaService> logger)
        {
            _logger = logger;
        }

        public (byte[] ImageData, string CaptchaText) GenerateCaptchaImage()
        {
            /*WARNING*/
            // for deployment in linux an additional package is required -> libSkiaSharp
            // in linux cmd line enter this cmd -> ls | grep libSkiaSharp
            // you should see something like this -> libSkiaSharp.so
            // if you do not see it then you need to install it before compilation
            // run this command in package manager console ->dotnet add package SkiaSharp.NativeAssets.Linux

            // publish the app

            try
            {
                var randomText = GenerateRandomText(6);

                using var bitmap = new SKBitmap(135, 40);

                using var canvas = new SKCanvas(bitmap);

                canvas.Clear(SKColors.Navy);

                var typeface = SKTypeface.Default;

                using var font = new SKFont(typeface, 24);

                using var paint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true
                };

                using var textBlob = SKTextBlob.Create(randomText, font);

                canvas.DrawText(textBlob, 19, 27, paint);

                using var image = SKImage.FromBitmap(bitmap);

                using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                var imageData = data.ToArray();

                return (imageData, randomText);
            }
            catch (Exception err)
            {
                _logger.LogError(err, "Exception occurred while generating captcha");
                return (Array.Empty<byte>(), string.Empty);
            }
        }

        private string GenerateRandomText(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(x => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}

