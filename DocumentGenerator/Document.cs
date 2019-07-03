
using System;
using System.IO;
using System.Windows.Forms;

namespace DocumentGenerator
{
    public class Document
    {
        /// <summary>
        /// Полный путь исходного документа.
        /// </summary>
        public string InputPath { get; set; }

        /// <summary>
        /// Полный путь нового документа.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Имя документа без расширения.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Название документа.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип документа (определяется по его расширению).
        /// </summary>
        public DocumentFormat Format { get; set; }

        public Document(string name, string path)
        {
            Name = name;
            InputPath = path;
            OutputPath = "";
            FileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path)?.ToLower();
            switch (extension)
            {
                case ".pdf": Format = DocumentFormat.PDF; break;
                case ".jpg": Format = DocumentFormat.JPG; break;
                case ".doc": Format = DocumentFormat.DOC; break;
                case ".docx": Format = DocumentFormat.DOCX; break;
                default: throw new ArgumentException(nameof(extension));
            }
        }

        public static string GetNameInGenitive(string name)
        {
            // Метод рассчитан только на этот проект. В него могут передаваться только имена документов ПИ, ДУЛ, ОГРН, ИНН, МАКЕТ.
            if (name.EndsWith("И"))
            {
                return name;
            }

            if (name == "МАКЕТ")
            {
                return name + "А";
            }

            return name + "а";
        }

    }
}