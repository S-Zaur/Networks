using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;

namespace Networks
{
    public class ACADHelperTemp
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
    }
}