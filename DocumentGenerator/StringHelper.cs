using System;
using System.Drawing;

namespace DocumentGenerator
{
    public static class StringHelper
    {
        /// <summary>
        /// Разбивает текст на две части по словам. Строки на выходе получаются максимально близкими по длине, но предпочтение в длине отдаётся всё же первой строке.
        /// При неудавшейся попытке будет возврашена одна строка, содержащая исходный текст.
        /// </summary>
        /// <param name="text">Текст для разбиение на две части.</param>
        /// <returns></returns>
        public static string[] SplitIntoTwoParts(string text)
        {
            if (text == null) return null;

            string[] words = text.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 2)
                return new[] { text };

            if (words.Length == 2)
                return new[] { words[0], words[1] };

            string[] twoStrings = new string[2];

            twoStrings[0] = words[0];
            
            for (int i = 1; i < words.Length; i++)
            {
                if ((twoStrings[0] + " " + words[i]).Length < text.Length / 2)
                {
                    twoStrings[0] += " " + words[i];
                }
                else
                {
                    if (twoStrings[1] == null &&
                        (twoStrings[0] + " " + words[i]).Length <
                        text.Length - twoStrings[0].Length - 1)
                    {
                        twoStrings[0] += " " + words[i];
                    }
                    else
                    {
                        twoStrings[1] += words[i] + " ";
                    }
                }
            }

            twoStrings[1] = twoStrings[1].TrimEnd(' ');

            return twoStrings;
        }

        /// <summary>
        /// Разбивает текст на три части по словам. Строки на выходе получаются
        /// максимально близкими по длине, но предпочтение в длине отдаётся строкам
        /// в порядке их очерёдности. При неудавшейся попытке будет возврашена
        /// одна строка, содержащая исходный текст.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string[] SplitIntoThreeParts(string text)
        {
            if (text == null) return null;

            string[] words = text.Split(new[] {' '},
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 3)
                return new[] {text};

            if (words.Length == 3)
                return new[] {words[0], words[1], words[2]};

            string[] threeStrings = new string[3];

            threeStrings[0] = words[0];

            for (int i = 1; i < words.Length; i++)
            {
                if ((threeStrings[0] + " " + words[i]).Length <
                    text.Length / 3 && i < words.Length - 2)
                {
                    threeStrings[0] += " " + words[i];
                }
                else
                {
                    if (threeStrings[1] == null &&
                        (threeStrings[0] + " " + words[i]).Length <
                        (text.Length - threeStrings[0].Length) / 2 - 1 &&
                        i < words.Length - 2)
                    {
                        threeStrings[0] += " " + words[i];
                    }
                    else
                    {
                        if (threeStrings[2] == null &&
                            (threeStrings[1] + words[i]).Length <
                            text.Length - (threeStrings[0] + threeStrings[1])
                            .Length - 1 && i < words.Length - 1)
                        {
                            threeStrings[1] += words[i] + " ";
                        }
                        else
                        {
                            threeStrings[2] += words[i] + " ";
                        }
                    }
                }
            }

            string[] firstStringWords = threeStrings[0].Split(new[] {' '},
                StringSplitOptions.RemoveEmptyEntries);
            if (firstStringWords.Length > 1)
            {
                string controversialString =
                    firstStringWords[firstStringWords.Length - 1];
                if (threeStrings[0].Length >=
                    (threeStrings[1] + controversialString).Length)
                {
                    threeStrings[0] = threeStrings[0]
                        .Substring(0, threeStrings[0].Length -
                                      controversialString.Length - 1);
                    threeStrings[1] = threeStrings[1]
                        .Insert(0, controversialString + " ");
                }
            }

            threeStrings[1] = threeStrings[1].TrimEnd(' ');
            threeStrings[2] = threeStrings[2].TrimEnd(' ');

            return threeStrings;
        }

        public static Font FitSizeF(string text, Graphics g, Font font, RectangleF drawRect)
        {
            float fontSize = font.Size;
            while (fontSize > 0 &&
                   g.MeasureString(text,
                       new Font(font.FontFamily, fontSize)).Width >
                   drawRect.Width)
            {
                fontSize -= 0.5f;
            }

            return new Font(font.FontFamily, Math.Max(fontSize, 0.5f));
        }

