using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace facesplitter03
{
    public class facesplitter03Command
    {
        // A small threshold to decide if an edge is "negligible"
        private const double PUSHINKA = 0.001;

        [CommandMethod("FaceSplitter03")]
        public void FaceSplitter03()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Prompt user for subdivision factor
            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nEnter subdivision factor (kancha): ");
            intOpts.AllowZero = false;
            intOpts.AllowNegative = false;
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;

            int kancha = intRes.Value;

            // Prompt for selection of 3D Faces
            PromptSelectionOptions selOpts = new PromptSelectionOptions();
            selOpts.MessageForAdding = "\nSelect 3D Faces to subdivide:";
            PromptSelectionResult selRes = ed.GetSelection(selOpts);

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo selection made.");
                return;
            }

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Get current space (ModelSpace) for adding new entities
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                SelectionSet sset = selRes.Value;
                foreach (SelectedObject so in sset)
                {
                    if (so == null) continue;
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent is Face faceObj)
                    {
                        // Retrieve the four corner points (some may be repeated for triangles)
                        Point3d p0 = faceObj.GetVertexAt(0);
                        Point3d p1 = faceObj.GetVertexAt(1);
                        Point3d p2 = faceObj.GetVertexAt(2);
                        Point3d p3 = faceObj.GetVertexAt(3);

                        // Determine if triangle or quad by checking for negligible edges
                        bool isTriangle = IsTriangle(p0, p1, p2, p3);

                        if (isTriangle)
                        {
                            // Subdivide a triangular Face
                            SubdivideTriangle(btr, p0, p1, p2, kancha, db);
                        }
                        else
                        {
                            // Subdivide a quadrilateral Face
                            SubdivideQuad(btr, p0, p1, p2, p3, kancha, db);
                        }
                    }
                }

                tr.Commit();
            }
        }

        private bool IsTriangle(Point3d p0, Point3d p1, Point3d p2, Point3d p3)
        {
            // Compute distances
            double d01 = p0.DistanceTo(p1);
            double d12 = p1.DistanceTo(p2);
            double d23 = p2.DistanceTo(p3);
            double d30 = p3.DistanceTo(p0);

            // If one edge is extremely small, treat as triangle
            // (In the Python code, any edge under 'pushinka' means it's effectively a triangle.)
            if (d01 < PUSHINKA || d12 < PUSHINKA || d23 < PUSHINKA || d30 < PUSHINKA)
                return true;

            return false;
        }

        private void SubdivideQuad(BlockTableRecord btr, Point3d p0, Point3d p1, Point3d p2, Point3d p3, int kancha, Database db)
        {
            // Build a 2D/3D grid of points between the four corners
            // Then create smaller 3D Faces for each subdivision cell.
            // Simplified example logic:

            // Generate the 2D grid
            Point3d[,] grid = new Point3d[kancha + 1, kancha + 1];

            for (int i = 0; i <= kancha; i++)
            {
                // Interpolate along the edges from p0->p1 and p3->p2
                Point3d left = Interpolate(p0, p3, i, kancha);
                Point3d right = Interpolate(p1, p2, i, kancha);

                for (int j = 0; j <= kancha; j++)
                {
                    grid[i, j] = Interpolate(left, right, j, kancha);
                }
            }

            // Now create 3DFaces for each cell of the grid
            for (int i = 0; i < kancha; i++)
            {
                for (int j = 0; j < kancha; j++)
                {
                    Point3d pA = grid[i, j];
                    Point3d pB = grid[i, j + 1];
                    Point3d pC = grid[i + 1, j + 1];
                    Point3d pD = grid[i + 1, j];

                    // Create a 3DFace
                    Face newFace = new Face(pA, pB, pC, pD, true, true, true, true);
                    btr.AppendEntity(newFace);
                    db.TransactionManager.AddNewlyCreatedDBObject(newFace, true);
                }
            }
        }

        private void SubdivideTriangle(BlockTableRecord btr, Point3d p0, Point3d p1, Point3d p2, int kancha, Database db)
        {
            // Similar approach but handle 3 corners, generating a triangular mesh

            // We'll treat p0->p1 as base, then p0->p2 as left edge, etc.
            // For brevity, you can replicate logic from the Python code to do the triangular interpolation.

            // Example logic:
            Point3d[,] grid = new Point3d[kancha + 1, kancha + 1];

            // p0->p1 is the "base" direction
            // p0->p2 is the "height" direction
            for (int i = 0; i <= kancha; i++)
            {
                Point3d rowStart = Interpolate(p0, p1, i, kancha);
                Point3d rowEnd = Interpolate(p0, p2, i, kancha);

                for (int j = 0; j <= i; j++)
                {
                    grid[i, j] = Interpolate(rowStart, rowEnd, j, i);
                }
            }

            // Then create 3DFaces row by row
            for (int i = 0; i < kancha; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    Point3d pA = grid[i, j];
                    Point3d pB = grid[i, j + 1];
                    Point3d pC = grid[i + 1, j + 1];

                    // First face
                    Face newFace1 = new Face(pA, pB, pC, pA, true, true, true, true);
                    btr.AppendEntity(newFace1);
                    db.TransactionManager.AddNewlyCreatedDBObject(newFace1, true);

                    // Second face (optional if you want a fully triangulated region)
                    if (j < i)
                    {
                        Point3d pD = grid[i + 1, j];
                        Face newFace2 = new Face(pA, pC, pD, pA, true, true, true, true);
                        btr.AppendEntity(newFace2);
                        db.TransactionManager.AddNewlyCreatedDBObject(newFace2, true);
                    }
                }
            }
        }

        private Point3d Interpolate(Point3d start, Point3d end, int step, int totalSteps)
        {
            double t = (totalSteps == 0) ? 0.0 : (double)step / totalSteps;
            double x = start.X + (end.X - start.X) * t;
            double y = start.Y + (end.Y - start.Y) * t;
            double z = start.Z + (end.Z - start.Z) * t;
            return new Point3d(x, y, z);
        }
    }
}
