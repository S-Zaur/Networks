using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;

namespace Networks
{
    internal static class AutocadHelper
    {
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
                Properties.Settings.Default.PowerCableLayerName
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
            PromptEntityOptions options = new PromptEntityOptions("Выберите кривую вдоль которой будут проложены коммуникации");
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
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions{
                MessageForAdding = "Выберите коммуникации которые необходимо учесть(необязательно)"
            };
            PromptSelectionResult selRes = ed.GetSelection(selectionOptions,filter);
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
                        else if (network == Networks.HouseholdSewer && sizes[1] != 0)
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
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance - sizes[0] / 2, offsetCoefficient,
                                         sizes[0]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.WaterPipe);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[0] / 2;
                        }
                        else if (network == Networks.HouseholdSewer && sizes[1] != 0)
                        {
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance - sizes[1] / 2, offsetCoefficient,
                                         sizes[1]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.HouseholdSewer);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[1] / 2;
                        }
                        else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                        {
                            foreach (Curve ignoreCurve in curve.OffsetWithSize(distance- sizes[2] / 2, offsetCoefficient,
                                         sizes[2]))
                            {
                                ignoreCurve.Layer = NetworkManager.GetNetworkName(Networks.HeatingNetworks);
                                currentIgnores = currentIgnores.Append(ignoreCurve).ToArray();
                            }

                            distance += sizes[2]/2;
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

            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            // Кривая вдоль которой прокладываются коммуникации
            PromptEntityOptions options = new PromptEntityOptions("Выберите кривую вдоль которой будут проложены коммуникации");
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
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions{
                MessageForAdding = "Выберите коммуникации которые необходимо учесть(необязательно)"
            };
            PromptSelectionResult selRes = ed.GetSelection(selectionOptions,filter);
            ObjectId[] objectIds = selRes.Status != PromptStatus.OK
                ? Array.Empty<ObjectId>()
                : selRes.Value.GetObjectIds();

            #endregion
            
            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                
            }
        }
    }
}