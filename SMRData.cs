using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelixPlayground
{
    public partial class SMRData
    {
        public List<Triple> StartTriple = new List<Triple>();
        public List<Triple> EndTriple = new List<Triple>();
        public List<Triple> ObsMaxTriple = new List<Triple>();
        public List<Triple> ObsMinTriple = new List<Triple>();

        public string[] ObsMaxX = null;
        public string[] ObsMaxY = null;
        public string[] ObsMaxZ = null;
        public string[] ObsMinX = null;
        public string[] ObsMinY = null;
        public string[] ObsMinZ = null;

        public string[] StartX = null;
        public string[] StartY = null;
        public string[] StartZ = null;
        public string[] EndX = null;
        public string[] EndY = null;
        public string[] EndZ = null;

        public MeshGeometryModel3D meshModel1;
        public MeshGeometryModel3D meshModel2;
    }
}
