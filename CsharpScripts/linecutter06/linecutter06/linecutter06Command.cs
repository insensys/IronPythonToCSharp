using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace linecutter06
{
    public class linecutter06Commands
    {
        // A small tolerance to handle floating-point comparisons
        private const double pushinka = 0.001;

        // 3D distance between two Point3d
        private static double Dist(Point3d a, Point3d b)
        {
            return Math.Sqrt(
                (b.X - a.X) * (b.X - a.X) +
                (b.Y - a.Y) * (b.Y - a.Y) +
                (b.Z - a.Z) * (b.Z - a.Z)
            );
        }

        // Intersection check helper used in Python version:
        // Returns a determinant-like value used to test line intersection in 2D/3D.
        // The original script lumps X, Y, Z into one formula. It effectively
        // attempts a parametric intersection in 3D. If 'denom' is near zero,
        // lines may be parallel or do not intersect in-plane.
        private static double LinesIntersect(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            // For convenience:
            double xA = A.X, yA = A.Y, zA = A.Z;
            double xB = B.X, yB = B.Y, zB = B.Z;
            double xC = C.X, yC = C.Y, zC = C.Z;
            double xD = D.X, yD = D.Y, zD = D.Z;

            // This is the same expression used in the Python script
            double denom = (
                -yB * xD + yB * xC + yA * xD - yA * xC
                - yD * xA + yD * xB + yC * xA - yC * xB
            );
            return denom;
        }

        // Computes the actual intersection point among two line segments (A->B and C->D)
        // using the formula from the original Python code.
        private static Point3d PointBetween4Points(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            double xA = A.X, yA = A.Y, zA = A.Z;
            double xB = B.X, yB = B.Y, zB = B.Z;
            double xC = C.X, yC = C.Y, zC = C.Z;
            double xD = D.X, yD = D.Y, zD = D.Z;

            double denom = LinesIntersect(A, B, C, D);
            // If denom is extremely close to zero, we can't reliably intersect.
            if (Math.Abs(denom) < pushinka)
            {
                // Just return A or something default. The caller must check this.
                return A;
            }

            // The Python script has these big expressions for x, y, z:
            double x = (
                xB * yA * xD - xB * yA * xC - xA * xC * yD - xB * yC * xD
              + xB * xC * yD + xA * yB * xC + xA * yC * xD - xA * yB * xD
            ) / denom;

            double y = (
                yB * xC * yD + yC * xA * yB - yD * xA * yB - yB * yC * xD
              - yA * xC * yD + yD * yA * xB + yA * yC * xD - yC * yA * xB
            ) / denom;

            double z = (
                -yD * xA * zB + yA * xD * zB + yC * xA * zB - yA * xC * zB
              - xD * zA * yB - zA * yC * xB - zA * xC * yD + zA * yC * xD
              - zB * yC * xD + zB * xC * yD + xC * zA * yB + zA * yD * xB
            ) / denom;

            return new Point3d(x, y, z);
        }

        // Checks which points among 'candidates' actually lie on the segment pnt1->pnt2.
        // Original logic used a distance-based check: dist(A,p) + dist(p,B) ~ dist(A,B).
        private static List<Point3d> SelectPointsBetween2Points(Point3d p1, Point3d p2, List<Point3d> candidates)
        {
            double segmentLen = Dist(p1, p2);
            var result = new List<Point3d>();

            // Keep only the points that appear to be on the line segment p1->p2
            foreach (var p in candidates)
            {
                double dA = Dist(p1, p);
                double dB = Dist(p, p2);
                double diff = Math.Abs(segmentLen - (dA + dB));
                if (diff < pushinka)
                {
                    // It's on the segment
                    result.Add(p);
                }
            }

            // Because the script tries to remove duplicates or nearly duplicates
            // let's filter them by distance to the last accepted point
            var cleaned = new List<Point3d>();
            for (int i = 0; i < result.Count; i++)
            {
                if (i == 0)
                {
                    cleaned.Add(result[i]);
                }
                else
                {
                    if (Dist(result[i], cleaned[cleaned.Count - 1]) > pushinka)
                    {
                        cleaned.Add(result[i]);
                    }
                }
            }

            return cleaned;
        }

        [CommandMethod("LineCutter06")]
        public void LineCutter06Command()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Prompt user to select objects
            PromptSelectionOptions opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect lines or 3D faces: "
            };
            PromptSelectionResult psr = ed.GetSelection(opts);
            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo objects selected. Exiting...");
                return;
            }

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                // We gather all lines and 3D faces from the selection,
                // grouping them by layer. (The original script does so, though
                // the usage is somewhat layer-based.)
                List<Line> lines = new List<Line>();
                List<Face> faces = new List<Face>();

                foreach (SelectedObject selObj in psr.Value)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent is Line lineEnt)
                    {
                        lines.Add(lineEnt);
                    }
                    else if (ent is Face faceEnt)
                    {
                        faces.Add(faceEnt);
                    }
                }

                // We'll store start/end points in parallel lists
                List<Point3d> starts = new List<Point3d>();
                List<Point3d> ends = new List<Point3d>();

                // For each line, gather start/end
                foreach (var ln in lines)
                {
                    starts.Add(ln.StartPoint);
                    ends.Add(ln.EndPoint);
                }

                // For each face (3d face), gather edges as if lines
                foreach (var fc in faces)
                {
                    // Face has four corners in AutoCAD
                    var A = fc.GetVertexAt(0);
                    var B = fc.GetVertexAt(1);
                    var C = fc.GetVertexAt(2);
                    var D = fc.GetVertexAt(3);

                    starts.Add(new Point3d(A.X, A.Y, A.Z));
                    ends.Add(new Point3d(B.X, B.Y, B.Z));

                    starts.Add(new Point3d(B.X, B.Y, B.Z));
                    ends.Add(new Point3d(C.X, C.Y, C.Z));

                    starts.Add(new Point3d(C.X, C.Y, C.Z));
                    ends.Add(new Point3d(D.X, D.Y, D.Z));

                    starts.Add(new Point3d(D.X, D.Y, D.Z));
                    ends.Add(new Point3d(A.X, A.Y, A.Z));
                }

                // We'll add intersection points as DBPoint objects to model space
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Main loop: compare each line with the others
                for (int i = 0; i < starts.Count; i++)
                {
                    Point3d A = starts[i];
                    Point3d B = ends[i];

                    // We'll store intersection points for line i
                    List<Point3d> intersectionPoints = new List<Point3d>();

                    // Compare with lines j>i
                    for (int j = i + 1; j < starts.Count; j++)
                    {
                        Point3d C = starts[j];
                        Point3d D = ends[j];

                        double denom = LinesIntersect(A, B, C, D);
                        if (Math.Abs(denom) > pushinka)
                        {
                            // There's some intersection
                            Point3d F = PointBetween4Points(A, B, C, D);

                            // Check if F is truly on both segments
                            double AF = Dist(A, F);
                            double FB = Dist(F, B);
                            double AB = Dist(A, B);

                            double CF = Dist(C, F);
                            double FD = Dist(F, D);
                            double CD = Dist(C, D);

                            // If F is within the segment extents
                            if (Math.Abs((AF + FB) - AB) < pushinka &&
                                Math.Abs((CF + FD) - CD) < pushinka)
                            {
                                intersectionPoints.Add(F);
                            }
                        }
                    }

                    // Now filter the intersection points that actually lie on A->B
                    var finalPointsOnSegment = SelectPointsBetween2Points(A, B, intersectionPoints);

                    // Place DBPoint entities for the intersection points
                    foreach (var pnt in finalPointsOnSegment)
                    {
                        DBPoint dbPt = new DBPoint(pnt);
                        // Optionally set layer, color, etc.:
                        // dbPt.Layer = "SomeLayer";
                        btr.AppendEntity(dbPt);
                        tr.AddNewlyCreatedDBObject(dbPt, true);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nLine intersections processed, points placed.");
        }
    }
}
