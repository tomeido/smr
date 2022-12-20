using System;
using System.Collections.Generic;
using System.Text;

public static class Util
{
    public static Triple ClosestPointOnLineSegment(Triple p, Triple s, Triple e)
    {
        Triple v = p - s;
        Triple d = e - s;
        float l = d.Length;
        d /= l;
        float t = v.Dot(d);
        return t <= 0f ? s : t > l ? e : s + d * t;
    }


    public static void ClosestPointsBetweenLineSegments(Triple s1, Triple e1, Triple s2, Triple e2, out Triple c1, out Triple c2)
    {
        Triple d1 = (e1 - s1);
        Triple d2 = (e2 - s2);
        float l1 = d1.Length;
        float l2 = d2.Length;
        d1 /= l1;
        d2 /= l2;
        Triple n = d1.Cross(d2);

        if (n.Length > 0.00001)
        {
            Triple n1 = d1.Cross(n);
            Triple n2 = d2.Cross(n);

            float t1 = (s2 - s1).Dot(n2) / d1.Dot(n2);
            float t2 = (s1 - s2).Dot(n1) / d2.Dot(n1);

            t1 = t1 < 0f ? 0f : t1 > l1 ? l1 : t1;
            t2 = t2 < 0f ? 0f : t2 > l2 ? l2 : t2;

            c1 = s1 + t1 * d1;
            c2 = s2 + t2 * d2;

            float minDist = c1.DistanceTo(c2);

            Triple c;
            float d;

            c = ClosestPointOnLineSegment(s1, s2, e2);
            d = c.DistanceTo(s1);
            if (d < minDist)
            {
                minDist = d;
                c1 = s1;
                c2 = c;
            }

            c = ClosestPointOnLineSegment(e1, s2, e2);
            d = c.DistanceTo(e1);
            if (d < minDist)
            {
                minDist = d;
                c1 = e1;
                c2 = c;
            }

            c = ClosestPointOnLineSegment(s2, s1, e1);
            d = c.DistanceTo(s2);
            if (d < minDist)
            {
                minDist = d;
                c1 = c;
                c2 = s2;
            }

            c = ClosestPointOnLineSegment(e2, s1, e1);
            d = c.DistanceTo(e2);
            if (d < minDist)
            {
                minDist = d;
                c1 = c;
                c2 = e2;
            }
        }
        else
        {
            if (d1.Dot(d2) < 0)
            {
                Triple temp = s1;
                s1 = e1;
                e1 = temp;
                d1 = -d1;
            }

            if ((s1 - s2).Dot(d2) < 0)
            {
                Triple temp;
                temp = s1;
                s1 = s2;
                s2 = temp;
                temp = e1;
                e1 = e2;
                e2 = temp;
                d2 = d1;
                l2 = l1;
            }

            float t2 = (s1 - s2).Dot(d2);
            if (t2 <= l2)
            {
                c1 = s1;
                c2 = s2 + t2 * d2;
            }
            else
            {
                c1 = s1;
                c2 = e2;
            }
        }
    }
}
