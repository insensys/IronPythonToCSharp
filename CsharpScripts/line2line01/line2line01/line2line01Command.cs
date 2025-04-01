using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;


namespace line2line01
{
    public class line2line01Command
    {
        [CommandMethod("Line2LineOffset")]
        public static void Line2LineOffsetCommand()
        {
            // Get the active document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Prompt user for the height 'h'
            PromptDoubleOptions heightOpts = new PromptDoubleOptions("\nEnter height offset (h): ");
            PromptDoubleResult heightRes = ed.GetDouble(heightOpts);
            if (heightRes.Status != PromptStatus.OK) return;
            double h = heightRes.Value;

            // Continuous loop, similar to the Python script's while(1==1).
            // We’ll allow the user to exit by pressing ESC or entering Cancel.
            while (true)
            {
                // Prompt for first point A
                PromptPointOptions ppoA = new PromptPointOptions("\nSpecify point A or press ESC to exit: ");
                PromptPointResult pprA = ed.GetPoint(ppoA);
                if (pprA.Status != PromptStatus.OK) break;
                Point3d A = pprA.Value;

                // Prompt for second point B
                PromptPointOptions ppoB = new PromptPointOptions("\nSpecify point B or press ESC to exit: ");
                PromptPointResult pprB = ed.GetPoint(ppoB);
                if (pprB.Status != PromptStatus.OK) break;
                Point3d B = pprB.Value;

                // Create points C and D with the offset h in Z direction
                Point3d C = new Point3d(B.X, B.Y, B.Z + h);
                Point3d D = new Point3d(A.X, A.Y, A.Z + h);

                // Add the line from C to D to Model Space
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    using (Line line = new Line(C, D))
                    {
                        btr.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                    }
                    tr.Commit();
                }
            }
        }
    }
}
