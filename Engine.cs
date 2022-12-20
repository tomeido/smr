using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// <Custom "using" statements>



// </Custom "using" statements>


public partial class MainWindow
{
    // <Custom additional code>

    public class Engine
    {
        public List<Pipe> Pipes;
        public int CurrentProgress;
        public int CurrentIteration;

        public List<string> Logs = new List<string>();

        public float PipeRadius = 0.3f;
        public bool EnableMomentum = true;
        public Triple MouseRayStart;
        public Triple MouseRayDirection;
        public float MouseManipulationRange = 3.0f;
        public bool LeftMousePressed;
        public int MouseEditMode;
        public List<Triple> iObstacleLineStarts = new List<Triple>();
        public List<Triple> iObstacleLineEnds = new List<Triple>();
        public List<float> iObstacleRadii = new List<float>();

        public float AnchorStrength = 100;
        public float WorldDirectionSnapWeight = 1f;
        public float MouseManipulationWeight = 10f;
        public float CollisionWeight = 10;

        public Random random = new Random(1);

        internal int SelectedNodeIndex = -1;
        internal Pipe SelectedPipe = null;

        private bool leftMouseJustClicked = false;
        private Stopwatch stopwatch;
        public List<Node> StartEndNodes = new List<Node>();


        public Node SelectedNode { get { return SelectedPipe != null ? SelectedPipe.Nodes[SelectedNodeIndex] : null;} }


        public Engine()
        {
        }


        public void Initialize(List<Triple> pipeStarts, List<Triple> pipeEnds, int segmentCountPerPipe)
        {
            CurrentProgress = 1;

            int N = pipeStarts.Count;

            Pipes = new List<Pipe>();

            for (int i = 0; i < N; i++)
            {
                List<Node> nodes = new List<Node>();

                Triple s = pipeStarts[i];
                Triple e = pipeEnds[i];

                nodes.Add(new Node(s));

                for (int j = 1; j < segmentCountPerPipe; j++)
                {
                    float k = (j) / (segmentCountPerPipe + 1f);
                    nodes.Add(new Node(s * (1f - k) + e * k + 1 * new Triple(RandomDouble(-1, 1), RandomDouble(-1, 1), RandomDouble(-1, 1))));
                }

                nodes.Add(new Node(e));

                StartEndNodes.Add(nodes.First());
                StartEndNodes.Add(nodes.Last());

                Pipe pipe = new Pipe(nodes) { System = this, StartPoint = s, EndPoint = e };
                Pipes.Add(pipe);
            }
        }


        public void Iterate(double milliseconds)
        {
            stopwatch.Restart();
            while (stopwatch.Elapsed.TotalMilliseconds < milliseconds) Iterate();
        }


        public void Iterate(int subiterationCount)
        {
            for (int i = 0; i < subiterationCount; i++) Iterate();
        }


        public void Iterate()
        {
            CurrentIteration++;
            Logs.Clear();

            //================================================================================

            if (EnableMomentum)
                foreach (Pipe pipe in Pipes)
                    foreach (Node node in pipe.Nodes) node.Position += node.Velocity;

            //================================================================================

            //ProcessTopologyChange();

            //================================================================================

            System.Threading.Tasks.Parallel.ForEach(Pipes, pipe => pipe.ComputeNodeLocalMoves()); //parrallel

            //foreach (var pipe in Pipes) pipe.ComputeNodeLocalMoves();

            //for (int i = 0; i < CurrentProgress; i++) Pipes[i].ComputeNodeLocalMoves();


            //================================================================================

            ProcessCollision();

            //================================================================================

            ProcessMouseManipulation();

            //================================================================================

            foreach (Pipe pipe in Pipes) pipe.ComputeNodeGlobalMoves();
        }


