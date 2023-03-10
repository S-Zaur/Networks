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
        private static int MaxDepth = 30;
        public static int MinAngle { get; set; } = 90;

        /// <summary>
        /// Получение названий всех слоев документа
        /// </summary>
        public static string[] GetAllLayers()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;

            List<string> layers = new List<string>();

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt is null) return Array.Empty<string>();
                foreach (ObjectId layerId in lt)
                {
                    LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    layers.Add(layer?.Name);
                }
            }


            return layers.ToArray();
        }

        /// <summary>
        /// Проверка наличия и создание необходимых слоев на чертеже
        /// </summary>
        public static void CheckLayers()
        {
            string[] layers = GetAllLayers();
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            var newLayers = new[]
            {
                Properties.Settings.Default.WaterPipeLayerName,
                Properties.Settings.Default.SewerLayerName,
                Properties.Settings.Default.HeatingNetworkLayerName,
                Properties.Settings.Default.CommunicationCableLayerName,
                Properties.Settings.Default.PowerCableLayerName,
                Properties.Settings.Default.GasPipeLayerName,
                Properties.Settings.Default.BuildingsFoundationLayerName,
                Properties.Settings.Default.StreetSideStoneLayerName,
                Properties.Settings.Default.ExternalEdgeLayerName,
                Properties.Settings.Default.HvlSupportsFoundation1LayerName,
                Properties.Settings.Default.HvlSupportsFoundation35LayerName,
                Properties.Settings.Default.HvlSupportsFoundationOverLayerName,
                Properties.Settings.Default.RedLineLayerName,
            };

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;

                foreach (var layer in newLayers)
                {
                    if (layers.Contains(layer))
                        continue;
                    LayerTableRecord acLyrTblRec = new LayerTableRecord
                    {
                        Name = layer
                    };
                    acLyrTbl?.Add(acLyrTblRec);
                    tr.AddNewlyCreatedDBObject(acLyrTblRec, true);
                }

                tr.Commit();
            }
        }

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
                if (!Properties.Settings.Default.AllowIntersection)
                    ignores = ignores.Select(x => x is Polyline y ? y.Jarvis() : x).ToArray();

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

                    for (int j = 0; j < ignoresCount; j++)
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

                    Polyline newLine;
                    if (Properties.Settings.Default.AllowIntersection)
                        newLine = ConnectPointsWithIntersect(pair.Value.First, pair.Value.Second, currentIgnores,
                            distanceToIgnores);
                    else
                        newLine = ConnectPoints(pair.Value.First, pair.Value.Second, currentIgnores,
                            distanceToIgnores);
                    while (newLine.Simplify(ignores.Union(buildingIgnores).ToArray(), distanceToIgnores) != 0)
                    {
                    }

                    newLine.Layer = NetworkManager.GetNetworkName(network);
                    tr.Draw(newLine);
                    ignores = ignores.Append(newLine).ToArray();
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