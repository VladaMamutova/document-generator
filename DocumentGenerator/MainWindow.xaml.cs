using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using PdfiumViewer;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Drawing.Image;
using MessageBox = System.Windows.MessageBox;
using PdfDocument = PdfSharp.Pdf.PdfDocument;
using Word = Microsoft.Office.Interop.Word;

namespace DocumentGenerator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private Image _applicantStampPhoto;
        //private Image _manufacturerStampPhoto;
        private string _previousSelectedFolder;
        
        public MainWindow()
        {
            InitializeComponent();

            Settings settings = Settings.GetSettings();
            try
            {
                if (!settings.FileExists(Environment.CurrentDirectory))
                {
                    settings.SaveInitialSettings(Environment.CurrentDirectory);
                }
                else
                {
                    settings.Load(Environment.CurrentDirectory);
                }

                settings.UpdateLaunchCount();
                settings.Save(Environment.CurrentDirectory);
            }
            catch { }

            CreateApplicantStampRadioButton.IsChecked = true;
            CreateManufacturerStampRadioButton.IsChecked = true;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!Settings.GetSettings().CanRunProgram)
                Application.Current.Shutdown();

            PaintApplicantStamp();
            PaintManufacturerStamp();
        }

        private void PaintApplicantStamp()
        {
            int scale = 1;
            int width = 160 * scale;
            int height = 160 * scale;
            int x = 20 * scale;
            int y = 20 * scale;
            int borderThickness = 1 * scale;

            Bitmap bitmap = new Bitmap(width, height);

            Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            SolidBrush drawBrush = new SolidBrush(Color.Blue);
            Rectangle drawRect = new Rectangle(x + borderThickness,
                y, width - x * 2,
                height - y * 2);
            StringFormat drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawEllipse(new Pen(drawBrush, borderThickness),
                new Rectangle(borderThickness, borderThickness, width - borderThickness * 2,
                    height - borderThickness * 2));
            g.DrawEllipse(new Pen(drawBrush, borderThickness), drawRect);

            string text = ApplicantName.Text;

            int padding = 1 * scale;
            drawRect.X += padding;
            drawRect.Y += padding;
            drawRect.Width -= padding * 2;
            drawRect.Height -= padding * 2;

            if (!string.IsNullOrWhiteSpace(text))
            {
                float fontSize = 20 * scale;
                Font drawFont = new Font("Arial", fontSize);
                Font minRecFont = new Font("Arial", 10 * scale);

                string[] twoStrings = StringHelper.SplitIntoTwoParts(text);

                SizeF minRecommendedSize = g.MeasureString(text, minRecFont);
                if (twoStrings.Length == 1 ||
                    minRecommendedSize.Width < drawRect.Width)
                {
                    StringHelper.DrawOneString(text, g, drawFont, drawBrush,
                        drawRect, drawFormat);
                }
                else
                {
                    minRecommendedSize =
                        StringHelper.GetMaxSize(twoStrings, g, minRecFont);
                    string[] threeStrings =
                        StringHelper.SplitIntoThreeParts(text);
                    if (threeStrings.Length == 1 ||
                        minRecommendedSize.Width < drawRect.Width)
                    {
                        StringHelper.DrawStringsInCircle(twoStrings, g, drawFont, drawBrush,
                            drawRect, drawFormat);
                    }
                    else
                    {
                        StringHelper.DrawStringsInCircle(threeStrings, g, drawFont, drawBrush,
                            drawRect, drawFormat);
                    }
                }
            }

            float radiusShift = 0;
            var registration = RegistrationNumber.Text.ToUpper();
            if (registration.Length < 40)
            {
                registration =
                    registration.PadRight(80 - RegistrationNumber.Text.Length);
            }
            else
            {
                radiusShift += (RegistrationNumber.Text.Length - 36) * 0.2f;
                radiusShift = Math.Min(3.6f, radiusShift);
            }

            registration += " * ";

            if (!string.IsNullOrWhiteSpace(registration))
            {
                using (var path = new GraphicsPath())
                {
                    Font font = new Font("Arial", 48 * scale);
                    float fFontSize = Points2PageUnits(g, font);

                    path.AddString(registration, font.FontFamily, (int)font.Style,
                        fFontSize, new System.Drawing.Point(0, 0), new StringFormat());

                    RectangleF rectf = path.GetBounds();
                    path.Transform(new Matrix(1, 0, 0, -1, -rectf.Left,
                        GetAscent(g, font)));

                    float fScale = (2 - 0.02f) * (float)Math.PI / rectf.Width;
                    path.Transform(new Matrix(fScale, 0, 0, fScale, 0, 0));

                    PointF[] aptf = path.PathPoints;

                    for (int i = 0; i < aptf.Length; i++)
                    {
                        aptf[i] = new PointF(
                            (64 * scale + radiusShift) * (1 + aptf[i].Y) *
                            (float) Math.Cos(1 + aptf[i].X),
                            (64 * scale + radiusShift) * (1 + aptf[i].Y) *
                            (float) Math.Sin(1 + aptf[i].X));
                    }

                    GraphicsPath transformed = new GraphicsPath(aptf, path.PathTypes);

                    g.TranslateTransform((width + borderThickness * 2) / 2.0f,
                        (height + borderThickness * 2) / 2.0f);
                    g.FillPath(drawBrush, transformed);
                }
            }

            ApplicantStamp.Image = bitmap;
        }

        public float GetAscent(Graphics g, Font font)
        {
            return font.GetHeight(g) * font.FontFamily.GetCellAscent(font.Style) /
                   font.FontFamily.GetLineSpacing(font.Style);
        }

        public float Points2PageUnits(Graphics g, Font font)
        {
            float fsize;

            if (g.PageUnit == GraphicsUnit.Display)
                fsize = 100 * font.SizeInPoints / 72;
            else
                fsize = g.DpiX * font.SizeInPoints / 72;
            return fsize;

        }

        private void PaintManufacturerStamp()
        {
            int scale = 1;
            int width = 200 * scale;
            int height = 120 * scale;
            int x = 20 * scale;
            int y = 20 * scale;
            int borderThickness = 2 * scale;

            Bitmap bitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bitmap);
            SolidBrush drawBrush = new SolidBrush(Color.Blue);
            StringFormat drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            Rectangle drawRect = new Rectangle(x + borderThickness, y + borderThickness,
                width - (x + borderThickness) * 2,
                height - (y + borderThickness) * 2);

            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            g.DrawRectangle(new Pen(drawBrush, borderThickness),
                new Rectangle(borderThickness, borderThickness,
                    width - borderThickness * 2, height - borderThickness * 2));
            g.DrawRectangle(new Pen(drawBrush, borderThickness), drawRect);

            string text = ManufacturerName.Text;

            int padding = 1 * scale;
            drawRect.X += padding;
            drawRect.Y += padding;
            drawRect.Width -= padding * 2;
            drawRect.Height -= padding * 2;

            if (!string.IsNullOrWhiteSpace(text))
            {
                float fontSize = 19 * scale;
                Font drawFont = new Font("Arial", fontSize);
                Font minRecFont = new Font("Arial", 10 * scale);

                string[] twoStrings = StringHelper.SplitIntoTwoParts(text);

                SizeF minRecommendedSize = g.MeasureString(text, minRecFont);
                if (twoStrings.Length == 1 ||
                    minRecommendedSize.Width < drawRect.Width)
                {
                    StringHelper.DrawOneString(text, g, drawFont, drawBrush,
                        drawRect, drawFormat);
                }
                else
                {
                    minRecommendedSize =
                        StringHelper.GetMaxSize(twoStrings, g, minRecFont);
                    string[] threeStrings =
                        StringHelper.SplitIntoThreeParts(text);
                    if (threeStrings.Length == 1 ||
                        minRecommendedSize.Width < drawRect.Width)
                    {
                        StringHelper.DrawStrings(twoStrings, g, drawFont, drawBrush,
                            drawRect, drawFormat);
                    }
                    else
                    {
                        StringHelper.DrawStrings(threeStrings, g, drawFont, drawBrush,
                            drawRect, drawFormat);
                    }
                }
            }

            StringHelper.DrawOneString(ManufacturerCountry.Text, g, new Font("Arial", 9 * scale), drawBrush,
            new RectangleF(borderThickness, height - y - borderThickness,
                width - borderThickness * 2, y), drawFormat);

            ManufacturerStamp.Image = bitmap;
        }

        private void Applicant_OnTextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (CreateApplicantStampRadioButton.IsChecked == true)
                PaintApplicantStamp();
        }

        private void Manufacturer_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CreateManufacturerStampRadioButton.IsChecked == true)
                PaintManufacturerStamp();
        }

        #region Загрузка документов

        private string LoadDocument(string title, DocumentFormat[] formats)
        {
            string filter = "";
            foreach (var format in formats)
            {
                filter += $"*.{format.ToString().ToLower()}|*.{format.ToString().ToLower()}|";
            }
            filter = filter.Remove(filter.Length - 1);

            OpenFileDialog openFileDialog = new OpenFileDialog
                { Filter = filter, Title = title};

            while (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool formatError = true;

                for (int i = 0; i < formats.Length && formatError; i++)
                {
                    if (openFileDialog.FileName.ToLower().EndsWith("." + formats[i].ToString().ToLower()))
                        formatError = false;
                }

                if(formatError)
                {
                    MessageBox.Show(
                        "Выбран файл с неверным форматом.", title,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else return openFileDialog.FileName;
            }

            return "";
        }

        private void LoadTestReport(object sender, RoutedEventArgs e)
        {
            string fileName =
                LoadDocument("Загрузка ПИ",
                    new[] {DocumentFormat.PDF});

            if (!string.IsNullOrEmpty(fileName))
            {
                TestReport.Text = fileName;
                TestReport.Focus();
                TestReport.SelectionStart = fileName.Length;
            }
        }

        private void LoadRegistrationDocument(object sender, RoutedEventArgs e)
        {
            string fileName =
                LoadDocument("Загрузка ОГРНа",
                    new[] {DocumentFormat.PDF, DocumentFormat.JPG});

            if (!string.IsNullOrEmpty(fileName))
            {
                RegistrationDocument.Text = fileName;
                RegistrationDocument.Focus();
                RegistrationDocument.SelectionStart = fileName.Length;
            }
        }

        private void LoadInn(object sender, RoutedEventArgs e)
        {
            string fileName =
                LoadDocument("Загрузка ИННа",
                    new[] { DocumentFormat.PDF, DocumentFormat.JPG });

            if (!string.IsNullOrEmpty(fileName))
            {
                Inn.Text = fileName;
                Inn.Focus();
                Inn.SelectionStart = fileName.Length;
            }
        }

        private void LoadModelDocument(object sender, RoutedEventArgs e)
        {
            string fileName =
                LoadDocument("Загрузка МАКЕТА",
                    new[] { DocumentFormat.DOCX, DocumentFormat.DOC });

            if (!string.IsNullOrEmpty(fileName))
            {
                ModelDocument.Text = fileName;
                ModelDocument.Focus();
                ModelDocument.SelectionStart = fileName.Length;
            }
        }

        #endregion

        private bool IsApplicantSignatureStampLoaded()
        {
            if (ApplicantSignatureImage.Image == null || !File.Exists(ApplicantSignatureImage.ImageLocation))
            {
                MessageBox.Show(
                    "Загрузите файл подписи заявителя.",
                    "Ошибка файла подписи заявителя", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (CreateApplicantStampRadioButton.IsChecked == false && NoApplicantStamp.Visibility == Visibility.Visible)
            {
                MessageBox.Show(
                    "Загрузите файл печати заявителя.",
                    "Ошибка файла печати заявителя", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool IsManufacturerSignatureStampLoaded()
        {
            if (ManufacturerSignatureImage.Image == null ||
                string.IsNullOrWhiteSpace(ManufacturerSignatureImage
                    .ImageLocation))
            {
                MessageBox.Show(
                    "Загрузите файл подписи производителя.",
                    "Ошибка файла подписи производителя", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (CreateManufacturerStampRadioButton.IsChecked == false && NoManufacturerStamp.Visibility == Visibility.Visible)
            {
                MessageBox.Show(
                    "Загрузите файл печати производителя.",
                    "Ошибка файла печати производителя", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool IsDocumentValid(string name, string path, DocumentFormat[] formats)
        {
            if (formats == null || formats.Length < 1)
                throw new ArgumentException(nameof(formats));

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    $"Загрузите {name}.",
                    $"Ошибка документа {Document.GetNameInGenitive(name)}", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            bool formatError = true;

            for (int i = 0; i < formats.Length && formatError; i++)
            {
                if (path.ToLower().EndsWith("." + formats[i].ToString().ToLower()))
                {
                    formatError = false;
                }
            }

            if (formatError)
            {
                string formatString = formats.Length == 1
                    ? "формате " + formats[0].ToString().ToLower()
                    : "одном из следующих форматов: " +
                      string.Join(", ", formats.Select(format => format.ToString().ToLower()));

                MessageBox.Show(
                    $"Документ \"{path}\" " +
                    $"должен быть в {formatString}. Попробуйте указать другой файл.",
                    $"Ошибка документа {Document.GetNameInGenitive(name)}", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Файл \"{path}\" " +
                    "не найден. Проверьте правильность указанного пути.",
                    $"Ошибка документа {Document.GetNameInGenitive(name)}", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void CreateСertifiedDocument(string filename, Image[] pagesAsImages,
            Image applicantSignatureImage, Image applicantStampImage, bool needCopyIsRightStamp = true)
        {
            int pageWidth = 595;
            int pageHeight = 841;

            double pageWidthInCentimeter = 21.0;
            double pageHeightInCentimeter = 29.7;

            double heightCoef = pageHeight / pageHeightInCentimeter;
            double widthCoef = pageWidth / pageWidthInCentimeter;

            using (PdfDocument outputDocument = new PdfDocument())
            {
                foreach (var page in pagesAsImages)
                {
                    PdfPage editablePage = outputDocument.AddPage();

                    XGraphics gfx = XGraphics.FromPdfPage(editablePage);

                    gfx.DrawImage(page, 0, 0, pageWidth, pageHeight);

                    XImage img;

                    if (needCopyIsRightStamp)
                    {
                        img = XImage.FromFile("Копия верна.png");
                        gfx.DrawImage(img, 2 * widthCoef,
                            pageHeight - 8.5 * heightCoef,
                            4 * widthCoef, 2 * heightCoef);
                    }

                    // Обработка изображений JPEG работает лучше, если использовать в
                    // PDFsharp 1.50 или более поздней версии XImage.FromStream
                    // вместо Image.FromStream плюс XImage.FromGdiPlusImage!!!

                    double width, height;
                    MemoryStream imageStream;

                    if (applicantSignatureImage != null)
                    {
                        // Масшабируем и рисуем подпись заявителя.
                        width = 5 * widthCoef;
                        height = 3 * heightCoef;

                        applicantSignatureImage.ScaleToFit(ref width,
                            ref height);

                        imageStream = new MemoryStream();
                        applicantSignatureImage.Save(imageStream,
                            ImageFormat.Png);
                        img = XImage.FromStream(imageStream);

                        gfx.DrawImage(img, 2 * widthCoef,
                            pageHeight - 2 * heightCoef - height, width,
                            height);
                    }

                    if (applicantStampImage != null)
                    {
                        // Масшабируем и рисуем печать заявителя.
                        width = 5 * widthCoef;
                        height = 4 * heightCoef;
                        applicantStampImage.ScaleToFit(ref width, ref height);

                        imageStream = new MemoryStream();
                        applicantStampImage.Save(imageStream, ImageFormat.Png);
                        img = XImage.FromStream(imageStream);

                        gfx.DrawImage(img, 2 * widthCoef,
                            pageHeight - 6 * heightCoef,
                            width, height);
                    }
                }

                // Выставляем опции для сжатия документа.
                outputDocument.Options.FlateEncodeMode =
                    PdfFlateEncodeMode.BestCompression;

                outputDocument.Save(filename);
            }
        }

        private Image[] GetGrayscalePagesAsImagesFromPdf(string fileName)
        {
            Image[] pagesAsImages;
            using (var document = PdfiumViewer.PdfDocument.Load(fileName))
            {
                pagesAsImages = new Image[document.PageCount];
                for (int i = 0; i < document.PageCount; i++)
                {
                    // the highest quality
                    pagesAsImages[i] = document.Render(i, 3508,
                        2480, 300, 300, PdfRenderFlags.Grayscale);

                    // 2105 * 1488 для уменьшения размера документа (- 40% от размера)
                    //pagesAsImages[i] = document.Render(i, 2105,
                    //    1488, 300, 300, PdfRenderFlags.Grayscale);
                }
            }

            return pagesAsImages;
        }

        private Image[] CompressImages(Image[] images)
        {
            Image[] compressedImages = new Image[images.Length];
            
            for (int i = 0; i < images.Length; i++)
            {
                MemoryStream inputStream = new MemoryStream();

                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpgEncoder = null;

                foreach (ImageCodecInfo codec in codecs)
                {
                    if (codec.FormatID == ImageFormat.Jpeg.Guid)
                    {
                        jpgEncoder = codec;
                    }
                }

                EncoderParameters ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, 50L);
                images[i].Save(inputStream, jpgEncoder, ep);
                inputStream.Position = 0;

                MemoryStream outputStream = new MemoryStream();
                Image newImage = Image.FromStream(inputStream);
                newImage.Save(outputStream, ImageFormat.Png);

                compressedImages[i] = new Bitmap(Image.FromStream(outputStream),
                    (int) (595 * 1.5), (int) (841 * 1.5));
            }

            return compressedImages;
        }

        private Image[] GetGrayscalePagesAsImagesFromDocOrDocx(string inputFileName, string outputFileName)
        {
            if (!outputFileName.EndsWith(".pdf"))
                throw new ArgumentException(nameof(outputFileName));
            
            Word.Application app = new Word.Application();
            try
            {
                app.Documents.Open(inputFileName);
            }
            catch (Exception e)
            {
                throw new Exception("Документ \"" + outputFileName + "открыт. " +
                                    "Закройте его и повторите попытку позже.");
            }

            app.ActiveDocument.SaveAs(outputFileName,
                Word.WdSaveFormat.wdFormatPDF);
            app.ActiveDocument.Close(false);
            app.Quit();

            return GetGrayscalePagesAsImagesFromPdf(outputFileName);
        }

        private Image[] GetPagesAsImagesFromDocument(string inputFileName,
            string outputFileName, DocumentFormat format)
        {
            Image[] pagesAsImages;
            switch (format)
            {
                case DocumentFormat.PDF:
                {
                    pagesAsImages =
                        GetGrayscalePagesAsImagesFromPdf(inputFileName);
                    break;
                }
                case DocumentFormat.DOCX:
                {
                    pagesAsImages =
                        GetGrayscalePagesAsImagesFromDocOrDocx(inputFileName,
                            outputFileName);
                    break;
                }
                case DocumentFormat.DOC:
                {
                    pagesAsImages =
                        GetGrayscalePagesAsImagesFromDocOrDocx(inputFileName,
                            outputFileName);
                    break;
                }
                case DocumentFormat.JPG:
                {
                    Image image = Image.FromFile(inputFileName);
                    double width = 1240;
                    double height = 1754;
                    Image scaledImage;

                    if (image.Width > width || image.Height > height)
                    {
                        scaledImage = new Bitmap((int) width, (int) height);
                        image.ScaleToFit(ref width, ref height);
                        Graphics g = Graphics.FromImage(scaledImage);
                        g.FillRectangle(System.Drawing.Brushes.White, 0, 0,
                            scaledImage.Width, scaledImage.Height);
                        g.DrawImage(image, 0, 0, (int) width, (int) height);
                    }
                    else if (image.Width < width || image.Height < height)
                    {
                        double scale = image.Width > image.Height
                            ? width / image.Width
                            : height / image.Height;
                        width = image.Width * scale;
                        height = image.Height * scale;

                        scaledImage = new Bitmap((int) width, (int) height);

                        image.ScaleToFit(ref width, ref height);
                        Graphics g = Graphics.FromImage(scaledImage);
                        g.FillRectangle(System.Drawing.Brushes.White, 0, 0,
                            scaledImage.Width, scaledImage.Height);
                        g.DrawImage(image, 0, 0, (int) width, (int) height);
                    }
                    else
                    {
                        scaledImage = new Bitmap(image, (int) width, (int) height);
                    }

                    MemoryStream imageStream = new MemoryStream();
                    scaledImage.CloneBlackAndWhite().Save(imageStream, ImageFormat.Png);
                       
                    pagesAsImages = new[] { Image.FromStream(imageStream) };
                    break;
                }
                default: throw new Exception(nameof(format));
            }

            return pagesAsImages;
        }

        /// <summary>
        /// Создаёт и скачивает сертифицированную копию документа по образцу файла.
        /// Сертифицированный документ становится чёрно-белым.
        /// На каждой странице в левом нижнем углу ставятся печать и подпись
        /// заявителя, а также печать "Копия верна".
        /// </summary>
        /// <param name="name">Название документа (может быть аббревиатурой).</param>
        /// <param name="path">Путь к исходном файлу.</param>
        /// <param name="formats">Допустимые форматы исходного файла.</param>
        private void DownloadDocumentCopyInPDF(string name, string path, DocumentFormat[] formats)
        {
            //if (!IsApplicantSignatureStampLoaded()) return;

            if (!IsDocumentValid(name, path, formats)) return;
            Document document = new Document(name, path);

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = @"*.pdf|*.pdf",
                FileName = document.Name,
                Title = @"Выгрузка " + Document.GetNameInGenitive(document.Name)
                
            };

            if (saveFileDialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {
                IsEnabled = false;
                string outputFileName = saveFileDialog.FileName;
                Image applicantSignatureImage = ApplicantSignatureImage.Image;
                Image applicantStampImage = ApplicantStamp.Image;

                LoadingWindow loadingWindow = new LoadingWindow { Owner = this };

                new Thread(() =>
                {
                    Dispatcher.Invoke(() =>
                        loadingWindow.Show()
                    );

                    try
                    {
                        Image[] pagesAsImages =
                            GetPagesAsImagesFromDocument(path, outputFileName,
                                document.Format);

                        pagesAsImages = CompressImages(pagesAsImages);

                        CreateСertifiedDocument(outputFileName, pagesAsImages,
                            applicantSignatureImage, applicantStampImage,
                            document.Format != DocumentFormat.DOC &&
                            document.Format != DocumentFormat.DOCX);

                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Hide();
                            IsEnabled = true;
                        }
                        );
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Hide();
                            IsEnabled = true;
                            MessageBox.Show(ex.Message,
                                "Ошибка при выгрузке " + Document.GetNameInGenitive(document.Name),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }

                }).Start();
            }
        }

        private void DownloadTestReport_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadDocumentCopyInPDF("ПИ", TestReport.Text, new[] {DocumentFormat.PDF});
        }

        private void DownloadRegistrationDocument_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadDocumentCopyInPDF("ОГРН", RegistrationDocument.Text, new[] { DocumentFormat.PDF, DocumentFormat.JPG });
        }

        private void DownloadInn_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadDocumentCopyInPDF("ИНН", Inn.Text, new[] { DocumentFormat.PDF, DocumentFormat.JPG });
        }

        private void DownloadModelDocument_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadDocumentCopyInPDF("МАКЕТ", ModelDocument.Text, new[] { DocumentFormat.DOCX, DocumentFormat.DOC });
        }

        private void DownloadAllDocuments_OnClick(object sender, RoutedEventArgs e)
        {
            //if (!IsApplicantSignatureStampLoaded() ||
            //    !IsManufacturerSignatureStampLoaded()) return;

            List<Document> documents = new List<Document>();

            if (!string.IsNullOrWhiteSpace(TestReport.Text))
            {
                if (!IsDocumentValid("ПИ", TestReport.Text,
                    new[] {DocumentFormat.PDF})) return;
                documents.Add(new Document("ПИ", TestReport.Text));
            }

            if (!string.IsNullOrWhiteSpace(RegistrationDocument.Text))
            {
                if (!IsDocumentValid("ОГРН", RegistrationDocument.Text,
                    new[] {DocumentFormat.PDF, DocumentFormat.JPG})) return;
                documents.Add(new Document("ОГРН", RegistrationDocument.Text));
            }

            if (!string.IsNullOrWhiteSpace(Inn.Text))
            {
                if (!IsDocumentValid("ИНН", Inn.Text,
                    new[] {DocumentFormat.PDF, DocumentFormat.JPG})) return;
                documents.Add(new Document("ИНН", Inn.Text));
            }

            if (!string.IsNullOrWhiteSpace(ModelDocument.Text))
            {
                if (!IsDocumentValid("МАКЕТ", ModelDocument.Text,
                    new[] {DocumentFormat.DOCX, DocumentFormat.DOC})) return;
                documents.Add(new Document("МАКЕТ", ModelDocument.Text));
            }

            string docNames =
                string.Join(", ", documents.Select(doc => doc.Name));

            bool needToDownloadAuthorizedContract;
            if (string.IsNullOrWhiteSpace(ManufacturerName.Text) &&
                string.IsNullOrWhiteSpace(ManufacturerCountry.Text))
            {
                needToDownloadAuthorizedContract = false;
            }
            else
            {
                docNames = docNames.Insert(0, docNames.Length > 0 ? "ДУЛ, " : "ДУЛ");
                needToDownloadAuthorizedContract = true;
            }

            if (string.IsNullOrEmpty(docNames))
            {
                MessageBox.Show("Нечего выгружать.",
                    "Выгрузка всех документов");
                return;
            }

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = _previousSelectedFolder,
                Description = @"Выберите папку, в которую будут выгружены следующие документы: " + docNames + @"."
            };

            if (folderBrowserDialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {
                IsEnabled = false;
                string folder = folderBrowserDialog.SelectedPath;
                _previousSelectedFolder = folder;
                foreach (var document in documents)
                {
                    int attemp = 2;
                    string path = Path.Combine(folder, document.Name + ".pdf");
                    while (File.Exists(path) || documents.Select(doc => doc.OutputPath).Contains(path))
                    {
                        path = Path.Combine(folder, document.Name + $" ({attemp++}).pdf");
                    }

                    document.OutputPath = path;
                }

                Image applicantSignatureImage = ApplicantSignatureImage.Image;
                Image applicantStampImage = ApplicantStamp.Image;
                string authorizedContractFilename = "";
                if (needToDownloadAuthorizedContract)
                {
                    authorizedContractFilename = Path.Combine(folder, "ДУЛ.pdf");
                }

                LoadingWindow loadingWindow = new LoadingWindow { Owner = this };

                new Thread(() =>
                {
                    Dispatcher.Invoke(() =>
                        loadingWindow.Show()
                    );

                    try
                    {
                        if (needToDownloadAuthorizedContract)
                        {
                            CreateAuthorizedСontract(
                                authorizedContractFilename);
                        }

                        foreach (var document in documents)
                        {
                            try
                            {
                                Image[] pagesAsImages =
                                    GetPagesAsImagesFromDocument(
                                        document.InputPath,
                                        document.OutputPath,
                                        document.Format);

                                pagesAsImages = CompressImages(pagesAsImages);

                                CreateСertifiedDocument(document.OutputPath,
                                    pagesAsImages,
                                    applicantSignatureImage,
                                    applicantStampImage,
                                    document.Format != DocumentFormat.DOC &&
                                    document.Format != DocumentFormat.DOCX);
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    loadingWindow.Hide();
                                    MessageBox.Show(ex.Message,
                                        "Ошибка при выгрузке " +
                                        Document.GetNameInGenitive(
                                            document.Name),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                    loadingWindow.Show();
                                });
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Hide();
                            IsEnabled = true;
                        }
                        );
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Hide();
                            IsEnabled = true;
                            MessageBox.Show(ex.Message,
                                "Ошибка при выгрузке документов",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }).Start();
            }
        }

        private void CreateAuthorizedСontract(string filename)
        {
            string authorizedСontractFileName = "";
            string registrationNumber = "";
            string applicantName = "";
            string applicantFio = "";
            string manufacturerName = "";
            string manufacturerCountry = "";

            Image applicantSignatureImage = new Bitmap(10, 10);
            Image applicantStampImage = new Bitmap(10, 10);
            Image manufacturerSignatureImage = new Bitmap(10, 10);
            Image manufacturerStampImage = new Bitmap(10, 10);

            Dispatcher.Invoke(() =>
            {
                authorizedСontractFileName =
                    Environment.CurrentDirectory +
                    "\\Загрузить ДУЛ.docx";
                registrationNumber = RegistrationNumber.Text;
                applicantName = ApplicantName.Text;
                applicantFio = ApplicantFio.Text;
                manufacturerName = ManufacturerName.Text;
                manufacturerCountry = ManufacturerCountry.Text;
                applicantSignatureImage = ApplicantSignatureImage.Image;
                applicantStampImage = ApplicantStamp.Image;
                manufacturerSignatureImage =
                    ManufacturerSignatureImage.Image;
                manufacturerStampImage = ManufacturerStamp.Image;
            });

            Word.Application app = new Word.Application();
            try
            {
                app.Documents.Open(authorizedСontractFileName);
            }
            catch(Exception ex)
            {
                throw new Exception("Документ \"Загрузить ДУЛ.docx\" открыт. " +
                                    "Закройте его и повторите попытку.");
            }

            object missing = Type.Missing;
            Word.Find find = app.Selection.Find;
            find.Text =
                "(название заявителя в английской транскрипции)";
            find.Replacement.Text =
                Transliteration.Front(applicantName);
            object wrap = Word.WdFindWrap.wdFindStop;
            object replace = Word.WdReplace.wdReplaceOne;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text =
                "(ФИО руководителя заявителя в английской транскрипции)";
            find.Replacement.Text =
                Transliteration.Front(applicantFio);
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceOne;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            int randomNumber = new Random().Next(100, 999);
            find = app.Selection.Find;
            find.Text = "Contract №";
            find.Replacement.Text = "Contract № " + randomNumber;
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceAll;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "Date:";
            find.Replacement.Text =
                "Date: " + (DateTime.Today - TimeSpan.FromDays(30))
                .ToShortDateString();
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceAll;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "Договор №";
            find.Replacement.Text = "Договор № " + randomNumber;
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceAll;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "Дата:";
            find.Replacement.Text =
                "Дата: " + (DateTime.Today - TimeSpan.FromDays(30))
                .ToShortDateString();
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceAll;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);


            find = app.Selection.Find;
            find.Text = "в лице (ФИО руководителя заявителя) действующего на основании Устава, ";
            find.Replacement.Text = "";
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceOne;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "(ФИО руководителя заявителя)";
            find.Replacement.Text = applicantFio;
            wrap = Word.WdFindWrap.wdFindContinue;
            replace = Word.WdReplace.wdReplaceAll;
            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "(название заявителя, ОГРН 1234567891234)";
            find.Replacement.Text =
                applicantName + ", " +
                registrationNumber.Substring(
                    registrationNumber.IndexOf("*",
                        StringComparison.InvariantCulture) + 1);

            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            find = app.Selection.Find;
            find.Text = "(Наименование производителя, страна)";
            find.Replacement.Text =
                manufacturerName + ", " +
                manufacturerCountry;

            find.Execute(Type.Missing, false, true, false, missing,
                false,
                true,
                wrap, false, missing, replace);

            app.ActiveDocument.SaveAs(filename,
                Word.WdSaveFormat.wdFormatPDF);
            app.ActiveDocument.Close(false);
            app.Quit();

            PdfDocument outputDocument = new PdfDocument();
            using (PdfDocument inputDocument =
                PdfReader.Open(filename,
                    PdfDocumentOpenMode.Import))
            {
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    PdfPage editablePage =
                        outputDocument.AddPage(
                            inputDocument.Pages[i]);
                    double heightCoef = editablePage.Height /
                                        editablePage.Height
                                            .Centimeter;
                    double widthCoef = editablePage.Width /
                                       editablePage.Width
                                           .Centimeter;
                    XGraphics gfx =
                        XGraphics.FromPdfPage(editablePage);

                    XImage img = XImage.FromFile("Копия верна.png");
                    gfx.DrawImage(img, 2 * widthCoef,
                        editablePage.Height - 8.5 * heightCoef,
                        4 * widthCoef, 2 * heightCoef);


                    // Обработка изображений JPEG работает лучше, если использовать в
                    // PDFsharp 1.50 или более поздней версии XImage.FromStream
                    // вместо Image.FromStream плюс XImage.FromGdiPlusImage!!!

                    double width, height;
                    MemoryStream imageStream;

                    if (applicantSignatureImage != null)
                    {
                        // Масштабируем и рисуем подпись заявителя.
                        width = 5 * widthCoef;
                        height = 3 * heightCoef;

                        applicantSignatureImage.ScaleToFit(ref width,
                            ref height);

                        imageStream = new MemoryStream();
                        applicantSignatureImage.Save(imageStream,
                            ImageFormat.Png);
                        img = XImage.FromStream(imageStream);

                        gfx.DrawImage(img, 2 * widthCoef,
                            editablePage.Height - 2 * heightCoef -
                            height, width, height);
                    }

                    if (applicantStampImage != null)
                    {
                        // Масшабируем и рисуем печать заявителя.
                        width = 5 * widthCoef;
                        height = 4 * heightCoef;

                        applicantStampImage.ScaleToFit(ref width,
                            ref height);

                        imageStream = new MemoryStream();
                        applicantStampImage.Save(imageStream, ImageFormat.Png);
                        img = XImage.FromStream(imageStream);

                        gfx.DrawImage(img, 2 * widthCoef,
                            editablePage.Height - 6 * heightCoef,
                            width, height);
                    }

                    if (i == 1)
                    {
                        if (manufacturerSignatureImage != null)
                        {
                            // Конвертируем в чёрно-белый, масшабируем и рисуем подпись производителя.
                            width = 5 * widthCoef;
                            height = 3 * heightCoef;

                            manufacturerSignatureImage.ScaleToFit(ref width,
                                ref height);

                            imageStream = new MemoryStream();
                            manufacturerSignatureImage.CloneBlackAndWhite()
                                .Save(imageStream, ImageFormat.Png);
                            img = XImage.FromStream(imageStream);

                            gfx.DrawImage(img, 1 * widthCoef,
                                10 * heightCoef, width,
                                height);
                        }

                        if (manufacturerStampImage != null)
                        {
                            // Конвертируем в чёрно-белый, масшабируем и рисуем подпись производителя.
                            width = 5 * widthCoef;
                            height = 4 * heightCoef;

                            manufacturerStampImage.ScaleToFit(ref width,
                                ref height);

                            imageStream = new MemoryStream();
                            manufacturerStampImage.CloneBlackAndWhite()
                                .Save(imageStream, ImageFormat.Png);
                            img = XImage.FromStream(imageStream);

                            gfx.DrawImage(img, 2 * widthCoef,
                                10 * heightCoef, width, height);
                        }

                        if (applicantSignatureImage != null)
                        {
                            // Конвертируем в чёрно-белый, масшабируем и рисуем подпись заявителя.
                            width = 5 * widthCoef;
                            height = 3 * heightCoef;

                            applicantSignatureImage.ScaleToFit(ref width,
                                ref height);

                            imageStream = new MemoryStream();
                            applicantSignatureImage.CloneBlackAndWhite()
                                .Save(imageStream, ImageFormat.Png);
                            img = XImage.FromStream(imageStream);

                            gfx.DrawImage(img,
                                editablePage.Width - 8 * widthCoef,
                                10 * heightCoef, width, height);
                        }

                        if (applicantStampImage != null)
                        {
                            // Конвертируем в чёрно-белый, масшабируем и рисуем печать заявителя.
                            width = 5 * widthCoef;
                            height = 4 * heightCoef;

                            applicantStampImage.ScaleToFit(ref width,
                                ref height);

                            imageStream = new MemoryStream();
                            applicantStampImage.CloneBlackAndWhite()
                                .Save(imageStream, ImageFormat.Png);
                            img = XImage.FromStream(imageStream);

                            gfx.DrawImage(img,
                                editablePage.Width - 6 * widthCoef,
                                10 * heightCoef, width, height);
                        }
                    }
                }
            }

            outputDocument.Save(filename);
        }

        private void DownloadAuthorizedСontract_OnClick(object sender,
            RoutedEventArgs e)
        {
            //if (!IsApplicantSignatureStampLoaded() ||
            //    !IsManufacturerSignatureStampLoaded()) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = @"*.pdf|*.pdf",
                FileName = "ДУЛ"
            };

            if (saveFileDialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {
                IsEnabled = false;
                string outputDocumentName = saveFileDialog.FileName;

                LoadingWindow loadingWindow = new LoadingWindow { Owner = this };

                new Thread(() =>
                {
                    Dispatcher.Invoke(() =>
                        loadingWindow.Show()
                    );

                    try
                    {
                        CreateAuthorizedСontract(outputDocumentName);

                        Dispatcher.Invoke(() =>
                            {
                                loadingWindow.Hide();
                                IsEnabled = true;
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Hide();
                            IsEnabled = true;
                            MessageBox.Show(ex.Message,
                                "Ошибка при записи файла ДУЛ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }

                }).Start();
            }
        }

        #region Загрузка изображений подписей и печатей

        private string LoadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            { Filter = @"*.png|*.png|*.jpg|*.jpg", FilterIndex = 1 };

            string fileName = "";

            if (openFileDialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {

                if (!openFileDialog.FileName.ToLower().EndsWith(".png") &&
                    !openFileDialog.FileName.ToLower().EndsWith(".jpg"))
                {
                    MessageBox.Show(
                        "Выбран файл с неверным форматом. Укажите файл в формате png или jpg.",
                        "Ошибка при загрузке файла",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    fileName = "";
                }
                else fileName = openFileDialog.FileName;
            }

            return fileName;
        }

        private void LoadApplicantSignature()
        {
            string fileName = LoadImage();
            if (string.IsNullOrEmpty(fileName)) return;

            NoApplicantSignature.Visibility = Visibility.Collapsed;
            ApplicantSignatureHost.Visibility = Visibility.Visible;
            ApplicantSignature.Background = Brushes.White;
            ApplicantSignatureImage.Image = Image.FromFile(fileName);
            ApplicantSignatureImage.ImageLocation = fileName;
        }

        private void LoadManufacturerSignature()
        {
            string fileName = LoadImage();
            if (string.IsNullOrEmpty(fileName)) return;

            NoManufacturerSignature.Visibility = Visibility.Collapsed;
            ManufacturerSignatureHost.Visibility = Visibility.Visible;
            ManufacturerSignature.Background = Brushes.White;
            ManufacturerSignatureImage.Image = Image.FromFile(fileName);
            ManufacturerSignatureImage.ImageLocation = fileName;
        }

        private void ManufacturerSignature_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadManufacturerSignature();
        }

        private void ManufacturerSignatureImage_OnClick(object sender, EventArgs e)
        {
            LoadManufacturerSignature();
        }

        private void ManufacturerSignature_OnClick(object sender, RoutedEventArgs e)
        {
            LoadManufacturerSignature();
        }

        private void ApplicantSignature_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadApplicantSignature();
        }

        private void ApplicantSignature_OnClick(object sender, RoutedEventArgs e)
        {
            LoadApplicantSignature();
        }

        private void ApplicantSignatureImage_OnClick(object sender, EventArgs e)
        {
            LoadApplicantSignature();
        }

        #endregion

        #region Выгрузка изображений печатей

        private void DownloadImage(Image image)
        {
            if (image == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = @"*.png|*.png|*.jpg|*.jpg",
                FileName = "Печать"
            };

            if (saveFileDialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    image.Save(saveFileDialog.FileName,
                        saveFileDialog.FilterIndex == 1
                            ? ImageFormat.Png
                            : ImageFormat.Jpeg);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                        "Ошибка при сохранении файла");
                }
            }
        }

        private void LoadApplicantStamp()
        {
            string fileName = LoadImage();
            if (string.IsNullOrEmpty(fileName)) return;

            NoApplicantStamp.Visibility = Visibility.Collapsed;
            ApplicantStampHost.Visibility = Visibility.Visible;
            ApplicantStampGrid.Background = Brushes.White;
            //_applicantStampPhoto = Image.FromFile(fileName);
            //ApplicantStamp.Image = _applicantStampPhoto;
            ApplicantStamp.Image = Image.FromFile(fileName);
        }

        private void ApplicantStamp_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CreateApplicantStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ApplicantStamp.Image, 160, 160));
            else LoadApplicantStamp();
        }

        private void ApplicantStamp_OnClick(object sender, EventArgs e)
        {
            if (CreateApplicantStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ApplicantStamp.Image, 160, 160));
            else LoadApplicantStamp();
        }

        private void DownloadApplicantStamp_OnClick(object sender, RoutedEventArgs e)
        {
            if (CreateApplicantStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ApplicantStamp.Image, 160, 160));
            else LoadApplicantStamp();
        }

        private void LoadManufacturerStamp()
        {
            string fileName = LoadImage();
            if (string.IsNullOrEmpty(fileName)) return;

            NoManufacturerStamp.Visibility = Visibility.Collapsed;
            ManufacturerStampHost.Visibility = Visibility.Visible;
            ManufacturerStampGrid.Background = Brushes.White;
            //_manufacturerStampPhoto = Image.FromFile(fileName);
            //ManufacturerStamp.Image = _manufacturerStampPhoto;
            ManufacturerStamp.Image = Image.FromFile(fileName);
        }

        private void ManufacturerStamp_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CreateManufacturerStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ManufacturerStamp.Image, 200, 120));
            else LoadManufacturerStamp();
        }

        private void ManufacturerStamp_OnClick(object sender, EventArgs e)
        {
            if (CreateManufacturerStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ManufacturerStamp.Image, 200, 120));
            else LoadManufacturerStamp();
        }

        private void DownloadManufacturerStamp_OnClick(object sender, RoutedEventArgs e)
        {
            if (CreateManufacturerStampRadioButton.IsChecked == true)
                DownloadImage(new Bitmap(ManufacturerStamp.Image, 200, 120));
            else LoadManufacturerStamp();
        }

        #endregion

        private void CreateApplicantStamp_OnChecked(object sender, RoutedEventArgs e)
        {
            ApplicantStampButton.Content = "Выгрузить";
            ApplicantStampGrid.ToolTip = "Выгрузить печать заявителя";
            ApplicantStampHost.Visibility = Visibility.Visible;
            NoApplicantStamp.Visibility = Visibility.Collapsed;
            ApplicantStampGrid.Background = Brushes.White;
            PaintApplicantStamp();
        }

        private void LoadApplicantStamp_OnChecked(object sender, RoutedEventArgs e)
        {
            ApplicantStampButton.Content = "Загрузить";
            ApplicantStampGrid.ToolTip = "Загрузить печать заявителя";
            ApplicantStampGrid.Background = Brushes.LightGray;
            ApplicantStamp.Image = null;
            //if (_applicantStampPhoto != null)
            //    ApplicantStamp.Image = _applicantStampPhoto;
            //else
            //{
            ApplicantStampHost.Visibility = Visibility.Collapsed;
            NoApplicantStamp.Visibility = Visibility.Visible;
            //}
        }

        private void CreateManufacturerStamp_OnChecked(object sender, RoutedEventArgs e)
        {
            ManufacturerStampButton.Content = "Выгрузить";
            ManufacturerStampGrid.ToolTip = "Выгрузить печать производителя";
            ManufacturerStampHost.Visibility = Visibility.Visible;
            NoManufacturerStamp.Visibility = Visibility.Collapsed;
            ManufacturerStampGrid.Background = Brushes.White;
            PaintManufacturerStamp();
        }

        private void LoadManufacturerStamp_OnChecked(object sender, RoutedEventArgs e)
        {
            ManufacturerStampButton.Content = "Загрузить";
            ManufacturerStampGrid.ToolTip = "Загрузить печать производителя";
            ManufacturerStampGrid.Background = Brushes.LightGray;
            ManufacturerStamp.Image = null;
            //if (_manufacturerStampPhoto != null)
            //    ManufacturerStamp.Image = _manufacturerStampPhoto;
            //else
            //{
            ManufacturerStampHost.Visibility = Visibility.Collapsed;
            NoManufacturerStamp.Visibility = Visibility.Visible;
            // }
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Вы действительно хотите очистить все поля и изображения?",
                    "Сбросить всё", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            RegistrationNumber.Clear();
            ApplicantName.Clear();
            ApplicantFio.Clear();
            ManufacturerName.Clear();
            ManufacturerCountry.Clear();
            TestReport.Clear();
            RegistrationDocument.Clear();
            Inn.Clear();
            ModelDocument.Clear();

            ApplicantSignatureImage.Image = null;
            NoApplicantSignature.Visibility = Visibility.Visible;
            ApplicantSignatureHost.Visibility = Visibility.Collapsed;
            ApplicantSignature.Background = Brushes.LightGray;
            ManufacturerSignatureImage.Image = null;
            NoManufacturerSignature.Visibility = Visibility.Visible;
            ManufacturerSignatureHost.Visibility = Visibility.Collapsed;
            ManufacturerSignature.Background = Brushes.LightGray;

            //_applicantStampPhoto = null;
            if (CreateApplicantStampRadioButton.IsChecked == false)
            {
                NoApplicantStamp.Visibility = Visibility.Visible;
                ApplicantStampHost.Visibility = Visibility.Collapsed;
                ApplicantStampGrid.Background = Brushes.LightGray;
            }
            else ApplicantStampGrid.Background = Brushes.White;

            //_manufacturerStampPhoto = null;
            if (CreateManufacturerStampRadioButton.IsChecked == false)
            {
                NoManufacturerStamp.Visibility = Visibility.Visible;
                ManufacturerStampHost.Visibility = Visibility.Collapsed;
                ManufacturerStampGrid.Background = Brushes.LightGray;
            }
            else ManufacturerStampGrid.Background = Brushes.White;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if(!Settings.GetSettings().CanRunProgram) return;
            
            if (MessageBox.Show("Вы уверены, что хотите выйти из программы?",
                    "Завершение", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No) !=
                MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }
    }
}
