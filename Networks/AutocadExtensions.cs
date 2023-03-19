using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;
using System.Collections.Generic;

// ReSharper disable MemberCanBePrivate.Global

namespace Networks
{
    internal static class AutocadExtensions
    {
        public static double GetMinDistanceToCurve(this Curve firstCurve, Curve secondCurve)
        {
            PointOnCurve3d[] pointOnCurve3d = firstCurve.GetGeCurve().GetClosestPointTo(secondCurve.GetGeCurve());
            Point3d pointOnFirstCurveClosestToSecondCurve = pointOnCurve3d.First().Point;
            Point3d pointOnSecondCurveClosestToFirstCurve =
                secondCurve.GetClosestPointTo(pointOnFirstCurveClosestToSecondCurve, false);
            return pointOnFirstCurveClosestToSecondCurve.DistanceTo(pointOnSecondCurveClosestToFirstCurve);
        }
        
        public static void Draw(this Transaction transaction, Entity entity)
        {
            if (entity is null) return;
            BlockTable acBlkTbl =
                transaction.GetObject(Autocad.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                    OpenMode.ForRead) as BlockTable;
            if (acBlkTbl is null) return;
            BlockTableRecord acBlkTblRec =
                transaction.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            if (acBlkTblRec is null) return;

            try
            {
                acBlkTblRec.AppendEntity(entity);
                transaction.AddNewlyCreatedDBObject(entity, true);
            }
            catch
            {
            }
        }

        public static void Simplify(this Polyline polyline)
        {
            if (polyline.NumberOfVertices <= 2)
                return;

            for (int i = 2; i < polyline.NumberOfVertices;)
            {
                var point1 = polyline.GetPoint2dAt(i - 2);
                var point2 = polyline.GetPoint2dAt(i - 1);
                var point3 = polyline.GetPoint2dAt(i);
                var segment = new LineSegment2d(point1, point3);
                if (segment.IsOn(point2))
                {
                    polyline.RemoveVertexAt(i - 1);
                    continue;
                }

                i++;
            }
        }

        public static int Simplify(this Polyline polyline, IReadOnlyList<Curve> curves, IReadOnlyList<double> distances)
        {
            if (polyline.NumberOfVertices <= 2)
                return 0;
            var oldNumberOfVertices = polyline.NumberOfVertices;
            for (int i = 1; i < polyline.NumberOfVertices - 2;)
            {
                if (polyline.GetPoint3dAt(i - 1) == polyline.GetPoint3dAt(i))
                {
                    polyline.RemoveVertexAt(i);
                    continue;
                }

                var line = new Line(polyline.GetPoint3dAt(i - 1), polyline.GetPoint3dAt(i + 1));
                var flag = true;
                for (int j = 0; j < curves.Count; j++)
                {
                    var curve = curves[j];
                    var distanceByTable = distances[j];
                    var distanceReal = curve.GetMinDistanceToCurve(line) + 0.01;

                    if (distanceReal > distanceByTable)
                        continue;
                    i++;
                    flag = false;
                    break;
                }

                if (flag)
                    polyline.RemoveVertexAt(i);
            }

            return polyline.NumberOfVertices - oldNumberOfVertices;
        }
        
        public static Polyline Jarvis(this Polyline polyline)
        {
            var polylineCopy = polyline.Clone() as Polyline;
            if (polylineCopy is null)
                return new Polyline();
            var result = new Polyline()
            {
                Layer = polyline.Layer
            };

            Point3d bottom = polyline.StartPoint;
            for (int i = 0; i < polyline.NumberOfVertices; i++)
                if (polyline.GetPoint3dAt(i).Y < bottom.Y)
                    bottom = polyline.GetPoint3dAt(i);
            result.AddVertexAt(result.NumberOfVertices, bottom.Convert2d(new Plane()), 0, 0, 0);

            var polarAngle = double.MaxValue;
            var polarPoint = new Point3d();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var point = polyline.GetPoint3dAt(i);
                var vector = point - bottom;
                var phi = Math.Atan2(vector.Y, vector.X);
                if (phi > 0 && phi < polarAngle)
                {
                    polarAngle = phi;
                    polarPoint = point;
                }
            }

            result.AddVertexAt(result.NumberOfVertices, polarPoint.Convert2d(new Plane()), 0, 0, 0);
            polylineCopy.RemoveVertexAt((int)polylineCopy.GetParameterAtPoint(polarPoint));

            do
            {
                polarAngle = double.MinValue;
                polarPoint = new Point3d();
                var vector = result.GetPoint3dAt(result.NumberOfVertices - 1) -
                             result.GetPoint3dAt(result.NumberOfVertices - 2);

                for (int i = 0; i < polylineCopy.NumberOfVertices; i++)
                {
                    var vector2 = polylineCopy.GetPoint3dAt(i) - result.GetPoint3dAt(result.NumberOfVertices - 1);
                    var phi = vector.DotProduct(vector2) / (vector.Length * vector2.Length);
                    if (phi > polarAngle)
                    {
                        polarAngle = phi;
                        polarPoint = polylineCopy.GetPoint3dAt(i);
                    }
                }

                result.AddVertexAt(result.NumberOfVertices, polarPoint.Convert2d(new Plane()), 0, 0, 0);
                if (polarPoint == polylineCopy.StartPoint && polarPoint == polylineCopy.EndPoint)
                    continue;
                if (polylineCopy.NumberOfVertices > 1)
                    polylineCopy.RemoveVertexAt((int)polylineCopy.GetParameterAtPoint(polarPoint));
            } while (polarPoint != result.StartPoint);

