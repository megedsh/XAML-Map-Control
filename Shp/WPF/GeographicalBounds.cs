using System;
using System.Globalization;

namespace Shp.WPF
{
    /// <summary>
    ///     Represents bounds in geographical (longitude-latitude) coordinates.
    /// </summary>
    public class GeographicalBounds
    {
        private readonly GeographicalPoint pointMin;

        private readonly GeographicalPoint pointMax;

        public GeographicalBounds(GeographicalPoint pointMin, GeographicalPoint pointMax)
        {
            this.pointMin = pointMin;
            this.pointMax = pointMax;
        }

        public GeographicalBounds(double minLongitude, double minLatitude, double maxLongitude, double maxLatitude)
        {
            pointMin = new GeographicalPoint(minLongitude, minLatitude);
            pointMax = new GeographicalPoint(maxLongitude, maxLatitude);
        }

        /// <summary>
        ///     Creates <see cref="GeographicalBounds" /> from string.
        /// </summary>
        /// <param name="s">OpenLayers Bounds format: left, bottom, right, top), string of comma-separated numbers.</param>
        /// <returns></returns>
        public static GeographicalBounds FromCommaSeparatedString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var items = s.Split(',');
            if (items.Length != 4)
            {
                throw new FormatException("String should contain 4 comma-separated values");
            }

            return new GeographicalBounds(
                new GeographicalPoint(
                    Double.Parse(items[0], CultureInfo.InvariantCulture),
                    Double.Parse(items[1], CultureInfo.InvariantCulture)),
                new GeographicalPoint(
                    Double.Parse(items[2], CultureInfo.InvariantCulture),
                    Double.Parse(items[3], CultureInfo.InvariantCulture))
            );
        }


        public static string ToCommaSeparatedString(GeographicalBounds bounds)
        {
            if (bounds == null)
            {
                return null;
            }

            return string.Join(",", bounds.Min.Longitude,
                bounds.Min.Latitude,
                bounds.Max.Longitude,
                bounds.Max.Latitude);
        }

        public GeographicalPoint Min
        {
            get { return pointMin; }
        }

        public GeographicalPoint Max
        {
            get { return pointMax; }
        }

        public double MinLongitude
        {
            get { return pointMin.Longitude; }
        }

        public double MinLatitude
        {
            get { return pointMin.Latitude; }
        }

        public double MaxLongitude
        {
            get { return pointMax.Longitude; }
        }

        public double MaxLatitude
        {
            get { return pointMax.Latitude; }
        }
    }
}