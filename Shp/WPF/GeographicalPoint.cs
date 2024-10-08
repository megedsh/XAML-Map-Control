﻿using System;
using System.Globalization;

namespace Shp.WPF
{
    public class GeographicalPoint
    {
        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public GeographicalPoint()
        {

        }

        public GeographicalPoint(double longitude, double latitude)
        {
            this.Longitude = longitude;
            this.Latitude = latitude;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} {1}", this.Longitude, this.Latitude);
        }
    }
}