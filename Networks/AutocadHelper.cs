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
        public static void ConnectCurves()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            PromptEntityOptions options = new PromptEntityOptions("");
            options.SetRejectMessage("");
            options.AddAllowedClass(typeof(Curve), false);
            PromptEntityResult entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                return;
            ObjectId id = entSelRes.ObjectId;

            entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                return;
            ObjectId id2 = entSelRes.ObjectId;

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(id, OpenMode.ForRead) as Curve;
                Curve curve2 = tr.GetObject(id2, OpenMode.ForRead) as Curve;
                if (curve is null || curve2 is null) return;

                PointOnCurve3d[] pointOnCurve3d = curve.GetGeCurve().GetClosestPointTo(curve2.GetGeCurve());

                var line = new Line(pointOnCurve3d.First().Point, pointOnCurve3d.Last().Point);
                ed.WriteMessage($"{line.Length}\n");
                tr.Draw(line);

                tr.Commit();
            }
        }

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

        public static void DrawNetworksByPoints(Networks network, double size)
        {
            #region Init

            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            PromptPointOptions optPoint = new PromptPointOptions("Выберите первую точку");
            PromptPointResult pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                return;
            Point3d point1 = pointSelRes.Value;

            optPoint = new PromptPointOptions("Выберите вторую точку");
            pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                return;
            Point3d point2 = pointSelRes.Value;

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

            #endregion

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (acBlkTbl is null) return;
                BlockTableRecord acBlkTblRec =
                    tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (acBlkTblRec is null) return;

                Curve[] ignores = objectIds.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve).ToArray();
                ignores = ignores.Select(x => x is Polyline ? (x as Polyline).Jarvis() : x).ToArray();

                double[] distanceToIgnores = ignores.Select(x => NetworkManager.GetDistance(network,
                    NetworkManager.GetType(x.Layer)) + size / 2).ToArray();

                var ignoresCount = ignores.Length;

                for (int j = 0; j < ignoresCount; j++)
                {
                    for (int k = j + 1; k < ignoresCount; k++)
                    {
                        var ignoreA = ignores[j];
                        var ignoreB = ignores[k];
                        var distanceBetween = ignoreA.GetMinDistanceToCurve(ignoreB);
                        var distanceA = distanceToIgnores[j];
                        var distanceB = distanceToIgnores[k];
                        if (distanceA + distanceB > distanceBetween)
                        {
                            Polyline additionalPolyline;
                            if (ignoreA is Polyline && ignoreB is Polyline)
                                additionalPolyline = (ignoreA as Polyline).Join(ignoreB as Polyline).Jarvis();
                            else if (ignoreA is Polyline && ignoreB is Line)
                                additionalPolyline = (ignoreA as Polyline).Join(ignoreB as Line).Jarvis();
                            else if (ignoreA is Line && ignoreB is Polyline)
                                additionalPolyline = (ignoreA as Line).Join(ignoreB as Polyline).Jarvis();
                            else
                                additionalPolyline = (ignoreA as Line).Join(ignoreB as Line).Jarvis();
                            ignores = ignores.Append(additionalPolyline).ToArray();
                            distanceToIgnores = distanceToIgnores.Append(Math.Min(distanceA, distanceB)).ToArray();
                        }
                    }
                }

                var newLine = ConnectPoints(point1, point2, ignores, distanceToIgnores);
                while (newLine.Simplify(ignores, distanceToIgnores) != 0)
                {
                }

                newLine.Layer = NetworkManager.GetNetworkName(network);
                acBlkTblRec.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);

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
                ignores = ignores.Select(x => x is Polyline ? (x as Polyline).Jarvis() : x).ToArray();

                Curve[] buildingIgnores = objectIdsBuildings.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve)
                    .ToArray();
                ignores = ignores.Select(x => x is Polyline ? (x as Polyline).Jarvis() : x).ToArray();

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

                    var ignoresCount = ignores.Length;

                    for (int j = 0; j < ignoresCount; j++)
                    {
                        for (int k = j + 1; k < ignoresCount; k++)
                        {
                            var ignoreA = ignores[j];
                            var ignoreB = ignores[k];
                            var distanceBetween = ignoreA.GetMinDistanceToCurve(ignoreB);
                            var distanceA = distanceToIgnores[j];
                            var distanceB = distanceToIgnores[k];
                            if (distanceA + distanceB > distanceBetween)
                            {
                                Polyline additionalPolyline;
                                if (ignoreA is Polyline && ignoreB is Polyline)
                                    additionalPolyline = (ignoreA as Polyline).Join(ignoreB as Polyline).Jarvis();
                                else if (ignoreA is Polyline && ignoreB is Line)
                                    additionalPolyline = (ignoreA as Polyline).Join(ignoreB as Line).Jarvis();
                                else if (ignoreA is Line && ignoreB is Polyline)
                                    additionalPolyline = (ignoreA as Line).Join(ignoreB as Polyline).Jarvis();
                                else
                                    additionalPolyline = (ignoreA as Line).Join(ignoreB as Line).Jarvis();
                                currentIgnores = currentIgnores.Append(additionalPolyline).ToArray();
                                distanceToIgnores = distanceToIgnores.Append(Math.Min(distanceA, distanceB)).ToArray();
                            }
                        }
                    }

                    var newLine = ConnectPoints(pair.Value.First, pair.Value.Second, currentIgnores, distanceToIgnores);
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

        public static Polyline ConnectPoints(Point3d pointFrom, Point3d pointTo, Curve[] curves, double[] distances)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);

            int stoper = 0;
            const double delta = 0.1;

            Vector3d vectorMem = new Vector3d();
            int signMem = 0;
            while (pointFrom.DistanceTo(pointTo) > 1 && stoper < 100000)
            {
                Vector3d vector3d = pointTo - pointFrom;
                vector3d /= vector3d.Length;
                vector3d *= delta;
                pointFrom += vector3d;

                for (int i = 0; i < curves.Length; i++)
                {
                    var curve = curves[i];
                    var distance = distances[i];
                    if (pointFrom.DistanceTo(curve.GetClosestPointTo(pointFrom, false)) < distance)
                    {
                        pointFrom -= vector3d;
                        vector3d = curve.GetFirstDerivative(curve.GetClosestPointTo(pointFrom, false));
                        vector3d /= vector3d.Length;
                        vector3d *= delta;


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
                            vector *= distance;
                            vector /= len;
                            vector = vector.Subtract(old);
                            pointFrom += vector;
                        }

                        vectorMem = vector3d;
                    }
                }

                if (stoper > 990)
                {
                    Autocad.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Не конец\n");
                }

                polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);
                ++stoper;
            }

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