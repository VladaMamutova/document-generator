using System;
using System.Drawing;

namespace DocumentGenerator
{
    public static class ImageExtensions
    {
        public static Bitmap CloneBlackAndWhite(this Image image)
        {
            if (image == null) return null;

            Bitmap input = new Bitmap(image);
            Bitmap output = new Bitmap(input.Width, input.Height);

            for (int j = 0; j < input.Height; j++)
            {
                for (int i = 0; i < input.Width; i++)
                {
                    int pixel = input.GetPixel(i, j).ToArgb();

                    float a = (pixel & 0xFF000000) >> 24;
                    float r = (pixel & 0x00FF0000) >> 16;
                    float g = (pixel & 0x0000FF00) >> 8;
                    float b = pixel & 0x000000FF;

                    r = g = b = (r + g + b) / 3.0f;

                    uint newPixel = ((uint)a << 24) | ((uint)r << 16) |
                                    ((uint)g << 8) | (uint)b;
                    output.SetPixel(i, j, Color.FromArgb((int)newPixel));
                }
            }

            return output;
        }

        public static void ScaleToFit(this Image image, ref double width, ref double height)
        {
            if(width <= 0)
                throw new ArgumentException(nameof(width));
            if (height <= 0)
                throw new ArgumentException(nameof(height));

            double scaleX = width / image.Width;
           
            var newWidth = image.Width * scaleX;
            var newHeight = image.Height * scaleX;

            double scaleY = height / newHeight;

            if (scaleY < 1)
            {
                newWidth = newWidth * scaleY;
                newHeight = newHeight * scaleY;
            }

            width = newWidth;
            height = newHeight;
        }
    }
}