        private void ProcessCollision()
        {
            for (int iP = 0; iP < CurrentProgress; iP++)
                for (int jP = iP + 1; jP < CurrentProgress; jP++)
                {
                    Pipe pipeI = Pipes[iP];
                    Pipe pipeJ = Pipes[jP];

                    for (int i = 0; i < pipeI.Nodes.Count - 1; i++)
                        for (int j = 0; j < pipeJ.Nodes.Count - 1; j++)
                        {
                            ////////////////////////////////////////////////////////////////////

                            Node nS1 = pipeI.Nodes[i];
                            Node nE1 = pipeI.Nodes[i + 1];
                            Node nS2 = pipeJ.Nodes[j];
                            Node nE2 = pipeJ.Nodes[j + 1];

                            Triple s1 = nS1.Position;
                            Triple e1 = nE1.Position;
                            Triple s2 = nS2.Position;
                            Triple e2 = nE2.Position;

                            Triple c1, c2;
                            Util.ClosestPointsBetweenLineSegments(s1, e1, s2, e2, out c1, out c2);

                            Triple move = c2 - c1;
                            double d = move.Length;

                            if (d < 2.0 * PipeRadius)
                            {
                                if (d > 0.0001)
                                    move = move / d * 0.5 * (2.0 * PipeRadius - d);
                                else
                                {
                                    move = (e2 - s1).Cross(e2 - s2);
                                    if (move.Length < 0.0000001)
                                        move = (e2 - s1).TryGeneratePerpendicular();
                                    else
                                        move = move.Normalise();
                                }

                                nS1.TotalMove -= CollisionWeight * move;
                                nE1.TotalMove -= CollisionWeight * move;
                                nS2.TotalMove += CollisionWeight * move;
                                nE2.TotalMove += CollisionWeight * move;
                                nS1.TotalWeight += CollisionWeight;
                                nE1.TotalWeight += CollisionWeight;
                                nS2.TotalWeight += CollisionWeight;
                                nE2.TotalWeight += CollisionWeight;
                            }

                        }
                }
        }

        private Triple ConvertToHelixVector3(Triple triple)
        {
            var vector3 = triple.ToHelixVector3();
            return new Triple(x: vector3.X, y: vector3.Y, z: vector3.Z);
        }

