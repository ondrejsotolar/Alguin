using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Alguin.Utilities;
using Emgu.CV;
using Emgu.CV.Structure;
using Kaliko.ImageLibrary;
using Kaliko.ImageLibrary.Filters;
using TestStack.White.UIItems;

namespace Alguin.VisualMethods
{
    /// <summary>
    /// Utility class for bitmap comparison
    /// </summary>
    public class BitmapUtils
    {
        #region Attributes

        private const int DefaultBlackThreshold = 128;
        
        private string tmppath;
        private Bitmap source = null;
        private IntPtr Iptr = IntPtr.Zero;
        private BitmapData bitmapData = null;
        private const double MatchTreshold = 0.7;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Rectangle Area { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public BitmapUtils()
        {
            this.Area = Screen.PrimaryScreen.WorkingArea;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Take screenshot of working area
        /// </summary> 
        /// <param name="window">window</param>
        public Bitmap TakeScreenshot(IUIItemContainer window)
        {
            return TakeScreenshot((int)window.Bounds.Width, (int)window.Bounds.Height, 
                (int)window.Bounds.X, (int)window.Bounds.Y);
        }

        /// <summary>
        /// Take screenshot entire screen
        /// </summary>
        public Bitmap TakeScreenshot()
        {
            return TakeScreenshot(Area.Width, Area.Height, 0, 0);
        }
        
        /// <summary>
        /// Prepare bitmap for OCR
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <param name="threshold">black threshold</param>
        /// <param name="multiplier">size multiplier</param>
        /// <param name="deviation">deviation for gaussian</param>
        /// <param name="threshold">(optional) threshold for b&w</param>
        /// <returns>name to bitmap</returns>
        public string PrepareForOcrPath(Bitmap bitmap, int multiplier,
            double deviation, int threshold = DefaultBlackThreshold)
        {
            var path = Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                DateTime.Now.Ticks.ToString() + ".png");
            var bw = ConvertImageToBlackAndWhite(bitmap);
            var big = Resize(bw, multiplier);
            var gaussian = new KalikoImage(big);
            var kblur = new GaussianBlurFilter((float)deviation);
            kblur.Run(gaussian);
            gaussian.SaveBmp(path);

            return path;
        }

        /// <summary>
        /// Find target image in a screenshot using exact match
        /// </summary>
        /// <param name="name">name to screenshot</param>
        /// <param name="screens">target bitmap</param>
        /// <returns>center point</returns>
        public Point ExactBitmapMatch(string path, Bitmap screens)
        {
            var model = System.Drawing.Image.FromFile(path);
            var positionInWindow = GetClickPointByBitmap(screens, model);
            if (positionInWindow == null)
                throw new Exception("Model image wasn't found in target.");
            return positionInWindow.Value;
        }

        /// <summary>
        /// Find target image in a screenshot
        /// </summary>
        /// <param name="name">name to screenshot</param>
        /// <param name="screens">target bitmap</param>
        /// <returns>center point</returns>
        public Point PartialBitmapMatch(string path, Bitmap screens)
        {
            double[] min, max;
            Point[] origin, foundTL;

            var observedImage = new Image<Gray, Byte>(screens);
            var modelImage = new Image<Gray, Byte>(path);
            var resultImage = observedImage.MatchTemplate(modelImage, 
                Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            
            resultImage.MinMax(out min, out max, out origin, out foundTL);
            if (max[0] < MatchTreshold)
                throw new Exception("Model image wasn't found in target.");

            var foundCenter = new Point();
            foundCenter.X = foundTL[0].X + (modelImage.Width / 2);
            foundCenter.Y = foundTL[0].Y + (modelImage.Height / 2);

            return foundCenter;
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// Take screenshot of arbitrary area
        /// </summary>
        /// <param name="height">height</param>
        /// <param name="x">x</param>
        /// <param name="y">y</param>        
        /// <param name="width">width</param>
        private Bitmap TakeScreenshot(int width, int height, int x, int y)
        {
            var bmpScreenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            gfxScreenshot.CopyFromScreen(x, y, 0, 0, new Size(Area.Width, Area.Height),
                CopyPixelOperation.SourceCopy);

            return bmpScreenshot;
        }

        /// <summary>
        /// Resize bitmap
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <param name="multiplier">size multiplier</param>
        /// <returns>bitmap</returns>
        private Bitmap Resize(Bitmap bitmap, int multiplier)
        {
            var target = new Bitmap(bitmap.Width * multiplier, bitmap.Height * multiplier);

            var graphics = Graphics.FromImage(target);
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(bitmap, 0, 0, target.Width, target.Height);

            return target;
        }

        /// <summary>
        /// Convert bitmap to b&w
        /// </summary>
        /// <param name="image">bitmap</param>
        /// <param name="threshold">b&w threshold [0-255]</param>
        /// <returns>bitmap</returns>
        private Bitmap ConvertImageToBlackAndWhite(System.Drawing.Image image,
            int threshold = DefaultBlackThreshold)
        {
            if (image == null)
                return null;

            if (threshold > 255 || threshold < 0)
                throw new ArgumentException("Threshold must be in the 0-255 range.");

            var bitmapBytesLength = image.Width * image.Height *
                (System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8);
            var bitmapBytes = new byte[bitmapBytesLength];

            var bitmap = image as Bitmap;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width - 1, bitmap.Height - 1),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var intPointer = bitmapData.Scan0;

            System.Runtime.InteropServices.Marshal.Copy(intPointer, bitmapBytes, 0, bitmapBytesLength);

            var bitsPerPixel = GetBitsPerPixel(bitmapData.PixelFormat);
            var size = bitmapData.Stride * bitmapData.Height;

            for (int i = 0; i < size; i += bitsPerPixel / 8)
            {
                var magnitude1 = 1 / 3d * (bitmapBytes[i] + bitmapBytes[i + 1] + bitmapBytes[i + 2]);

                if (magnitude1 < threshold)
                {
                    bitmapBytes[i] = 0;
                    bitmapBytes[i + 1] = 0;
                    bitmapBytes[i + 2] = 0;
                }
                else
                {
                    bitmapBytes[i] = 255;
                    bitmapBytes[i + 1] = 255;
                    bitmapBytes[i + 2] = 255;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, intPointer, bitmapBytesLength);
            var newBtmp = new Bitmap(bitmap.Width, bitmap.Height, bitmapData.Stride,
                bitmapData.PixelFormat, intPointer);
            bitmap.UnlockBits(bitmapData);

            return newBtmp;
        }

        /// <summary>
        /// Find target image center relative to window
        /// </summary>
        /// <param name="target">bitmap</param>
        /// <param name="window">window</param>
        /// <param name="leftXOffset">left x offset</param>
        /// <param name="topYOffset">top y offset</param>
        /// <param name="throwException">optional suppression of exceptions</param>
        /// <returns>point</returns>
        private System.Windows.Point? FindClickPointByBitmap(System.Drawing.Image target, UIItemContainer window,
            int leftXOffset = 0, int topYOffset = 0, bool throwException = false)
        {
            var windowScreenshot = TakeScreenshot(window);
            var windowTopLeft = new Point((int)window.Bounds.X, (int)window.Bounds.Y);

            var positionInWindow = GetClickPointByBitmap(windowScreenshot, target);

            if (!positionInWindow.HasValue)
            {
                if (throwException)
                    throw new Exception("The bitmap was not found.");
                else
                    return null;
            }

            var result = new System.Windows.Point(
                x: positionInWindow.Value.X + windowTopLeft.X,
                y: positionInWindow.Value.Y + windowTopLeft.Y
            );

            return result;
        }

        private byte GetBitsPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 24;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 32;
                    break;
                default:
                    throw new ArgumentException("Only 24 and 32 bit images are supported");
            }
        }

        private Point? GetClickPointByBitmap(System.Drawing.Image left, System.Drawing.Image right)
        {
            if (left == null || right == null)
                throw new ArgumentException("One of the bitmaps is null.");

            if (!left.PixelFormat.Equals(right.PixelFormat))
                throw new ArgumentException("Can't compare different bitmap formats.");

            var leftBitmap = left as Bitmap;
            var rightBitmap = right as Bitmap;

            int bytes1 = left.Width * left.Height * (System.Drawing.Image.GetPixelFormatSize(left.PixelFormat) / 8);
            int bytes2 = right.Width * right.Height * (System.Drawing.Image.GetPixelFormatSize(right.PixelFormat) / 8);

            byte[] b1bytes = new byte[bytes1];
            byte[] b2bytes = new byte[bytes2];

            var bmd1 = leftBitmap.LockBits(new Rectangle(0, 0, leftBitmap.Width - 1, leftBitmap.Height - 1), 
                ImageLockMode.ReadOnly, leftBitmap.PixelFormat);
            var bmd2 = rightBitmap.LockBits(new Rectangle(0, 0, rightBitmap.Width - 1, rightBitmap.Height - 1), 
                ImageLockMode.ReadOnly, rightBitmap.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(bmd1.Scan0, b1bytes, 0, bytes1);
            System.Runtime.InteropServices.Marshal.Copy(bmd2.Scan0, b2bytes, 0, bytes2);

            var res = ContainsMatch(b1bytes, leftBitmap.Width, leftBitmap.Height, b2bytes, rightBitmap.Width, 
                rightBitmap.Height, leftBitmap.PixelFormat);

            leftBitmap.UnlockBits(bmd1);
            rightBitmap.UnlockBits(bmd2);

            return res;
        }

        private Point? ContainsMatch(byte[] source, int sourceLength, int sourceHeight, byte[] target, 
            int targetLength, int targetHeight, PixelFormat pixelFormat)
        {
            var counter = 0;

            var columnIterations = sourceLength > targetLength
                ? sourceLength - targetLength
                : 1;

            var rowIterations = sourceHeight > targetHeight
                ? sourceHeight - targetHeight
                : 1;

            var pos = 0;
            Point? point = null;

            for (int i = 0; i < columnIterations; i++) // source columns
            {
                for (int j = 0; j <= rowIterations; j++) // source rows
                {
                    pos = (i * BitmapColumnWidth(pixelFormat)) + (j * BitmapRowLength(sourceLength, pixelFormat));

                    for (int k = 0; k < targetHeight; k++) // target rows
                    {
                        var targetRowLength = BitmapRowLength(targetLength, pixelFormat);

                        if (ExactMatch(source.Slice(pos, pos + targetRowLength), 
                            GetBitmapRow(k, target, targetLength, pixelFormat)) == 0)
                            counter++;
                        else
                            break;

                        if (counter == targetHeight)
                            break;

                        pos += BitmapRowLength(sourceLength, pixelFormat);
                    }

                    if (counter == targetHeight)
                    {
                        point = new Point(i, j);
                        break;
                    }
                    counter = 0;
                }
                if (counter == targetHeight)
                    break;

                counter = 0;
            }

            if (point == null)
                return point;
            return
                CenterOfImage(point.Value, targetLength, targetHeight);
        }

        private int ExactMatch(byte[] b1bytes, byte[] b2bytes)
        {
            if (b1bytes.Length != b2bytes.Length)
                throw new ArgumentException("The bitmaps can't be compared by this method.");

            var counter = 0;

            for (int n = 0; n <= b1bytes.Length - 1; n++)
            {
                if (b1bytes[n] != b2bytes[n])
                {
                    counter++;
                }
            }

            return counter;
        }

        private Point CenterOfImage(Point point, int targetLength, int targetHeight)
        {
            return new Point(point.X + targetLength / 2, point.Y + targetHeight / 2);
        }

        private byte[] GetBitmapRow(int row, byte[] bitmap, int width, PixelFormat pixelFormat)
        {
            var remainder = (bitmap.Length / (System.Drawing.Image.GetPixelFormatSize(pixelFormat) / 8)) % width;

            if (remainder != 0)
                throw new ArgumentException("Invalid bitmap length.");

            byte[] result = bitmap
                .Slice(row * BitmapRowLength(width, pixelFormat), (row + 1) * BitmapRowLength(width, pixelFormat));

            return result;
        }

        private int BitmapRowLength(int length, PixelFormat pixelFormat)
        {
            var result = length * BitmapColumnWidth(pixelFormat);

            return result;
        }

        private int BitmapColumnWidth(PixelFormat pixelFormat)
        {
            var result = System.Drawing.Image.GetPixelFormatSize(pixelFormat) / 8;

            return result;
        }

        #endregion
    }
}
