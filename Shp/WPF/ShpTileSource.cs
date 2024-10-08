// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2024 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using MapControl;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;

namespace Shp.WPF
{
    public class ShpTileSource : TileSource, IDisposable
    {
        private ISpatialIndex<Feature> m_spatialIndex;
        private bool m_initialized;
        private int m_minZoom;
        private int m_maxZoom;
        public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        public Task OpenAsync(string file, int minZoom, int maxZoom)
        {
            m_minZoom = minZoom;
            m_maxZoom = maxZoom;
            Close();
            if (string.IsNullOrEmpty(file))
            {
                throw new InvalidOperationException("file property is null or empty");
            }

            if (!File.Exists(file))
            {
                if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,file)))
                {
                    file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                }
                else
                {
                    throw new InvalidOperationException($"file {file} not found");
                }
            }

            return Task.Run(() =>
            {
                ShpIndexer indexer = new ShpIndexer(new FileInfo(file).FullName);
                m_spatialIndex = indexer.Index(out Feature[] _);
                m_initialized = true;
            });
        }

        public void Close()
        {
            m_initialized = false;
            m_spatialIndex = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        public async Task<byte[]> ReadImageBufferAsync(int x, int y, int zoomLevel)
        {
            var tileAsync = await  getTileAsync(x, y, zoomLevel);
            return null;
        }

        private async Task<byte[]> getTileAsync(int x, int y, int z)
        {
            if (!m_initialized)
            {
                return null;
            }

            if (z < m_minZoom ||
                z > m_maxZoom ||
                x < 0 ||
                y < 0 ||
                x > TileCount(z) ||
                y > TileCount(z))
            {
                return null;
            }

            byte[] tileSvg = null;
            try
            {

                tileSvg = await buildTile(x, y, z);
            }
            catch (Exception ex)
            {

            }

            return tileSvg;
        }

        private async Task<byte[]> buildTile(int x, int y, int z)
        {
            if (m_spatialIndex == null)
            {
                return null;
            }


            NetTopologySuite.IO.VectorTiles.Tiles.Tile tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(x, y, z);

            VectorTile vt = new VectorTile { TileId = tileDefinition.Id };

            Layer lyr = new Layer { Name = "layer1" };

            vt.Layers.Add(lyr);

            GeographicalBounds tileBounds = WebMercator.GetTileGeographicalBounds(x, y, z);
            double minLat = tileBounds.MinLatitude;
            double minLong = tileBounds.MinLongitude;

            double maxLat = tileBounds.MaxLatitude;
            double maxLong = tileBounds.MaxLongitude;


            Coordinate[] coordinates =
            {
                new Coordinate(minLong, minLat),
                new Coordinate(maxLong, minLat),
                new Coordinate(maxLong, maxLat),
                new Coordinate(minLong, maxLat),
                new Coordinate(minLong, minLat),
            };

            Polygon p = new Polygon(new LinearRing(coordinates));

            //string asText = p.AsText();

            Envelope e = p.EnvelopeInternal;

            return await Task.Run(() =>
            {
                IntersectVisitor intersectVisitor = new IntersectVisitor(p);
                m_spatialIndex.Query(e, intersectVisitor);

                foreach (Feature feature in intersectVisitor.Intersects)
                {
                    lyr.Features.Add(feature);
                }

                byte[] result;
                MemoryStream fs;
                using (fs = new MemoryStream(1024 * 32))
                {
                    vt.Write(fs, 1U, 2U);
                    result = fs.ToArray();
                }

                return result;
            });
        }

        private int TileCount(int zoom)
        {
            return WebMercator.TileCount(zoom);
        }

        public override async Task<ImageSource> LoadImageAsync(int x, int y, int zoomLevel)
        {
            ImageSource image = null;

            try
            {
                var buffer = await ReadImageBufferAsync(x, y, zoomLevel);

                if (buffer != null)
                {
                    image = await ImageLoader.LoadImageAsync(buffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(ShpTileSource)}: {ex.Message}");
            }

            return image;
        }
    }
}