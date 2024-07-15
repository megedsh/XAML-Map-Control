﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2024 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MapControl
{
    /// <summary>
    /// Base class of MapPolyline, MapPolygon and MapMultiPolygon.
    /// </summary>
    public class MapPolypoint : MapPath, IWeakEventListener
    {
        public static readonly DependencyProperty FillRuleProperty =
            DependencyPropertyHelper.Register<MapPolygon, FillRule>(nameof(FillRule), FillRule.EvenOdd,
                (polypoint, oldValue, newValue) => ((StreamGeometry)polypoint.Data).FillRule = newValue);

        public FillRule FillRule
        {
            get => (FillRule)GetValue(FillRuleProperty);
            set => SetValue(FillRuleProperty, value);
        }

        protected MapPolypoint()
        {
            Data = new StreamGeometry();
        }

        protected void DataCollectionPropertyChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                CollectionChangedEventManager.RemoveListener(oldCollection, this);
            }

            if (newValue is INotifyCollectionChanged newCollection)
            {
                CollectionChangedEventManager.AddListener(newCollection, this);
            }

            UpdateData();
            InvalidateVisual(); // necessary for StreamGeometry
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            UpdateData();
            InvalidateVisual(); // necessary for StreamGeometry

            return true;
        }

        protected void UpdateData(IEnumerable<Location> locations, bool closed)
        {
            using (var context = ((StreamGeometry)Data).Open())
            {
                if (ParentMap != null && locations != null)
                {
                    var longitudeOffset = GetLongitudeOffset(Location ?? locations.FirstOrDefault());

                    AddPolylinePoints(context, locations, longitudeOffset, closed);
                }
            }
        }

        protected void UpdateData(IEnumerable<IEnumerable<Location>> polygons)
        {
            using (var context = ((StreamGeometry)Data).Open())
            {
                if (ParentMap != null && polygons != null)
                {
                    var longitudeOffset = GetLongitudeOffset(Location);

                    foreach (var polygon in polygons)
                    {
                        AddPolylinePoints(context, polygon, longitudeOffset, true);
                    }
                }
            }
        }

        private void AddPolylinePoints(StreamGeometryContext context, IEnumerable<Location> locations, double longitudeOffset, bool closed)
        {
            var points = locations
                .Select(location => LocationToView(location, longitudeOffset))
                .Where(point => point.HasValue)
                .Select(point => point.Value);

            if (points.Any())
            {
                context.BeginFigure(points.First(), true, closed);
                context.PolyLineTo(points.Skip(1).ToList(), true, true);
            }
        }
    }
}
