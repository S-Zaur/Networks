using Autodesk.AutoCAD.Geometry;

namespace Networks
{
    internal struct FromToPoints
    {
        public FromToPoints(Point3d from, Point3d to)
        {
            From = from;
            To = to;
        }

        public Point3d From { get; set; }
        public Point3d To { get; set; }
    }
}