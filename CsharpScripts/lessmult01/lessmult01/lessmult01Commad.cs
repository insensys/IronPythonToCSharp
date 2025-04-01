using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace lessmult01
{
    public class lessmult01Command
    {
        // A small tolerance for point-matching, mirroring the Python pushinka=0.001
        private const double Tolerance = 0.001;

        // CommandMethod attribute makes this function callable as a command in AutoCAD (command: LESSMULT)
        [CommandMethod("LESSMULT")]
        public void LessMult()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Repeatedly prompt and process, until user cancels
            // (In Python, the script had a while True loop. Here we break on cancel or error.)
            while (true)
            {
                PromptSelectionResult psr = ed.GetSelection(new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelect objects (points, lines, faces) or press ESC to exit:"
                });
                if (psr.Status != PromptStatus.OK)
                {
                    // User canceled or no selection => exit the command loop
                    break;
                }

                // Gather all points from the selected objects
                List<Point3d> allPoints = new List<Point3d>();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    SelectionSet sset = psr.Value;
                    if (sset == null || sset.Count == 0)
                    {
                        ed.WriteMessage("\nNo objects selected.");
                        tr.Commit();
                        continue;
                    }

                    foreach (SelectedObject selObj in sset)
                    {
                        if (selObj == null) continue;
                        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        if (ent is DBPoint dbPt)
                        {
                            // DBPoint
                            allPoints.Add(dbPt.Position);
                        }
                        else if (ent is Line line)
                        {
                            // StartPoint, EndPoint
                            allPoints.Add(line.StartPoint);
                            allPoints.Add(line.EndPoint);
                        }
                        else if (ent is Face face)
                        {
                            // Face is an old AutoCAD mesh type with up to 4 corners
                            // We'll gather each of the 4 corners
                            // (Note: Face.GetVertexAt(i) returns a Point3d)
                            for (int i = 0; i < 4; i++)
                            {
                                Point3d p = face.GetVertexAt((short)i);
                                allPoints.Add(p);
                            }
                        }
                        // You could extend with more geometry types if needed.
                    }
                    tr.Commit();
                }

                if (allPoints.Count == 0)
                {
                    ed.WriteMessage("\nNo point data gathered from selection.");
                    continue;
                }

                // Prompt for first and second reference points
                PromptPointResult ppr1 = ed.GetPoint("\nPick first reference point (or ESC to stop):");
                if (ppr1.Status != PromptStatus.OK) break;
                Point3d pnt1 = ppr1.Value;

                PromptPointResult ppr2 = ed.GetPoint("\nPick second reference point (or ESC to stop):");
                if (ppr2.Status != PromptStatus.OK) break;
                Point3d pnt2 = ppr2.Value;

                // Find midpoints along the line from pnt1 to pnt2 that coincide with existing points
                List<Point3d> midpoints = SelectPointsBetween2Points(allPoints, pnt1, pnt2);

                int howMany = 1; // Number of "segments" + 1, as in the Python variable "skolko"
                if (midpoints.Count > 0)
                {
                    howMany = midpoints.Count + 1;
                    ed.WriteMessage($"\nFound {midpoints.Count} midpoints between the two points. So total segments = {howMany}.\n");
                }
                else
                {
                    ed.WriteMessage("\nNo midpoints found between the chosen points. Only 1 segment.\n");
                }

                // Prompt for the next two points (pnt3, pnt4)
                PromptPointResult ppr3 = ed.GetPoint("\nPick third point:");
                if (ppr3.Status != PromptStatus.OK) break;
                Point3d pnt3 = ppr3.Value;

                PromptPointResult ppr4 = ed.GetPoint("\nPick fourth point:");
                if (ppr4.Status != PromptStatus.OK) break;
                Point3d pnt4 = ppr4.Value;

                // Now replicate the Python logic to create 3DFaces forming a "patch" from pnt1->pnt2 to pnt3->pnt4
                // We'll do so in a transaction:
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // The logic from Python sets up a "matrix" of points:
                    //  - top edge from pnt1->pnt2, subdivided by midpoints
                    //  - bottom edge from pnt4->pnt3, subdivided in the same manner
                    //  - Then fill in 3D faces between them.

                    // We'll store those "two edges" as lists
                    List<Point3d> topEdge = new List<Point3d>();
                    List<Point3d> bottomEdge = new List<Point3d>();

                    // topEdge: pnt1 => (midpoints...) => pnt2
                    topEdge.Add(pnt1);
                    topEdge.AddRange(midpoints);
                    topEdge.Add(pnt2);

                    // bottomEdge should have "howMany+1" points in the Python code,
                    // But let's just do linear interpolation from pnt4->pnt3 across howMany segments (like the Python did).
                    // Actually in Python "skolko2" was 1 => so there's only one "row" below. We'll do the same.
                    for (int i = 0; i < howMany; i++)
                    {
                        double fraction = (howMany > 1) ? (double)i / (howMany - 1) : 0.0;
                        double x = Interpolate(pnt4.X, pnt3.X, fraction);
                        double y = Interpolate(pnt4.Y, pnt3.Y, fraction);
                        double z = Interpolate(pnt4.Z, pnt3.Z, fraction);
                        bottomEdge.Add(new Point3d(x, y, z));
                    }

                    // Now create 3DFaces between topEdge and bottomEdge
                    // topEdge[i], topEdge[i+1], bottomEdge[i], bottomEdge[i+1]
                    // We'll do it in pairs, as the python does:
                    for (int i = 0; i < howMany; i++)
                    {
                        if (i < howMany - 1)
                        {
                            // Face 1: topEdge[i], topEdge[i+1], bottomEdge[i], repeat topEdge[i]
                            Face face1 = new Face(
                                topEdge[i],
                                topEdge[i + 1],
                                bottomEdge[i],
                                topEdge[i], // The 4th vertex repeated for 3D Face
                                true, true, true, true
                            );
                            btr.AppendEntity(face1);
                            tr.AddNewlyCreatedDBObject(face1, true);

                            // Face 2: bottomEdge[i], bottomEdge[i+1], topEdge[i+1], bottomEdge[i]
                            // In Python: the second face is basically the other half of the quad
                            if (i < bottomEdge.Count - 1)
                            {
                                Face face2 = new Face(
                                    bottomEdge[i + 1],
                                    topEdge[i + 1],
                                    bottomEdge[i],
                                    bottomEdge[i], // Repeated as the 4th vertex
                                    true, true, true, true
                                );
                                btr.AppendEntity(face2);
                                tr.AddNewlyCreatedDBObject(face2, true);
                            }
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\nCreated patch of 3D faces. Segment count: {howMany}.\n");
            } // end while
        }

        // Interpolation helper (like the Python function interpol)
        private double Interpolate(double start, double end, double fraction)
        {
            return start + (end - start) * fraction;
        }

        // Equivalent to Python 'dist' function, but we can just use Point3d.GetDistanceTo
        private double Dist(Point3d a, Point3d b)
        {
            return a.DistanceTo(b);
        }

        // This method mirrors the Python select_points_between_2_points function.
        private List<Point3d> SelectPointsBetween2Points(List<Point3d> allPts, Point3d pnt1, Point3d pnt2)
        {
            double lineLen = Dist(pnt1, pnt2);

            // Points that lie exactly on the segment from pnt1 to pnt2, within tolerance
            List<(Point3d pt, double distA, double distB)> onSegment = new List<(Point3d, double, double)>();

            foreach (Point3d p in allPts)
            {
                double distA = Dist(pnt1, p);
                double distB = Dist(p, pnt2);
                double sum = distA + distB;
                // If sum ~ lineLen, then p lies on the line segment (within Tolerance)
                // We'll do an absolute check of sum-lineLen
                if (Math.Abs(lineLen - sum) < Tolerance)
                {
                    onSegment.Add((p, distA, distB));
                }
            }

            // We only want midpoints strictly between pnt1 and pnt2 (not pnt1 or pnt2)
            // so ignore distA or distB < Tolerance
            // Then we gather unique distA values
            List<double> distances = new List<double>();
            foreach (var info in onSegment)
            {
                if (info.distA > Tolerance && info.distB > Tolerance)
                {
                    distances.Add(info.distA);
                }
            }

            distances = distances.Distinct().OrderBy(x => x).ToList();

            List<Point3d> midPoints = new List<Point3d>();
            foreach (double d in distances)
            {
                // Find the first point with that distance in onSegment
                // (like the python used a "j==0" approach)
                // Then skip duplicates.
                var found = onSegment.FirstOrDefault(o => Math.Abs(o.distA - d) < Tolerance);
                if (found.pt != Point3d.Origin) // quick check that we found something
                {
                    midPoints.Add(found.pt);
                }
            }

            // Remove near-duplicates again if needed
            List<Point3d> checkedPoints = new List<Point3d>();
            foreach (Point3d mp in midPoints)
            {
                if (checkedPoints.Count == 0)
                {
                    checkedPoints.Add(mp);
                }
                else
                {
                    if (Dist(mp, checkedPoints[checkedPoints.Count - 1]) > Tolerance)
                    {
                        checkedPoints.Add(mp);
                    }
                }
            }

            return checkedPoints;
        }
    }
}
