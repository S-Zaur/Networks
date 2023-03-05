using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;

// ReSharper disable MemberCanBePrivate.Global

namespace Networks
{
    internal static class AutocadExtensions
    {
        /// <summary>
        /// Минимальное расстояние от кривой до кривой
        /// </summary>
        public static double GetMinDistanceToCurve(this Curve firstCurve, Curve secondCurve)
        {
            PointOnCurve3d[] pointOnCurve3d = firstCurve.GetGeCurve().GetClosestPointTo(secondCurve.GetGeCurve());
            Point3d pointOnFirstCurveClosestToSecondCurve = pointOnCurve3d.First().Point;
            Point3d pointOnSecondCurveClosestToFirstCurve =
                secondCurve.GetClosestPointTo(pointOnFirstCurveClosestToSecondCurve, false);
            return pointOnFirstCurveClosestToSecondCurve.DistanceTo(pointOnSecondCurveClosestToFirstCurve);
        }
        /// <summary>
        /// Добавление объекта на чертеж
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="entity">Объект который небходимо добавить на чертеж</param>
        public static void Draw(this Transaction transaction, Entity entity)
        {
            BlockTable acBlkTbl =
                transaction.GetObject(Autocad.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                    OpenMode.ForRead) as BlockTable;
            if (acBlkTbl is null) return;
            BlockTableRecord acBlkTblRec =
                transaction.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            if (acBlkTblRec is null) return;

            acBlkTblRec.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
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
        public static int Simplify(this Polyline polyline, Curve[] curves, double[] distances)
        {
            if (polyline.NumberOfVertices <= 2)
                return 0;
            var oldNumberOfVertices = polyline.NumberOfVertices;
            for (int i = 1; i < polyline.NumberOfVertices - 1;)
            {
                var point = polyline.GetPoint2dAt(i);

                polyline.RemoveVertexAt(i);

                for (int j = 0; j < curves.Length; j++)
                {
                    var curve = curves[j];
                    var distanceByTable = distances[j];
                    var distanceReal = curve.GetMinDistanceToCurve(polyline) + 0.01;

                    if (distanceReal > distanceByTable)
                        continue;

                    polyline.AddVertexAt(i, point, 0, 0, 0);
                    i++;
                    break;
                }
            }

            return polyline.NumberOfVertices - oldNumberOfVertices;
        }
        /// <summary>
        /// Алгоритм Джарвиса для Поиска минимальной выпуклой оболочки для полилинии
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
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
        public static Polyline Join(this Polyline firstPolyline, Polyline secondPolyline)
        {
            var polylineCopy = firstPolyline.Clone() as Polyline;
            if (polylineCopy is null)
                return new Polyline();

            for (int i = 0; i < secondPolyline.NumberOfVertices; i++)
            {
                polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,
                    secondPolyline.GetPoint2dAt(i),0,0,0);
            }

            return polylineCopy;
        }
        public static Polyline Join(this Polyline polyline, Line line)
        {
            var polylineCopy = polyline.Clone() as Polyline;
            if (polylineCopy is null)
                return new Polyline();

            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,line.StartPoint.Convert2d(new Plane()),0,0,0);
            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,line.EndPoint.Convert2d(new Plane()),0,0,0);

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

            result.AddVertexAt(0,firstLine.StartPoint.Convert2d(new Plane()),0,0,0);
            result.AddVertexAt(1,firstLine.EndPoint.Convert2d(new Plane()),0,0,0);
            result.AddVertexAt(2,secondLine.StartPoint.Convert2d(new Plane()),0,0,0);
            result.AddVertexAt(3,secondLine.EndPoint.Convert2d(new Plane()),0,0,0);
            
            return result;
        }
        public static int IntersectionsCount(this Entity first, Entity second)
        {
            Point3dCollection pts = new Point3dCollection();
            first.IntersectWith(second, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            return pts.Count;
        }
    }
}