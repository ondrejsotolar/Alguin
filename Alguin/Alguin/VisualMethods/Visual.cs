using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using System.IO;

namespace Alguin.VisualMethods
{
    /// <summary>
    /// Class in an entry point to visual methods
    /// </summary>
    public class Visual
    {
        #region Attributes

        private const int DefaultMultiplier = 3;
        private const double DefaultDeviation = 0.7;

        private Random random;
        private BitmapUtils bitmapUtils;
        private Tesseract ocrEngine;
        private int multiplier;
        private string bitmapSource;

        public UIItemContainer Window { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Visual()
        {
            this.random = new Random();
            this.bitmapUtils = new BitmapUtils();
            this.ocrEngine = new Tesseract();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Find UIItem matching to label by position. 
        /// Label center point (plus offset or not) must be inside UIItem bounds.
        /// </summary>
        /// <typeparam name="T">control type</typeparam>
        /// <param name="pattern">regex pattern</param>
        /// <param name="windowBounds">windows bounds</param>
        /// <param name="labels">labels</param>
        /// <param name="controls">controls</param>
        /// <param name="leftXOffset">(optional) left X offset - added to label click point X coordinate</param>
        /// <param name="topYOffset">(optional) top Y offset - added to label click point Y coordinate</param>
        /// <param name="order">(optional) 1-based. In case of multiple matching labels. 1 means first, 2 second...</param>
        /// <param name="throwException">suppress exceptions</param>
        /// <returns>UIItem</returns>
        public T FindControlForLabel<T>(
            string pattern, 
            System.Windows.Rect windowBounds, 
            Dictionary<Point, string> labels, 
            UIItemCollection controls, 
            int leftXOffset = 0, 
            int topYOffset = 0, 
            int order = 0, 
            bool throwException = true) 
            where T : IUIItem
        {
            var count = 0;
            var windowTopLeft = new Point((int)windowBounds.X, (int)windowBounds.Y);
            var possibleLabels = labels
                .Where(x => this.ocrEngine.RegexMatch(x.Value, pattern))
                .Select(y => y.Key);

            foreach (var control in controls)
            {
                if (!IsSameOrSubclass(typeof(T), control.GetType()))
                    continue;

                if (possibleLabels.Any(x => LabelForControl(windowTopLeft, x, control, leftXOffset, topYOffset)))
                {
                    if (order == 0)
                        return (T)control;
                    else
                    {
                        count++;
                        if (count == order)
                            return (T)control;
                    }
                }
            }

            var labelsToPrint = string.Join("\n", labels.Where(x => x.Value.Length > 2).Select(y => y.Value));

            if (throwException)
                throw new Exception(string.Format(
                    "OCR could not find a match for the pattern '{0}', \n all OCR labels: {1}",
                    pattern, labelsToPrint));

            return default(T);
        }

        /// <summary>
        /// Find UIItem matching to bitmap model image. 
        /// Label center point (plus offset or not) must be inside UIItem bounds.
        /// </summary>
        /// <typeparam name="T">control type</typeparam>
        /// <param name="name">model name</param>
        /// <param name="bcType">comparison method</param>
        /// <param name="leftXOffset">(optional) left X offset - added to label click point X coordinate</param>
        /// <param name="topYOffset">(optional) top Y offset - added to label click point Y coordinate</param>
        /// <param name="order">(optional) 1-based. In case of multiple matching labels. 1 means first, 2 second...</param>
        /// <param name="throwException">suppress exceptions</param>
        /// <returns></returns>
        public T FindControlForBitmap<T>(
            string name,
            BitmapComparison bcType = BitmapComparison.Partial,
            int leftXOffset = 0,
            int topYOffset = 0,
            int order = 0,
            bool throwException = true)
            where T : IUIItem
        {
            this.multiplier = 1;
            Point match;
            var modelPath = Path.Combine(this.bitmapSource, name);

            if (bcType == BitmapComparison.Partial)
                match = this.bitmapUtils.PartialBitmapMatch(modelPath, this.bitmapUtils.TakeScreenshot(this.Window));
            else
                match = this.bitmapUtils.ExactBitmapMatch(modelPath, this.bitmapUtils.TakeScreenshot(this.Window));
            
            if (match == null)
                return default(T);

            var windowTopLeft = new Point((int)this.Window.Bounds.X, (int)this.Window.Bounds.Y);
            var controls = this.GetAllControls();
            foreach (var control in controls)
            {
                if (!IsSameOrSubclass(typeof(T), control.GetType()))
                    continue;

                if (LabelForControl(windowTopLeft, match, control, leftXOffset, topYOffset))
                    return (T)control;
            }
            return default(T);
        }

        /// <summary>
        /// Find point matching to label by position. 
        /// </summary>
        /// <param name="pattern">regex pattern</param>
        /// <param name="labels">labels</param>
        /// <param name="order">(optional) 0-based. In case of multiple matching labels.</param>
        /// <returns>point</returns>
        public Point? FincClickPointForLabel(string pattern, Dictionary<Point, string> labels, int order = 0)
        {
            var possibleLabels = labels
                .Where(x => this.ocrEngine.RegexMatch(x.Value, pattern))
                .Select(y => y.Key);

            if (possibleLabels.Count() > order)
                return possibleLabels.ToArray()[order];

            return null;
        }

        /// <summary>
        /// Find point mathinch to bitmap model image.
        /// </summary>
        /// <param name="name">image name</param>
        /// <param name="bcType">comparison method</param>
        /// <returns>point</returns>
        public Point? FindClickPointForBitmap(string name, BitmapComparison bcType = BitmapComparison.Partial)
        {
            this.multiplier = 1;
            Point? match;
            var modelPath = Path.Combine(this.bitmapSource, name);

            if (bcType == BitmapComparison.Partial)
                match = this.bitmapUtils.PartialBitmapMatch(modelPath, this.bitmapUtils.TakeScreenshot(this.Window));
            else
                match = this.bitmapUtils.ExactBitmapMatch(modelPath, this.bitmapUtils.TakeScreenshot(this.Window));

            return match;
        }

        /// <summary>
        /// Find all labels and their center point location in a Window
        /// </summary>
        /// <param name="Window">Window</param>
        /// <param name="multiplier">(optional) OCR - screenshot resize coeficient - default is 3</param>
        /// <param name="deviation">(optional) OCR - screenshot gaussian blur deviation - default is 0.7</param>
        /// <returns>label and its center point</returns>
        public Dictionary<Point, string> FindAllLabels(IUIItemContainer window, int multiplier = DefaultMultiplier, double deviation = DefaultDeviation, string savePath = null)
        {
            this.multiplier = multiplier;

            if (window == null)
                throw new NullReferenceException("Window is null. Cannot take screenshot.");

            var bmp = this.bitmapUtils.TakeScreenshot(window);

            if (savePath != null)
                bmp.Save(savePath);

            return FindAllLabels(bmp, multiplier, deviation);
        }

        /// <summary>
        /// Set bitmap source
        /// </summary>
        /// <param name="path">name to folder with model images</param>
        public void SetBitmapSource(string path) 
        {
            this.bitmapSource = path;
        }
        
        #endregion

        #region Other methods

        private bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }

        private bool LabelForControl(Point windowTopLeft, Point point, IUIItem control, int leftXOffset, int topYOffset)
        {
            var controlX = control.Bounds.X - windowTopLeft.X;
            var controlY = control.Bounds.Y - windowTopLeft.Y;

            var result = controlX <= point.X / this.multiplier + leftXOffset
                      && controlX + control.Bounds.Width >= point.X / this.multiplier + leftXOffset
                      && controlY <= point.Y / this.multiplier + topYOffset
                      && controlY + control.Bounds.Height >= point.Y / this.multiplier + topYOffset;

            return result;
        }

        /// <summary>
        /// Find all labels and their center point location in a Window screenshot
        /// </summary>
        /// <param name="Window">Window screenshot</param>
        /// <param name="multiplier">(optional) OCR - screenshot resize coeficient</param>
        /// <param name="deviation">(optional) OCR - screenshot gaussian blur deviation</param>
        /// <returns>label and its center point</returns>
        private Dictionary<Point, string> FindAllLabels(Bitmap bmp, int multiplier = DefaultMultiplier, double deviation = DefaultDeviation)
        {
            this.multiplier = multiplier;

            var ocrBitmapPath = this.bitmapUtils.PrepareForOcrPath(bmp, multiplier: multiplier, deviation: deviation);

            var result = this.ocrEngine.TextFromFullscreenBitmap(ocrBitmapPath);
            return result;
        }

        private UIItemCollection GetAllControls()
        {
            return new UIItemCollection(this.Window.GetMultiple(SearchCriteria.All));
        }

        #endregion
    }

    public enum BitmapComparison
    {
        Exact, Partial
    }
}
