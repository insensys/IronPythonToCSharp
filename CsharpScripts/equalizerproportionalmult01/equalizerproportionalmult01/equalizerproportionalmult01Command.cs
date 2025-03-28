using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

public class equalizerproportionalmult01Command
{
    private const double Tolerance = 0.001;

    // Distance method
    private static double Distance(Point3d a, Point3d b)
    {
        return Math.Sqrt(
            Math.Pow(b.X - a.X, 2) +
            Math.Pow(b.Y - a.Y, 2) +
            Math.Pow(b.Z - a.Z, 2));
    }

    // Find points strictly between pnt1 and pnt2 from a given set
    private static List<Point3d> SelectPointsBetween(
        Point3d pnt1,
        Point3d pnt2,
        List<Point3d> allPoints)
    {
        double lineLength = Distance(pnt1, pnt2);
        var selected = new List<(Point3d, double)>();

        // Select only those that lie on the line segment from pnt1 to pnt2
        foreach (Point3d pt in allPoints)
        {
            double distA = Distance(pnt1, pt);
            double distB = Distance(pt, pnt2);
            double sum = distA + distB;

            // If sum ~ lineLength, the point is on the segment
            if (Math.Abs(sum - lineLength) < Tolerance)
            {
                // Exclude the endpoints themselves
                if (distA > Tolerance && distB > Tolerance)
                {
                    selected.Add((pt, distA));
                }
            }
        }

        // Sort by distance from pnt1
        selected.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        // Return only the ordered points
        var result = new List<Point3d>();
        foreach (var (pt, _) in selected)
            result.Add(pt);

        return result;
    }

    // Interpolate a value given fraction start->end
    private static double Interpolate(double start, double end, double fraction)
    {
        return start + fraction * (end - start);
    }

    // 3D interpolation
    private static Point3d InterpolatePoint(Point3d pA, Point3d pB, double fraction)
    {
        double x = Interpolate(pA.X, pB.X, fraction);
        double y = Interpolate(pA.Y, pB.Y, fraction);
        double z = Interpolate(pA.Z, pB.Z, fraction);
        return new Point3d(x, y, z);
    }

