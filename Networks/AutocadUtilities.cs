using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Networks
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    internal static class AutocadUtilities
    {
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

        public static void DrawPipe()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            var filterList = new[]
            {
                new TypedValue(0, "LWPOLYLINE"),
            };
            SelectionFilter filter = new SelectionFilter(filterList);
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Выберите объекты"
            };
            PromptSelectionResult selRes = ed.GetSelection(selectionOptions, filter);
            ObjectId[] objectIds = selRes.Status != PromptStatus.OK
                ? Array.Empty<ObjectId>()
                : selRes.Value.GetObjectIds();

            var size = ed.GetDouble("Введите размер (мм)").Value / 1000;
            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline[] pipes = objectIds.Select(x => tr.GetObject(x, OpenMode.ForRead) as Polyline).ToArray();
                foreach (var pipe in pipes)
                {
                    try
                    {
                        tr.Draw(pipe.DoubleOffset(size));
                    }
                    catch (Exception)
                    {
                        ed.WriteMessage($"Удалось сместить кривую\n");
                    }
                }

                tr.Commit();
            }
        }

        public static FromToPoints GetStartEndPoints()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Editor ed = acDoc.Editor;

            PromptPointOptions optPoint = new PromptPointOptions($"Выберите первую точку\n");
            PromptPointResult pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                throw new Exception("Точка не выбрана");
            Point3d pointFrom = pointSelRes.Value;

            optPoint = new PromptPointOptions($"Выберите вторую точку\n");
            pointSelRes = ed.GetPoint(optPoint);
            if (pointSelRes.Status != PromptStatus.OK)
                throw new Exception("Точка не выбрана");
            Point3d pointTo = pointSelRes.Value;

            return new FromToPoints(pointFrom, pointTo);
        }

        public static Xline GetGravityLine()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;
            
            PromptEntityOptions options = new PromptEntityOptions("");
            options.SetRejectMessage("");
            options.AddAllowedClass(typeof(Line), false);
            PromptEntityResult entSelRes = ed.GetEntity(options);
            if (entSelRes.Status != PromptStatus.OK)
                throw new Exception();
            ObjectId id = entSelRes.ObjectId;
            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Curve line = tr.GetObject(id, OpenMode.ForRead) as Curve;
                if (line is null)
                    throw new Exception();
                return new Xline
                {
                    BasePoint = line.StartPoint,
                    SecondPoint = line.EndPoint,
                };
            }
        }
    }
}