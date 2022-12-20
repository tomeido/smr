using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Diagnostics;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Color = System.Windows.Media.Color;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using System.IO;
using static MainWindow;
using Point = System.Windows.Point;

namespace HelixPlayground
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private double t = 0.0;
        private Viewport3DX viewport;
        private LineGeometryModel3D lineModel;
        //private LineGeometryModel3D lineModel2;
        private Random random = new Random();
        private DirectionalLight3D light;
        private ShadowMap3D shadowMap;
        private Engine system;

        SMRData Data = new SMRData();

        public MainWindow()
        {
            InitializeComponent();

            LoadSMROrigin(data: ref this.Data);
            LoadSMRVoid(data: ref this.Data);
            InitEngine(data: ref this.Data);
            InitViewPort();

            DrawGeometry(data: ref this.Data);
            CreateSMRPipeFile();
            InitEvent(data: ref this.Data);

            //timer
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        public Vector3? GetPoint(Point position, Vector3? vector3)
        {
            var firstHit = viewport.FindHits(position).FirstOrDefault();
            if (firstHit != null)
            {
                if (firstHit.ModelHit != null)
                {
                    if (firstHit.ModelHit.GetType().Name.Equals(SMRType.MESH_GEOMETRY_MODEL_3D))
                    {
                        MeshGeometryModel3D mesh3d = (MeshGeometryModel3D)firstHit.ModelHit;
                        return mesh3d.BoundsSphere.Center;
                    }
                }
            }
            return vector3;
        }


        public void InitEvent(ref SMRData data)
        {
            viewport.MouseMove3D += (sender, e) =>
            {

            };
            //viewport.mouse
            viewport.MouseLeftButtonDown += (sender, e) =>
            {
                System.Windows.Point startPosition = e.GetPosition(viewport);
                Point3D? point3D = viewport.FindNearestPoint(startPosition);
                Vector3? vector3d = point3D?.ToVector3();

                vector3d = GetPoint(e.GetPosition(viewport), vector3d);
                
                if (system.MouseEditMode >= 0)
                {
                    if (vector3d != null)
                    {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            if (!this.system.LeftMousePressed)
                            {
                                this.system.SelectedNodeIndex = -1;
                                this.system.SelectedPipe = null;

                                this.timer.Stop();
                                if (this.timer.IsEnabled == true)
                                {
                                    return;
                                }

                                Debug.WriteLine("start");

                                this.system.MouseRayStart = new Triple(x: vector3d.Value.X, y: vector3d.Value.Y, z: vector3d.Value.Z);
                                this.system.MouseRayDirection = new Triple(x: vector3d.Value.X, y: vector3d.Value.Y, z: vector3d.Value.Z);
                                this.system.LeftMousePressed = true;
                                this.timer.Stop();
                                this.system.Iterate(1);
                                this.timer.Start();
                                this.system.LeftMousePressed = true;
                            }
                        }
                    }
                }
            };

            viewport.MouseMove += (sender, e) =>
            {
                System.Windows.Point movePosition = e.GetPosition(viewport);
                Point3D? point3D = viewport.FindNearestPoint(movePosition);

                var results = viewport.FindHits(movePosition);

                //e.MouseDevice.OverrideCursor.

                if (this.system.LeftMousePressed)
                {
                    if (point3D != null)
                    {
                        var vector3d = viewport.CurrentPosition;
                        //var vector3d = point3D?.ToVector3();
                        //vector3d = GetPoint(movePosition, vector3d);

                        Debug.WriteLine("move");
                        //this.system.MouseRayDirection = new Triple(x: vector3d.Value.X, y: vector3d.Value.Y, z: vector3d.Value.Z);
                        this.system.MouseRayDirection = new Triple(x: vector3d.X, y: vector3d.Y, z: vector3d.Z);
                        this.timer.Stop();
                        this.system.Iterate(1);
                        this.timer.Start();
                        //this.system.MouseRayStart = new Triple(x: vector3d.X, y: vector3d.Y, z: vector3d.Z);
                        //this.system.MouseRayStart = new Triple(x: vector3d.Value.X, y: vector3d.Value.Y, z: vector3d.Value.Z);
                    }
                    else
                    {
                        //강제 종료
                    }
                }
            };


            viewport.MouseLeftButtonUp += (sender, e) =>
            {
                if (this.system.LeftMousePressed)
                {
                    Debug.WriteLine("end");
                    this.system.LeftMousePressed = false;
                    //CreateSMRPipeFile();
                    this.timer.Start();
                }
            };



            //viewport.MouseDown += new MouseButtonEventHandler(viewport_OnMouseDown); 
            //viewport.MouseLeftButtonDown += new MouseButtonEventHandler(lineModel_MouseLeftButtonDown);
            //viewport.MouseMove3D += new RoutedEventHandler(viewport_Mouse3DMove);
            //viewport.MouseDown3D += new RoutedEventHandler(viewport_Mouse3DDown);
            //data.meshModel2.MouseLeftButtonDown += new MouseButtonEventHandler(lineModel_MouseLeftButtonDown);
            //data.meshModel2.MouseDown3D += new RoutedEventHandler(lineModel_Mouse3DDown);
        }


        public void LoadSMROrigin(ref SMRData data)
        {
            data = new SMRData();
            //ReadTxtFile

            string pipecoord = File.ReadAllText(SMRPath.READ_PATH);

            //start end obs parse
            string[] onlyStartCoords = extractStartCoord(pipecoord);
            //for (int i = 0; i < onlyStartCoords.Length; i++)
            //{
            //    Debug.WriteLine(onlyStartCoords[i]);
            //}
            data.StartX = extractX(onlyStartCoords);
            data.StartY = extractY(onlyStartCoords);
            data.StartZ = extractZ(onlyStartCoords);
            //List<Triple> startTriple = new List<Triple> { };
            for (int i = 0; i < data.StartX.Length; i++)
            {
                data.StartTriple.Add(new Triple(float.Parse(data.StartX[i]), float.Parse(data.StartY[i]), float.Parse(data.StartZ[i])));
            }

            string[] onlyEndCoords = extractEndCoord(pipecoord);
            //for (int i = 0; i < onlyEndCoords.Length; i++)
            //{
            //    Debug.WriteLine(onlyEndCoords[i]);
            //}
            data.EndX = extractX(onlyEndCoords);
            data.EndY = extractY(onlyEndCoords);
            data.EndZ = extractZ(onlyEndCoords);
            //List<Triple> endTriple = new List<Triple> { };
            for (int i = 0; i < data.EndX.Length; i++)
            {
                data.EndTriple.Add(new Triple(float.Parse(data.EndX[i]), float.Parse(data.EndY[i]), float.Parse(data.EndZ[i])));
            }
        }

        public void LoadSMRVoid(ref SMRData data)
        {
            //ReadTxtFile

            string voidcoord = File.ReadAllText(SMRPath.READ_VOID_PATH);
            string[] onlyMaxObsCoords = extractMaxObsCoord(voidcoord);
            //for (int i = 0; i < onlyMaxObsCoords.Length; i++)
            //{
            //    Debug.WriteLine("obsMax " + onlyMaxObsCoords[i]);
            //}
            data.ObsMaxX = extractX(onlyMaxObsCoords);
            data.ObsMaxY = extractY(onlyMaxObsCoords);
            data.ObsMaxZ = extractZ(onlyMaxObsCoords);

            for (int i = 0; i < data.ObsMaxX.Length; i++)
            {
                data.ObsMaxTriple.Add(new Triple(float.Parse(data.ObsMaxX[i]), float.Parse(data.ObsMaxY[i]), float.Parse(data.ObsMaxZ[i])));
            }

            string[] onlyMinObsCoords = extractMinObsCoord(voidcoord);
            //for (int i = 0; i < onlyMinObsCoords.Length; i++)
            //{
            //    Debug.WriteLine("obsMin " + onlyMinObsCoords[i]);
            //}
            data.ObsMinX = extractX(onlyMinObsCoords);
            data.ObsMinY = extractY(onlyMinObsCoords);
            data.ObsMinZ = extractZ(onlyMinObsCoords);

            for (int i = 0; i < data.ObsMinX.Length; i++)
            {
                data.ObsMinTriple.Add(new Triple(float.Parse(data.ObsMinX[i]), float.Parse(data.ObsMinY[i]), float.Parse(data.ObsMinZ[i])));
            }
        }

        public void InitEngine(ref SMRData data)
        {
            this.system = new Engine();
            //obstacle
            this.system.iObstacleLineStarts = new List<Triple> { };
            this.system.iObstacleLineEnds = new List<Triple> { };
            this.system.iObstacleRadii = new List<float> { };
            for (int i = 0; i < data.ObsMaxX.Length; i++)
            {
                this.system.iObstacleLineStarts.Add(new Triple(data.ObsMinTriple[i].X, (data.ObsMinTriple[i].Y + data.ObsMaxTriple[i].Y) / 2, (data.ObsMinTriple[i].Z + data.ObsMaxTriple[i].Z) / 2));
                this.system.iObstacleLineEnds.Add(new Triple(data.ObsMaxTriple[i].X, (data.ObsMinTriple[i].Y + data.ObsMaxTriple[i].Y) / 2, (data.ObsMinTriple[i].Z + data.ObsMaxTriple[i].Z) / 2));
                this.system.iObstacleRadii.Add(MathF.Sqrt(MathF.Pow(data.ObsMaxTriple[i].Y - data.ObsMinTriple[i].Y, 2) + MathF.Pow(data.ObsMaxTriple[i].Z - data.ObsMinTriple[i].Z, 2)));

                this.system.iObstacleLineStarts.Add(new Triple((data.ObsMinTriple[i].X + data.ObsMaxTriple[i].X) / 2, (data.ObsMinTriple[i].Y + data.ObsMaxTriple[i].Y) / 2, data.ObsMinTriple[i].Z));
                this.system.iObstacleLineEnds.Add(new Triple((data.ObsMinTriple[i].X + data.ObsMaxTriple[i].X) / 2, (data.ObsMinTriple[i].Y + data.ObsMaxTriple[i].Y) / 2, data.ObsMaxTriple[i].Z));
                this.system.iObstacleRadii.Add(MathF.Sqrt(MathF.Pow(data.ObsMaxTriple[i].X - data.ObsMinTriple[i].X, 2) + MathF.Pow(data.ObsMaxTriple[i].Y - data.ObsMinTriple[i].Y, 2)));

                this.system.iObstacleLineStarts.Add(new Triple((data.ObsMinTriple[i].X + data.ObsMaxTriple[i].X) / 2, data.ObsMinTriple[i].Y, (data.ObsMinTriple[i].Z + data.ObsMaxTriple[i].Z) / 2));
                this.system.iObstacleLineEnds.Add(new Triple((data.ObsMinTriple[i].X + data.ObsMaxTriple[i].X) / 2, data.ObsMaxTriple[i].Y, (data.ObsMinTriple[i].Z + data.ObsMaxTriple[i].Z) / 2));
                this.system.iObstacleRadii.Add(MathF.Sqrt(MathF.Pow(data.ObsMaxTriple[i].X - data.ObsMinTriple[i].X, 2) + MathF.Pow(data.ObsMaxTriple[i].Z - data.ObsMinTriple[i].Z, 2)));
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            this.system.Initialize(data.StartTriple, data.EndTriple, 9);
            //if (iProgress > system.CurrentProgress && iProgress <= system.Pipes.Count) system.CurrentProgress = iProgress;
            this.system.CurrentProgress = data.StartTriple.Count;
            //system.Iterate(itercount);//zombie
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///
        }

        public void InitViewPort()
        {
            this.viewport = new Viewport3DX();
            ((Grid)Content).Children.Add(this.viewport);
            this.viewport.EffectsManager = new DefaultEffectsManager();
            this.viewport.BackgroundColor = Color.FromArgb(255, 255, 255, 255);
            this.viewport.Camera = new PerspectiveCamera();
            this.viewport.Camera.Position = new Point3D(0, 0, 30);
            this.viewport.Camera.LookDirection = new Vector3D(0, 0, -1);
            this.viewport.IsShadowMappingEnabled = true;
            this.viewport.EnableCurrentPosition = true;
        }

        //make material
        Material whiteMaterial = new PhongMaterial()
        {
            DiffuseColor = new Color4(0.8f, 0.8f, 0.8f, 1.0f),
            RenderShadowMap = true
        };

        Material blueMaterial = new PhongMaterial()
        {
            DiffuseColor = new Color4(0.2f, 0.2f, 0.8f, 1.0f),
            RenderShadowMap = true
        };

        Material EdgeMaterial = new PhongMaterial()
        {
            DiffuseColor = new Color4(1.0f, 0.5f, 0.5f, 0.5f),
            RenderShadowMap = true
        };

        Material SelectedEdgeMaterial = new PhongMaterial()
        {
            DiffuseColor = new Color4(1.0f, 0.0f, 0.0f, 0.5f),
            RenderShadowMap = true
        };

        public void DrawGeometry(ref SMRData data, bool isEdge = true)
        {
            //make point sphere
            MeshBuilder meshBuilder = new MeshBuilder();
            //meshBuilder.AddSphere(new Vector3(), 1);
            for (int i = 0; i < data.StartX.Length; i++)
            {
                meshBuilder.AddSphere(new Vector3(float.Parse(data.StartX[i]), float.Parse(data.StartZ[i]), -float.Parse(data.StartY[i])));
                meshBuilder.AddSphere(new Vector3(float.Parse(data.EndX[i]), float.Parse(data.EndZ[i]), -float.Parse(data.EndY[i])));
                //meshBuilder.AddSphere(new Vector3(float.Parse(objMaxX[i]), float.Parse(objMaxZ[i]), -float.Parse(objMaxY[i])));
                //meshBuilder.AddSphere(new Vector3(float.Parse(objMinX[i]), float.Parse(objMinZ[i]), -float.Parse(objMinY[i])));
            }

            //if(isEdge)
            //{
            //    /////node sphere
            //    for (int i = 0; i < system.Pipes.Count; i++)
            //    {
            //        for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
            //        {
            //            meshBuilder.AddSphere(system.Pipes[i].Nodes[j].Position.ToHelixVector3());
            //        }
            //    }
            //}

            data.meshModel1 = new MeshGeometryModel3D()
            {
                Geometry = meshBuilder.ToMesh(),
                Material = blueMaterial,
                IsThrowingShadow = true
            };
            data.meshModel1.Transform = new TranslateTransform3D(0, 0, 0);

            //make box
            meshBuilder = new MeshBuilder();
            //meshBuilder.AddBox(new Vector3(), 50, 0.5, 50);
            List<Vector3> obsMid = new List<Vector3> { };

            for (int i = 0; i < data.ObsMaxX.Length; i++)
            {
                obsMid.Add(new Vector3((float.Parse(data.ObsMaxX[i]) + float.Parse(data.ObsMinX[i])) / 2, (float.Parse(data.ObsMaxZ[i]) + float.Parse(data.ObsMinZ[i])) / 2, -(float.Parse(data.ObsMaxY[i]) + float.Parse(data.ObsMinY[i])) / 2));
            }
            for (int i = 0; i < data.ObsMaxX.Length; i++)
            {
                meshBuilder.AddBox(obsMid[i], (double.Parse(data.ObsMaxX[i]) - double.Parse(data.ObsMinX[i])), (double.Parse(data.ObsMaxZ[i]) - double.Parse(data.ObsMinZ[i])), (double.Parse(data.ObsMaxY[i]) - double.Parse(data.ObsMinY[i])));
            }

            data.meshModel2 = new MeshGeometryModel3D()
            {
                Geometry = meshBuilder.ToMesh(),
                Material = whiteMaterial,
                IsThrowingShadow = true
            };




            //add model to viewport
            viewport.Items.Add(data.meshModel1);
            viewport.Items.Add(data.meshModel2);

            //light
            light = new DirectionalLight3D()
            {
                Direction = new Vector3D(1, -1, -1),
                Color = Color.FromArgb(255, 255, 255, 255)
            };
            viewport.Items.Add(light);

            //camera
            viewport.Camera.Position = new Point3D(float.Parse(data.StartX[0]), float.Parse(data.StartZ[0]) + 100, -float.Parse(data.StartY[0]) + 100);
            viewport.Camera.LookDirection = new Vector3D(0, -2, -2);

            //shadow
            shadowMap = new ShadowMap3D()
            {
                Intensity = 0.5,
                Resolution = new Size(2048, 2048),
            };
            viewport.Items.Add(shadowMap);

            light.Direction = new Vector3D(-1, -1, -1);


            ////////////////////////////////////////////////////////////////////////////////////////////////
            ////zombie

            ////LineGeometry3D smartrouted
            //for (int i = 0; i < system.Pipes.Count; i++)
            //{
            //    LineGeometry3D lineGeometry = new LineGeometry3D()
            //    {
            //        Positions = new Vector3Collection(),
            //        Indices = new IntCollection(),
            //        Colors = new Color4Collection(),
            //    };
            //    for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
            //    {
            //        lineGeometry.Positions.Add(system.Pipes[i].Nodes[j].Position.ToHelixVector3());
            //        lineGeometry.Indices.Add(lineGeometry.Positions.Count - 1);
            //        lineGeometry.Indices.Add(lineGeometry.Positions.Count - 2);
            //        lineGeometry.Colors.Add(new Color4(1.0f, 0.5f, 0.1f, 1f));
            //    }
            //    lineModel = new LineGeometryModel3D()
            //    {
            //        Geometry = lineGeometry,
            //        Color = Color.FromArgb(255, 255, 255, 255),
            //        Thickness = 3,
            //    };
            //    viewport.Items.Add(lineModel);
            //}



            ////linebuilder smartrouted line
            //var lineBuilder = new LineBuilder();
            //LineGeometryModel3D lineModel2 = new LineGeometryModel3D();
            //for (int i = 0; i < system.Pipes.Count; i++)
            //{
            //    List<Vector3> vectors = new List<Vector3>();
            //    for (int j = 0; j < system.Pipes[i].Nodes.Count - 1; j++)
            //    {
            //        lineBuilder.AddLine(system.Pipes[i].Nodes[j].Position.ToHelixVector3(), system.Pipes[i].Nodes[j + 1].Position.ToHelixVector3());
            //        //Debug.WriteLine(system.Pipes[i].Nodes[j].Position.ToVector3());
            //    }
            //}

            //lineModel2.Geometry = lineBuilder.ToLineGeometry3D();
            //lineModel2.Geometry.UpdateVertices();
            //viewport.Items.Add(lineModel2);


            /////////////////////////////////////////////////////////////////////////////////////////////////////


        }


        public void CreateSMRPipeFile()
        {
            //write txt

            string pipetxt = "";
            for (int i = 0; i < system.Pipes.Count; i++)
            {
                for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
                {
                    pipetxt += "(X" + system.Pipes[i].Nodes[j].Position.X + ",Y" + system.Pipes[i].Nodes[j].Position.Y + ",Z" + system.Pipes[i].Nodes[j].Position.Z + ") ";
                }
                pipetxt += "\r\n";
            }

            using (StreamWriter sw = new StreamWriter(SMRPath.DATA_PATH))
            {
                sw.WriteLine(pipetxt);
            }
        }




        private void Timer_Tick(object sender, EventArgs e)
        {
            //this.itercount = this.itercount + 1;
            this.system.Iterate(1);

            while (viewport.Items.Count > 4)
            {
                this.viewport.Items.RemoveAt(viewport.Items.Count - 1);
            }

            //List<LineGeometryModel3D> removeLines = new List<LineGeometryModel3D>();
            //
            //foreach (var item in viewport.Items)
            //{
            //    if (item.GetType().Name.Equals(SMRType.LINE_GEOMETRY_MODEL_3D))
            //    {
            //        removeLines.Add((LineGeometryModel3D)item);
            //    }
            //}
            //
            //foreach (var item in removeLines)
            //{
            //    this.viewport.Items.Remove((LineGeometryModel3D)item);
            //}

            //LineGeometry3D smartrouted
            for (int i = 0; i < system.Pipes.Count; i++)
            {
                LineGeometry3D lineGeometry = new LineGeometry3D()
                {
                    Positions = new Vector3Collection(),
                    Indices = new IntCollection(),
                    Colors = new Color4Collection(),
                };

                for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
                {
                    lineGeometry.Positions.Add(system.Pipes[i].Nodes[j].Position.ToHelixVector3());
                    lineGeometry.Indices.Add(lineGeometry.Positions.Count - 1);
                    lineGeometry.Indices.Add(lineGeometry.Positions.Count - 2);
                    lineGeometry.Colors.Add(new Color4(1.0f, 0.5f, 0.1f, 1f));
                }

                this.lineModel = new LineGeometryModel3D()
                {
                    Geometry = lineGeometry,
                    Color = Color.FromArgb(255, 255, 255, 255),
                    Thickness = 3,
                };

                this.lineModel.Geometry.UpdateVertices();
                this.viewport.Items.Add(this.lineModel);
            }

            //linebuilder smartrouted line
            var lineBuilder = new LineBuilder();
            LineGeometryModel3D lineModel = new LineGeometryModel3D();
            for (int i = 0; i < system.Pipes.Count; i++)
            {
                List<Vector3> vectors = new List<Vector3>();
                for (int j = 0; j < system.Pipes[i].Nodes.Count - 1; j++)
                {
                    lineBuilder.AddLine(system.Pipes[i].Nodes[j].Position.ToHelixVector3(), system.Pipes[i].Nodes[j + 1].Position.ToHelixVector3());

                }
            }
            lineModel.Geometry = lineBuilder.ToLineGeometry3D();
            lineModel.Geometry.UpdateVertices();
            this.viewport.Items.Add(lineModel);

            if (this.system.SelectedNode == null)
            {
                Debug.WriteLine("not found");
            }

            //LineGeometry3D smartrouted

            for (int i = 0; i < system.Pipes.Count; i++)
            {
                for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
                {
                    //Start/End Edge는 다시 그리지 않는다.
                    if (this.system.StartEndNodes.Contains(system.Pipes[i].Nodes[j]))
                        continue;

                    if (this.system.SelectedNode != null)
                    {
                        if (this.system.SelectedNode == system.Pipes[i].Nodes[j])
                        {
                            MeshBuilder selectedEdgeBuilder = new MeshBuilder();
                            //edgeBuilder.AddSphere(system.Pipes[i].Nodes[j].Position.ToHelixVector3());
                            selectedEdgeBuilder.AddBox(system.Pipes[i].Nodes[j].Position.ToHelixVector3(), xlength: 2, zlength: 2, ylength: 2);

                            MeshGeometryModel3D selectMeshModel = new MeshGeometryModel3D()
                            {
                                Geometry = selectedEdgeBuilder.ToMesh(),
                                Material = this.SelectedEdgeMaterial,
                                IsThrowingShadow = true
                            };

                            //meshModel.Transform = new TranslateTransform3D(0, 0, 0);
                            selectMeshModel.Geometry.UpdateVertices();
                            this.viewport.Items.Add(selectMeshModel);

                            continue;

                        }
                        else
                        {

                        }
                    }
                    MeshBuilder edgeBuilder = new MeshBuilder();
                    edgeBuilder.AddSphere(system.Pipes[i].Nodes[j].Position.ToHelixVector3());

                    MeshGeometryModel3D meshModel = new MeshGeometryModel3D()
                    {
                        Geometry = edgeBuilder.ToMesh(),
                        Material = this.EdgeMaterial,
                        IsThrowingShadow = true
                    };

                    //meshModel.Transform = new TranslateTransform3D(0, 0, 0);
                    meshModel.Geometry.UpdateVertices();
                    this.viewport.Items.Add(meshModel);
                }

                CreateSMRPipeFile();
            }

            //////////////mesh builder
            //MeshBuilder edgeBuilder = new MeshBuilder();
            //MeshGeometryModel3D edgeModel = new MeshGeometryModel3D();
            //for (int i = 0; i < system.Pipes.Count; i++)
            //{
            //    for (int j = 0; j < system.Pipes[i].Nodes.Count; j++)
            //    {
            //        edgeBuilder.AddSphere(system.Pipes[i].Nodes[j].Position.ToHelixVector3());
            //    }
            //}
            //
            //edgeModel.Geometry = edgeBuilder.ToMeshGeometry3D();
            //edgeModel.Geometry.UpdateVertices();
            //this.viewport.Items.Add(edgeModel);
        }

        private DispatcherTimer timer;
        int itercount = 0;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.D5)
            {
                light.Direction = new Vector3D(light.Direction.X + 1, -1, -1);
                var a = shadowMap.LightCamera;

                Vector3 lastPos = lineModel.Geometry.Positions.Last();
                lineModel.Geometry.Positions.Add(lastPos);
                lineModel.Geometry.Positions.Add(
                    new Vector3(
                        lastPos.X + (float)random.NextDouble(-2, 2),
                        lastPos.Y + (float)random.NextDouble(-2, 2),
                        0f));
                lineModel.Geometry.Indices.Add(lineModel.Geometry.Indices.Count);
                lineModel.Geometry.Indices.Add(lineModel.Geometry.Indices.Count);
                lineModel.Geometry.Colors.Add(lineModel.Geometry.Colors.Last());
                lineModel.Geometry.Colors.Add(new Color4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1f));
            }
            lineModel.Geometry.UpdateVertices();
            lineModel.Geometry.UpdateTriangles();
            lineModel.Geometry.UpdateColors();

            if (e.Key == Key.D4)
            {
                CreateSMRPipeFile();
            }

            if (e.Key == Key.D1)
            {
                system.MouseEditMode = 0;
            }

            if (e.Key == Key.D2)
            {
                system.MouseEditMode = 1;
            }

            if (e.Key == Key.D3)
            {
                system.MouseEditMode = 2;
            }
        }

        public string[] extractCoord(string txt)
        {
            string[] txtline = txt.Split('\n');
            string onlycoord = "";
            for (int i = 0; i < txtline.Length; i++)
            {
                if (txtline[i].Contains("="))
                {
                    onlycoord += txtline[i] + "\n";
                }
            }
            string[] onlycoords = onlycoord.Trim().Split('\n');
            return onlycoords;
        }

        public string[] extractStartCoord(string txt)
        {
            string startline = txt.Split('t')[3];
            string[] txtline = startline.Split('\n');
            string onlycoord = "";

            for (int i = 0; i < txtline.Length; i++)
            {
                if (txtline[i].Contains("="))
                {
                    onlycoord += txtline[i] + "\n";
                }
            }
            string[] onlycoords = onlycoord.Trim().Split('\n');
            return onlycoords;
        }

        public string[] extractEndCoord(string txt)
        {
            string endline = txt.Split('t')[4];
            string[] txtline = endline.Split('\n');
            string onlycoord = "";
            for (int i = 0; i < txtline.Length; i++)
            {
                if (txtline[i].Contains("="))
                {
                    onlycoord += txtline[i] + "\n";
                }
            }
            string[] onlycoords = onlycoord.Trim().Split('\n');
            return onlycoords;
        }

        public string[] extractMaxObsCoord(string txt)
        {
            string obsline = txt.Split('t')[1];
            string[] txtline = obsline.Split('\n');
            string onlycoord = "";
            for (int i = 0; i < txtline.Length; i++)
            {
                if (txtline[i].Contains("max="))
                {
                    onlycoord += txtline[i] + "\n";
                }
            }
            string[] onlycoords = onlycoord.Trim().Split('\n');
            return onlycoords;
        }

        public string[] extractMinObsCoord(string txt)
        {
            string obsline = txt.Split('t')[1];
            string[] txtline = obsline.Split('\n');
            string onlycoord = "";
            for (int i = 0; i < txtline.Length; i++)
            {
                if (txtline[i].Contains("min="))
                {
                    onlycoord += txtline[i] + "\n";
                }
            }
            string[] onlycoords = onlycoord.Trim().Split('\n');
            return onlycoords;
        }

        public string[] extractX(string[] coord)
        {
            string x = "";
            for (int i = 0; i < coord.Length; i++)
            {
                x += coord[i].Split('=')[1] + "\n";
            }
            string[] xlist = x.Trim().Split('\n');

            string Xa = "";
            for (int i = 0; i < xlist.Length; i++)
            {
                Xa += xlist[i].Split(' ')[0] + "\n";
            }
            string[] X = Xa.Trim().Split('\n');

            return X;
        }

        public string[] extractY(string[] coord)
        {
            string y = "";
            for (int i = 0; i < coord.Length; i++)
            {
                y += coord[i].Split('=')[2] + "\n";
            }
            string[] ylist = y.Trim().Split('\n');

            string Ya = "";
            for (int i = 0; i < ylist.Length; i++)
            {
                Ya += ylist[i].Split(' ')[0] + "\n";
            }
            string[] Y = Ya.Trim().Split('\n');

            return Y;
        }

        public string[] extractZ(string[] coord)
        {
            string z = "";
            for (int i = 0; i < coord.Length; i++)
            {
                z += coord[i].Split('=')[3] + "\n";
            }
            string[] zlist = z.Trim().Split('\n');

            string Za = "";
            for (int i = 0; i < zlist.Length; i++)
            {
                Za += zlist[i].Split(' ')[0] + "\n";
            }
            string[] Z = Za.Trim().Split('\n');

            return Z;
        }

        /*
        private void lineModel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point location = e.GetPosition(this.viewport);
            try
            {
                //var viewport = (HelixViewport3D)sender;
                var firstHit = viewport.FindHits(e.GetPosition(viewport)).FirstOrDefault();
                //List<HitTestResult> result = viewport.FindHits(location).ToList();
            }
            catch
            {

            }
        }


        private void viewport_Mouse3DDown(object sender, RoutedEventArgs e)
        {
            FrameworkElement feSource = e.Source as FrameworkElement;
            //feSource.

        }

        private void viewport_Mouse3DMove(object sender, RoutedEventArgs e)
        {

        }


        private void viewport_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //timer.Stop();
            int test = this.system.SelectedNodeIndex;
            Pipe pipe = this.system.SelectedPipe;

            var firstHit = viewport.FindHits(e.GetPosition(viewport)).FirstOrDefault();
            if (firstHit != null)
            {
                if (firstHit.ModelHit != null)
                {
                    if (firstHit.ModelHit.GetType().Name.Equals(SMRType.LINE_GEOMETRY_MODEL_3D))
                    {
                        LineGeometryModel3D line3d = firstHit.ModelHit as LineGeometryModel3D;

                        Element3D element3D = firstHit.ModelHit as Element3D;
                        //line3d = element3D as LineGeometry3D;


                        if (line3d != null)
                        {
                            Vector3Collection vector = new Vector3Collection();
                            foreach (Vector3 vector3 in line3d.Geometry.Positions)
                            {
                                vector.Add(new Vector3(x: vector3.X, y: vector3.Y + 1, z: vector3.Z));
                            }
                            line3d.Geometry.Positions = vector;
                            line3d.Geometry.UpdateVertices();
                        }


                    }
                    else if (firstHit.ModelHit.GetType().Name.Equals(SMRType.MESH_GEOMETRY_MODEL_3D))
                    {
                        MeshGeometryModel3D mesh3d = firstHit.ModelHit as MeshGeometryModel3D;

                        if (mesh3d != null)
                        {
                            Vector3Collection vector = new Vector3Collection();
                            foreach (Vector3 vector3 in mesh3d.Geometry.Positions)
                            {
                                vector.Add(new Vector3(x: vector3.X, y: vector3.Y + 1, z: vector3.Z));
                            }
                            mesh3d.Geometry.Positions = vector;

                            mesh3d.Geometry.UpdateVertices();
                        }

                    }


                }



            }

            //timer.Start();
            //viewport.Items.Remove(firstHit);
            //var viewport = (HelixViewport3D)sender;
            //var firstHit = viewport.Viewport.FindHits(e.GetPosition(viewport)).FirstOrDefault();
            //if (firstHit != null)
            //{
            //    this.viewModel.Select(firstHit.Visual);
            //}
            //else
            //{
            //    this.viewModel.Select(null);
            //}
        }

        */

    }

}