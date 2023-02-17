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
    public static class AutocadHelper2
    {
        public static void Func()
        {
            Document acDoc = Autocad.DocumentManager.MdiActiveDocument;
            Database db = acDoc.Database;
            Editor ed = acDoc.Editor;

            var selRes = ed.SelectAll();

            var ids = selRes.Value.GetObjectIds();
            
            ed.WriteMessage($"{ids.Length}");

            using (DocumentLock _ = acDoc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var obj = tr.GetObject(id, OpenMode.ForRead);
                    ed.WriteMessage(obj.GetType()+"\n");
                }
            }
        }
    }
}