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
        private const double Epsilon = 1e-7;

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
        /// Получение коэффициента смещения кривой для смещения ее в сторону точки
        /// </summary>
        public static int GetOffsetCoefficient(this Curve curve, Point3d point)
        {
            int offsetCoefficient = 1;
            DBObjectCollection acDbObjColl = curve.GetOffsetCurves(offsetCoefficient * 0.001);
            foreach (Curve acEnt in acDbObjColl)
            {
                if (point.DistanceTo(curve.GetClosestPointTo(point, false)) <
                    point.DistanceTo(acEnt.GetClosestPointTo(point, false)))
                    offsetCoefficient = -1;
                break;
            }

            return offsetCoefficient;
        }

        /// <summary>
        /// Смещение кривой на заданное расстояние с заданними размерами
        /// </summary>
        /// <param name="curve">Кривая которая будет смещена</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="offsetCoefficient">Коэффициент смещения</param>
        /// <param name="size">Необходимый размер кривой</param>
        /// <returns>Коллекция объектов соответствующая коммуникации с заданными размерами</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        /// <exception cref="ArgumentException"></exception>
        public static DBObjectCollection OffsetWithSize(
            this Curve curve,
            double offset,
            double offsetCoefficient,
            double size)
        {
            DBObjectCollection acDbObjColl = curve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (DBObject cur in curve.GetOffsetCurves((offset + size) * offsetCoefficient))
            {
                acDbObjColl.Add(cur);
            }

            if (acDbObjColl.Count <= 1)
                throw new InvalidOperationException();
            var curve1 = acDbObjColl[0] as Curve;
            var curve2 = acDbObjColl[1] as Curve;
            if (curve1 is null || curve2 is null)
                throw new ArgumentException();
            acDbObjColl.Add(new Line(curve1.StartPoint, curve2.StartPoint));
            acDbObjColl.Add(new Line(curve1.EndPoint, curve2.EndPoint));
            return acDbObjColl;
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

        public static void DrawNetworks(
            this Transaction transaction,
            Curve curve,
            double[] sizes,
            double offsetCoefficient,
            Networks[] networks,
            Curve[] ignores)
        {
            var distance = 1.0;
            Networks network;
            for (int i = 0; i < networks.Length; i++)
            {
                network = networks[i];
                Curve nextCurve;
                var deltas = ignores.Select(x =>
                    NetworkManager.GetDistance(network, NetworkManager.GetType(x.Layer))).ToArray();
                if (network == Networks.WaterPipe && sizes[0] != 0)
                    nextCurve = curve.Offset(sizes[0], distance * offsetCoefficient, ignores, deltas);
                else if (network == Networks.Sewer && sizes[1] != 0)
                    nextCurve = curve.Offset(sizes[1], distance * offsetCoefficient, ignores, deltas);
                else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                    nextCurve = curve.Offset(sizes[2], distance * offsetCoefficient, ignores, deltas);
                else
                    nextCurve = curve.Offset(distance * offsetCoefficient, ignores, deltas);
                nextCurve.Layer = NetworkManager.GetNetworkName(network);
                transaction.Draw(nextCurve);

                if (i >= networks.Length - 1)
                    continue;

                distance = curve.GetMinDistanceToCurve(nextCurve);
                distance += NetworkManager.GetDistance(networks[i], networks[i + 1]);
                if (network == Networks.WaterPipe && sizes[0] != 0)
                    distance += sizes[0] / 2;
                else if (network == Networks.Sewer && sizes[1] != 0)
                    distance += sizes[1] / 2;
                else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                    distance += sizes[2] / 2;
                ignores = ignores.Append(nextCurve).ToArray();
            }
        }

        /// <summary>
        /// Смещение кривой на заданное минимальное расстояние с учетом расстояний до других кривых и необходимых размеров самой кривой
        /// </summary>
        /// <param name="firstCurve">Кривая которая будет смещена</param>
        /// <param name="size">Необходимый размер кривой</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="otherCurves">Игнорируемые кривые</param>
        /// <param name="otherCurvesDeltas">Минимальные расстояния до игнорируемых кривых</param>
        /// <returns>Итоговая смещенная кривая соответствующая минимальным расстояниям</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        public static Curve Offset(this Curve firstCurve,
            double size,
            double offset,
            Curve[] otherCurves,
            double[] otherCurvesDeltas)
        {
            var offsetCoefficient = offset / Math.Abs(offset);
            offset = Math.Abs(offset);
            var deltaOffset = 0.1;
            var coefficient = 1;

            DBObjectCollection acDbObjColl = firstCurve.OffsetWithSize(offset, offsetCoefficient, size);
            var allRightFlag = true;
            foreach (Curve newCurve in acDbObjColl)
            {
                var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                for (int i = 0; i < distances.Length; i++)
                {
                    if (distances[i] >= otherCurvesDeltas[i])
                        continue;
                    allRightFlag = false;
                    break;
                }

                if (!allRightFlag)
                    break;
            }

            if (allRightFlag)
            {
                var curves = firstCurve.GetOffsetCurves((offset + size / 2) * offsetCoefficient);
                foreach (Curve variableCurve in curves)
                {
                    return variableCurve;
                }
            }

            allRightFlag = true;
            while (deltaOffset > Epsilon)
            {
                while (true)
                {
                    acDbObjColl = firstCurve.OffsetWithSize(offset, offsetCoefficient, size);
                    if (acDbObjColl.Count == 0)
                        throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
                    foreach (Curve newCurve in acDbObjColl)
                    {
                        var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                        double[] d = new double[distances.Length];
                        for (int i = 0; i < distances.Length; i++)
                            d[i] = distances[i] - otherCurvesDeltas[i];

                        if (d.Any(x => x < 0 && coefficient < 0))
                            break;

                        if (!d.All(x => x > 0) && coefficient > 0 ||
                            !d.Any(x => x < 0) && coefficient < 0)
                        {
                            allRightFlag = false;
                            break;
                        }
                    }

                    if (allRightFlag)
                    {
                        coefficient *= -1;
                        break;
                    }

                    offset += deltaOffset * coefficient;
                    allRightFlag = true;
                }

                deltaOffset /= 10;
            }

            var resultCurves = firstCurve.GetOffsetCurves((offset + size / 2) * offsetCoefficient);
            foreach (Curve variableCurve in resultCurves)
            {
                return variableCurve;
            }

            throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
        }

        /// <summary>
        /// Смещение кривой на заданное минимальное расстояние с учетом расстояний до других кривых
        /// </summary>
        /// <param name="firstCurve">Кривая которая будет смещена</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="otherCurves">Игнорируемые кривые</param>
        /// <param name="otherCurvesDeltas">Минимальные расстояния до игнорируемых кривых</param>
        /// <returns>Итоговая смещенная кривая соответствующая минимальным расстояниям</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        public static Curve Offset(this Curve firstCurve,
            double offset,
            Curve[] otherCurves,
            double[] otherCurvesDeltas)
        {
            var offsetCoefficient = offset / Math.Abs(offset);
            offset = Math.Abs(offset);
            var deltaOffset = 0.1;
            var allRightFlag = true;
            var coefficient = 1;

            DBObjectCollection acDbObjColl = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (Curve newCurve in acDbObjColl)
            {
                var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                for (int i = 0; i < distances.Length; i++)
                {
                    if (distances[i] >= otherCurvesDeltas[i])
                        continue;
                    allRightFlag = false;
                    break;
                }

                if (!allRightFlag)
                    break;
                return newCurve;
            }

            allRightFlag = true;
            while (deltaOffset > Epsilon)
            {
                while (true)
                {
                    acDbObjColl = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
                    if (acDbObjColl.Count == 0)
                        throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
                    foreach (Curve newCurve in acDbObjColl)
                    {
                        var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                        double[] d = new double[distances.Length];
                        for (int i = 0; i < distances.Length; i++)
                            d[i] = distances[i] - otherCurvesDeltas[i];

                        if (d.All(x => x > 0) && coefficient > 0 ||
                            d.Any(x => x < 0) && coefficient < 0)
                        {
                            allRightFlag = false;
                            coefficient *= -1;
                            break;
                        }
                    }

                    if (!allRightFlag)
                        break;
                    offset += deltaOffset * coefficient;
                }

                deltaOffset /= 10;
                allRightFlag = true;
            }

            var resultCurves = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (Curve variableCurve in resultCurves)
            {
                return variableCurve;
            }

            throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
        }

        /// <summary>
        /// Получение суммы массива от индекса <paramref name="fromIndex"/> до индекса <paramref name="toIndex"/>
        /// </summary>
        public static double FromToSum(this double[] array, int fromIndex, int toIndex)
        {
            double sum = 0;
            for (int i = fromIndex; i < toIndex; i++)
                sum += array[i];
            return sum;
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
        public static double[] Cumulative(this double[] array)
        {
            var result = new double[array.Length];
            result[0] = array[0];
            for (int i = 1; i < array.Length; i++)
            {
                result[i] = result[i - 1] + array[i];
            }
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
    }
}