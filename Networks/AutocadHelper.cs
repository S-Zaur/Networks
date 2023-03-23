using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.Colors;

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
        private static int MaxDepth = 10;

        private static double _minAngle = Math.PI / 2;
        public static double MinAngle
        {
            get => _minAngle;
            set => _minAngle = value / 180 * Math.PI;
        }

        private static readonly PolylineComparer Comparer = new PolylineComparer();
        private static IReadOnlyList<Curve> _ignoredCurves;
        private static IReadOnlyList<double> _distancesToIgnoredCurves;

        private static double _bestDistance;
        private static Polyline _bestPolyline;
        private static Point3d _startPoint;

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
                _startPoint = points.Value.First;
                _bestPolyline = null;
                _bestDistance = points.Value.First.DistanceTo(points.Value.Second);
                var newLine = ConnectPoints(points.Value.First, points.Value.Second, 1);
                while (newLine.Simplify(_ignoredCurves, _distancesToIgnoredCurves) != 0)
                {
                }

                newLine.Layer = NetworkManager.GetNetworkName(network);
                tr.Draw(newLine);
                AddPolylineToIgnores(newLine, sizes);
            }
            catch (Exception)
            {
                if (_bestPolyline is object)
                {
                    while (_bestPolyline.Simplify(_ignoredCurves, _distancesToIgnoredCurves) != 0)
                    {
                    }

                    _bestPolyline.Layer = NetworkManager.GetNetworkName(network);
                    tr.Draw(_bestPolyline);
                    AddPolylineToIgnores(_bestPolyline, sizes);
                    Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"Не удалось проложить сеть {NetworkManager.GetNetworkName(network)}. Проложен один из наиболее удачных вариантов\n");
                }
                else
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

        private static void AddPolylineToIgnores(Polyline polyline, IReadOnlyList<double> sizes)
        {
            var network = NetworkManager.GetType(polyline.Layer);
            switch (network)
            {
                case Networks.WaterPipe:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(sizes[0])).ToArray();
                    break;
                case Networks.Sewer:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(sizes[1])).ToArray();
                    break;
                case Networks.HeatingNetworks:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(sizes[2])).ToArray();
                    break;
                default:
                    _ignoredCurves = _ignoredCurves.Append(polyline).ToArray();
                    break;
            }
        }

        private static Polyline ConnectPoints(Point3d pointFrom, Point3d pointTo, int depth)
        {
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
                if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) >= distance)
                    continue;

                var bypassedPoints = BypassCurve(pointFrom, pointTo, curve, distance).ToArray();
                bypassedPoints = bypassedPoints
                    .Where(x => !double.IsNaN(x.X) && !double.IsNaN(x.Y) && !double.IsNaN(x.Z)).ToArray();
                if (bypassedPoints.Length == 0) return null;

                List<Polyline> variants = new List<Polyline>();

                foreach (var point in bypassedPoints)
                {
                    polyline.AddVertexAt(polyline.NumberOfVertices, point.Convert2d(new Plane()), 0, 0, 0);
                    if (!CanIntersect(curve, point, pointTo) && !Properties.Settings.Default.AllowIntersection)
                        continue;
                    var pointFromWithIntersect = IntersectCurve(point, pointTo, curve, distance);
                    variants.Add(polyline.Join(ConnectPoints(pointFromWithIntersect, pointTo, depth + 1)));
                }

                variants.Add(polyline.Join(ConnectPoints(bypassedPoints.Last(), pointTo, depth + 1)));

                variants = variants.Where(x => x is object).ToList();
                if (variants.Count == 0)
                    return null;
                variants.Sort(Comparer);
                polyline = variants.First();
                break;
            }

            MemorizePolyline(polyline);
            return polyline;
        }

        private static Point3d IntersectCurve(Point3d pointFrom, Point3d pointTo, Curve curve, double distance)
        {
            // TODO Проверять на пересечение с красной линией 
            var point = pointFrom;
            var vector = pointTo - point;
            var derivative = curve.GetFirstDerivative(
                curve.GetClosestPointTo(point, false),
                true
            );
            var angle = vector.GetAngleTo(derivative);
            angle = Math.Min(angle, Math.PI - angle);
            vector *= Delta / vector.Length;
            if (angle < MinAngle - 0.01)
            {
                var deltaAngle = MinAngle - angle;
                vector = vector.RotateBy(deltaAngle, new Vector3d(0, 0, 1));
                angle = vector.GetAngleTo(derivative);
                angle = Math.Min(angle, Math.PI - angle);
                if (angle < MinAngle - 0.01)
                    vector = vector.RotateBy(-2 * deltaAngle, new Vector3d(0, 0, 1));
            }

            do
            {
                point += vector;
            } while (point.DistanceTo(curve.GetClosestPointTo(point, false)) < distance);

            return point;
        }

        private static IEnumerable<Point3d> BypassCurve(Point3d pointFrom, Point3d pointTo,
            Curve curve,
            double distance)
        {
            // TODO Можно поробовать идти в обе стороны
            Vector3d prevVector = new Vector3d();
            int prevSign = 0;
            do
            {
                var derivative = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFrom, false), true);
                derivative *= Delta / derivative.Length;

                if (prevVector == derivative)
                {
                    pointFrom += derivative * prevSign;
                    pointFrom = MovePointAway(pointFrom, curve, distance);
                    continue;
                }

                yield return pointFrom;

                var point1 = pointFrom + derivative;
                var point2 = pointFrom - derivative;
                prevSign = pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2) ? 1 : -1;
                pointFrom += derivative * prevSign;

                pointFrom = MovePointAway(pointFrom, curve, distance);
                prevVector = derivative;
            } while (!CanNextStep(pointFrom, pointTo, curve, distance) && CheckAllDistances(pointFrom));

            yield return pointFrom;
        }

        private static bool CheckAllDistances(Point3d point)
        {
            var currentDistances = _ignoredCurves.Select(x =>
                point.DistanceTo(x.GetClosestPointTo(point, false))
            ).ToArray();
            for (int i = 0; i < currentDistances.Length; i++)
                currentDistances[i] -= _distancesToIgnoredCurves[i];
            return currentDistances.All(x => x >= 0);
        }

        private static bool CanNextStep(Point3d pointFrom, Point3d pointTo, Curve curve, double distance)
        {
            var vectorToCheck = pointTo - pointFrom;
            vectorToCheck *= Delta / vectorToCheck.Length;
            var pointToCheck = pointFrom + vectorToCheck;
            return pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck, false)) > distance;
        }

        private static Point3d MovePointAway(Point3d point, Curve curve, double distance)
        {
            if (curve.GetClosestPointTo(point, false).DistanceTo(point) >= distance)
                return point;

            var vector = point - curve.GetClosestPointTo(point, false);
            var old = vector;
            vector *= distance / vector.Length;
            vector = vector.Subtract(old);
            point += vector;

            return point;
        }

        private static void MemorizePolyline(Polyline polyline)
        {
            var distance = _startPoint.DistanceTo(polyline.GetClosestPointTo(_startPoint, false));
            if (distance > _bestDistance)
                return;
            const double tolerance = 0.000000001;
            if (Math.Abs(distance - _bestDistance) < tolerance && _bestPolyline.Length < polyline.Length)
                return;
            _bestPolyline = polyline;
            _bestDistance = distance;
        }

        private static Point3d GetLastGoodPoint(Point3d pointFrom, Point3d pointTo)
        {
            if (!CheckAllDistances(pointFrom))
                return pointFrom;
            Vector3d vector3d = pointTo - pointFrom;
            vector3d *= Delta / vector3d.Length;
            do
            {
                pointFrom += vector3d;
            } while (pointFrom.DistanceTo(pointTo) > 2 * Delta && CheckAllDistances(pointFrom));

            if (pointFrom.DistanceTo(pointTo) < 2 * Delta)
                return pointTo;
            return pointFrom;
        }

        private static bool CanIntersect(Entity entity, Point3d pointFrom, Point3d pointTo)
        {
            var count = entity.IntersectionsCount(new Line(pointFrom, pointTo));
            return count > 0 && count < 5 && entity.Layer != Properties.Settings.Default.RedLineLayerName;
        }
    }
}