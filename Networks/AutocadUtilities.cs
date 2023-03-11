using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Networks
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    public static class AutocadUtilities
    {
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
    }
}