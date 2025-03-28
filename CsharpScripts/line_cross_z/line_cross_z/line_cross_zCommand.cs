using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace line_cross_z
{
    public class line_cross_zCommand
    {
        [CommandMethod("LineCrossZ")]
        public void LineCrossZ()
        {
            // Get active document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Prompt for first point
            PromptPointOptions ppo1 = new PromptPointOptions("\nSelect first point: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d p1 = ppr1.Value;

            // Prompt for second point
            PromptPointOptions ppo2 = new PromptPointOptions("\nSelect second point: ");
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d p2 = ppr2.Value;

            // Prompt for the desired Z
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEnter Z: ");
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;
            double desiredZ = pdr.Value;

            // Calculate the intermediate point at the specified Z
            // We treat the line from p1 to p2 and solve for the point whose Z == desiredZ
            // Formula (similar to the Python):
            //   x = p1.X + (p2.X - p1.X) / (p2.Z - p1.Z) * (desiredZ - p1.Z)
            //   y = p1.Y + (p2.Y - p1.Y) / (p2.Z - p1.Z) * (desiredZ - p1.Z)
            //   z = desiredZ

            // Avoid division by zero if p2.Z == p1.Z
            double zDelta = p2.Z - p1.Z;
            if (System.Math.Abs(zDelta) < 1e-9)
            {
                ed.WriteMessage("\nCannot compute point: the two picked points have the same Z.");
                return;
            }

            double newX = p1.X + (p2.X - p1.X) / zDelta * (desiredZ - p1.Z);
            double newY = p1.Y + (p2.Y - p1.Y) / zDelta * (desiredZ - p1.Z);

            Point3d resultPoint = new Point3d(newX, newY, desiredZ);

            // Add the resulting point to Model Space
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open Model Space for write
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Create a DBPoint
                DBPoint dbPoint = new DBPoint(resultPoint);

                // Add to Model Space
                btr.AppendEntity(dbPoint);
                tr.AddNewlyCreatedDBObject(dbPoint, true);

                // Commit
                tr.Commit();
            }

            // Report
            ed.WriteMessage(
                $"\nCreated a point at X={newX:F3}, Y={newY:F3}, Z={desiredZ:F3}."
            );
        }
    }
}
