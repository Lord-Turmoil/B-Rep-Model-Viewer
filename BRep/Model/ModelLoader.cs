// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Newtonsoft.Json;

namespace BRep.Model;

// Load model from file.
internal abstract class ModelLoader
{
    private static readonly List<Color> ColorSet = new()
    {
        Color.FromArgb(96, 244, 67, 54),
        Color.FromArgb(96, 233, 30, 99),
        Color.FromArgb(96, 156, 39, 176),
        Color.FromArgb(96, 103, 58, 183),
        Color.FromArgb(96, 63, 81, 181),
        Color.FromArgb(96, 33, 150, 243),
        Color.FromArgb(96, 33, 150, 243),
        Color.FromArgb(96, 3, 169, 244),
        Color.FromArgb(96, 0, 188, 212),
        Color.FromArgb(96, 0, 150, 136),
        Color.FromArgb(96, 76, 175, 80),
        Color.FromArgb(96, 139, 195, 74),
        Color.FromArgb(96, 205, 220, 57),
        Color.FromArgb(96, 255, 235, 59),
        Color.FromArgb(96, 255, 193, 7),
        Color.FromArgb(96, 255, 152, 0),
        Color.FromArgb(96, 255, 87, 34)
    };

    protected Point3D _geometricCenter;
    protected Point3D[] _vertices;

    public BRepModel Model { get; private set; }

    protected static IEnumerable<Color> SelectRandomColors(int n)
    {
        if (n <= ColorSet.Count)
        {
            Random random = new();
            return ColorSet.OrderBy(x => random.Next()).Take(n);
        }

        IEnumerable<Color> colors = new List<Color>();
        while (n > 0)
        {
            colors = colors.Concat(SelectRandomColors(Math.Min(ColorSet.Count, n)));
            n -= ColorSet.Count;
        }

        return colors;
    }

    public BRepModelGroup LoadFromJson(string filename)
    {
        // First load the file.
        string json;
        try
        {
            using StreamReader reader = new(filename);
            json = reader.ReadToEnd();
        }
        catch
        {
            throw new ModelException($"Failed to load model from file: {filename}");
        }

        // Then get the model.
        var model = JsonConvert.DeserializeObject<BRepModel>(json);
        if (model == null)
        {
            throw new ModelException("Unrecognized data");
        }

        if (!model.Verify())
        {
            throw new ModelException("Invalid model");
        }

        // Finally construct the model.
        return LoadFromModel(model);
    }

    public BRepModelGroup LoadFromModel(BRepModel model)
    {
        Model = model;
        _vertices = Model.Vertices.Select(vertex => new Point3D(vertex.X, vertex.Y, vertex.Z)).ToArray();
        BRepModelGroup modelGroup = BuildModel();
        return modelGroup;
    }

    // Build a model from a BRepModel.
    protected abstract BRepModelGroup BuildModel();