        private void ProcessMouseManipulation()
        {

            if (MouseEditMode == 0)
            {
                if (!LeftMousePressed)
                {
                    //SelectedNodeIndex = -1;
                }
                else
                {
                    if (SelectedNodeIndex == -1)
                    {
                        //float minDistance = MouseManipulationRange;
                        float minDistance = float.MaxValue;
                        List<Tuple<float, Pipe, int>> list = new List<Tuple<float, Pipe, int>>();

                        foreach (Pipe pipe in Pipes)
                            for (int i = 0; i < pipe.Nodes.Count; i++)
                            {
                                Triple c = MouseRayStart;
                                Triple p = ConvertToHelixVector3(pipe.Nodes[i].Position);

                                //Triple nodePosition = pipe.Nodes[i].Position;
                                //var vector3 = nodePosition.ToHelixVector3();
                                //float t = MouseRayDirection.Dot(p - MouseRayStart);
                                //Triple c = MouseRayStart + t * MouseRayDirection;
                                //Triple c = MouseRayStart - MouseRayDirection;
                                //Triple p = new Triple(x: vector3.X, y: vector3.Y, z: vector3.Z);
                                //Debug.WriteLine($"x{p.X} y{p.Y} z{p.Z}");

                                float d = c.DistanceTo(p);
                                float d2 = p.DistanceTo(c);

                                list.Add(new Tuple<float, Pipe, int>(d, pipe, i));
                            }

                        //Debug.WriteLine($"x{MouseRayStart.X} y{MouseRayStart.Y} z{MouseRayStart.Z}");

                        minDistance = list.Min(x => x.Item1);

                        if (minDistance < MouseManipulationRange)
                        {
                            var tItem = list.Where(x => x.Item1 == minDistance).First();

                            SelectedNodeIndex = tItem.Item3;
                            SelectedPipe = tItem.Item2;
                            Debug.WriteLine($"minDistance {minDistance} - {MouseManipulationRange}");
                        }
                    }

                    if (SelectedNodeIndex >= 0)
                    {
                        Node selectedNode = SelectedPipe.Nodes[SelectedNodeIndex];
                        //Triple p = ConvertToHelixVector3(selectedNode.Position);

                        Triple p = MouseRayStart;
                        Triple target = MouseRayDirection;

                        //Triple target2 = MouseRayStart + MouseRayDirection * selectedNode.Position - MouseRayStart;
                        //Triple target = MouseRayStart + MouseRayDirection * MouseRayDirection.Dot(selectedNode.Position - MouseRayStart);
                        //selectedNode.TotalMove += MouseManipulationWeight * (target - p);

                        float d = target.DistanceTo(p);
                        Debug.WriteLine($"move distance {d}");
                        selectedNode.TotalMove = (target - p);
                        Debug.WriteLine($"x {selectedNode.TotalMove.X} y{selectedNode.TotalMove.Y} z{selectedNode.TotalMove.Z}");

                        selectedNode.TotalWeight = MouseManipulationWeight * d;
                        selectedNode = SelectedPipe.Nodes[SelectedNodeIndex];
                    }
                }
            }

            if (MouseEditMode == 1)
            {
                if (!LeftMousePressed)
                {
                    //SelectedNodeIndex = -1;
                }
                else
                {
                    if (SelectedNodeIndex == -1)
                    {
                        //float minDistance = MouseManipulationRange;
                        float minDistance = float.MaxValue;
                        List<Tuple<float, Pipe, int>> list = new List<Tuple<float, Pipe, int>>();

                        foreach (Pipe pipe in Pipes)
                            for (int i = 0; i < pipe.Nodes.Count; i++)
                            {
                                Triple c = MouseRayStart;
                                Triple p = ConvertToHelixVector3(pipe.Nodes[i].Position);

                                //Triple nodePosition = pipe.Nodes[i].Position;
                                //var vector3 = nodePosition.ToHelixVector3();
                                //float t = MouseRayDirection.Dot(p - MouseRayStart);
                                //Triple c = MouseRayStart + t * MouseRayDirection;
                                //Triple c = MouseRayStart - MouseRayDirection;
                                //Triple p = new Triple(x: vector3.X, y: vector3.Y, z: vector3.Z);
                                //Debug.WriteLine($"x{p.X} y{p.Y} z{p.Z}");

                                float d = c.DistanceTo(p);
                                float d2 = p.DistanceTo(c);

                                list.Add(new Tuple<float, Pipe, int>(d, pipe, i));
                            }

                        //Debug.WriteLine($"x{MouseRayStart.X} y{MouseRayStart.Y} z{MouseRayStart.Z}");

                        minDistance = list.Min(x => x.Item1);

                        if (minDistance < MouseManipulationRange)
                        {
                            var tItem = list.Where(x => x.Item1 == minDistance).First();

                            SelectedNodeIndex = tItem.Item3;
                            SelectedPipe = tItem.Item2;
                            Debug.WriteLine($"minDistance {minDistance} - {MouseManipulationRange}");
                        }
                    }

                    if (SelectedNodeIndex >= 0)
                    {

                        double minDistance = 1.0;

                        foreach (Pipe pipe in Pipes)
                            for (int i = 0; i < pipe.Nodes.Count; i++)
                            {
                                Triple p = pipe.Nodes[i].Position;
                                float t = MouseRayDirection.Dot(p - MouseRayStart);

                                Triple c = MouseRayStart + t * MouseRayDirection;
                                float d = c.DistanceTo(p);

                                if (d < minDistance)
                                {
                                    minDistance = d;
                                    SelectedNodeIndex = i;
                                    SelectedPipe = pipe;
                                }
                            }

                        if (0 < SelectedNodeIndex && SelectedNodeIndex < SelectedPipe.Nodes.Count - 1)
                        {
                            SelectedPipe.Nodes.RemoveAt(SelectedNodeIndex);
                        }


                    }
                }
            }

            if (MouseEditMode == 2)
            {
                if (!LeftMousePressed)
                {
                    //SelectedNodeIndex = -1;
                }
                else
                {
                    if (SelectedNodeIndex == -1)
                    {
                        //float minDistance = MouseManipulationRange;
                        float minDistance = float.MaxValue;
                        List<Tuple<float, Pipe, int>> list = new List<Tuple<float, Pipe, int>>();

                        foreach (Pipe pipe in Pipes)
                            for (int i = 0; i < pipe.Nodes.Count; i++)
                            {
                                Triple c = MouseRayStart;
                                Triple p = ConvertToHelixVector3(pipe.Nodes[i].Position);

                                //Triple nodePosition = pipe.Nodes[i].Position;
                                //var vector3 = nodePosition.ToHelixVector3();
                                //float t = MouseRayDirection.Dot(p - MouseRayStart);
                                //Triple c = MouseRayStart + t * MouseRayDirection;
                                //Triple c = MouseRayStart - MouseRayDirection;
                                //Triple p = new Triple(x: vector3.X, y: vector3.Y, z: vector3.Z);
                                //Debug.WriteLine($"x{p.X} y{p.Y} z{p.Z}");

                                float d = c.DistanceTo(p);
                                float d2 = p.DistanceTo(c);

                                list.Add(new Tuple<float, Pipe, int>(d, pipe, i));
                            }

                        //Debug.WriteLine($"x{MouseRayStart.X} y{MouseRayStart.Y} z{MouseRayStart.Z}");

                        minDistance = list.Min(x => x.Item1);

                        if (minDistance < MouseManipulationRange)
                        {
                            var tItem = list.Where(x => x.Item1 == minDistance).First();

                            SelectedNodeIndex = tItem.Item3;
                            SelectedPipe = tItem.Item2;
                            Debug.WriteLine($"minDistance {minDistance} - {MouseManipulationRange}");
                        }
                    }

                    if (SelectedNodeIndex >= 0)
                    {
                        double minDistance = 1.0;

                        foreach (Pipe pipe in Pipes)
                            for (int i = 0; i < pipe.Nodes.Count - 1; i++)
                            {
                                Triple s = pipe.Nodes[i].Position;
                                Triple e = pipe.Nodes[i + 1].Position;

                                Triple c1, c2;

                                Util.ClosestPointsBetweenLineSegments(s, e, MouseRayStart, MouseRayStart + 99999f * MouseRayDirection, out c1, out c2);

                                float d = c1.DistanceTo(c2);

                                if (d < minDistance)
                                {
                                    minDistance = d;
                                    SelectedNodeIndex = i;
                                    SelectedPipe = pipe;
                                }
                            }

                        if (0 < SelectedNodeIndex && SelectedNodeIndex < SelectedPipe.Nodes.Count - 1)
                        {
                            Triple newNodePosition = 0.5 * (SelectedPipe.Nodes[SelectedNodeIndex].Position + SelectedPipe.Nodes[SelectedNodeIndex + 1].Position);
                            SelectedPipe.Nodes.Insert(SelectedNodeIndex + 1, new Node(newNodePosition, false));
                        }
                    }
                }
            }
        }

