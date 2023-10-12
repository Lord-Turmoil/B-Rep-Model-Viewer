// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace BRep.Model;

internal class BRepModelGroup
{
    public BRepModelGroup()
    {
        Models = new List<GeometryModel3D>();
        Transforms = new List<Transform3DGroup?>();
    }

    public BRepModelGroup(List<GeometryModel3D> models)
    {
        Models = models;
        Transforms = new List<Transform3DGroup?>();
        foreach (GeometryModel3D model in Models)
        {
            model.Transform ??= new Transform3DGroup();
            Transforms.Add((model.Transform as Transform3DGroup)?.Clone());
        }
    }

    public List<GeometryModel3D> Models { get; set; }
    public List<Transform3DGroup?> Transforms { get; set; }

    public void Add(GeometryModel3D model)
    {
        Models.Add(model);
        model.Transform ??= new Transform3DGroup();
        Transforms.Add((model.Transform as Transform3DGroup)?.Clone());
    }

    public int Count()
    {
        return Models.Count;
    }

    public void Accept(Action<GeometryModel3D> action)
    {
        foreach (GeometryModel3D model in Models)
        {
            action(model);
        }
    }

    public void ResetTransform()
    {
        for (int i = 0; i < Models.Count; i++)
        {
            Models[i].Transform = Transforms[i]?.Clone();
        }
    }
}