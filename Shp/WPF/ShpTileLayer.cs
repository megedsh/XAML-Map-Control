// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2024 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)


using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using MapControl;
using DependencyPropertyHelper = MapControl.DependencyPropertyHelper;

namespace Shp.WPF
{
    /// <summary>
    /// MapTileLayer that uses an MBTiles SQLite Database. See https://wiki.openstreetmap.org/wiki/MBTiles.
    /// </summary>
    public class ShpTileLayer : MapTileLayer
    {
        public static readonly DependencyProperty FileProperty =
            DependencyPropertyHelper.Register<ShpTileLayer, string>(nameof(File), null,
                async (layer, oldValue, newValue) => await layer.filePropertyChanged(newValue));

        public string File
        {
            get => (string)GetValue(FileProperty);
            set => SetValue(FileProperty, value);
        }

        /// <summary>
        /// May be overridden to create a derived MBTileSource that handles other tile formats than png and jpg.
        /// </summary>
        protected virtual async Task<ShpTileSource> CreateTileSourceAsync(string file)
        {
            MinZoomLevel = 0;
            MaxZoomLevel = 20;
            var tileSource = new ShpTileSource();
            await tileSource.OpenAsync(file, MinZoomLevel, MaxZoomLevel);

            if (tileSource.Metadata.TryGetValue("format", out string format) && format != "png" && format != "jpg")
            {
                tileSource.Dispose();

                throw new NotSupportedException($"Tile image format {format} is not supported.");
            }

            return tileSource;
        }

        private async Task filePropertyChanged(string file)
        {
            (TileSource as ShpTileSource)?.Close();

            ClearValue(TileSourceProperty);
            ClearValue(SourceNameProperty);
            ClearValue(DescriptionProperty);
            ClearValue(MinZoomLevelProperty);
            ClearValue(MaxZoomLevelProperty);

            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    var tileSource = await CreateTileSourceAsync(file);

                    TileSource = tileSource;

                    SourceName = file;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{nameof(ShpTileLayer)}: {ex.Message}");
                }
            }
        }
    }
}
