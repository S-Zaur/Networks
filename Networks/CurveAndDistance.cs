using Autodesk.AutoCAD.DatabaseServices;

namespace Networks
{
    public struct CurveAndDistance
    {
        public CurveAndDistance(Curve curve, double distance)
        {
            Curve = curve;
            Distance = distance;
        }

        public Curve Curve { get; set; }
        public double Distance { get; set; }
    }
}