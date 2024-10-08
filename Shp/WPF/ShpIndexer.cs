using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Esri;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;

namespace Shp.WPF
{
    public class ShpIndexer
    {
        private readonly string m_shpPath;

        public ShpIndexer(string shpPath) => m_shpPath = shpPath;

        public ISpatialIndex<Feature> Index(out Feature[] features)
        {
            STRtree<Feature> tree = new STRtree<Feature>();

            features = Shapefile.ReadAllFeatures(m_shpPath, new ShapefileReaderOptions { GeometryBuilderMode = GeometryBuilderMode.IgnoreInvalidShapes });
            
            foreach (Feature feature in features)
            {
                tree.Insert(feature.BoundingBox, feature);
            }
            tree.Build();
            return tree;
        }

        private List<Geometry> getAllBounds(GeometryFactory geomf, AbstractNode<Envelope, Feature> node)
        {
            List<Geometry> result = new List<Geometry>();
            result.Add(geomf.ToGeometry(node.Bounds));
            foreach (IBoundable<Envelope, Feature> child in node.ChildBoundables)
            {
                if (child is AbstractNode<Envelope, Feature> ac)
                {
                    result.AddRange(getAllBounds(geomf, ac));
                }
            }

            return result;
        }
    }
}