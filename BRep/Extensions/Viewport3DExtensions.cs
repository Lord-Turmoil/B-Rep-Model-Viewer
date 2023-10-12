// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace BRep.Extensions;

internal static class Model3DGroupExtensions
{
    public static void AlphaSortModels(this Model3DGroup model3DGroup, ProjectionCamera camera)
    {
        var models = new List<GeometryModel3D>();
        var backup = new List<Model3D>();
        foreach (Model3D model in model3DGroup.Children)
        {
            if (model is GeometryModel3D m)
            {
                models.Add(m);
            }
            else
            {
                backup.Add(model);
            }
        }

        // note:
        //  the following method works well most of the time but sometimes the sort is wrong as the sort is simply based on model bounds,
        //  to get rid of artifacts we would need something like binary space partitioning
        SceneSortingHelper.AlphaSort(
            camera.Position,
            models,
            new Transform3DGroup()
        );

        model3DGroup.Children.Clear();
        foreach (Model3D model in backup)
        {
            model3DGroup.Children.Add(model);
        }

        foreach (GeometryModel3D c in models)
        {
            model3DGroup.Children.Add(c);
        }
    }
}