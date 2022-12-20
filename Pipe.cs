using System;
using System.Collections.Generic;

// <Custom "using" statements>



// </Custom "using" statements>


public partial class MainWindow
{
    // <Custom additional code>


    public class Pipe
    {
        public List<Node> Nodes;
        public Triple StartPoint;
        public Triple EndPoint;

        internal Engine System;

        public Pipe(List<Node> nodes)
        {
            Nodes = nodes;
        }


        public void ComputeNodeLocalMoves()
        {
            foreach (Node node in Nodes)
            {
                node.TotalMove = Triple.Zero;
                node.TotalWeight = 0.0;
            }

            ProcessWorldDirectionSnap();
            // ProcessLength();
            ProcessObstacleAvoidance();
            ProcessAnchors();
        }

        private void ProcessLength()
        {
            double w = 1;

            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                Node nodeA = Nodes[i];
                Node nodeB = Nodes[i + 1];

                Triple pA = nodeA.Position;
                Triple pB = nodeB.Position;

                Triple move = pA - pB;
                move *= 0.5 * (move.Length - 0.5) / move.Length;


                nodeA.TotalMove -= w * move;
                nodeB.TotalMove += w * move;
                nodeA.TotalWeight += w;
                nodeB.TotalWeight += w;
            }
        }


        public void ComputeNodeGlobalMoves()
        {
            if (System.EnableMomentum)
                foreach (Node node in Nodes)
                {
                    if (node.TotalWeight == 0) continue;
                    Triple move = node.TotalMove / node.TotalWeight;
                    node.Position += move;
                    node.Velocity += move;
                    node.Velocity *= (node.Velocity.Dot(move)) < 0f ? 0.0f : 1f;
                }
            else
                foreach (Node node in Nodes)
                {
                    if (node.TotalWeight == 0) continue;
                    node.Position += node.TotalMove / node.TotalWeight;
                    node.Velocity = Triple.Zero;
                }
        }


        private void ProcessObstacleAvoidance()
        {
            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                Node nS = Nodes[i];
                Node nE = Nodes[i + 1];

                Triple s = nS.Position;
                Triple e = nE.Position;

                for (int j = 0; j < System.iObstacleLineStarts.Count; j++)
                {
                    float obstacleRadius = System.iObstacleRadii[j];
                    Triple c1, c2;

                    Util.ClosestPointsBetweenLineSegments(s, e, System.iObstacleLineStarts[j], System.iObstacleLineEnds[j], out c1, out c2);

                    Triple move = c2 - c1;
                    double d = move.Length;

                    if (d < (obstacleRadius + System.PipeRadius))
                    {
                        if (d > 0.0001)
                            move = move / d * (obstacleRadius + System.PipeRadius - d);
                        else
                        {
                            move = Triple.Zero;
                        }

                        nS.TotalMove -= System.CollisionWeight * move;
                        nE.TotalMove -= System.CollisionWeight * move;
                        nS.TotalWeight += System.CollisionWeight;
                        nE.TotalWeight += System.CollisionWeight;
                    }
                }
            }
        }

        private void ProcessWorldDirectionSnap()
        {
            int N = Nodes.Count;

            for (int i = 0; i < N - 1; i++)
            {
                Node nodeA = Nodes[i];
                Node nodeB = Nodes[i + 1];

                Triple pA = nodeA.Position;
                Triple pB = nodeB.Position;

                double dX = Math.Abs(pA.X - pB.X);
                double dY = Math.Abs(pA.Y - pB.Y);
                double dZ = Math.Abs(pA.Z - pB.Z);

                double w = System.WorldDirectionSnapWeight;

                if (dX > dY)
                {
                    if (dX > dZ)
                    {
                        double mY = 0.5 * (pA.Y + pB.Y);
                        double mZ = 0.5 * (pA.Z + pB.Z);
                        nodeA.TotalMove += w * new Triple(0, mY - pA.Y, mZ - pA.Z);
                        nodeB.TotalMove += w * new Triple(0, mY - pB.Y, mZ - pB.Z);
                        nodeA.TotalWeight += w;
                        nodeB.TotalWeight += w;
                    }
                    else
                    {
                        double mX = 0.5 * (pA.X + pB.X);
                        double mY = 0.5 * (pA.Y + pB.Y);
                        nodeA.TotalMove += w * new Triple(mX - pA.X, mY - pA.Y, 0);
                        nodeB.TotalMove += w * new Triple(mX - pB.X, mY - pB.Y, 0);
                        nodeA.TotalWeight += w;
                        nodeB.TotalWeight += w;
                    }
                }
                else // dY > dX
                {
                    if (dY > dZ)
                    {
                        double mX = 0.5 * (pA.X + pB.X);
                        double mZ = 0.5 * (pA.Z + pB.Z);
                        nodeA.TotalMove += w * new Triple(mX - pA.X, 0, mZ - pA.Z);
                        nodeB.TotalMove += w * new Triple(mX - pB.X, 0, mZ - pB.Z);
                        nodeA.TotalWeight += w;
                        nodeB.TotalWeight += w;
                    }
                    else
                    {
                        double mX = 0.5 * (pA.X + pB.X);
                        double mY = 0.5 * (pA.Y + pB.Y);
                        nodeA.TotalMove += w * new Triple(mX - pA.X, mY - pA.Y, 0);
                        nodeB.TotalMove += w * new Triple(mX - pB.X, mY - pB.Y, 0);
                        nodeA.TotalWeight += w;
                        nodeB.TotalWeight += w;
                    }
                }
            }
        }


        private void ProcessAnchors()
        {
            double w = System.AnchorStrength;

            Node s = Nodes[0];
            s.TotalMove += w * (StartPoint - s.Position);
            s.TotalWeight += w;

            Node e = Nodes[Nodes.Count - 1];
            e.TotalMove += w * (EndPoint - e.Position);
            e.TotalWeight += w;
        }
    }


    // </Custom additional code>
}
