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
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    internal static class AutocadHelper
    {
        private const double Delta = 0.2;
        private const int MaxDepth = 15;
        public static int MinAngle { get; set; } = 90;

        public static void DrawNetworks(Dictionary<Networks, Pair<Point3d, Point3d>> points, double[] sizes)
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
                //if (!Properties.Settings.Default.AllowIntersection)
                //    ignores = ignores.Select(x => x is Polyline y ? y.Jarvis() : x).ToArray();

                Curve[] buildingIgnores = objectIdsBuildings.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve)
                    .ToArray();

                foreach (var pair in points)
                {
                    var network = pair.Key;
                    var currentIgnores = ignores.Select(x => x).ToArray();
                    double[] distanceToIgnores = ignores.Select(x => NetworkManager.GetDistance(network,
                        NetworkManager.GetType(x.Layer))).ToArray();

                    double[] distanceToBuildingIgnores = buildingIgnores.Select(x =>
                        NetworkManager.GetDistanceToBuilding(network,
                            NetworkManager.GetBuildingType(x.Layer))).ToArray();

                    currentIgnores = currentIgnores.Concat(buildingIgnores).ToArray();
                    distanceToIgnores = distanceToIgnores.Concat(distanceToBuildingIgnores).ToArray();

                    if (network == Networks.WaterPipe && sizes[0] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[0] / 2).ToArray();
                    if (network == Networks.Sewer && sizes[1] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[1] / 2).ToArray();
                    if (network == Networks.HeatingNetworks && sizes[2] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[2] / 2).ToArray();

                    var ignoresCount = Properties.Settings.Default.AllowIntersection ? 0 : ignores.Length;

                    for (int j = 0; j < 0; j++)
                    {
                        for (int k = j + 1; k < ignoresCount; k++)
                        {
                            var ignoreA = ignores[j];
                            var ignoreB = ignores[k];
                            var distanceBetween = ignoreA.GetMinDistanceToCurve(ignoreB);
                            var distanceA = distanceToIgnores[j];
                            var distanceB = distanceToIgnores[k];
                            if (distanceA + distanceB <= distanceBetween)
                                continue;

                            Polyline additionalPolyline = ignoreA.Join(ignoreB);
                            currentIgnores = currentIgnores.Append(additionalPolyline).ToArray();
                            distanceToIgnores = distanceToIgnores.Append(Math.Min(distanceA, distanceB)).ToArray();
                        }
                    }

                    try
                    {
                        Polyline newLine;
                        if (Properties.Settings.Default.AllowIntersection)
                            newLine = ConnectPointsWithIntersect(pair.Value.First, pair.Value.Second, currentIgnores,
                                distanceToIgnores);
                        else
                        {
                            newLine = tr.SuperConnect(pair.Value.First, pair.Value.Second, currentIgnores,
                                distanceToIgnores, 1);
                        }
    
                        ed.WriteMessage($"{newLine.NumberOfVertices} ");

                        //while (newLine.Simplify(ignores.Union(buildingIgnores).ToArray(), distanceToIgnores) != 0)
                        {
                        }

                        ed.WriteMessage($"{newLine.NumberOfVertices} \n");

                        newLine.Layer = NetworkManager.GetNetworkName(network);
                        tr.Draw(newLine);
                        ignores = ignores.Append(newLine).ToArray();
                    }
                    catch (Exception ex)
                    {
                        ed.WriteMessage($"{ex.Message}\n");
                        ed.WriteMessage($"Не удалось проложить сеть {NetworkManager.GetNetworkName(network)}\n");
                    }
                }

                tr.Commit();
            }
        }

        private static Polyline ConnectPoints(Point3d pointFrom, Point3d pointTo, Curve[] curves, double[] distances)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);

            int stopper = 0;

            Vector3d vectorMem = new Vector3d();
            int signMem = 0;
            while (pointFrom.DistanceTo(pointTo) > 1 && stopper < 1000)
            {
                Vector3d vector3d = pointTo - pointFrom;
                vector3d *= Delta / vector3d.Length;
                pointFrom += vector3d;

                for (int i = 0; i < curves.Length; i++)
                {
                    var curve = curves[i];
                    var distance = distances[i];
                    if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) >= distance)
                        continue;

                    pointFrom -= vector3d;
                    vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFrom, false));
                    vector3d *= Delta / vector3d.Length;

                    if (vectorMem == vector3d)
                    {
                        pointFrom += vector3d * signMem;
                        if (curve.GetClosestPointTo(pointFrom, false).DistanceTo(pointFrom) < distance)
                        {
                            var vector = pointFrom - curve.GetClosestPointTo(pointFrom, false);
                            var old = vector;
                            var len = vector.Length;
                            vector *= distance / len;
                            vector = vector.Subtract(old);
                            pointFrom += vector;
                        }

                        polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                        continue;
                    }

                    var point1 = pointFrom + vector3d;
                    var point2 = pointFrom - vector3d;

                    if (pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2))
                    {
                        pointFrom += vector3d;
                        signMem = 1;
                    }
                    else
                    {
                        pointFrom -= vector3d;
                        signMem = -1;
                    }

                    if (curve.GetClosestPointTo(pointFrom, false).DistanceTo(pointFrom) < distance)
                    {
                        var vector = pointFrom - curve.GetClosestPointTo(pointFrom, false);
                        var old = vector;
                        var len = vector.Length;
                        vector *= distance / len;
                        vector = vector.Subtract(old);
                        pointFrom += vector;
                    }

                    vectorMem = vector3d;
                }

                if (stopper == 999)
                {
                    Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Не конец\n");
                }

                polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                ++stopper;
            }

            polyline.AddVertexAt(0, pointTo.Convert2d(new Plane()), 0, 0, 0);
            polyline.Simplify();

            return polyline;
        }

        private static Polyline ConnectPointsWithIntersect(Point3d pointFrom, Point3d pointTo, Curve[] curves,
            double[] distances)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);

            int stopper = 0;
            double maxCounts = pointFrom.DistanceTo(pointTo) / Delta * 5;

            Vector3d vectorMem = new Vector3d();
            int signMem = 0;
            while (polyline.EndPoint.DistanceTo(pointTo) > 1 && stopper < maxCounts)
            {
                Vector3d vector3d = pointTo - pointFrom;
                vector3d /= vector3d.Length;
                vector3d *= Delta;
                pointFrom += vector3d;

                for (int i = 0; i < curves.Length; i++)
                {
                    var curve = curves[i];
                    var distance = distances[i];
                    if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) >= distance)
                        continue;

                    pointFrom -= vector3d;

                    var testLine = new Line(pointFrom, pointTo);
                    var count = curve.IntersectionsCount(testLine);

                    if (count > 2 || count == 0)
                    {
                        vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFrom, false));
                        vector3d *= Delta / vector3d.Length;

                        if (vectorMem == vector3d)
                        {
                            pointFrom += vector3d * signMem;
                            if (curve.GetClosestPointTo(pointFrom, false).DistanceTo(pointFrom) < distance)
                            {
                                var vector = pointFrom - curve.GetClosestPointTo(pointFrom, false);
                                var old = vector;
                                var len = vector.Length;
                                vector *= distance;
                                vector /= len;
                                vector = vector.Subtract(old);
                                pointFrom += vector;
                            }

                            polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                            continue;
                        }

                        var point1 = pointFrom + vector3d;
                        var point2 = pointFrom - vector3d;

                        if (pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2))
                            signMem = 1;
                        else
                            signMem = -1;
                        pointFrom += signMem * vector3d;

                        if (curve.GetClosestPointTo(pointFrom, false).DistanceTo(pointFrom) < distance)
                        {
                            var vector = pointFrom - curve.GetClosestPointTo(pointFrom, false);
                            var old = vector;
                            var len = vector.Length;
                            vector *= distance;
                            vector /= len;
                            vector = vector.Subtract(old);
                            pointFrom += vector;
                        }

                        vectorMem = vector3d;
                    }
                    else
                    {
                        var vector1 = pointTo - pointFrom;
                        var point = curve.GetClosestPointTo(pointFrom, false);
                        var vector2 = curve.GetFirstDerivative(point);
                        var angle = vector1.GetAngleTo(vector2) / Math.PI * 180;
                        if (angle > 90)
                            angle = 180 - angle;
                        vector3d = pointTo - pointFrom;
                        vector3d /= vector3d.Length;
                        vector3d *= Delta;
                        if (angle < MinAngle)
                        {
                            var deltaAngle = MinAngle - angle;
                            deltaAngle = deltaAngle / 180 * Math.PI;
                            vector3d = vector3d.RotateBy(deltaAngle, new Vector3d(0, 0, 1));
                            angle = vector1.GetAngleTo(vector2) / Math.PI * 180;
                            if (angle < MinAngle)
                                vector3d = vector3d.RotateBy(-2 * deltaAngle, new Vector3d(0, 0, 1));
                        }

                        do
                        {
                            pointFrom += vector3d;
                        } while (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) < distance);
                    }
                }

                polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                ++stopper;
            }

            if (stopper >= maxCounts - 1)
                Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage("ERROR\n");

            polyline.AddVertexAt(0, pointTo.Convert2d(new Plane()), 0, 0, 0);
            polyline.Simplify();

            return polyline;
        }

        private static Polyline SuperConnect(this Transaction tr, Point3d pointFrom, Point3d pointTo,
            IReadOnlyList<Curve> curves,
            IReadOnlyList<double> distances, int depth)
        {
            if (depth > MaxDepth)
                return null;
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(polyline.NumberOfVertices, pointFrom.Convert2d(new Plane()), 0, 0, 0);
            var stopper = 0;
            const int maxIterations = 1000;
            while (polyline.EndPoint.DistanceTo(pointTo) > 2 * Delta && stopper < maxIterations)
            {
                ++stopper;
                pointFrom = GetLastGoodPoint(pointFrom, pointTo, curves, distances);
                polyline.AddVertexAt(polyline.NumberOfVertices, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                for (int i = 0; i < curves.Count; i++)
                {
                    var curve = curves[i];
                    var distance = distances[i];
                    if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) >= distance + Delta)
                        continue;

                    var pointFromWithOutIntersect =
                        tr.GetPointWithOutIntersect2(pointFrom, pointTo, curve, distance);
                    var pointFromWithIntersect = GetPointWithIntersect(pointFrom, pointTo, curve, distance);
                    
                    if (MayNotIntersect(curve,pointFrom,pointTo,distance,curves,distances) &&
                        CanIntersect(curve, pointFrom, pointTo))
                    {
                        foreach (var VARIABLE in pointFromWithOutIntersect)
                        {
                            //polyline.AddVertexAt(polyline.NumberOfVertices,VARIABLE.Convert2d(new Plane()),0,0,0);
                        }
                        var variant1 = tr.SuperConnect(pointFromWithOutIntersect.First(), pointTo, curves, distances,
                            depth + 1);
                        var variant2 = tr.SuperConnect(pointFromWithIntersect, pointTo, curves, distances, depth + 1);
                        tr.Draw(polyline.Join(variant1));
                        tr.Draw(polyline.Join(variant2));
                        polyline = polyline.TryJoin(variant1, variant2);
                    }
                    else if (MayNotIntersect(curve,pointFrom,pointTo,distance,curves,distances))
                    {
                        foreach (var point in pointFromWithOutIntersect)
                        {
                            polyline.AddVertexAt(polyline.NumberOfVertices,point.Convert2d(new Plane()),0,0,0);
                        }
                        Polyline variant1 = tr.SuperConnect(polyline.EndPoint, pointTo, curves, distances,
                            depth + 1);
                        polyline = polyline.Join(variant1);
                        tr.Draw(polyline);
                    }
                    else if (CanIntersect(curve, pointFrom, pointTo))
                    {
                        Polyline variant2 =
                            tr.SuperConnect(pointFromWithIntersect, pointTo, curves, distances, depth + 1);
                        polyline = polyline.Join(variant2);
                        tr.Draw(polyline);
                    }
                    else
                    {
                        return null;
                    }

                    if (polyline is null) return null;
                    pointFrom = polyline.EndPoint;
                }

                polyline.AddVertexAt(polyline.NumberOfVertices, pointFrom.Convert2d(new Plane()), 0, 0, 0);
            }

            polyline.AddVertexAt(polyline.NumberOfVertices, pointTo.Convert2d(new Plane()), 0, 0, 0);
            //polyline.Simplify();
            //tr.Draw(polyline);
            if (stopper >= maxIterations || polyline.NumberOfVertices <= 1)
                return null;

            return polyline;
        }

        private static Point3d GetPointWithIntersect(Point3d pointFrom, Point3d pointTo, Curve curve, double distance)
        {
            var pointFromWithIntersect = pointFrom;
            var vector1 = pointTo - pointFromWithIntersect;
            var point = curve.GetClosestPointTo(pointFromWithIntersect, false);
            var vector2 = curve.GetFirstDerivative(point, true);
            var angle = vector1.GetAngleTo(vector2) / Math.PI * 180;
            if (angle > 90)
                angle = 180 - angle;
            var vector3d = pointTo - pointFromWithIntersect;
            vector3d *= Delta / vector3d.Length;
            if (angle < MinAngle)
            {
                var deltaAngle = MinAngle - angle;
                deltaAngle = deltaAngle / 180 * Math.PI;
                vector3d = vector3d.RotateBy(deltaAngle, new Vector3d(0, 0, 1));
                angle = vector1.GetAngleTo(vector2) / Math.PI * 180;
                if (angle < MinAngle)
                    vector3d = vector3d.RotateBy(-2 * deltaAngle, new Vector3d(0, 0, 1));
            }

            do
            {
                pointFromWithIntersect += vector3d;
            } while (pointFromWithIntersect.DistanceTo(curve.GetClosestPointTo(pointFromWithIntersect, false)) <
                     distance);

            return pointFromWithIntersect;
        }

        private static Point3d GetPointWithOutIntersect(Point3d pointFrom, Point3d pointTo, Curve curve,
            double distance)
        {
            var pointFromWithOutIntersect = pointFrom;
            Point3d pointToCheck;
            do
            {
                var vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false));
                vector3d *= Delta / vector3d.Length;
                var point1 = pointFrom + vector3d;
                var point2 = pointFrom - vector3d;

                if (pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2))
                    pointFromWithOutIntersect += vector3d;
                else
                    pointFromWithOutIntersect -= vector3d;

                var vector = pointTo - pointFromWithOutIntersect;
                vector *= Delta / vector.Length;
                pointToCheck = pointFromWithOutIntersect + vector;
            } while (pointFromWithOutIntersect.DistanceTo(curve.GetClosestPointTo(pointFromWithOutIntersect,
                         false)) < distance ||
                     pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck,
                         false)) < distance);

            return pointFromWithOutIntersect;
        }

        private static IEnumerable<Point3d> GetPointWithOutIntersect2(this Transaction tr, Point3d pointFrom, Point3d pointTo,
            Curve curve,
            double distance)
        {
            //if (curve is Polyline pl)
            //    curve = pl.Jarvis();
            Vector3d vectorMem = new Vector3d();
            var signMem = 0;
            var pointFromWithOutIntersect = pointFrom;
            Point3d pointToCheck;
            do
            {
                Vector3d vectorToCheck;
                var vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false));
                vector3d *= Delta / vector3d.Length;
                //tr.Draw(vector3d.ToPolyline(pointFromWithOutIntersect));
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
                    
                    continue;
                }

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
                yield return pointFromWithOutIntersect;
            } while (pointFromWithOutIntersect.DistanceTo(curve.GetClosestPointTo(pointFromWithOutIntersect,
                         false)) < distance ||
                     pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck,
                         false)) < distance);

            yield return pointFromWithOutIntersect;
        }

        private static Point3d GetPointWithOutIntersect3(Point3d pointFrom, Point3d pointTo, Curve curve,
            double distance, IReadOnlyList<Curve> curves,
            IReadOnlyList<double> distances)
        {
            var pointFromWithOutIntersect = pointFrom;
            Vector3d vectorMem = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false),true);
            vectorMem *= Delta / vectorMem.Length;
            Vector3d vector3d;
            var point1 = pointFrom + vectorMem;
            var point2 = pointFrom - vectorMem;
            Point3d pointToCheck;
            double[] currentDistances;
            var sign = pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2) ? 1 : -1;
            do
            {
                vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false),true);
                vector3d *= Delta / vector3d.Length;

                pointFromWithOutIntersect += sign * vectorMem;
                var vectorToCheck = pointTo - pointFromWithOutIntersect;
                vectorToCheck *= Delta / vectorToCheck.Length;
                pointToCheck = pointFromWithOutIntersect + vectorToCheck;
                currentDistances = curves.Select(x =>
                        x.GetClosestPointTo(pointFromWithOutIntersect, false).DistanceTo(pointFromWithOutIntersect))
                    .ToArray();
                for (int i = 0; i < currentDistances.Length; i++)
                {
                    currentDistances[i] -= distances[i];
                }
            } while (vectorMem == vector3d &&
                     pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck, false)) < distance//);
            && currentDistances.All(x=>x>0));

            return pointFromWithOutIntersect;
        }

        private static Point3d GetLastGoodPoint(Point3d pointFrom, Point3d pointTo, IReadOnlyList<Curve> curves,
            IReadOnlyList<double> distances)
        {
            double[] currentDistances;
            Vector3d vector3d;
            do
            {
                vector3d = pointTo - pointFrom;
                vector3d *= Delta / vector3d.Length;
                pointFrom += vector3d;
                currentDistances = curves.Select(x => pointFrom.DistanceTo(x.GetClosestPointTo(pointFrom, false)))
                    .ToArray();
                for (int i = 0; i < currentDistances.Length; i++)
                {
                    currentDistances[i] -= distances[i];
                }
            } while (pointFrom.DistanceTo(pointTo) > 2 * Delta
                     && currentDistances.All(x => x > 0));

            if (pointFrom.DistanceTo(pointTo) < 2 * Delta)
                return pointTo;
            pointFrom -= vector3d;
            return pointFrom;
        }

        private static bool CanIntersect(Entity entity, Point3d pointFrom, Point3d pointTo)
        {
            //return false;
            var count = entity.IntersectionsCount(new Line(pointFrom, pointTo));
            return count == 1 || count == 2;
        }

        private static bool MayNotIntersect(Curve curve, Point3d pointFrom, Point3d pointTo, double distance,
            IReadOnlyList<Curve> curves,
            IReadOnlyList<double> distances)
        {
            var pointFromWithOutIntersect = pointFrom;
            Vector3d vectorMem = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFromWithOutIntersect, false), true);
            vectorMem *= Delta / vectorMem.Length;
            var point1 = pointFrom + vectorMem;
            var point2 = pointFrom - vectorMem;
            var sign = pointTo.DistanceTo(point1) < pointTo.DistanceTo(point2) ? 1 : -1;
            pointFromWithOutIntersect += sign * vectorMem;
            var currentDistances = curves.Select(x =>
                    x.GetClosestPointTo(pointFromWithOutIntersect, false).DistanceTo(pointFromWithOutIntersect))
                .ToArray();
            for (int i = 0; i < currentDistances.Length; i++)
            {
                currentDistances[i] -= distances[i];
            }
            //} while (vectorMem == vector3d &&
            //         pointToCheck.DistanceTo(curve.GetClosestPointTo(pointToCheck, false)) < distance);
            //&& currentDistances.All(x=>x>0));

            return currentDistances.All(x => x > 0);
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