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
        /// Смещение кривой на заданное расстояние и сохраниение ее на заданном слое
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="curve">Кривая смещение которой будет производиться</param>
        /// <param name="offset">Расстояние на которое будет выполнено смещение</param>
        /// <param name="layer">Слой на котором будет сохранена смещенная кривая</param>
        private static void OffsetCurve(this Transaction tr, Curve curve, double offset, string layer)
        {
            BlockTable acBlkTbl =
                tr.GetObject(Autocad.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as
                    BlockTable;
            if (acBlkTbl is null) return;
            BlockTableRecord acBlkTblRec =
                tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            if (acBlkTblRec is null) return;
            DBObjectCollection acDbObjColl = curve.GetOffsetCurves(offset);

            if (acDbObjColl.Count == 0)
                throw new InvalidOperationException();

            foreach (Entity acEnt in acDbObjColl)
            {
                acEnt.Layer = layer;
                acEnt.Linetype = "BYLAYER";
                acBlkTblRec.AppendEntity(acEnt);
                tr.AddNewlyCreatedDBObject(acEnt, true);
                return;
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

        /// <summary>
        /// Основаная функция для размещения коммуникаций вдоль кривой
        /// </summary>
        /// <param name="networks">Сети которые необходимо разместить</param>
        /// <param name="sizes">Размеры сетей { Водопровод, Канализация, Теплосеть }</param>
        public static void DrawNetworksByLine(Networks[] networks, double[] sizes)
        {
            #region Init

            if (networks.Length == 0)
                return;

            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            // Кривая вдоль которой прокладываются коммуникации
            PromptEntityOptions options =
                new PromptEntityOptions("Выберите кривую вдоль которой будут проложены коммуникации");
            options.SetRejectMessage("");
            options.AddAllowedClass(typeof(Curve), false);
            PromptEntityResult entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                return;
            ObjectId id = entSelRes.ObjectId;

            // Точка для определения стороны кривой
            PromptPointOptions optPoint = new PromptPointOptions("Выберите точку для определения стороны кривой");
            PromptPointResult pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                return;
            Point3d point = pointSelRes.Value;

            // Существующие сети которые нужно учесть
            var filterList = new[]
            {
                new TypedValue(-4, "<OR"),
                new TypedValue(0, "LINE"),
                new TypedValue(0, "LWPOLYLINE"),
                new TypedValue(0, "SPLINE"),
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
                Curve curve = tr.GetObject(id, OpenMode.ForRead) as Curve;
                if (curve is null) return;

                int offsetCoefficient = curve.GetOffsetCoefficient(point);

                try
                {
                    if (objectIds.Length == 0)
                    {
                        tr.DrawWithoutIgnores(curve, offsetCoefficient, networks, sizes);
                    }
                    else
                    {
                        Curve[] ignores = objectIds.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve)
                            .ToArray();
                        tr.DrawWithIgnores(curve, offsetCoefficient, networks, sizes, ignores);
                    }
                }
                catch (InvalidOperationException)
                {
                    ed.WriteMessage("Не удалось проложить сети");
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Размещение сетей без учета игнорируемых сетей
        /// </summary>
        /// <param name="transaction">Транзакция в которой производится размещение сетей</param>
        /// <param name="curve">Основная кривая вдоль которой размещаются сети</param>
        /// <param name="offsetCoefficient">Коэффициент смещения для определения стороны кривой</param>
        /// <param name="networks">Сети которые необходимо проложить</param>
        /// <param name="sizes">Размеры сетей { Водопровод, Канализация, Теплосеть }</param>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        private static void DrawWithoutIgnores(this Transaction transaction,
            Curve curve,
            double offsetCoefficient,
            Networks[] networks,
            double[] sizes
        )
        {
            // Получение оптимальной перестановки и расстояний между сетями
            networks = NetworkManager.GetBestPermutation(networks);
            double[] distances = NetworkManager.GetDistances(networks, sizes);
            for (int i = 0; i < distances.Length; i++)
                distances[i] *= offsetCoefficient;

            // Добавление полученных сетей на чертеж
            try
            {
                double distance = distances[0];
                for (int i = 0; i < networks.Length - 1; i++)
                {
                    transaction.OffsetCurve(curve, distance, NetworkManager.GetNetworkName(networks[i]));
                    distance += distances[i + 1];
                }

                transaction.OffsetCurve(curve, distance, NetworkManager.GetNetworkName(networks[networks.Length - 1]));
            }
            catch (Exception)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Размещение сетей с учетом игнорируемых
        /// </summary>
        /// <param name="transaction">Транзакция в которой производится размещение сетей</param>
        /// <param name="curve">Основная кривая вдоль которой размещаются сети</param>
        /// <param name="offsetCoefficient">Коэффициент смещения для определения стороны кривой</param>
        /// <param name="networks">Сети которые необходимо проложить</param>
        /// <param name="sizes">Размеры сетей { Водопровод, Канализация, Теплосеть }</param>
        /// <param name="ignores">Игнорируемые коммуникации</param>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        private static void DrawWithIgnores(
            this Transaction transaction,
            Curve curve,
            double offsetCoefficient,
            Networks[] networks,
            double[] sizes,
            Curve[] ignores
        )
        {
            var min = double.PositiveInfinity;
            var bestPerm = new Networks[networks.Length];
            do
            {
                try
                {
                    Networks network;
                    var distance = 1.0;
                    Curve[] currentIgnores = new Curve[ignores.Length];
                    ignores.CopyTo(currentIgnores, 0);
                    for (int i = 0; i < networks.Length; i++)
                    {
                        network = networks[i];
                        Curve nextCurve;
                        var deltas = currentIgnores.Select(x =>
                            NetworkManager.GetDistance(network, NetworkManager.GetType(x.Layer))).ToArray();
                        if (network == Networks.WaterPipe && sizes[0] != 0)
                            nextCurve = curve.Offset(sizes[0], distance * offsetCoefficient, currentIgnores,
                                deltas);
                        else if (network == Networks.Sewer && sizes[1] != 0)
                            nextCurve = curve.Offset(sizes[1], distance * offsetCoefficient, currentIgnores,
                                deltas);
                        else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                            nextCurve = curve.Offset(sizes[2], distance * offsetCoefficient, currentIgnores,
                                deltas);
                        else
                            nextCurve = curve.Offset(distance * offsetCoefficient, currentIgnores, deltas);
                        nextCurve.Layer = NetworkManager.GetNetworkName(network);
                        distance = curve.GetMinDistanceToCurve(nextCurve);

                        if (i >= networks.Length - 1)
                            continue;

                        if (network == Networks.WaterPipe && sizes[0] != 0)
                        {
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance - sizes[0] / 2,
                                         offsetCoefficient,
                                         sizes[0]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.WaterPipe);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[0] / 2;
                        }
                        else if (network == Networks.Sewer && sizes[1] != 0)
                        {
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance - sizes[1] / 2,
                                         offsetCoefficient,
                                         sizes[1]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.Sewer);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[1] / 2;
                        }
                        else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                        {
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance - sizes[2] / 2,
                                         offsetCoefficient,
                                         sizes[2]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.HeatingNetworks);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[2] / 2;
                        }
                        else
                        {
                            currentIgnores = currentIgnores.Append(nextCurve).ToArray();
                        }

                        distance += NetworkManager.GetDistance(networks[i], networks[i + 1]);
                    }

                    if (distance >= min)
                        continue;
                    min = distance;
                    networks.CopyTo(bestPerm, 0);
                }
                catch
                {
                    continue;
                }
            } while (NetworkManager.Narayana.NextPermutation(networks,
                         (n1, n2) => (int)n1 < (int)n2));

            if (double.IsPositiveInfinity(min))
                throw new InvalidOperationException();
            transaction.DrawNetworks(curve, sizes, offsetCoefficient, bestPerm, ignores);
        }

        /// <summary>
        /// Основаная функция для размещения коммуникаций в области
        /// </summary>
        /// <param name="networks">Сети которые необходимо разместить</param>
        /// <param name="sizes">Размеры сетей { Водопровод, Канализация, Теплосеть }</param>
        public static void DrawNetworksByArea(Networks[] networks, double[] sizes)
        {
            #region Init

            if (networks.Length == 0)
                return;

            networks = NetworkManager.GetBestPermutation(networks);

            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            // Кривая вдоль которой прокладываются коммуникации
            PromptEntityOptions options =
                new PromptEntityOptions("Выберите первую линию");
            options.SetRejectMessage("");
            options.AddAllowedClass(typeof(Line), false);
            options.Message = "Выберите вторую линию";
            PromptEntityResult entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                return;
            ObjectId id1 = entSelRes.ObjectId;

            entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                return;
            ObjectId id2 = entSelRes.ObjectId;

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

            #endregion

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (acBlkTbl is null) return;
                BlockTableRecord acBlkTblRec =
                    tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (acBlkTblRec is null) return;

                Line line1 = tr.GetObject(id1, OpenMode.ForRead) as Line;
                Line line2 = tr.GetObject(id2, OpenMode.ForRead) as Line;
                Curve[] ignores = objectIds.Select(x => tr.GetObject(x, OpenMode.ForRead) as Curve).ToArray();
                ignores = ignores.Select(x => x is Polyline ? (x as Polyline).Jarvis() : x).ToArray();
                if (line1 is null || line2 is null) return;

                var distances = NetworkManager.GetDistances(networks, new[] { 0.0, 0, 0 });
                distances[0] = 0;
                distances = distances.Cumulative();
                for (int i = 0; i < networks.Length; i++)
                {
                    var currentIgnores = ignores.Select(x => x).ToArray();
                    double[] distanceToIgnores = ignores.Select(x => NetworkManager.GetDistance(networks[i],
                        NetworkManager.GetType(x.Layer))).ToArray();

                    if (networks[i] == Networks.WaterPipe && sizes[0] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[0] / 2).ToArray();
                    if (networks[i] == Networks.Sewer && sizes[1] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[1] / 2).ToArray();
                    if (networks[i] == Networks.HeatingNetworks && sizes[2] != 0)
                        distanceToIgnores = distanceToIgnores.Select(x => x + sizes[2] / 2).ToArray();

                    var ignoresCount = ignores.Length;

                    for (int j = 0; j < ignoresCount; j++)
                    {
                        for (int k = j+1; k < ignoresCount; k++)
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
                                    additionalPolyline = Join(ignoreA as Polyline, ignoreB as Polyline).Jarvis();
                                else if (ignoreA is Polyline && ignoreB is Line)
                                    additionalPolyline = Join(ignoreA as Polyline, ignoreB as Line).Jarvis();
                                else if (ignoreA is Line && ignoreB is Polyline)
                                    additionalPolyline = Join(ignoreA as Line, ignoreB as Polyline).Jarvis();
                                else
                                    additionalPolyline = Join(ignoreA as Line, ignoreB as Line).Jarvis();
                                currentIgnores = currentIgnores.Append(additionalPolyline).ToArray();
                                distanceToIgnores = distanceToIgnores.Append(Math.Min(distanceA,distanceB)).ToArray();
                            }
                        }
                    }
                    
                    Point3d point1 = line1.GetPointAtParameter(distances[i]);
                    Point3d point2 = line2.GetPointAtParameter(distances[i]);

                    for (int j = 0; j < ignores.Length; j++)
                    {
                        var curve = ignores[j];
                        var distance = distanceToIgnores[j];
                        var distanceReal = curve.GetClosestPointTo(point1, false).DistanceTo(point1);
                        var parameter = distances[i];
                        while (distanceReal < distance)
                        {
                            point1 = line1.GetPointAtParameter(parameter);
                            distanceReal = curve.GetClosestPointTo(point1, false).DistanceTo(point1);
                            for (int k = i; k < distances.Length; k++)
                            {
                                parameter += 0.01;
                            }
                        }

                        distanceReal = curve.GetClosestPointTo(point2, false).DistanceTo(point2);
                        parameter = distances[i];
                        while (distanceReal < distance)
                        {
                            point2 = line2.GetPointAtParameter(parameter);
                            distanceReal = curve.GetClosestPointTo(point2, false).DistanceTo(point2);
                            for (int k = i; k < distances.Length; k++)
                            {
                                parameter += 0.01;
                            }
                        }
                    }

                    var newLine = ConnectPoints(point1, point2, currentIgnores, distanceToIgnores);
                    if (Properties.Settings.Default.SimplifyPolyline)
                        while (newLine.Simplify(ignores, distanceToIgnores) != 0)
                        {
                        }
                    newLine.Layer = NetworkManager.GetNetworkName(networks[i]);
                    acBlkTblRec.AppendEntity(newLine);
                    tr.AddNewlyCreatedDBObject(newLine, true);
                    ignores = ignores.Append(newLine).ToArray();
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
                    for (int k = j+1; k < ignoresCount; k++)
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
                                additionalPolyline = Join(ignoreA as Polyline, ignoreB as Polyline).Jarvis();
                            else if (ignoreA is Polyline && ignoreB is Line)
                                additionalPolyline = Join(ignoreA as Polyline, ignoreB as Line).Jarvis();
                            else if (ignoreA is Line && ignoreB is Polyline)
                                additionalPolyline = Join(ignoreA as Line, ignoreB as Polyline).Jarvis();
                            else
                                additionalPolyline = Join(ignoreA as Line, ignoreB as Line).Jarvis();
                            ignores = ignores.Append(additionalPolyline).ToArray();
                            distanceToIgnores = distanceToIgnores.Append(Math.Min(distanceA,distanceB)).ToArray();
                        }
                    }
                }
                
                var newLine = ConnectPoints(point1, point2, ignores, distanceToIgnores);
                if (Properties.Settings.Default.SimplifyPolyline)
                    while (newLine.Simplify(ignores, distanceToIgnores) != 0)
                    {
                    }
                
                newLine.Layer = NetworkManager.GetNetworkName(network);
                acBlkTblRec.AppendEntity(newLine);
                tr.AddNewlyCreatedDBObject(newLine, true);

                tr.Commit();
            }
        }
        private static Polyline ConnectPoints(Point3d pointFrom, Point3d pointTo, Curve[] curves, double[] distances)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, pointFrom.Convert2d(new Plane()), 0, 0, 0);

            int stoper = 0;
            const double delta = 0.1;

            Vector3d vectorMem = new Vector3d();
            int signMem = 0;
            while (pointFrom.DistanceTo(pointTo) > 1 && stoper < 1000)
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
        private static Polyline Join(Polyline firstPolyline, Polyline secondPolyline)
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
        private static Polyline Join(Polyline polyline, Line line)
        {
            var polylineCopy = polyline.Clone() as Polyline;
            if (polylineCopy is null)
                return new Polyline();

            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,line.StartPoint.Convert2d(new Plane()),0,0,0);
            polylineCopy.AddVertexAt(polylineCopy.NumberOfVertices,line.EndPoint.Convert2d(new Plane()),0,0,0);

            return polylineCopy;
        }
        private static Polyline Join(Line line, Polyline polyline)
        {
            return Join(polyline, line);
        }
        private static Polyline Join(Line firstLine, Line secondLine)
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