        private static Font FitSizeFInCircle(string text, Graphics g, Font font,
            RectangleF drawRect)
        {
            // Рассчитываем длину хорды, которая будет являться верхней частью
            // прямоугольника, в который будет вписываться текст.
            // l = 2 * sqrt(2 * radius * height - height ^ 2)
            // (height - высота от точки, лежащей на окружности, а не от центра).
            float radius = drawRect.Width / 2.0f;
            float height = radius - font.GetHeight(g);
            double textChordLength =
                2 * Math.Sqrt(2 * radius * height - Math.Pow(height, 2));

            float fontSize = font.Size;
            while (fontSize > 0 &&
                   g.MeasureString(
                       text, new Font(font.FontFamily, fontSize)).Width >
                   textChordLength)
            {
                fontSize -= 0.5f;
                height = radius - font.GetHeight(g);
                textChordLength =
                    2 * Math.Sqrt(2 * radius * height - Math.Pow(height, 2));
            }

            return new Font(font.FontFamily, Math.Max(fontSize, 0.5f));
        }

        public static void DrawOneString(string text, Graphics g, Font font, Brush brush, RectangleF drawRect, StringFormat format)
        {
            font = FitSizeF(text, g, font, drawRect);
            g.DrawString(text, font, brush, drawRect, format);
        }

        public static void DrawStringsInCircle(string[] strings, Graphics g,
            Font font, Brush brush, RectangleF drawRect, StringFormat format)
        {
            // Расчитываем размер шрифта, чтобы текст попадал в центральный квадрат.
            int maxWidthIndex = GetIndexOfMaxWidth(strings, g, font);
            font = FitSizeF(strings[maxWidthIndex], g, font, drawRect);

            // Корректируем размер шрифта, чтобы текст попадал в круг.
            maxWidthIndex =
                GetIndexOfMaxWidth(
                    new[] {strings[0], strings[strings.Length - 1]}, g, font);
            font = FitSizeFInCircle(strings[maxWidthIndex], g, font, drawRect);
            g.DrawString(string.Join(" ", strings), font, brush, drawRect,
                format);
        }

        public static void DrawStrings(string[] strings, Graphics g, Font font, Brush brush, RectangleF drawRect, StringFormat format)
        {
            int maxWidthIndex = GetIndexOfMaxWidth(strings, g, font);
            font = FitSizeF(strings[maxWidthIndex], g, font, drawRect);
            g.DrawString(string.Join(" ", strings), font, brush, drawRect, format);
        }

        public static SizeF[] GetSizes(string[] strings, Graphics g, Font font)
        {
            if (strings == null) return new[] {SizeF.Empty};

            if (strings.Length == 1)
                return new[] {g.MeasureString(strings[0], font)};

            SizeF[] sizes = new SizeF[strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                sizes[i] = g.MeasureString(strings[i], font);
            }

            return sizes;
        }

        public static SizeF GetMaxSize(string[] strings, Graphics g, Font font)
        {
            if (strings == null || strings.Length == 0) return SizeF.Empty;

            if(strings.Length == 1) return g.MeasureString(strings[0], font);

            SizeF[] sizes = GetSizes(strings, g, font);

            SizeF maxSizeF = sizes[0];
            for (int i = 1; i < sizes.Length; i++)
            {
                if (maxSizeF.Width < sizes[i].Width)
                {
                    maxSizeF = sizes[i];
                }
            }

            return maxSizeF;
        }

        public static int GetIndexOfMaxWidth(string[] strings, Graphics g, Font font)
        {
            if (strings == null || strings.Length == 0) return 0;

            if (strings.Length == 1) return 1;

            SizeF[] sizes = GetSizes(strings, g, font);

            int maxWidthIndex = 0;
            for (int i = 1; i < sizes.Length; i++)
            {
                if (sizes[maxWidthIndex].Width < sizes[i].Width)
                {
                    maxWidthIndex = i;
                }
            }

            return maxWidthIndex;
        }

        public static string RemoveInvalidCharsFromFileName(this string name)
        {
            string[] invalidChars =
                {"\\", "/", ":", "*", "?", "\"", "<", ">", "|"};
            foreach (var invalidChar in invalidChars)
            {
                name = name.Replace(invalidChar, string.Empty);
            }
            return name;
        }
    }
}
