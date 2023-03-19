﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Networks
{
    internal class PolylineComparer : IComparer<Polyline>
    {
        public int Compare(Polyline x, Polyline y)
        {
            if (x is null || y is null)
                throw new ArgumentNullException();
            return x.Length < y.Length ? -1 : x.Length > y.Length ? 1 : 0;
        }
    }

    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    internal static class AutocadHelper
    {
        // Большой TODO Возможность сместить сроящуюся кривую к какой-то другой
        private const double Delta = 0.2;
        private const int MaxDepth = 10;
        public static int MinAngle { get; set; } = 90;
        private static readonly PolylineComparer Comparer = new PolylineComparer();
        private static IReadOnlyList<Curve> _ignoredCurves;
        private static IReadOnlyList<double> _distancesToIgnoredCurves;

        public static void DrawNetworks(Dictionary<Networks, Pair<Point3d, Point3d>> points,
            IReadOnlyList<double> sizes)
        {
            #region Init

            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            // Существующие сети которые нужно учесть
            var filterList = new[]
            {
                new TypedValue(-4, "<OR"),
                new TypedValue(0, "LINE"),
                new TypedValue(0, "LWPOLYLINE"),
                new TypedValue(-4, "OR>")
            };
            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Выберите коммуникации которые необходимо учесть(необязательно)"
            };
            PromptSelectionResult selRes = ed.GetSelection(selectionOptions, filter);
            ObjectId[] objectIds = selRes.Status != PromptStatus.OK
                ? Array.Empty<ObjectId>()
                : selRes.Value.GetObjectIds();

            selectionOptions.MessageForAdding = "Выберите здания и сооружения которые необходимо учесть(необязательно)";
            selRes = ed.GetSelection(selectionOptions, filter);
            ObjectId[] objectIdsBuildings = selRes.Status != PromptStatus.OK
                ? Array.Empty<ObjectId>()
                : selRes.Value.GetObjectIds();

            #endregion

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Curve[] ignores = objectIds.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve).ToArray();
                Curve[] buildingIgnores = objectIdsBuildings.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve)
                    .ToArray();
                _ignoredCurves = ignores.Concat(buildingIgnores).ToArray();
                foreach (var pair in points)
                {
                    var network = pair.Key;
                    _distancesToIgnoredCurves = _ignoredCurves.Select(x => NetworkManager.GetDistance(network,
                        NetworkManager.GetUniversalType(x.Layer))).ToArray();
                    AddSizes(network, sizes);

                    if (!Properties.Settings.Default.AllowIntersection)
                        AddAdditionalPolylines();
                    tr.DrawNetwork(pair, sizes);
                }

                tr.Commit();
            }
        }

        private static void DrawNetwork(this Transaction tr, KeyValuePair<Networks, Pair<Point3d, Point3d>> points,
            IReadOnlyList<double> sizes)
        {
            Networks network = points.Key;
            try
            {
                var newLine = ConnectPoints(points.Value.First, points.Value.Second, 1);
                while (newLine.Simplify(_ignoredCurves, _distancesToIgnoredCurves) != 0)
                {
                }

                newLine.Layer = NetworkManager.GetNetworkName(network);
                tr.Draw(newLine);

                switch (network)
                {
                    case Networks.WaterPipe:
                        _ignoredCurves = _ignoredCurves.Append(newLine.DoubleOffset(sizes[0])).ToArray();
                        break;
                    case Networks.Sewer:
                        _ignoredCurves = _ignoredCurves.Append(newLine.DoubleOffset(sizes[1])).ToArray();
                        break;
                    case Networks.HeatingNetworks:
                        _ignoredCurves = _ignoredCurves.Append(newLine.DoubleOffset(sizes[2])).ToArray();
                        break;
                    default:
                        _ignoredCurves = _ignoredCurves.Append(newLine).ToArray();
                        break;
                }
            }
            catch (Exception)
            {
                Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    $"Не удалось проложить сеть {NetworkManager.GetNetworkName(network)}\n");
            }
        }

        private static void AddSizes(Networks network, IReadOnlyList<double> sizes)
        {
            double size = 0;
            switch (network)
            {
                case Networks.WaterPipe:
                    size = sizes[0] / 2;
                    break;
                case Networks.Sewer:
                    size = sizes[1] / 2;
                    break;
                case Networks.HeatingNetworks:
                    size = sizes[2] / 2;
                    break;
                case Networks.PowerCable:
                case Networks.CommunicationCable:
                case Networks.GasPipe:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(network), network, null);
            }

            _distancesToIgnoredCurves = _distancesToIgnoredCurves.Select(x => x + size).ToArray();
        }

        private static void AddAdditionalPolylines()
        {
            var ignoresCount = _ignoredCurves.Count;

            for (int j = 0; j < ignoresCount; j++)
            {
                for (int k = j + 1; k < ignoresCount; k++)
                {
                    var ignoreA = _ignoredCurves[j];
                    var ignoreB = _ignoredCurves[k];
                    var distanceBetween = ignoreA.GetMinDistanceToCurve(ignoreB);
                    var distanceA = _distancesToIgnoredCurves[j];
                    var distanceB = _distancesToIgnoredCurves[k];
                    if (distanceA + distanceB <= distanceBetween)
                        continue;

                    Polyline additionalPolyline = ignoreA.Join(ignoreB);
                    _ignoredCurves = _ignoredCurves.Append(additionalPolyline).ToArray();
                    _distancesToIgnoredCurves =
                        _distancesToIgnoredCurves.Append(Math.Min(distanceA, distanceB)).ToArray();
                }
            }
        }

        private static Polyline ConnectPoints(Point3d pointFrom, Point3d pointTo, int depth)
        {
            // ВОПРОС Сейчас находит лучшее либо ничего. Если не получислось построить лучшее вернуть почти лучшее?
            // TODO Оптимизация работы если конечная точка на кривой
            if (depth > MaxDepth)
                return null;
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(polyline.NumberOfVertices, pointFrom.Convert2d(new Plane()), 0, 0, 0);

            pointFrom = GetLastGoodPoint(pointFrom, pointTo);
            polyline.AddVertexAt(polyline.NumberOfVertices, pointFrom.Convert2d(new Plane()), 0, 0, 0);
            if (pointFrom == pointTo)
                return polyline;
            for (int i = 0; i < _ignoredCurves.Count; i++)
            {
                var curve = _ignoredCurves[i];
                var distance = _distancesToIgnoredCurves[i];
                if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) >= distance + Delta)
                    continue;

                var pointsWithOutIntersect =
                    GetPointWithOutIntersect(pointFrom, pointTo, curve, distance).ToArray();
                pointsWithOutIntersect = pointsWithOutIntersect
                    .Where(x => !double.IsNaN(x.X) && !double.IsNaN(x.Y) && !double.IsNaN(x.Z)).ToArray();
                if (pointsWithOutIntersect.Length == 0) return null;

                List<Polyline> variants = new List<Polyline>();

                foreach (var point in pointsWithOutIntersect)
                {
                    polyline.AddVertexAt(polyline.NumberOfVertices, point.Convert2d(new Plane()), 0, 0, 0);
                    if (!CanIntersect(curve, point, pointTo) && !Properties.Settings.Default.AllowIntersection)
                        continue;
                    var pointFromWithIntersect = GetPointWithIntersect(point, pointTo, curve, distance);
                    variants.Add(polyline.Join(ConnectPoints(pointFromWithIntersect, pointTo, depth + 1)));
                }

                variants.Add(polyline.Join(ConnectPoints(pointsWithOutIntersect.Last(), pointTo, depth + 1)));

                variants = variants.Where(x => x is object).ToList();
                if (variants.Count == 0)
                    return null;
                variants.Sort(Comparer);
                polyline = variants.First();
                break;
            }

            return polyline;
        }

        private static Point3d GetPointWithIntersect(Point3d pointFrom, Point3d pointTo, Curve curve, double distance)
        {
            // TODO поработать с названиями переменных
            var pointFromWithIntersect = pointFrom;
            var vector1 = pointTo - pointFromWithIntersect;
            var point = curve.GetClosestPointTo(pointFromWithIntersect, false);
            var vector2 = curve.GetFirstDerivative(point, true);
            var angle = vector1.GetAngleTo(vector2) / Math.PI * 180;
            if (angle > 90)
                angle = 180 - angle;
            var vector3d = pointTo - pointFromWithIntersect;
            vector3d *= Delta / vector3d.Length;
            if (angle < MinAngle - 0.01)
            {
                var deltaAngle = MinAngle - angle;
                deltaAngle = deltaAngle / 180 * Math.PI;
                vector3d = vector3d.RotateBy(deltaAngle, new Vector3d(0, 0, 1));
                angle = vector3d.GetAngleTo(vector2) / Math.PI * 180;
                if (angle - 0.01 > 90)
                    angle = 180 - angle;
                if (angle < MinAngle - 0.01)
                    vector3d = vector3d.RotateBy(-2 * deltaAngle, new Vector3d(0, 0, 1));
            }

            do
            {
                pointFromWithIntersect += vector3d;
            } while (pointFromWithIntersect.DistanceTo(curve.GetClosestPointTo(pointFromWithIntersect, false)) <
                     distance);

            return pointFromWithIntersect;
        }

        private static IEnumerable<Point3d> GetPointWithOutIntersect(Point3d pointFrom, Point3d pointTo,
            Curve curve,
            double distance)
        {
            // TODO Можно поробовать идти в обе стороны
            // TODO Проверять на пересечение с красной линией 
            // TODO Упростить
            Vector3d vectorMem = new Vector3d();
            var signMem = 0;
            var pointFromWithOutIntersect = pointFrom;
            Point3d pointToCheck;
            double[] currentDistances;
            do
            {
                Vector3d vectorToCheck;
                var vector3d =
                    curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false), true);
                vector3d *= Delta / vector3d.Length;
                if (vectorMem == vector3d)
                {
                    pointFromWithOutIntersect += vector3d * signMem;
                    if (curve.GetClosestPointTo(pointFromWithOutIntersect, false)
                            .DistanceTo(pointFromWithOutIntersect) < distance)
                    {
                        var vector = pointFromWithOutIntersect -
                                     curve.GetClosestPointTo(pointFromWithOutIntersect, false);
                        var old = vector;
                        var len = vector.Length;
                        vector *= distance / len;
                        vector = vector.Subtract(old);
                        pointFromWithOutIntersect += vector;
                    }

                    vectorToCheck = pointTo - pointFromWithOutIntersect;
                    vectorToCheck *= Delta / vectorToCheck.Length;
                    pointToCheck = pointFromWithOutIntersect + vectorToCheck;

                    currentDistances = _ignoredCurves
                        .Select(x =>
                            pointFromWithOutIntersect.DistanceTo(x.GetClosestPointTo(pointFromWithOutIntersect, false)))
                        .ToArray();
                    for (int i = 0; i < currentDistances.Length; i++)
                        currentDistances[i] -= _distancesToIgnoredCurves[i];

                    if (currentDistances.Any(x => x < 0))
                    {
                        yield return pointFromWithOutIntersect;
                        yield break;
                    }

                    continue;
                }

                yield return pointFromWithOutIntersect;

                var point1 = pointFromWithOutIntersect + vector3d;
                var point2 = pointFromWithOutIntersect - vector3d;

                if (pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2))
                {
                    pointFromWithOutIntersect += vector3d;
                    signMem = 1;
                }
                else
                {
                    pointFromWithOutIntersect -= vector3d;
                    signMem = -1;
                }

                if (curve.GetClosestPointTo(pointFromWithOutIntersect, false).DistanceTo(pointFromWithOutIntersect) <
                    distance)
                {
                    var vector = pointFromWithOutIntersect - curve.GetClosestPointTo(pointFromWithOutIntersect, false);
                    var old = vector;
                    var len = vector.Length;
                    vector *= distance / len;
                    vector = vector.Subtract(old);
                    pointFromWithOutIntersect += vector;
                }

                vectorToCheck = pointTo - pointFromWithOutIntersect;
                vectorToCheck *= Delta / vectorToCheck.Length;
                pointToCheck = pointFromWithOutIntersect + vectorToCheck;
                vectorMem = vector3d;

                currentDistances = _ignoredCurves.Select(x =>
                        pointFromWithOutIntersect.DistanceTo(x.GetClosestPointTo(pointFromWithOutIntersect, false)))
                    .ToArray();
                for (int i = 0; i < currentDistances.Length; i++)
                    currentDistances[i] -= _distancesToIgnoredCurves[i];
            } while (pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck, false)) < distance
                     && currentDistances.All(x => x >= 0));

            yield return pointFromWithOutIntersect;
        }

        private static Point3d GetLastGoodPoint(Point3d pointFrom, Point3d pointTo)
        {
            double[] currentDistances;
            Vector3d vector3d = pointTo - pointFrom;
            vector3d *= Delta / vector3d.Length;
            do
            {
                pointFrom += vector3d;
                currentDistances = _ignoredCurves
                    .Select(x => pointFrom.DistanceTo(x.GetClosestPointTo(pointFrom, false)))
                    .ToArray();
                for (int i = 0; i < currentDistances.Length; i++)
                    currentDistances[i] -= _distancesToIgnoredCurves[i];
            } while (pointFrom.DistanceTo(pointTo) > 2 * Delta
                     && currentDistances.All(x => x > 0));

            if (pointFrom.DistanceTo(pointTo) < 2 * Delta)
                return pointTo;
            pointFrom -= vector3d;
            return pointFrom;
        }

        private static bool CanIntersect(Entity entity, Point3d pointFrom, Point3d pointTo)
        {
            var count = entity.IntersectionsCount(new Line(pointFrom, pointTo));
            return count > 0 && count < 5 && entity.Layer != Properties.Settings.Default.RedLineLayerName;
        }

        public static Pair<Point3d, Point3d> GetStartEndPoints()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Editor ed = acDoc.Editor;

            PromptPointOptions optPoint = new PromptPointOptions($"Выберите первую точку\n");
            PromptPointResult pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                throw new Exception("Точка не выбрана");
            Point3d point1 = pointSelRes.Value;

            optPoint = new PromptPointOptions($"Выберите вторую точку\n");
            pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                throw new Exception("Точка не выбрана");
            Point3d point2 = pointSelRes.Value;

            return new Pair<Point3d, Point3d>(point1, point2);
        }
    }
}