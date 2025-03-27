using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace linesbyfacesmult03
{
    public class linesbyfacesmult03Command
    {
        // A small tolerance used to compare distances
        private const double Tolerance = 0.001;

        // Command entry point
        [CommandMethod("LinesByFacesMult03")]
        public void LinesByFacesMult03()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 1) Prompt user to select objects
            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect points, lines, or faces: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo objects selected.");
                return;
            }

            // We'll collect all 3D points in this list
            List<Point3d> allPoints = new List<Point3d>();

            // 2) Open a transaction to examine the selected entities
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                SelectionSet sset = psr.Value;
                foreach (SelectedObject selObj in sset)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // If it's a DBPoint, gather its single coordinate
                    if (ent is DBPoint dbpt)
                    {
                        allPoints.Add(dbpt.Position);
                    }

                    // If it's a Line, gather start and end
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }

                    // If it's a Face (AutoCAD 3DFace), gather its vertices
                    else if (ent is Face face)
                    {
                        // Face has up to 4 corners
                        try
                        {
                            // 3DFace typically has 4 vertices
                            for (int i = 0; i < 4; i++)
                            {
                                Point3d vertex = face.GetVertexAt((short)i);
                                allPoints.Add(vertex);
                            }
                        }
                        catch
                        {
                            // If fewer than 4 vertices, or any mismatch
                        }
                    }
                }
                tr.Commit();
            }

            // 3) Prompt the user for two reference points
            PromptPointResult ppr1 = ed.GetPoint("\nSpecify first point: ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nSpecify second point: ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            // 4) Find all intermediate points along the segment [pnt1 - pnt2]
            List<Point3d> midPoints = GetPointsOnSegment(allPoints, pnt1, pnt2, Tolerance);

            // 5) Draw lines and points in a new transaction
            using (Transaction trDraw = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = trDraw.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ms = trDraw.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                DrawLine(ms, trDraw, pnt1, pnt2);


                // If we found any intermediate points, draw them
                if (midPoints.Count > 0)
                {
                    // Draw line from first picked point to first midpoint
                    //DrawLine(ms, pnt1, midPoints[0]);

                    //// Draw line from last midpoint to second picked point
                    //DrawLine(ms, midPoints[midPoints.Count - 1], pnt2);

                    // Add point markers for each midpoint
                    for (int i = 0; i < midPoints.Count; i++)
                    {
                        Point3d mp = midPoints[i];
                        DBPoint dbpt = new DBPoint(mp);
                        ms.AppendEntity(dbpt);
                        trDraw.AddNewlyCreatedDBObject(dbpt, true);

                        // If there's more than one midpoint, draw lines between them in sequence
                        if (i > 0)
                        {
                            DrawLine(ms, trDraw, midPoints[i - 1], mp);
                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nNo intermediate points found on the segment.");
                }

                trDraw.Commit();
            }
        }

        // Finds all points from 'allPoints' that lie on the segment [p1 -> p2].
        // Returns a sorted list by distance from p1.
        private List<Point3d> GetPointsOnSegment(List<Point3d> allPoints, Point3d p1, Point3d p2, double tolerance)
        {
            List<(Point3d pt, double dist)> validPoints = new List<(Point3d pt, double dist)>();

            double totalDist = p1.DistanceTo(p2);

            foreach (Point3d p in allPoints)
            {
                double distA = p1.DistanceTo(p);
                double distB = p.DistanceTo(p2);
                // If distA + distB is effectively the same as totalDist, the point lies on the segment
                if (Math.Abs(distA + distB - totalDist) < tolerance)
                {
                    // Exclude the endpoints themselves if desired (like the Python code excludes them if close).
                    // But the user likely wants them if they truly appear. Adjust logic as needed:
                    if (distA > tolerance && distB > tolerance)
                    {
                        validPoints.Add((p, distA));
                    }
                }
            }

            // Sort by distance from p1
            validPoints.Sort((a, b) => a.dist.CompareTo(b.dist));

            // Remove near-duplicates
            List<Point3d> result = new List<Point3d>();
            foreach (var (pt, _) in validPoints)
            {
                if (result.Count == 0)
                {
                    result.Add(pt);
                }
                else
                {
                    if (result[result.Count - 1].DistanceTo(pt) > tolerance)
                        result.Add(pt);
                }
            }

            return result;
        }

        // Helper to quickly create a line in ModelSpace
        private void DrawLine(BlockTableRecord ms, Transaction tr, Point3d start, Point3d end)
        {
            using (Line ln = new Line(start, end))
            {
                ms.AppendEntity(ln);
                tr.AddNewlyCreatedDBObject(ln, true);
            }
        }

    }
}
