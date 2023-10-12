// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BRep.Model;

internal class ColorfulModelLoader : ModelLoader
{
    protected override BRepModelGroup BuildModel()
    {
        var group = new BRepModelGroup();
        List<Color> colors = SelectRandomColors(Model.Faces.Count).ToList();

        // First get geometric center.
        _geometricCenter = GetGeometricCenter(Model.Vertices);

        // Build faces.
        foreach (Face face in Model.Faces)
        {
            GeometryModel3D model = BuildFace(face.Id);
            // Add material
            model.Material = BuildFrontMaterial(colors.ElementAt(face.Id));
            model.BackMaterial = BuildBackMaterial(colors.ElementAt(face.Id));
            group.Add(model);
        }

        if (group.Count() == 0)
        {
            throw new ModelException("No model can be built");
        }

        return group;
    }

    // Build a face from a BRepModel.
    // Be careful about the direction of the normal of the face, since we 
    // do not draw the complete model, the 2D plane cannot be seen from its
    // back.
    // This is a compromised solution. Since we still, cannot make transparent
    // model in this way. But at least we can have light effect. :)
    private GeometryModel3D BuildFace(int faceId)
    {
        // 1, 2
        // 2, 3
        // 3, 1
        // Try to make edges a ring.
        List<Edge> edges = FormatEdges(
            Model.Faces[faceId].Edges.Select(edgeId => Model.Edges[edgeId]).ToList());

        // It seems we have to explicitly add all points
        var vertices = new Point3DCollection();
        for (int i = 1; i < edges.Count - 1; i++)
        {
            vertices.Add(new Point3D(
                Model.Vertices[edges[0].Vertices[0]].X,
                Model.Vertices[edges[0].Vertices[0]].Y,
                Model.Vertices[edges[0].Vertices[0]].Z));
            vertices.Add(new Point3D(
                Model.Vertices[edges[i].Vertices[0]].X,
                Model.Vertices[edges[i].Vertices[0]].Y,
                Model.Vertices[edges[i].Vertices[0]].Z));
            vertices.Add(new Point3D(
                Model.Vertices[edges[i + 1].Vertices[0]].X,
                Model.Vertices[edges[i + 1].Vertices[0]].Y,
                Model.Vertices[edges[i + 1].Vertices[0]].Z));
        }

        // Create a mesh.
        var mesh = new MeshGeometry3D
        {
            // Add all points.
            Positions = vertices
        };

        // Check normal direction.
        Vector3D normal = GetTriangleNormal(edges, _vertices);
        bool flag = Vector3D.DotProduct(
            normal,
            _geometricCenter - _vertices[edges[0].Vertices.ElementAt(0)]) < 0;

        // Add all triangles
        // n edges -> n - 2 triangles
        for (int i = 0; i < edges.Count - 2; i++)
        {
            if (flag)
            {
                mesh.TriangleIndices.Add(i * 3);
                mesh.TriangleIndices.Add(i * 3 + 1);
                mesh.TriangleIndices.Add(i * 3 + 2);
            }
            else
            {
                mesh.TriangleIndices.Add(i * 3);
                mesh.TriangleIndices.Add(i * 3 + 2);
                mesh.TriangleIndices.Add(i * 3 + 1);
            }
        }

        return new GeometryModel3D
        {
            Geometry = mesh,
            Transform = new Transform3DGroup()
        };
    }
}