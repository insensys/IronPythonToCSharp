using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace rectmult
{
    public class rectmultCommand
    {
        [CommandMethod("RectMult")]
        public void CreateRectangularMesh()
        {
            // Get the current document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 1) Prompt for the first point
                PromptPointResult ppr1 = ed.GetPoint("\nSelect first point: ");
                if (ppr1.Status != PromptStatus.OK) return;
                Point3d pt1 = ppr1.Value;

                // 2) Prompt for the second point
                PromptPointResult ppr2 = ed.GetPoint("\nSelect second point: ");
                if (ppr2.Status != PromptStatus.OK) return;
                Point3d pt2 = ppr2.Value;

                // 3) Prompt for number of subdivisions between first and second points
                PromptIntegerResult pi1 = ed.GetInteger("\nNumber of divisions between first and second points: ");
                if (pi1.Status != PromptStatus.OK) return;
                int nDiv1 = pi1.Value;
                if (nDiv1 < 1) nDiv1 = 1;

                // 4) Prompt for the third point
                PromptPointResult ppr3 = ed.GetPoint("\nSelect third point: ");
                if (ppr3.Status != PromptStatus.OK) return;
                Point3d pt3 = ppr3.Value;

                // 5) Prompt for number of subdivisions between second and third points
                PromptIntegerResult pi2 = ed.GetInteger("\nNumber of divisions between second and third points: ");
                if (pi2.Status != PromptStatus.OK) return;
                int nDiv2 = pi2.Value;
                if (nDiv2 < 1) nDiv2 = 1;

                // 6) Prompt for the fourth point
                PromptPointResult ppr4 = ed.GetPoint("\nSelect fourth point: ");
                if (ppr4.Status != PromptStatus.OK) return;
                Point3d pt4 = ppr4.Value;

                // Prepare a 2D array to hold grid points
                Point3d[,] grid = new Point3d[nDiv2 + 1, nDiv1 + 1];

                // Fill the corners of the grid
                // Top row: pt1 (left) ... pt2 (right)
                grid[0, 0] = pt1;
                grid[0, nDiv1] = pt2;

                // Bottom row: pt4 (left) ... pt3 (right)
                grid[nDiv2, 0] = pt4;
                grid[nDiv2, nDiv1] = pt3;

                // Interpolate along top row
                for (int i = 1; i < nDiv1; i++)
                {
                    double t = (double)i / nDiv1;
                    grid[0, i] = Interpolate(pt1, pt2, t);
                }

                // Interpolate along bottom row
                for (int i = 1; i < nDiv1; i++)
                {
                    double t = (double)i / nDiv1;
                    grid[nDiv2, i] = Interpolate(pt4, pt3, t);
                }

                // Interpolate interior rows
                for (int row = 1; row < nDiv2; row++)
                {
                    double rt = (double)row / nDiv2;
                    for (int col = 0; col <= nDiv1; col++)
                    {
                        Point3d topPt = grid[0, col];
                        Point3d botPt = grid[nDiv2, col];
                        grid[row, col] = Interpolate(topPt, botPt, rt);
                    }
                }

                // Now create 3D faces for each cell in the grid
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    // BlockTable and BlockTableRecord (model space)
                    BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Create 3D faces by connecting adjacent grid points
                    for (int row = 0; row < nDiv2; row++)
                    {
                        for (int col = 0; col < nDiv1; col++)
                        {
                            // Grab the corners of this cell
                            Point3d p1 = grid[row, col];
                            Point3d p2 = grid[row, col + 1];
                            Point3d p3 = grid[row + 1, col + 1];
                            Point3d p4 = grid[row + 1, col];

                            // Create a 3D face
                            Face face = new Face(p1, p2, p3, p4, true, true, true, true);

                            // Add to model space
                            btr.AppendEntity(face);
                            tr.AddNewlyCreatedDBObject(face, true);
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\nMesh of 3D faces created successfully.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: " + ex.Message);
            }
        }

        // Linear interpolation helper
        private Point3d Interpolate(Point3d start, Point3d end, double t)
        {
            return new Point3d(
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t,
                start.Z + (end.Z - start.Z) * t
            );
        }
    }
}
