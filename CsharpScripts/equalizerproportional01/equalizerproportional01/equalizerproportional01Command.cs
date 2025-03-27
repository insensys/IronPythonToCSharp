// AutoCAD namespaces
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace equalizerproportional01
{
    public class equalizerproportional01Command
    {
        private const double ToleranceValue = 0.001;

        // Distance function
        private static double Dist(Point3d a, Point3d b)
        {
            return a.GetVectorTo(b).Length;
        }

        // Select points strictly between pnt1 and pnt2 (with tolerance checks)
        private static List<Point3d> SelectPointsBetween2Points(
            Point3d pnt1,
            Point3d pnt2,
            List<Point3d> allPoints)
        {
            double lineLength = Dist(pnt1, pnt2);
            var selectedPoints = new List<Tuple<Point3d, double, double, double>>();

            foreach (var p in allPoints)
            {
                double distA = Dist(pnt1, p);
                double distB = Dist(p, pnt2);
                double check = lineLength - distA - distB;

                // If sum of distA + distB is effectively lineLength, point p is on the line segment
                if (Math.Abs(check) < ToleranceValue)
                {
                    selectedPoints.Add(
                        new Tuple<Point3d, double, double, double>(p, distA, distB, check));
                }
            }

            // Gather distances for middle points
            List<double> distances = new List<double>();
            foreach (var sp in selectedPoints)
            {
                double distA = sp.Item2;
                double distB = sp.Item3;
                // Exclude endpoints themselves
                if (distA > ToleranceValue && distB > ToleranceValue)
                    distances.Add(distA);
            }

            distances = distances.Distinct().OrderBy(d => d).ToList();

            var midpoints = new List<Point3d>();
            foreach (double d in distances)
            {
                bool found = false;
                foreach (var sp in selectedPoints)
                {
                    if (!found && Math.Abs(d - sp.Item2) < ToleranceValue)
                    {
                        found = true;
                        midpoints.Add(sp.Item1);
                    }
                }
            }

            // Filter duplicates that might be extremely close
            var checkedPoints = new List<Point3d>();
            for (int i = 0; i < midpoints.Count; i++)
            {
                if (i == 0) checkedPoints.Add(midpoints[i]);
                else
                {
                    if (Dist(midpoints[i], checkedPoints.Last()) > ToleranceValue)
                        checkedPoints.Add(midpoints[i]);
                }
            }

            return checkedPoints;
        }

        // Finds intersection point among 4 corner points
        private static Point3d PointBetween4Points(
            Point3d A,
            Point3d B,
            Point3d C,
            Point3d D)
        {
            // This replicates the math logic in the Python function:
            //     point_between_4_points(A, B, C, D)
            // using an approach similar to solving for intersection in 3D
            // The direct coefficient expansions from the Python version are used here.

            double xA = A.X, yA = A.Y, zA = A.Z;
            double xB = B.X, yB = B.Y, zB = B.Z;
            double xC = C.X, yC = C.Y, zC = C.Z;
            double xD = D.X, yD = D.Y, zD = D.Z;

            // The direct formula from the Python script
            double denom =
                -yB * xD + yB * xC + yA * xD - yA * xC
                - yD * xA + yD * xB + yC * xA - yC * xB;

            // To avoid division by zero:
            if (Math.Abs(denom) < ToleranceValue)
            {
                // Fall back or handle as you prefer, here just returning A
                return A;
            }

            double X = (
                xB * yA * xD - xB * yA * xC - xA * xC * yD - xB * yC * xD
                + xB * xC * yD + xA * yB * xC + xA * yC * xD - xA * yB * xD
            ) / denom;

            double Y = (
                yB * xC * yD + yC * xA * yB - yD * xA * yB
                - yB * yC * xD - yA * xC * yD + yD * yA * xB
                + yA * yC * xD - yC * yA * xB
            ) / denom;

            double Z = (
                -yD * xA * zB + yA * xD * zB + yC * xA * zB - yA * xC * zB
                - xD * zA * yB - zA * yC * xB - zA * xC * yD + zA * yC * xD
                - zB * yC * xD + zB * xC * yD + xC * zA * yB + zA * yD * xB
            ) / denom;

            return new Point3d(X, Y, Z);
        }

        // Helper to do linear interpolation
        private static double Interp(double n1, double n2, double ratio, double total)
        {
            return (n2 - n1) * ratio / total + n1;
        }

        [CommandMethod("EqualizeProportional")]
        public void EqualizeProportions()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Collect points from user selection
            var selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect points, lines, or faces:"
            };
            var selRes = ed.GetSelection(selOpts);
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNothing selected or selection canceled.");
                return;
            }

            var points = new List<Point3d>();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                var selSet = selRes.Value;
                // Collect all relevant points from DBPoint, Line endpoints, Face vertices
                foreach (SelectedObject selObj in selSet)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // If it's a DBPoint
                    if (ent is DBPoint dbp)
                    {
                        points.Add(dbp.Position);
                    }
                    // If it's a Line
                    else if (ent is Line ln)
                    {
                        points.Add(ln.StartPoint);
                        points.Add(ln.EndPoint);
                    }
                    // If it's a Face
                    else if (ent is Face faceEnt)
                    {
                        // Face in AutoCAD is an older object; it has up to 4 vertices
                        // Typically, you would do something like:
                        // Instead of faceEnt.NumVertices, just loop 4 times
                        for (short i = 0; i < 4; i++)
                        {
                            Point3d vertex = faceEnt.GetVertexAt(i);
                            
                            points.Add(vertex);
                        }

                    }
                }
                tr.Commit();
            }

            if (!points.Any())
            {
                ed.WriteMessage("\nNo points found in the selection.");
                return;
            }

            // Prompt user for 4 points: pnt1, pnt2, pnt3, pnt4
            var p1Res = ed.GetPoint("\nPick first point (pnt1): ");
            if (p1Res.Status != PromptStatus.OK) return;
            Point3d pnt1 = p1Res.Value;

            var p2Res = ed.GetPoint("\nPick second point (pnt2): ");
            if (p2Res.Status != PromptStatus.OK) return;
            Point3d pnt2 = p2Res.Value;

            // Compute midpoints on line pnt1→pnt2
            var midpoints = SelectPointsBetween2Points(pnt1, pnt2, points);

            int howManySegments = 1; // how many subdivisions along pnt1→pnt2
            if (midpoints.Count > 0) howManySegments = midpoints.Count + 1;

            ed.WriteMessage($"\nSegments between pnt1 and pnt2: {howManySegments}.");

            var p3Res = ed.GetPoint("\nPick third point (pnt3): ");
            if (p3Res.Status != PromptStatus.OK) return;
            Point3d pnt3 = p3Res.Value;

            var p4Res = ed.GetPoint("\nPick fourth point (pnt4): ");
            if (p4Res.Status != PromptStatus.OK) return;
            Point3d pnt4 = p4Res.Value;

            // Now find midpoints between pnt2→pnt3
            var midpoints2 = SelectPointsBetween2Points(pnt2, pnt3, points);
            int howManySegments2 = 1;
            if (midpoints2.Count > 0) howManySegments2 = midpoints2.Count + 1;

            ed.WriteMessage($"\nSegments between pnt2 and pnt3: {howManySegments2}.");

            // Build the matrix of points
            // P[j][i] in Python => let's replicate with 2D array of Point3d
            Point3d[,] P = new Point3d[howManySegments2 + 1, howManySegments + 1];

            // Fill corners:
            P[0, 0] = pnt1;
            P[0, howManySegments] = pnt2;
            P[howManySegments2, howManySegments] = pnt3;
            P[howManySegments2, 0] = pnt4;

            // Distances for interpolation
            double dist_pnt1_pnt2 = Dist(pnt1, pnt2);

            // Fill the top row, from pnt1 to pnt2, using the midpoints
            for (int i = 1; i < howManySegments; i++)
            {
                P[0, i] = midpoints[i - 1]; // from the sorted midpoints
            }

            // Interpolate the bottom row, from pnt4 to pnt3, at each subdiv length
            double dist_pnt4_pnt3 = Dist(pnt4, pnt3);

            // For each sub-segment along pnt1->pnt2, we set the corresponding point on pnt4->pnt3
            for (int i = 1; i < howManySegments; i++)
            {
                double partialLen = Dist(pnt1, P[0, i]);
                // Interpolate from pnt4 to pnt3 proportionally to partialLen / dist_pnt1_pnt2
                double ratio = partialLen / dist_pnt1_pnt2;

                double xVal = Interp(pnt4.X, pnt3.X, partialLen, dist_pnt1_pnt2);
                double yVal = Interp(pnt4.Y, pnt3.Y, partialLen, dist_pnt1_pnt2);
                double zVal = Interp(pnt4.Z, pnt3.Z, partialLen, dist_pnt1_pnt2);
                P[howManySegments2, i] = new Point3d(xVal, yVal, zVal);
            }

            // Fill the right column, from pnt2 down to pnt3, using midpoints2
            for (int j = 1; j < howManySegments2; j++)
            {
                P[j, howManySegments] = midpoints2[j - 1];
            }

            // Fill the left column, from pnt1 down to pnt4, by interpolation
            double dist_pnt1_pnt4 = Dist(pnt1, pnt4);
            double dist_pnt2_pnt3 = Dist(pnt2, pnt3); // might be used if needed

            for (int j = 1; j < howManySegments2; j++)
            {
                double partialLen = Dist(pnt2, P[j, howManySegments]);
                // Actually we might want partialLen from pnt1->pnt4 or pnt2->pnt3,
                // but let's follow the logic from Python code. They used Dist(pnt0[skolko], p[j][skolko]).
                double ratio = partialLen / dist_pnt2_pnt3;

                double xVal = Interp(pnt1.X, pnt4.X, partialLen, dist_pnt2_pnt3);
                double yVal = Interp(pnt1.Y, pnt4.Y, partialLen, dist_pnt2_pnt3);
                double zVal = Interp(pnt1.Z, pnt4.Z, partialLen, dist_pnt2_pnt3);
                P[j, 0] = new Point3d(xVal, yVal, zVal);
            }

            // Fill internal points with the intersection logic point_between_4_points
            for (int j = 1; j < howManySegments2; j++)
            {
                for (int i = 1; i < howManySegments; i++)
                {
                    // in the Python code: point_between_4_points(P[0][i],P[skolko2][i],P[j][0],P[j][skolko])
                    Point3d A = P[0, i];
                    Point3d B = P[howManySegments2, i];
                    Point3d C = P[j, 0];
                    Point3d D = P[j, howManySegments];
                    P[j, i] = PointBetween4Points(A, B, C, D);
                }
            }

            // Finally, create 3D faces for each cell
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                for (int j = 0; j < howManySegments2; j++)
                {
                    for (int i = 0; i < howManySegments; i++)
                    {
                        // corners in clockwise or ccw
                        Point3d p1 = P[j, i];
                        Point3d p2 = P[j, i + 1];
                        Point3d p3 = P[j + 1, i + 1];
                        Point3d p4 = P[j + 1, i];

                        Face face = new Face(p1, p2, p3, p4, true, true, true, true);
                        btr.AppendEntity(face);
                        tr.AddNewlyCreatedDBObject(face, true);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nGrid of 3D faces has been created successfully.");
        }
    }
}
