// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace BRep.Model;

internal class Vertex
{
    public int Id { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public bool Verify()
    {
        return true;
    }
}

internal class Edge
{
    public Edge(int id, int v1, int v2)
    {
        Id = id;
        Vertices = new List<int> { v1, v2 };
    }

    public int Id { get; set; }

    // This list must contain exactly two elements.
    public List<int> Vertices { get; set; }

    public Edge Copy()
    {
        return new Edge(Id, Vertices[0], Vertices[1]);
    }

    public bool Verify()
    {
        return Vertices.Count == 2 && Vertices[0] != Vertices[1];
    }
}

internal class Face
{
    public int Id { get; set; }

    public List<int> Edges { get; set; } = new();

    public bool Verify()
    {
        return Edges.Count >= 3;
    }
}

internal class BRepModel
{
    public List<Vertex> Vertices { get; set; }
    public List<Edge> Edges { get; set; }
    public List<Face> Faces { get; set; }

    // Verify the model.
    public bool Verify()
    {
        if (Vertices.Any(vertex => !vertex.Verify()))
        {
            return false;
        }

        for (int i = 0; i < Vertices.Count; i++)
        {
            if (Vertices.All(vertex => vertex.Id != i))
            {
                return false;
            }
        }

        if (Edges.Any(edge => !edge.Verify()))
        {
            return false;
        }

        if (Edges.Any(edge => edge.Vertices.Any(vertex => vertex >= Vertices.Count)))
        {
            return false;
        }

        for (int i = 0; i < Edges.Count; i++)
        {
            if (Edges.All(edge => edge.Id != i))
            {
                return false;
            }
        }

        if (Faces.Any(face => !face.Verify()))
        {
            return false;
        }

        if (Faces.Any(face => face.Edges.Any(edge => edge >= Edges.Count)))
        {
            return false;
        }

        for (int i = 0; i < Faces.Count; i++)
        {
            if (Faces.All(face => face.Id != i))
            {
                return false;
            }
        }

        return true;
    }
}