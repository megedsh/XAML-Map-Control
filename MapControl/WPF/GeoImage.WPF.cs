﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2024 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapControl
{
    public partial class GeoImage
    {
        private static Task<GeoBitmap> ReadGeoTiffAsync(string sourcePath)
        {
            return Task.Run(() =>
            {
                var geoBitmap = new GeoBitmap();

                using (var stream = File.OpenRead(sourcePath))
                {
                    geoBitmap.Bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }

                var metadata = (BitmapMetadata)geoBitmap.Bitmap.Metadata;

                if (metadata.GetQuery(QueryString(ModelPixelScaleTag)) is double[] pixelScale &&
                    pixelScale.Length == 3 &&
                    metadata.GetQuery(QueryString(ModelTiePointTag)) is double[] tiePoint &&
                    tiePoint.Length >= 6)
                {
                    geoBitmap.Transform = new Matrix(pixelScale[0], 0d, 0d, -pixelScale[1], tiePoint[3], tiePoint[4]);
                }
                else if (metadata.GetQuery(QueryString(ModelTransformationTag)) is double[] transformValues &&
                         transformValues.Length == 16)
                {
                    geoBitmap.Transform = new Matrix(transformValues[0], transformValues[1],
                                                     transformValues[4], transformValues[5],
                                                     transformValues[3], transformValues[7]);
                }
                else
                {
                    throw new ArgumentException("No coordinate transformation found.");
                }

                if (metadata.GetQuery(QueryString(GeoKeyDirectoryTag)) is short[] geoKeyDirectory)
                {
                    geoBitmap.Projection = GetProjection(geoKeyDirectory);
                }

                if (metadata.GetQuery(QueryString(NoDataTag)) is string noData &&
                    int.TryParse(noData, out int noDataValue))
                {
                    geoBitmap.Bitmap = ConvertTransparentPixel(geoBitmap.Bitmap, noDataValue);
                }

                return geoBitmap;
            });
        }

        private static BitmapSource ConvertTransparentPixel(BitmapSource source, int transparentPixel)
        {
            BitmapPalette sourcePalette = null;
            var targetFormat = source.Format;

            if (source.Format == PixelFormats.Indexed8 ||
                source.Format == PixelFormats.Indexed4 ||
                source.Format == PixelFormats.Indexed2 ||
                source.Format == PixelFormats.Indexed1)
            {
                sourcePalette = source.Palette;
            }
            else if (source.Format == PixelFormats.Gray8)
            {
                sourcePalette = BitmapPalettes.Gray256;
                targetFormat = PixelFormats.Indexed8;
            }
            else if (source.Format == PixelFormats.Gray4)
            {
                sourcePalette = BitmapPalettes.Gray16;
                targetFormat = PixelFormats.Indexed4;
            }
            else if (source.Format == PixelFormats.Gray2)
            {
                sourcePalette = BitmapPalettes.Gray4;
                targetFormat = PixelFormats.Indexed2;
            }
            else if (source.Format == PixelFormats.BlackWhite)
            {
                sourcePalette = BitmapPalettes.BlackAndWhite;
                targetFormat = PixelFormats.Indexed1;
            }

            if (sourcePalette == null || transparentPixel >= sourcePalette.Colors.Count)
            {
                return source;
            }

            var colors = sourcePalette.Colors.ToList();

            colors[transparentPixel] = Colors.Transparent;

            var stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            var buffer = new byte[stride * source.PixelHeight];

            source.CopyPixels(buffer, stride, 0);

            var target = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY,
                targetFormat, new BitmapPalette(colors), buffer, stride);

            target.Freeze();

            return target;
        }
    }
}
