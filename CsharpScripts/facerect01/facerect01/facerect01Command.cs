using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace facerect01
{
    public class facerect01Command
    {
        [CommandMethod("Make3DFace")]
        public void Make3DFace()
        {
            // Get the current document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Prompt for three points A, B, C
            PromptPointResult pprA = ed.GetPoint("\nSelect point A: ");
            if (pprA.Status != PromptStatus.OK) return;
            Point3d A = pprA.Value;

            PromptPointOptions ppoB = new PromptPointOptions("\nSelect point B: ");
            PromptPointResult pprB = ed.GetPoint(ppoB);
            if (pprB.Status != PromptStatus.OK) return;
            Point3d B = pprB.Value;

            PromptPointOptions ppoC = new PromptPointOptions("\nSelect point C: ");
            PromptPointResult pprC = ed.GetPoint(ppoC);
            if (pprC.Status != PromptStatus.OK) return;
            Point3d C = pprC.Value;

            // Compute vector AB and the fourth point D
            // AB = B - A
            Vector3d AB = B.GetVectorTo(A);  // or (A->B). Depending on direction, you can switch as needed
            // We'll do D = C - AB, so that D = C + (A - B).
            Point3d D = new Point3d(
                C.X - (B.X - A.X),
                C.Y - (B.Y - A.Y),
                C.Z - (B.Z - A.Z)
            );

            // Now create a 3D Face using A, B, C, and D
            // We'll place it in model space
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                // Get the current database
                Database db = doc.Database;
                // Open model space block table record for write
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                using (Face face = new Face(A, B, C, D, true, true, true, true))
                {
                    btr.AppendEntity(face);
                    tr.AddNewlyCreatedDBObject(face, true);
                }

                tr.Commit();
            }

            ed.WriteMessage("\n3D Face created successfully.");
        }
    }
}
