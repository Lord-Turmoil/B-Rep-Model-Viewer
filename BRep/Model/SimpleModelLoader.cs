// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BRep.Model;

internal class SimpleModelLoader : ModelLoader
{
    protected override BRepModelGroup BuildModel()
    {
        _geometricCenter = GetGeometricCenter(Model.Vertices);

        // Build faces.
        var vertices = new Point3DCollection();
        var indices = new List<int>();
        foreach (Face face in Model.Faces)
        {
            int count = vertices.Count;

            BuildFace(face.Id, out Point3DCollection faceVertices, out List<int> faceIndices);
            foreach (Point3D vertex in faceVertices)
            {
                vertices.Add(vertex);
            }

            foreach (int index in faceIndices)
            {
                indices.Add(count + index);
            }
        }

        // Build triangles.
        var mesh = new MeshGeometry3D
        {
            Positions = vertices,
            TriangleIndices = new Int32Collection(indices)
        };

        // Build model.
        Color color = SelectRandomColors(1).ElementAt(0);
        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = BuildFrontMaterial(color),
            BackMaterial = BuildBackMaterial(color),
            Transform = new Transform3DGroup()
        };

        return new BRepModelGroup(new List<GeometryModel3D> { model });
    }

    private void BuildFace(int faceId, out Point3DCollection vertices, out List<int> indices)
    {
        // 1, 2
        // 2, 3
        // 3, 1
        // Try to make edges a ring.
        List<Edge> edges = FormatEdges(
            Model.Faces[faceId].Edges.Select(edgeId => Model.Edges[edgeId]).ToList());

        // It seems we have to explicitly add all points
        vertices = new Point3DCollection();
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
                Model.Vertices[edges[i].Vertices[1]].X,
                Model.Vertices[edges[i].Vertices[1]].Y,
                Model.Vertices[edges[i].Vertices[1]].Z));
        }

        // Build indices.
        // Check normal direction.
        Vector3D normal = GetTriangleNormal(edges, _vertices);
        bool flag = Vector3D.DotProduct(
            normal,
            _geometricCenter - _vertices[edges[0].Vertices.ElementAt(0)]) < 0;

        indices = new List<int>();
        // Add all triangles
        // n edges -> n - 2 triangles
        for (int i = 0; i < edges.Count - 2; i++)
        {
            if (flag)
            {
                indices.Add(i * 3);
                indices.Add(i * 3 + 1);
                indices.Add(i * 3 + 2);
            }
            else
            {
                indices.Add(i * 3);
                indices.Add(i * 3 + 2);
                indices.Add(i * 3 + 1);
            }
        }
    }
}