    // Example command method
    [CommandMethod("EqualizerProportionalMult")]
    public void RunEqualizerProportionalMult()
    {
        Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        Editor ed = doc.Editor;

        using (Transaction tr = doc.TransactionManager.StartTransaction())
        {
            // 1) Prompt user to select objects
            var opts = new PromptSelectionOptions()
            {
                MessageForAdding = "\nSelect points, lines, or 3D faces: "
            };
            PromptSelectionResult psr = ed.GetSelection(opts);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNothing selected.");
                return;
            }

            // Collect points from selected objects
            List<Point3d> allPoints = new List<Point3d>();

            // We'll look inside the selection for DBPoint, Line, or Face geometry
            foreach (SelectedObject so in psr.Value)
            {
                if (so == null) continue;
                Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                switch (ent)
                {
                    case DBPoint dbPt:
                        allPoints.Add(dbPt.Position);
                        break;
                    case Line ln:
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                        break;
                    case Face faceEnt:
                        // Each face has 4 vertices
                        allPoints.Add(faceEnt.GetVertexAt(0));
                        allPoints.Add(faceEnt.GetVertexAt(1));
                        allPoints.Add(faceEnt.GetVertexAt(2));
                        allPoints.Add(faceEnt.GetVertexAt(3));
                        break;
                }
            }

            // 2) Ask user for two pairs of points (pnt1,pnt2) and (pnt3,pnt4)
            //    For simplicity, we'll request just two pairs. 
            PromptPointResult ppr1 = ed.GetPoint("\nFirst point: ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nSecond point: ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            // We'll do the same for the other two points
            PromptPointResult ppr3 = ed.GetPoint("\nThird point: ");
            if (ppr3.Status != PromptStatus.OK) return;
            Point3d pnt3 = ppr3.Value;

            PromptPointResult ppr4 = ed.GetPoint("\nFourth point: ");
            if (ppr4.Status != PromptStatus.OK) return;
            Point3d pnt4 = ppr4.Value;

            // 3) From the selected geometry, find points that lie between pnt1/pnt2
            var midpoints1 = SelectPointsBetween(pnt1, pnt2, allPoints);
            // Similarly, points between pnt2/pnt3
            var midpoints2 = SelectPointsBetween(pnt2, pnt3, allPoints);

            // We'll form a grid-like matrix based on those segments
            int count1 = midpoints1.Count + 2; // including endpoints
            int count2 = midpoints2.Count + 2;

            // Prepare a 2D array of points
            Point3d[,] matrix = new Point3d[count2, count1];

            // Let's set the four corners in the "matrix"
            // We'll treat the top row as pnt1->pnt2 and the bottom row as pnt4->pnt3
            // Then fill in between
            matrix[0, 0] = pnt1;
            matrix[0, count1 - 1] = pnt2;
            matrix[count2 - 1, 0] = pnt4;
            matrix[count2 - 1, count1 - 1] = pnt3;

            // Fill the top row from pnt1->pnt2 using the midpoints
            for (int i = 1; i < count1 - 1; i++)
            {
                matrix[0, i] = midpoints1[i - 1];
            }

            // Fill the right column from pnt2->pnt3 using midpoints2
            for (int j = 1; j < count2 - 1; j++)
            {
                matrix[j, count1 - 1] = midpoints2[j - 1];
            }

            // Now we interpolate the left column pnt1->pnt4 and the bottom row pnt4->pnt3
            // We'll do linear interpolation (like the Python code)
            // Distances for top row vs bottom row might differ, but we keep things conceptual
            double length12 = Distance(pnt1, pnt2);
            double length43 = Distance(pnt4, pnt3);

            // Fill left column by interpolation
            for (int j = 1; j < count2 - 1; j++)
            {
                double fraction = (double)j / (count2 - 1);
                matrix[j, 0] = InterpolatePoint(pnt1, pnt4, fraction);
            }

            // Fill bottom row by interpolation
            for (int i = 1; i < count1 - 1; i++)
            {
                double fraction = (double)i / (count1 - 1);
                matrix[count2 - 1, i] = InterpolatePoint(pnt4, pnt3, fraction);
            }

            // Finally, fill the interior by bilinear approximation
            // For each column we have top->bottom edges, for each row left->right edges
            for (int j = 1; j < count2 - 1; j++)
            {
                for (int i = 1; i < count1 - 1; i++)
                {
                    // We have corners matrix[0, i] & matrix[count2-1, i]
                    // and matrix[j, 0], matrix[j, count1-1]
                    // Do a bilinear approach or a simpler approach:

                    Point3d topPt = matrix[0, i];
                    Point3d bottomPt = matrix[count2 - 1, i];
                    Point3d leftPt = matrix[j, 0];
                    Point3d rightPt = matrix[j, count1 - 1];

                    // We'll do the same approach as the python "point_between_4_points" logic
                    // For simplicity, let's do linear interpolation by row
                    double fracRow = (double)j / (count2 - 1);
                    Point3d verticalA = InterpolatePoint(topPt, bottomPt, fracRow);

                    double fracCol = (double)i / (count1 - 1);
                    Point3d verticalB = InterpolatePoint(leftPt, rightPt, fracCol);

                    // average them or do a full bilinear approach
                    // here we'll just do a midpoint of verticalA & verticalB
                    double x = (verticalA.X + verticalB.X) / 2.0;
                    double y = (verticalA.Y + verticalB.Y) / 2.0;
                    double z = (verticalA.Z + verticalB.Z) / 2.0;

                    matrix[j, i] = new Point3d(x, y, z);
                }
            }

            // 4) Create 3D faces in the AutoCAD drawing
            // We'll create them cell-by-cell from the matrix
            BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

            for (int j = 0; j < count2 - 1; j++)
            {
                for (int i = 0; i < count1 - 1; i++)
                {
                    // We have corners: 
                    // P1 = matrix[j, i]
                    // P2 = matrix[j, i+1]
                    // P3 = matrix[j+1, i+1]
                    // P4 = matrix[j+1, i]
                    Point3d p1 = matrix[j, i];
                    Point3d p2 = matrix[j, i + 1];
                    Point3d p3 = matrix[j + 1, i + 1];
                    Point3d p4 = matrix[j + 1, i];

                    using (Face face = new Face(
                        p1, p2, p3, p4, true, true, true, true))
                    {
                        btr.AppendEntity(face);
                        tr.AddNewlyCreatedDBObject(face, true);
                    }
                }
            }

            tr.Commit();
        }

        ed.WriteMessage("\nMesh creation completed!");
    }
}
