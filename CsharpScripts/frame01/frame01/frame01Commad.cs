using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace frame01
{
    public class frame01Commad
    {
        // Slight tolerance used to compare distances (analogous to pushinka=0.001)
        private const double Tolerance = 0.001;

        [CommandMethod("Frame01")]
        public void Frame01Command()
        {
            // Get current document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Start by prompting the user to select objects
            PromptSelectionOptions selOptions = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect objects (Points, Lines, Faces)..."
            };

            PromptSelectionResult selResult = ed.GetSelection(selOptions);
            if (selResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo objects selected.");
                return;
            }

            // We'll collect all point coordinates from DBPoints, Lines, and Faces
            List<Point3d> allPoints = new List<Point3d>();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                // Retrieve the selection set
                SelectionSet sset = selResult.Value;

                // Read each selected object
                foreach (SelectedObject selObj in sset)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // If it's a DBPoint, add its position
                    if (ent is DBPoint dbPt)
                    {
                        allPoints.Add(dbPt.Position);
                    }
                    // If it's a Line, add start/end
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }
                    // If it's a Face, add its 4 vertices
                    else if (ent is Face face)
                    {
                        // Face has 4 corners
                        for (int i = 0; i < 4; i++)
                        {
                            allPoints.Add(face.GetVertexAt((short)i));
                        }
                    }
                }

                tr.Commit();
            }

            // Ask user for the first two points
            PromptPointResult ppr1 = ed.GetPoint("\nPick first point (pnt1): ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nPick second point (pnt2): ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            // Process points between pnt1 and pnt2
            List<Point3d> midpoints = SelectPointsBetween2Points(pnt1, pnt2, allPoints);

            ed.WriteMessage($"\nFound {midpoints.Count} midpoints between pnt1 and pnt2.");

            // Ask for pnt3
            PromptPointResult ppr3 = ed.GetPoint("\nPick third point (pnt3): ");
            if (ppr3.Status != PromptStatus.OK) return;
            Point3d pnt3 = ppr3.Value;

            // Ask user for how many subdivisions between pnt2 and pnt3
            PromptIntegerResult intRes = ed.GetInteger("\nHow many subdivisions between pnt2 and pnt3?: ");
            if (intRes.Status != PromptStatus.OK) return;
            int subdivisions = intRes.Value;

            // Ask for pnt4
            PromptPointResult ppr4 = ed.GetPoint("\nPick fourth point (pnt4): ");
            if (ppr4.Status != PromptStatus.OK) return;
            Point3d pnt4 = ppr4.Value;

            // Build the final grid (similar to the original Python code)
            // We'll place DBPoints for each subdivided coordinate
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                // Create a 2D matrix of points
                // "subdivisions" for vertical and "midpoints.Count+1" horizontally, matching the original logic
                int colCount = midpoints.Count + 1;
                int rowCount = subdivisions + 1;

                // We'll store the corners: 
                // top-left = pnt1, top-right = pnt2, bottom-left = pnt4, bottom-right = pnt3, etc.
                // The original script does more advanced interpolation but let's replicate the essentials.

                // We can replicate the same approach:
                //  - top row: pnt1... (midpoints) ... pnt2
                //  - bottom row: pnt4... ... pnt3
                // Then subdivide vertically.

                // Prepare top row as a list of points
                List<Point3d> topRow = new List<Point3d>();
                topRow.Add(pnt1);
                foreach (var mp in midpoints)
                    topRow.Add(mp);
                topRow.Add(pnt2);

                // Prepare bottom row by interpolating from pnt4 to pnt3
                List<Point3d> bottomRow = new List<Point3d>();
                for (int i = 0; i < topRow.Count; i++)
                {
                    // For each horizontal step, linearly interpolate between pnt4 and pnt3
                    double t = (double)i / (topRow.Count - 1);
                    Point3d br = Interpolate(pnt4, pnt3, t);
                    bottomRow.Add(br);
                }

                // Now fill out the matrix by interpolating between the top row and bottom row
                for (int row = 0; row < rowCount; row++)
                {
                    double v = (double)row / (rowCount - 1);
                    for (int col = 0; col < colCount; col++)
                    {
                        Point3d pTop = topRow[col];
                        Point3d pBot = bottomRow[col];
                        Point3d finalP = Interpolate(pTop, pBot, v);

                        // Add a DBPoint to the drawing
                        DBPoint dbPt = new DBPoint(finalP);
                        btr.AppendEntity(dbPt);
                        tr.AddNewlyCreatedDBObject(dbPt, true);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nAll subdivided points have been placed.");
        }

        // Equivalent to dist(a,b) from the Python script
        private double Dist(Point3d a, Point3d b)
        {
            return a.DistanceTo(b);
        }

        // Equivalent to the "select_points_between_2_points" logic
        private List<Point3d> SelectPointsBetween2Points(Point3d pnt1, Point3d pnt2, List<Point3d> allPts)
        {
            double L = Dist(pnt1, pnt2);
            List<(Point3d pt, double d1, double d2)> selected = new List<(Point3d, double, double)>();

            // Find all points that lie exactly on line segment pnt1->pnt2 (within a Tolerance)
            foreach (var p in allPts)
            {
                double A = Dist(pnt1, p);
                double B = Dist(p, pnt2);
                double diff = L - (A + B);
                if (Math.Abs(diff) < Tolerance)
                {
                    selected.Add((p, A, B));
                }
            }

            // We only keep those that are not at the endpoints (like the Python code)
            // Then sort by distance from pnt1
            var insidePoints = selected
                .Where(x => x.d1 > Tolerance && x.d2 > Tolerance)
                .OrderBy(x => x.d1)
                .Select(x => x.pt)
                .ToList();

            return insidePoints;
        }

        // Simple linear interpolation between two points
        private Point3d Interpolate(Point3d a, Point3d b, double t)
        {
            double x = (b.X - a.X) * t + a.X;
            double y = (b.Y - a.Y) * t + a.Y;
            double z = (b.Z - a.Z) * t + a.Z;
            return new Point3d(x, y, z);
        }
    }
}
