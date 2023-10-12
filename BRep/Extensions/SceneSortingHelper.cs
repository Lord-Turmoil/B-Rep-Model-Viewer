// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace BRep.Extensions;

// Reference
// https://stackoverflow.com/questions/48924505/how-to-handle-diffuse-material-transparency-in-wpf-3d
internal static class SceneSortingHelper
{
    /// <summary>
    ///     Sort Modelgroups in farthest to closest order, to enable transparency
    ///     Should be applied whenever the scene is significantly re-oriented
    /// </summary>
    public static void AlphaSort(Point3D cameraPosition, List<GeometryModel3D> models, Transform3D worldTransform)
    {
        var list = new ArrayList();
        int i = 0;
        foreach (GeometryModel3D model in models)
        {
            Point3D location;
            if (model.Geometry is MeshGeometry3D mesh)
            {
                location = worldTransform.TransformBounds(
                    model.Transform.TransformBounds(
                        mesh.Bounds
                    )
                ).Location;
            }
            else
            {
                location = worldTransform.TransformBounds(
                    model.Transform.TransformBounds(
                        model.Transform.TransformBounds(
                            model.Bounds
                        )
                    )
                ).Location;
            }

            double distance = Point3D.Subtract(cameraPosition, location).Length;
            list.Add(new GeometryModel3DDistance(distance, model, i++));
        }

        list.Sort(new DistanceComparer(SortDirection.FarToNear));
        models.Clear();
        foreach (GeometryModel3DDistance d in list)
        {
            models.Add(d.Model);
        }
    }

    private class GeometryModel3DDistance
    {
        public readonly double Distance;
        public readonly GeometryModel3D Model;

        // To achieve stable sort.
        public readonly int SortIndex;

        public GeometryModel3DDistance(double distance, GeometryModel3D model, int sortIndex)
        {
            Distance = distance;
            Model = model;
            SortIndex = sortIndex;
        }
    }

    private enum SortDirection
    {
        NearToFar,
        FarToNear
    }

    private class DistanceComparer : IComparer
    {
        private readonly SortDirection _sortDirection;

        public DistanceComparer(SortDirection sortDirection)
        {
            _sortDirection = sortDirection;
        }

        int IComparer.Compare(object? lhs, object? rhs)
        {
            if (lhs == null || rhs == null)
            {
                return 0;
            }

            var left = (GeometryModel3DDistance)lhs;
            var right = (GeometryModel3DDistance)rhs;

            if (_sortDirection == SortDirection.FarToNear)
            {
                if (left.Distance > right.Distance)
                {
                    return -1;
                }

                if (left.Distance < right.Distance)
                {
                    return 1;
                }

                return right.SortIndex.CompareTo(left.SortIndex);
            }

            if (left.Distance > right.Distance)
            {
                return 1;
            }

            if (left.Distance < right.Distance)
            {
                return -1;
            }

            return left.SortIndex.CompareTo(right.SortIndex);
        }
    }
}