using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using SharpDX;
public static class HelixUtil
{
    public static Triple ToTriple(this Point3D p)
    {
        return new Triple(p.X, p.Y, p.Z);
    }

    public static Triple ToTriple(this Vector3D v)
    {
        return new Triple(v.X, v.Y, v.Z);
    }

    public static Point3D ToPoint3d(this Triple t)
    {
        return new Point3D(t.X, t.Y, t.Z);
    }

    public static Vector3 ToVector3(this Triple t)
    {
        return new Vector3(t.X, t.Y, t.Z);
    }

    public static Vector3 ToHelixVector3(this Triple t)
    {
        return new Vector3(t.X, t.Z, -t.Y);
    }

    public static List<Triple> ToTriples(this List<Point3D> points)
    {
        List<Triple> triples = new List<Triple>();
        foreach (Point3D p in points) triples.Add(new Triple(p.X, p.Y, p.Z));
        return triples;
    }

    public static List<float> ToFloats(this List<double> doubles)
    {
        List<float> floats = new List<float>();
        foreach (double d in doubles) floats.Add((float)d);
        return floats;
    }
}