        private void ProcessTopologyChange()
        {
            if (MouseEditMode == 1)
            {
                if (!LeftMousePressed)
                {
                    leftMouseJustClicked = false;
                }
                else if (!leftMouseJustClicked)
                {
                    leftMouseJustClicked = true;

                    SelectedNodeIndex = -1;

                    double minDistance = 1.0;

                    foreach (Pipe pipe in Pipes)
                        for (int i = 0; i < pipe.Nodes.Count; i++)
                        {
                            Triple p = pipe.Nodes[i].Position;
                            float t = MouseRayDirection.Dot(p - MouseRayStart);

                            Triple c = MouseRayStart + t * MouseRayDirection;
                            float d = c.DistanceTo(p);

                            if (d < minDistance)
                            {
                                minDistance = d;
                                SelectedNodeIndex = i;
                                SelectedPipe = pipe;
                            }
                        }

                    if (0 < SelectedNodeIndex && SelectedNodeIndex < SelectedPipe.Nodes.Count - 1)
                    {
                        SelectedPipe.Nodes.RemoveAt(SelectedNodeIndex);
                    }
                }
            }
            else if (MouseEditMode == 2)
            {
                if (!LeftMousePressed)
                {
                    leftMouseJustClicked = false;
                }
                else if (!leftMouseJustClicked)
                {
                    leftMouseJustClicked = true;

                    SelectedNodeIndex = -1;

                    double minDistance = 1.0;

                    foreach (Pipe pipe in Pipes)
                        for (int i = 0; i < pipe.Nodes.Count - 1; i++)
                        {
                            Triple s = pipe.Nodes[i].Position;
                            Triple e = pipe.Nodes[i + 1].Position;

                            Triple c1, c2;

                            Util.ClosestPointsBetweenLineSegments(s, e, MouseRayStart, MouseRayStart + 99999f * MouseRayDirection, out c1, out c2);

                            float d = c1.DistanceTo(c2);

                            if (d < minDistance)
                            {
                                minDistance = d;
                                SelectedNodeIndex = i;
                                SelectedPipe = pipe;
                            }
                        }

                    if (0 < SelectedNodeIndex && SelectedNodeIndex < SelectedPipe.Nodes.Count - 1)
                    {
                        Triple newNodePosition = 0.5 * (SelectedPipe.Nodes[SelectedNodeIndex].Position + SelectedPipe.Nodes[SelectedNodeIndex + 1].Position);
                        SelectedPipe.Nodes.Insert(SelectedNodeIndex + 1, new Node(newNodePosition, false));
                    }
                }
            }
        }


