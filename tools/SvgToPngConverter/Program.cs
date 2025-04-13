using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace SvgToPngConverter
{
    class Program
    {
        static int Main(string[] args)
        {
            string? input = null;
            string? output = null;
            int width = 24;
            int height = 24;

            // Parse args
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input":
                        input = args[++i];
                        break;
                    case "--output":
                        output = args[++i];
                        break;
                    case "--width":
                        width = int.Parse(args[++i]);
                        break;
                    case "--height":
                        height = int.Parse(args[++i]);
                        break;
                }
            }

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                Console.Error.WriteLine("Usage: SvgToPngConverter --input input.svg --output output.png [--width 64] [--height 64]");
                return 1;
            }

            if (!File.Exists(input))
            {
                Console.Error.WriteLine($"Input file not found: {input}");
                return 2;
            }

            try
            {
                // Load SVG directly
                using var stream = File.OpenRead(input);
                var svg = new SKSvg();
                svg.Load(stream);

                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.Transparent);

                var scaleX = width / svg.Picture.CullRect.Width;
                var scaleY = height / svg.Picture.CullRect.Height;
                var scale = Math.Min(scaleX, scaleY);

                canvas.Scale((float)scale);

                // Tint all non-transparent parts to #b3b3b3 using SrcIn blend mode
                using var paint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateBlendMode(SKColor.Parse("#b3b3b3"), SKBlendMode.SrcIn)
                };
                canvas.DrawPicture(svg.Picture, paint);
                canvas.Flush();

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var outStream = File.OpenWrite(output);
                data.SaveTo(outStream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error converting SVG: {ex.Message}");
                return 3;
            }

            return 0;
        }
    }
}