            return result;
        }

        public static Polyline Join(this Curve firstCurve, Curve secondCurve)
        {
            if (firstCurve is Polyline fc && secondCurve is Polyline sc)
                return fc.Join(sc).Jarvis();
            if (firstCurve is Polyline fc2 && secondCurve is Line sc2)
                return fc2.Join(sc2).Jarvis();
            if (firstCurve is Line fc3 && secondCurve is Polyline sc3)
                return fc3.Join(sc3).Jarvis();
            if (firstCurve is Line fc4 && secondCurve is Line sc4)
                return fc4.Join(sc4).Jarvis();
            throw new ArgumentException("Only line and polyline are allowed");
        }

        public static Polyline Join(this Polyline firstPolyline, Polyline secondPolyline)
        {
            var polylineCopy = firstPolyline.Clone() as Polyline;
            if (polylineCopy is null || secondPolyline is null)
                return null;

            for (int i = 0; i < secondPolyline.NumberOfVertices; i++)
            {
                polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,
                    secondPolyline.GetPoint2dAt(i), 0, 0, 0);
            }

            return polylineCopy;
        }

        public static Polyline Join(this Polyline polyline, Line line)
        {
            var polylineCopy = polyline.Clone() as Polyline;
            if (polylineCopy is null)
                return new Polyline();

            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices, line.StartPoint.Convert2d(new Plane()), 0, 0, 0);
            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices, line.EndPoint.Convert2d(new Plane()), 0, 0, 0);

            return polylineCopy;
        }

        public static Polyline Join(this Line line, Polyline polyline)
        {
            return polyline.Join(line);
        }

        public static Polyline Join(this Line firstLine, Line secondLine)
        {
            Polyline result = new Polyline()
            {
                Layer = firstLine.Layer
            };

            result.AddVertexAt(0, firstLine.StartPoint.Convert2d(new Plane()), 0, 0, 0);
            result.AddVertexAt(1, firstLine.EndPoint.Convert2d(new Plane()), 0, 0, 0);
            result.AddVertexAt(2, secondLine.StartPoint.Convert2d(new Plane()), 0, 0, 0);
            result.AddVertexAt(3, secondLine.EndPoint.Convert2d(new Plane()), 0, 0, 0);

            return result;
        }

        public static Polyline TryJoin(this Polyline polyline, Polyline first, Polyline second)
        {
            if (first is null && second is null)
                return null;
            if (first is null)
                polyline = polyline.Join(second);
            else if (second is null)
                polyline = polyline.Join(first);
            else
                polyline = polyline.Join(first.Length < second.Length ? first : second);

            return polyline;
        }

        public static Polyline ToPolyline(this Vector3d vector, Point3d cs)
        {
            var result = new Polyline();
            result.AddVertexAt(0, cs.Convert2d(new Plane()), 0, 0, 0);
            cs += vector;
            result.AddVertexAt(1, cs.Convert2d(new Plane()), 0, 0, 0);
            vector *= 0.2;
            vector = vector.RotateBy(Math.PI * 3 / 4, new Vector3d(0, 0, 1));
            cs += vector;
            result.AddVertexAt(2, cs.Convert2d(new Plane()), 0, 0, 0);

            vector = vector.RotateBy(Math.PI * 3 / 4, new Vector3d(0, 0, 1));
            vector *= Math.Sqrt(2);
            cs += vector;
            result.AddVertexAt(3, cs.Convert2d(new Plane()), 0, 0, 0);

            vector = vector.RotateBy(Math.PI * 3 / 4, new Vector3d(0, 0, 1));
            vector /= Math.Sqrt(2);
            cs += vector;
            result.AddVertexAt(4, cs.Convert2d(new Plane()), 0, 0, 0);

            return result;
        }

        public static int IntersectionsCount(this Entity first, Entity second)
        {
            Point3dCollection pts = new Point3dCollection();
            first.IntersectWith(second, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            return pts.Count;
        }

        public static Vector3d GetFirstDerivative(this Curve curve, Point3d point, bool checkEnd)
        {
            if (!checkEnd)
                return curve.GetFirstDerivative(point);
            if (point == curve.EndPoint)
                point = curve.GetPointAtDist(curve.GetDistanceAtParameter(curve.EndParam) - 0.0001);
            return curve.GetFirstDerivative(point);
        }

        public static Polyline DoubleOffset(this Polyline curve, double size)
        {
            var offset1 = curve.GetOffsetCurves(size / 2);
            var offset2 = curve.GetOffsetCurves(-size / 2);
            Polyline res = null;
            foreach (DBObject c in offset1)
            {
                res = c.Clone() as Polyline;
            }

            if (res is null) throw new InvalidOperationException();
            res.SetBulgeAt(res.NumberOfVertices - 1, 0);
            foreach (DBObject c in offset2)
            {
                var pl = c as Polyline;
                if (pl is null) throw new InvalidOperationException();
                for (int i = pl.NumberOfVertices - 1; i >= 0; i--)
                {
                    res.AddVertexAt(res.NumberOfVertices,
                        pl.GetPoint2dAt(i),
                        -pl.GetBulgeAt(Math.Max(i - 1, 0)),
                        0,
                        0);
                }
            }
            res.AddVertexAt(res.NumberOfVertices,
                res.GetPoint2dAt(0),
                0,
                0,
                0);
            return res;
        }
    }
}