        // public List<Polyline> GetPipePolylineCurves()
        // {
        //     List<Polyline> curves = new List<Polyline>();
        //     for (int i = 0; i < CurrentProgress; i++)
        //     {
        //         Pipe pipe = Pipes[i];
        //         List<Point3d> points = new List<Point3d>();
        //         foreach (Node node in pipe.Nodes)
        //             points.Add(node.Position.ToPoint3d());
        //         curves.Add(new Polyline(points));
        //     }
        //     return curves;
        // }
        //
        //
        // public List<Mesh> GetPipeMeshes()
        // {
        //     List<Mesh> meshes = new List<Mesh>();
        //     for (int j = 0; j< CurrentProgress; j++)
        //     {
        //         Pipe pipe = Pipes[j];
        //         for (int i = 0; i < pipe.Nodes.Count - 1; i++)
        //         {
        //             Vector3d z = pipe.Nodes[i + 1].Position.ToPoint3d() - pipe.Nodes[i].Position.ToPoint3d();
        //             Cylinder cylinder = new Cylinder(new Circle(new Plane(pipe.Nodes[i].Position.ToPoint3d(), z), PipeRadius), z.Length);
        //             if (cylinder.IsValid)
        //                 meshes.Add(Mesh.CreateFromCylinder(cylinder, 1, 16, false, false));
        //         }
        //     }
        //
        //     return meshes;
        // }
        //
        // public void DrawWires(IGH_PreviewArgs args)
        // {
        //     for (int i = 0; i < CurrentProgress; i++)
        //     {
        //         Pipe pipe = Pipes[i];
        //
        //         foreach (Node node in pipe.Nodes)
        //             args.Display.DrawPoint(node.Position.ToPoint3d(), PointStyle.Circle, 10.0f, Color.Black);
        //
        //     }
        //
        //     //========================================================
        //
        // }

        public double RandomDouble(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }
    }

    // </Custom additional code>
}
