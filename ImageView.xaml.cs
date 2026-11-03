using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkiaSharp;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace ImageViewer
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : UserControl
    {
        private VMS.TPS.Common.Model.API.Image _image;
        private int _sliceNumber;
        private VMS.TPS.Common.Model.API.StructureSet _structureSet;

        //Constructor =>What information do I need from ESAPI to produce this image.
        public ImageView(VMS.TPS.Common.Model.API.StructureSet structureSet)
        {
            InitializeComponent();
            //Set a couple of variables to allow access to my image and slice number in other methods.
            _image = structureSet.Image;
            _sliceNumber = _image.ZSize / 2;
            _structureSet = structureSet;
            GenerateImage();
        }

        private unsafe void GenerateImage()
        {
            int width = _image.XSize;
            int height = _image.YSize;
            int totalPixels = width * height;

            // Get voxel data from ESAPI
            int[,] pixels = new int[height, width];
            _image.GetVoxels(_sliceNumber, pixels);

            // Find min/max using pointer arithmetic over the 2D array's contiguous memory
            int drr_min = int.MaxValue;
            int drr_max = int.MinValue;
            fixed (int* pPixels = pixels)
            {
                int* p = pPixels;
                for (int i = 0; i < totalPixels; i++, p++)
                {
                    if (*p < drr_min) drr_min = *p;
                    if (*p > drr_max) drr_max = *p;
                }
            }

            double range = drr_max - drr_min;

            // Create a Gray8 SKBitmap and write normalized voxel values directly into its native pixel buffer
            using (var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque))
            {
                byte* pDst = (byte*)bitmap.GetPixels().ToPointer();
                fixed (int* pPixels = pixels)
                {
                    int* pSrc = pPixels;
                    for (int i = 0; i < totalPixels; i++, pSrc++, pDst++)
                        *pDst = (byte)(255 * (*pSrc - drr_min) / range);
                }

                // Rotate 90 degrees via an SKCanvas onto a new BGRA bitmap with swapped dimensions.
                // BGRA8888 lets us draw colored contours on top of the grayscale background.
                using (var rotated = new SKBitmap(height, width, SKColorType.Bgra8888, SKAlphaType.Premul))
                using (var canvas = new SKCanvas(rotated))
                {
                    // Apply rotation transform — every subsequent draw shares this coordinate space,
                    // so contour points expressed in source image pixel coordinates are rotated automatically.
                    canvas.Translate(height, 0);
                    canvas.RotateDegrees(90);

                    // Draw the grayscale background
                    canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);

                    // Draw structure contours
                    using (var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f })
                    {
                        foreach (var structure in _structureSet.Structures)
                        {
                            VVector[][] contours = structure.GetContoursOnImagePlane(_sliceNumber);
                            if (contours == null || contours.Length == 0) continue;

                            var c = structure.Color;
                            paint.Color = new SKColor(c.R, c.G, c.B);

                            foreach (var contour in contours)
                            {
                                if (contour == null || contour.Length < 2) continue;

                                var builder = new SKPathBuilder();
                                builder.MoveTo(
                                    
                                    (float)((contour[0].y - _image.Origin.y) / _image.YRes),
                                    (float)((contour[0].x - _image.Origin.x) / _image.XRes));

                                for (int i = 1; i < contour.Length; i++)
                                    builder.LineTo(
                                        
                                        (float)((contour[i].y - _image.Origin.y) / _image.YRes),
                                        (float)((contour[i].x - _image.Origin.x) / _image.XRes));

                                builder.Close();
                                using (var path = builder.Detach())
                                    canvas.DrawPath(path, paint);
                            }
                        }
                    }

                    // Pass the native pixel pointer directly to BitmapSource — zero extra copies.
                    // DPI axes are swapped to match the rotated dimensions (new width = original Y, new height = original X).
                    TransverseImage.Source = BitmapSource.Create(
                        rotated.Width, rotated.Height,
                        25.4 / _image.YRes, 25.4 / _image.XRes,
                        PixelFormats.Pbgra32, null,
                        rotated.GetPixels(), rotated.RowBytes * rotated.Height,
                        rotated.RowBytes);
                }
            }
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            _sliceNumber--;
            //what happens if the slice number is already 0? can't have -1
            if (_sliceNumber < 0)
            {
                _sliceNumber = _image.ZSize - 1;
            }
            GenerateImage();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _sliceNumber++;
            if(_sliceNumber>= _image.ZSize)
            {
                _sliceNumber = 0;
            }
            GenerateImage();
        }
    }
}
