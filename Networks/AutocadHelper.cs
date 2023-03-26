using Autodesk.AutoCAD.ApplicationServices;
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
        private const int MaxDepth = 15;

        private static double _minAngle = Math.PI / 2;

        public static double MinAngle
        {
            get => _minAngle;
            set => _minAngle = value / 180 * Math.PI;
        }
        public static bool AllowIntersection { get; set; }
        private static readonly PolylineComparer Comparer = new PolylineComparer();
        private static IReadOnlyList<Curve> _ignoredCurves;
        private static IReadOnlyList<double> _distancesToIgnoredCurves;
        private static IReadOnlyList<Curve> _redLines;
        private static IReadOnlyList<double> _pipeSizes;

        private static double _bestDistance;
        private static Polyline _bestPolyline;
        private static Point3d _startPoint;

        public static void DrawNetworks(Dictionary<Networks, FromToPoints> points,
            IReadOnlyList<double> sizes)
        {
            #region Init

            _pipeSizes = sizes;

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
                _redLines = buildingIgnores.Where(x => x.Layer == Properties.Settings.Default.RedLineLayerName)
                    .ToArray();
                foreach (var pair in points)
                {
                    var network = pair.Key;
                    _distancesToIgnoredCurves = _ignoredCurves.Select(x => NetworkManager.GetDistance(network,
                        NetworkManager.GetUniversalType(x.Layer))).ToArray();
                    AddSizes(network);

                    if (!AllowIntersection)
                        AddAdditionalPolylines();
                    tr.DrawNetwork(pair);
                }

                tr.Commit();
            }
        }
        
        private static void AddSizes(Networks network)
        {
            double size = 0;
            switch (network)
            {
                case Networks.WaterPipe:
                    size = _pipeSizes[0] / 2;
                    break;
                case Networks.Sewer:
                    size = _pipeSizes[1] / 2;
                    break;
                case Networks.HeatingNetworks:
                    size = _pipeSizes[2] / 2;
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

        private static void DrawNetwork(this Transaction tr, KeyValuePair<Networks, FromToPoints> points)
        {
            Networks network = points.Key;
            try
            {
                _startPoint = points.Value.From;
                _bestPolyline = null;
                _bestDistance = points.Value.From.DistanceTo(points.Value.To);
                var newLine = ConnectPoints(points.Value, 1);
                while (newLine.Simplify(_ignoredCurves, _distancesToIgnoredCurves) != 0)
                {
                }

                newLine.Layer = NetworkManager.GetNetworkName(network);
                tr.Draw(newLine);
                AddPolylineToIgnores(newLine);
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
                    AddPolylineToIgnores(_bestPolyline);
                    Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"Не удалось проложить сеть {NetworkManager.GetNetworkName(network)}. Проложен один из наиболее удачных вариантов\n");
                }
                else
                    Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"Не удалось проложить сеть {NetworkManager.GetNetworkName(network)}\n");
            }
        }
        
        private static void AddPolylineToIgnores(Polyline polyline)
        {
            var network = NetworkManager.GetType(polyline.Layer);
            switch (network)
            {
                case Networks.WaterPipe:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(_pipeSizes[0])).ToArray();
                    break;
                case Networks.Sewer:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(_pipeSizes[1])).ToArray();
                    break;
                case Networks.HeatingNetworks:
                    _ignoredCurves = _ignoredCurves.Append(polyline.DoubleOffset(_pipeSizes[2])).ToArray();
                    break;
                case Networks.PowerCable:
                case Networks.CommunicationCable:
                case Networks.GasPipe:
                    _ignoredCurves = _ignoredCurves.Append(polyline).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(network), network, null);
            }
        }

        private static Polyline ConnectPoints(FromToPoints points, int depth)
        {
            if (depth > MaxDepth)
                return null;
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(polyline.NumberOfVertices, points.From.Convert2d(new Plane()), 0, 0, 0);

            points.From = GetLastGoodPoint(points);
            polyline.AddVertexAt(polyline.NumberOfVertices, points.From.Convert2d(new Plane()), 0, 0, 0);
            if (points.From == points.To)
                return polyline;
            for (int i = 0; i < _ignoredCurves.Count; i++)
            {
                var curve = _ignoredCurves[i];
                var distance = _distancesToIgnoredCurves[i];
                if (points.From.DistanceTo(curve.GetClosestPointTo(points.From, false)) >= distance)
                    continue;

                var bypassedPoints = BypassCurve(points, new CurveAndDistance(curve, distance)).ToArray();
                bypassedPoints = bypassedPoints
                    .Where(x => !double.IsNaN(x.X) && !double.IsNaN(x.Y) && !double.IsNaN(x.Z)).ToArray();
                if (bypassedPoints.Length == 0) return null;

                List<Polyline> variants = new List<Polyline>();

                foreach (var point in bypassedPoints)
                {
                    polyline.AddVertexAt(polyline.NumberOfVertices, point.Convert2d(new Plane()), 0, 0, 0);
                    if (!CanIntersect(curve, new FromToPoints(point, points.To)) &&
                        !AllowIntersection)
                        continue;
                    var pointFromWithIntersect = IntersectCurve(new FromToPoints(point, points.To),
                        new CurveAndDistance(curve, distance));
                    if (IntersectRedLines(new FromToPoints(point, pointFromWithIntersect)))
                        continue;
                    variants.Add(polyline.Join(ConnectPoints(new FromToPoints(pointFromWithIntersect, points.To),
                        depth + 1)));
                }

                variants.Add(
                    polyline.Join(ConnectPoints(new FromToPoints(bypassedPoints.Last(), points.To), depth + 1)));

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

        private static Point3d GetLastGoodPoint(FromToPoints points)
        {
            if (!CheckAllDistances(points.From))
                return points.From;
            Vector3d vector3d = points.To - points.From;
            vector3d *= Delta / vector3d.Length;
            do
            {
                points.From += vector3d;
            } while (points.From.DistanceTo(points.To) > 2 * Delta && CheckAllDistances(points.From));

            if (points.From.DistanceTo(points.To) < 2 * Delta)
                return points.To;
            return points.From;
        }
        
        private static IEnumerable<Point3d> BypassCurve(FromToPoints points, CurveAndDistance curveAndDistance)
        {
            Vector3d prevVector = new Vector3d();
            int prevSign = 0;
            do
            {
                var derivative =
                    curveAndDistance.Curve.GetFirstDerivative(
                        curveAndDistance.Curve.GetClosestPointTo(points.From, false), true);
                derivative *= Delta / derivative.Length;

                if (prevVector == derivative)
                {
                    points.From += derivative * prevSign;
                    points.From = MovePointAway(points.From, curveAndDistance);
                    continue;
                }

                yield return points.From;

                var point1 = points.From + derivative;
                var point2 = points.From - derivative;
                prevSign = points.To.DistanceTo(point1) < points.To.DistanceTo(point2) ? 1 : -1;
                points.From += derivative * prevSign;

                points.From = MovePointAway(points.From, curveAndDistance);
                prevVector = derivative;
            } while (!CanNextStep(points, curveAndDistance) && CheckAllDistances(points.From));

            yield return points.From;
        }

        private static Point3d IntersectCurve(FromToPoints points, CurveAndDistance curveAndDistance)
        {
            var vector = points.To - points.From;
            var derivative = curveAndDistance.Curve.GetFirstDerivative(
                curveAndDistance.Curve.GetClosestPointTo(points.From, false),
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
                points.From += vector;
            } while (points.From.DistanceTo(curveAndDistance.Curve.GetClosestPointTo(points.From, false)) <
                     curveAndDistance.Distance);

            return points.From;
        }
        
        private static bool CanIntersect(Entity entity, FromToPoints points)
        {
            var count = entity.IntersectionsCount(new Line(points.From, points.To));
            return count > 0 && count < 5 && entity.Layer != Properties.Settings.Default.RedLineLayerName;
        }
        
        private static bool IntersectRedLines(FromToPoints points)
        {
            Line line = new Line(points.From, points.To);
            return _redLines.Any(x => line.IntersectionsCount(x) > 0);
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

        private static bool CanNextStep(FromToPoints points, CurveAndDistance curveAndDistance)
        {
            var vectorToCheck = points.To - points.From;
            vectorToCheck *= Delta / vectorToCheck.Length;
            var pointToCheck = points.From + vectorToCheck;
            return pointToCheck.DistanceTo(curveAndDistance.Curve.GetClosestPointTo(pointToCheck, false)) >
                   curveAndDistance.Distance;
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

        private static Point3d MovePointAway(Point3d point, CurveAndDistance curveAndDistance)
        {
            if (curveAndDistance.Curve.GetClosestPointTo(point, false).DistanceTo(point) >= curveAndDistance.Distance)
                return point;

            var vector = point - curveAndDistance.Curve.GetClosestPointTo(point, false);
            var old = vector;
            vector *= curveAndDistance.Distance / vector.Length;
            vector = vector.Subtract(old);
            point += vector;

            return point;
        }
    }
}