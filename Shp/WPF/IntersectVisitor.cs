using System;
using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index;

namespace Shp.WPF
{
    public class IntersectVisitor : IItemVisitor<Feature>
    {
        private readonly Geometry m_testPolygon;
        private List<Feature> m_intersect = new List<Feature>();
        private List<Feature> m_candidates = new List<Feature>();
        public IntersectVisitor(Geometry testPolygon)
        {
            m_testPolygon = testPolygon;
        }

        public List<Feature> Candidates => m_candidates;
        public List<Feature> Intersects => m_intersect;

        public void VisitItem(Feature item)
        {
            m_candidates.Add(item);
            try
            {
                if (item.Geometry is MultiPolygon mp)
                {
                    foreach (Geometry geometry in mp)
                    {
                        if (!geometry.Intersects(m_testPolygon))
                        {
                            continue;
                        }

                        m_intersect.Add(item);
                        break;
                    }
                }
                else
                {
                    if (item.Geometry.Intersects(m_testPolygon))
                    {
                        m_intersect.Add(item);
                    }
                }


            }
            catch (Exception e)
            {

            }

        }
    }
}