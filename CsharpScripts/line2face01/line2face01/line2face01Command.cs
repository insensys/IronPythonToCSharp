using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Line2FacePlugin
{
    public class Commands
    {
        [CommandMethod("LINE2FACE")] // The command name typed in AutoCAD
        public void Line2FaceCommand()
        {
            // 1. Get the current document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 2. Prompt user for the height (h)
            PromptDoubleOptions hOpts = new PromptDoubleOptions("\nEnter height (h): ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptDoubleResult hRes = ed.GetDouble(hOpts);
            if (hRes.Status != PromptStatus.OK) return; // user canceled
            double h = hRes.Value; // The vertical offset

            // We'll do a loop that keeps asking for points until user cancels
            while (true)
            {
                // 3. Prompt for point A
                PromptPointOptions aOpts = new PromptPointOptions("\nSpecify point A or press ESC to exit:");
                PromptPointResult aRes = ed.GetPoint(aOpts);
                if (aRes.Status != PromptStatus.OK) break; // user canceled
                Point3d A = aRes.Value;

                // 4. Prompt for point B
                PromptPointOptions bOpts = new PromptPointOptions("\nSpecify point B or press ESC to exit:");
                bOpts.UseBasePoint = false;
                PromptPointResult bRes = ed.GetPoint(bOpts);
                if (bRes.Status != PromptStatus.OK) break; // user canceled
                Point3d B = bRes.Value;

                // 5. Calculate points C, D in Z dimension
                // C = B + (0,0,h), D = A + (0,0,h)
                Point3d C = new Point3d(B.X, B.Y, B.Z + h);
                Point3d D = new Point3d(A.X, A.Y, A.Z + h);

                // 6. Create a 3D Face in the current drawing
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // The 3DFace object
                    Face face = new Face(A, B, C, D, true, true, true, true);
                    ms.AppendEntity(face);
                    tr.AddNewlyCreatedDBObject(face, true);

                    tr.Commit();
                }
            }
        }
    }
}