    protected static MaterialGroup BuildFrontMaterial(Color color)
    {
        var diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(color));
        var specularMaterial = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)), 30.0);
        var material = new MaterialGroup();
        material.Children.Add(diffuseMaterial);
        material.Children.Add(specularMaterial);
        return material;
    }

    protected static Material BuildBackMaterial(Color color)
    {
        var darkColor = new Color
        {
            A = (byte)Math.Min(color.A * 1.1, 255),
            R = (byte)(color.R * 0.9),
            G = (byte)(color.G * 0.9),
            B = (byte)(color.B * 0.9)
        };

        return new DiffuseMaterial(new SolidColorBrush(darkColor));
    }

    protected static Point3D GetGeometricCenter(IReadOnlyCollection<Vertex> vertices)
    {
        double x = vertices.Sum(vertex => vertex.X) / vertices.Count;
        double y = vertices.Sum(vertex => vertex.Y) / vertices.Count;
        double z = vertices.Sum(vertex => vertex.Z) / vertices.Count;
        return new Point3D(x, y, z);
    }

    // Use first two edges to get normal of a plane.
    protected static Vector3D GetTriangleNormal(IReadOnlyList<Edge> edges, IReadOnlyList<Point3D> vertices)
    {
        Vector3D v1 = new(
            vertices[edges[0].Vertices[0]].X,
            vertices[edges[0].Vertices[0]].Y,
            vertices[edges[0].Vertices[0]].Z);
        Vector3D v2 = new(
            vertices[edges[0].Vertices[1]].X,
            vertices[edges[0].Vertices[1]].Y,
            vertices[edges[0].Vertices[1]].Z);
        Vector3D v3 = new(
            vertices[edges[1].Vertices[1]].X,
            vertices[edges[1].Vertices[1]].Y,
            vertices[edges[1].Vertices[1]].Z);

        return Vector3D.CrossProduct(v2 - v1, v3 - v2);
    }

    protected static List<Edge> FormatEdges(List<Edge> edges)
    {
        var ring = new List<Edge>
        {
            edges[0].Copy()
        };

        while (ring.Count < edges.Count)
        {
            Edge pivot = ring.Last();

            // Select one edge and add it to the ring.
            Edge? edge = edges.Find(e => ring.All(x => x.Id != e.Id) && e.Vertices.Contains(pivot.Vertices[1]));
            if (edge == null)
            {
                throw new ModelException("Edges of face not connected");
            }

            ring.Add(edge.Vertices[0] == pivot.Vertices[1]
                ? edge.Copy()
                : new Edge(edge.Id, edge.Vertices[1], edge.Vertices[0]));
        }

        // At last, check the head and tail.
        if (ring.First().Vertices[0] != ring.Last().Vertices[1])
        {
            throw new ModelException("Edges of face not connected");
        }

        return ring;
    }

    // Example comes from:
    // https://xoax.net/blog/rendering-transparent-3d-surfaces-in-wpf-with-c/
    // Modifications made.
    public static BRepModelGroup LoadDefault()
    {
        // Define the geometry
        const double kdSqrt2 = 1.4142135623730950488016887242097;
        const double kdSqrt6 = 2.4494897427831780981972840747059;
        // Create a collection of vertex positions
        var qaV = new Point3D[]
        {
            new(0.0, 1.0, 0.0),
            new(2.0 * kdSqrt2 / 3.0, -1.0 / 3.0, 0.0),
            new(-kdSqrt2 / 3.0, -1.0 / 3.0, -kdSqrt6 / 3.0),
            new(-kdSqrt2 / 3.0, -1.0 / 3.0, kdSqrt6 / 3.0)
        };
        var qPoints = new Point3DCollection();
        // Designate Vertices
        // My Scheme (0, 1, 2), (1, 0, 3), (2, 3, 0), (3, 2, 1)
        for (int i = 0; i < 12; ++i)
        {
            if (i / 3 % 2 == 0)
            {
                qPoints.Add(qaV[i % 4]);
            }
            else
            {
                qPoints.Add(qaV[i * 3 % 4]);
            }
        }

        // Designate Triangles
        var qTriangles = new Int32Collection();
        for (int i = 0; i < 12; ++i)
        {
            qTriangles.Add(i);
        }

        var qBackTriangles = new Int32Collection();
        // Designate Back Triangles in the opposite orientation
        for (int i = 0; i < 12; ++i)
        {
            qBackTriangles.Add(3 * (i / 3) + 2 * (i % 3) % 3);
        }
        // 0, 2, 3, 5, 6, 

        // Inner Tetrahedron: Define the mesh, material and transformation.
        var qFrontMesh = new MeshGeometry3D();
        qFrontMesh.Positions = qPoints;
        qFrontMesh.TriangleIndices = qTriangles;
        var qInnerGeometry = new GeometryModel3D();
        qInnerGeometry.Geometry = qFrontMesh;
        // *** Material ***
        var qDiffGreen =
            new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 0, 128, 0)));
        var qSpecWhite = new
            SpecularMaterial(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), 30.0);
        var qInnerMaterial = new MaterialGroup();
        qInnerMaterial.Children.Add(qDiffGreen);
        qInnerMaterial.Children.Add(qSpecWhite);
        qInnerGeometry.Material = qInnerMaterial;
        // *** Transformation ***
        var qScale = new ScaleTransform3D(new Vector3D(.5, .5, .5));
        var innerTransformGroup = new Transform3DGroup();
        innerTransformGroup.Children.Add(qScale);
        qInnerGeometry.Transform = innerTransformGroup;

        // Outer Tetrahedron (semi-transparent) : Define the mesh, material and transformation.
        var qOuterGeometry = new GeometryModel3D();
        qOuterGeometry.Geometry = qFrontMesh;
        // *** Material ***
        var qDiffTransYellow =
            new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(64, 255, 255, 0)));
        var qSpecTransWhite =
            new SpecularMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)), 30.0);
        var qDiffBrown =
            new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 200, 175, 0)));
        var qOuterMaterial = new MaterialGroup();
        qOuterMaterial.Children.Add(qDiffTransYellow);
        qOuterMaterial.Children.Add(qSpecTransWhite);
        qOuterGeometry.Material = qOuterMaterial;
        qOuterGeometry.BackMaterial = qDiffBrown;
        // *** Transformation ***
        var outerTransformGroup = new Transform3DGroup();
        qOuterGeometry.Transform = outerTransformGroup;

        return new BRepModelGroup(new List<GeometryModel3D>
        {
            qInnerGeometry,
            qOuterGeometry
        });
    }
}