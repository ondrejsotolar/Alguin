using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Linq;
using Kaliko.ImageLibrary;
using Kaliko.ImageLibrary.Filters;
using System.Reflection;
using System.Text.RegularExpressions;
using Alguin.Utilities;

namespace Alguin.VisualMethods
{
    /// <summary>
    /// Class for OCR engine management
    /// </summary>
    public class Tesseract
    {
        #region Attributes

        private string commandpath;
        private string outputPath;
        private string tmpPath;
        private const int defaultWordSpace = 25;
        private int wordSpacing = defaultWordSpace;

        #endregion

        #region Constructor

        public Tesseract(int? wordSpace = null)
        {
            this.tmpPath = GetTmpPath();
            this.outputPath = GetOutputPath();
            this.commandpath = GetTesseractPath();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set word space in pixels
        /// </summary>
        /// <param name="wordSpace">no. of pixels</param>
        public void SetWordSpace(int wordSpace)
        {
            this.wordSpacing = wordSpace;
        }

        /// <summary>
        /// Set default word spacing
        /// </summary>
        public void SetDefaultWordSpace()
        {
            this.wordSpacing = defaultWordSpace;
        }

        /// <summary>
        /// Run OCR on an image
        /// </summary>
        /// <param name="name">name to image</param>
        /// <returns>word boxes</returns>
        public Dictionary<Point, string> TextFromFullscreenBitmap(string path)
        {
            var bmp = new KalikoImage(path);
            bmp.SaveBmp(this.tmpPath);
            var result = AnalyzeFullScreen(this.tmpPath);

            if (File.Exists(this.tmpPath))
                File.Delete(this.tmpPath);

            return result;
        }

        /// <summary>
        /// Regex match. Ignores diavritic and case.
        /// </summary>
        /// <param name="label">label</param>
        /// <param name="pattern">regex pattern</param>
        /// <param name="patternToLower">pattern to lowercase</param>
        /// <returns>match result</returns>
        public bool RegexMatch(string label, string pattern, bool patternToLower = true)
        {
            var labelLowerCase = label.ToLower();
            var processedLabel = RemoveDiacritics(labelLowerCase);

            var patternLowerCase = patternToLower ? pattern.ToLower() : pattern;
            var processedPattern = RemoveDiacritics(patternLowerCase);

            var result = Regex.Match(processedLabel, processedPattern, RegexOptions.IgnoreCase);

            return result.Success;
        }

        /// <summary>
        /// Run OCR engine
        /// </summary>
        /// <param name="inputPath">name to image</param>
        /// <returns>word boxes</returns>
        public Dictionary<Point, string> AnalyzeFullScreen(string inputPath)
        {
            var outPath = this.outputPath.Replace(".txt", "");
            var args = string.Join(" ", inputPath, outPath, "-l", "ces", "hocr");

            File.Create(this.outputPath).Close();

            var startinfo = new ProcessStartInfo(commandpath, args);
            startinfo.CreateNoWindow = true;
            startinfo.UseShellExecute = false;
            Process.Start(startinfo).WaitForExit();

            var resultPath = outPath + ".html";
            var result = ReadHocrOutput(resultPath);

            //if (File.Exists(resultPath))
            //    File.Delete(resultPath);

            return result;
        }

        #endregion

        #region Other Methods

        private string RemoveDiacritics(String normalized)
        {
            normalized = normalized.Normalize(System.Text.NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder();

            for (int i = 0; i < normalized.Length; i++)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(normalized[i]) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    builder.Append(normalized[i]);

            return builder.ToString();
        }

        private Dictionary<Point, string> ReadHocrOutput(string path)
        {
            var root = XDocument.Load(path);

            var lines = root.Descendants()
                .Where(x => (string)x.Attribute("class") == "ocr_line")
                .ToDictionary(x => x.Attribute("id").Value.LastNumber(), x => x);

            var labels = RecognizeLabels(lines);

            var labelCenters = new Dictionary<Point, string>();

            foreach (var entry in labels)
            {
                var point = new Point(entry.Key.X + entry.Key.Width / 2, entry.Key.Y + entry.Key.Height / 2);
                if (labelCenters.ContainsKey(point))
                {
                    point.X += 1;
                    labelCenters.Add(point, entry.Value);
                }
                else
                {
                    labelCenters.Add(point, entry.Value);
                }
            }

            /*var labelCenters = labels.ToDictionary(
                x => new Point(x.Key.X + x.Key.Width / 2, x.Key.Y + x.Key.Height / 2),
                x => x.Value);*/

            return labelCenters;
        }

        private Dictionary<Rectangle, string> RecognizeLabels(Dictionary<int, XElement> lines)
        {
            var labels = new Dictionary<Rectangle, string>();

            foreach (var pair in lines)
            {
                var words = pair.Value.Descendants().Where(x => (string)x.Attribute("class") == "ocrx_word");
                var wordBoxes = JoinWordBoxes(words);

                foreach (var label in wordBoxes)
                {
                    labels.Add(label.Key, label.Value);
                }
            }

            return labels;
        }

        private Dictionary<Rectangle, string> JoinWordBoxes(IEnumerable<XElement> boxes)
        {
            if (boxes.Count() < 1)
                return null;

            var words = boxes.Select(x => new
            {
                Text = x.Value,
                Box = BoxToRectangle(x.Attribute("title").Value),
                WordNumber = x.Attribute("id").Value.LastNumber()
            }).OrderBy(x => x.WordNumber).ToList();

            var result = new Dictionary<Rectangle, string>();
            Rectangle current = new Rectangle();
            string currentText = string.Empty;

            for (int i = 0; i < words.Count(); i++)
            {
                var item = words[i];

                if (i == 0 || ((item.Box.X - wordSpacing) > (current.X + current.Width)))
                {
                    result.Add(item.Box, item.Text);
                    current = item.Box;
                    currentText = item.Text;
                }
                else
                {
                    result.Remove(current);
                    current = new Rectangle(current.X, current.Y, item.Box.Width + item.Box.X - current.X, current.Height);
                    currentText = string.Join(" ", currentText, item.Text);
                    result.Add(current, currentText);
                }
            }

            return result;
        }

        private Rectangle BoxToRectangle(string box)
        {
            if (string.IsNullOrEmpty(box))
                return new Rectangle();

            var boxParts = box.Split(' ');

            if (boxParts.Length != 5)
                throw new ArgumentException("Invalid box coordinates format.");

            var x = int.Parse(boxParts[1]);
            var y = int.Parse(boxParts[2]);
            var width = int.Parse(boxParts[3]) - x;
            var height = int.Parse(boxParts[4]) - y;

            var rectangle = new Rectangle(x, y, width, height);

            return rectangle;
        }

        /// <summary>
        /// C:\Users\username\AppData\Roaming\out.tif
        /// </summary>
        /// <returns></returns>
        private string GetTmpPath()
        {
            var path = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\out.tif";
            return path;
        }

        /// <summary>
        /// C:\Users\username\AppData\Roaming\out.txt
        /// </summary>
        /// <returns></returns>
        private string GetOutputPath()
        {
            var path = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\out.txt";
            return path;
        }

        /// <summary>
        /// ..\tesseract.exe
        /// </summary>
        /// <returns></returns>
        private string GetTesseractPath()
        {
            var path = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
            while (!path.GetDirectories().Any(x => x.Name == "ProjectLibraries"))
            {
                path = path.Parent;
            }
            var pathToTesseract = Path.Combine(path.FullName, "ProjectLibraries", "Tesseract-OCR", "tesseract.exe");
            return pathToTesseract;
        }

        #endregion
    }
}
