using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace less01
{
    public class less01Command
    {
        private const double Tolerance = 0.001;

        /// <summary>
        /// Computes the Euclidean distance between two 3D points.
        /// </summary>
        private double Dist(Point3d a, Point3d b)
        {
            return Math.Sqrt(
                (b.X - a.X) * (b.X - a.X)
              + (b.Y - a.Y) * (b.Y - a.Y)
              + (b.Z - a.Z) * (b.Z - a.Z)
            );
        }

        /// <summary>
        /// Finds all points in 'allPoints' that lie directly between pnt1 and pnt2
        /// and returns them in a sorted list based on distance from pnt1.
        /// </summary>
        private List<Point3d> SelectPointsBetweenTwoPoints(
            Point3d pnt1,
            Point3d pnt2,
            List<Point3d> allPoints)
        {
            double segmentLength = Dist(pnt1, pnt2);
            var selectedPoints = new List<(Point3d pt, double distA, double distB)>();

            // Collect points on the segment
            foreach (var p in allPoints)
            {
                double distA = Dist(pnt1, p);
                double distB = Dist(p, pnt2);
                double check = segmentLength - distA - distB;

                // If close to zero, the point p lies on the line between pnt1 and pnt2
                if (Math.Abs(check) < Tolerance)
                {
                    selectedPoints.Add((p, distA, distB));
                }
            }

            // Sort them based on distance from pnt1, ignoring any that coincide
            // exactly with pnt1 or pnt2
            selectedPoints = selectedPoints
                .Where(sp => sp.distA > Tolerance && sp.distB > Tolerance)
                .OrderBy(sp => sp.distA)
                .ToList();

            // Extract the sorted point list
            return selectedPoints.Select(sp => sp.pt).Distinct().ToList();
        }

        /// <summary>
        /// Command that replicates the Python script logic:
        /// 1) Prompt to select objects containing points/lines/faces.
        /// 2) Gather all relevant points.
        /// 3) Ask for 4 points from the user.
        /// 4) Create 3D faces (or lines) between these points.
        /// </summary>
        [CommandMethod("LESS01")]
        public void Less01_CS()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Step 1: Prompt user to select objects
            var pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect objects (Points, Lines, 3D Faces): "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nSelection canceled or invalid.");
                return;
            }

            // Step 2: Gather points from selection
            var allPoints = new List<Point3d>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in psr.Value)
                {
                    if (selObj == null) continue;

                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // If it's a DBPoint, add that point
                    if (ent is DBPoint dbp)
                    {
                        allPoints.Add(dbp.Position);
                    }
                    // If it's a Line, add start/end points
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }
                    // If it's a Face, add its vertices
                    else if (ent is Face face)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            var v = face.GetVertexAt((short)i);
                            allPoints.Add(new Point3d(v.X, v.Y, v.Z));
                        }
                    }
                }

                tr.Commit();
            }

            // Step 3: Prompt for four points in space
            // pnt1, pnt2 used to find midpoints, pnt3, pnt4 for building 3D patch
            PromptPointResult ppr1 = ed.GetPoint("\nPick first point (pnt1): ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nPick second point (pnt2): ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            // Find midpoints along the segment
            var midpoints = SelectPointsBetweenTwoPoints(pnt1, pnt2, allPoints);

            ed.WriteMessage($"\nNumber of intermediate points found on the line: {midpoints.Count}");

            PromptPointResult ppr3 = ed.GetPoint("\nPick third point (pnt3): ");
            if (ppr3.Status != PromptStatus.OK) return;
            Point3d pnt3 = ppr3.Value;

            PromptPointResult ppr4 = ed.GetPoint("\nPick fourth point (pnt4): ");
            if (ppr4.Status != PromptStatus.OK) return;
            Point3d pnt4 = ppr4.Value;

            // Step 4: Build 3D faces or lines bridging these points
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null) return;

                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (ms == null) return;

                //  Example logic: create 3D Faces between [pnt1, pnt2] and [pnt3, pnt4]
                //  or create segments for midpoints. This is just an illustration:

                // (A) Connect pnt1 -> first midpoint, then connect last midpoint -> pnt2
                if (midpoints.Count > 0)
                {
                    // Create a line from pnt1 to the first midpoint
                    Line lineStart = new Line(pnt1, midpoints[0]);
                    ms.AppendEntity(lineStart);
                    tr.AddNewlyCreatedDBObject(lineStart, true);

                    // Create a line from the last midpoint to pnt2
                    Line lineEnd = new Line(midpoints[midpoints.Count - 1], pnt2);
                    ms.AppendEntity(lineEnd);
                    tr.AddNewlyCreatedDBObject(lineEnd, true);

                    // Connect each adjacent pair of midpoints
                    for (int i = 1; i < midpoints.Count; i++)
                    {
                        Line lineMid = new Line(midpoints[i - 1], midpoints[i]);
                        ms.AppendEntity(lineMid);
                        tr.AddNewlyCreatedDBObject(lineMid, true);
                    }
                }

                // (B) Optionally create a 3D Face using pnt1, pnt2, pnt3, pnt4
                // Just as a demonstration, create one single 3DFace:
                Face face = new Face(
                    new Point3d(pnt1.X, pnt1.Y, pnt1.Z),
                    new Point3d(pnt2.X, pnt2.Y, pnt2.Z),
                    new Point3d(pnt3.X, pnt3.Y, pnt3.Z),
                    new Point3d(pnt4.X, pnt4.Y, pnt4.Z),
                    true, true, true, true
                );
                ms.AppendEntity(face);
                tr.AddNewlyCreatedDBObject(face, true);

                tr.Commit();
            }

            ed.WriteMessage("\n3D Faces and lines created successfully.");
        }
    }
}
