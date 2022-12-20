public partial class MainWindow
{
    // <Custom additional code>


    public class Node
    {
        public Triple Position;
        public Triple Velocity;
        public Triple TotalMove;
        public double TotalWeight;
        public bool IsFixed;

        public Node(Triple position, bool isFixed = false)
        {
            Position = position;
            Velocity = Triple.Zero;
            IsFixed = isFixed;
        }
    }

    // </Custom additional code>
}