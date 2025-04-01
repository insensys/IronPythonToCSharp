using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace n_arc02
{
    public class n_arc02Command
    {
        [CommandMethod("ARCSUBDIV")]
        public static void CreateArcSubdivision()
        {
            // 1) Get the active document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 2) Prompt user for 3 points:
            PromptPointResult ppr1 = ed.GetPoint("\nSelect start point of arc: ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nSelect end point of arc: ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            PromptPointResult pprA = ed.GetPoint("\nSelect a point on the arc: ");
            if (pprA.Status != PromptStatus.OK) return;
            Point3d pntA = pprA.Value;

            // 3) Prompt for number of subdivisions:
            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nNumber of subdivisions: ")
            {
                AllowZero = false,
                AllowNegative = false,
                DefaultValue = 5
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;
            int subdivisions = intRes.Value;

            // Convert Points to double for simpler math
            double x1 = pnt1.X, y1 = pnt1.Y, z = pnt1.Z;
            double x2 = pnt2.X, y2 = pnt2.Y;
            double x3 = pntA.X, y3 = pntA.Y;

            // 4) Compute center (x0, y0) of circle through the 3 points
            //    This uses the same algebraic approach as in the Python script
            double denom = (x1 * y2 - x1 * y3 - y1 * x2 + y1 * x3 - x3 * y2 + y3 * x2) * 2.0;
            if (Math.Abs(denom) < 1e-9)
            {
                ed.WriteMessage("\nCould not compute arc center (points may be collinear).");
                return;
            }

            double x0 = (
                 -y1 * (x2 * x2)
               + y3 * (x2 * x2)
               - (y2 * y2) * y1
               + y2 * (y1 * y1)
               + y1 * (x3 * x3)
               + (y3 * y3) * y1
               - (x1 * x1) * y3
               + (x1 * x1) * y2
               - y3 * (y1 * y1)
               + (y2 * y2) * y3
               - (x3 * x3) * y2
               - y2 * (y3 * y3)
            ) / denom;

            double y0 = -(
                 (x1 * x1) * x2
               - (x1 * x1) * x3
               - x1 * (x2 * x2)
               - x1 * (y2 * y2)
               + x1 * (x3 * x3)
               + x1 * (y3 * y3)
               + (y1 * y1) * x2
               - (y1 * y1) * x3
               - (x3 * x3) * x2
               + x3 * (x2 * x2)
               + x3 * (y2 * y2)
               - (y3 * y3) * x2
            ) / denom;

            // We assume the Z remains the same as the original points
            double cx = x0;
            double cy = y0;
            double cz = z;

            // 5) Compute radius:
            double dx = x1 - cx;
            double dy = y1 - cy;
            double radius = Math.Sqrt(dx * dx + dy * dy);

            // 6) We'll compute angles for start & end points around the center
            //    Then subdivide the arc by angle
            //    We’ll do a function that returns the angle for a point wrt the center
            Func<double, double, double, double, double> angleWRT = (px, py, cx0, cy0) =>
            {
                double ax = px - cx0;
                double ay = py - cy0;
                double ang = Math.Atan2(ay, ax);
                return ang;
            };

            double alpha = angleWRT(x1, y1, cx, cy);
            double beta = angleWRT(x2, y2, cx, cy);

            // Because arcs can wrap more than 180°, let's ensure we pick
            // the smaller arc or do something consistent with the Python script.
            // In the Python code, we didn't specifically handle arcs bigger vs smaller.
            // We'll assume we want the direct arc from alpha to beta.
            // If the direction is wrong, consider adjusting, e.g. if (beta < alpha) beta += 2π, etc.

            // Attempt a consistent direction by checking which side pntA is on:
            // We'll do a quick check with cross product approach:
            double angleA = angleWRT(x3, y3, cx, cy);

            // We'll unify angles so alpha < beta by adding 2π if needed
            while (beta < alpha) beta += 2.0 * Math.PI;
            // Check if angleA is outside or inside:
            // The python script doesn't do advanced checks; it just used the direct angle difference.

            // 7) Add objects (circle + subdiv points) in a transaction
            Database db = doc.Database;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null)
                {
                    ed.WriteMessage("\nBlock table not found.");
                    return;
                }

                BlockTableRecord btrMs = acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (btrMs == null)
                {
                    ed.WriteMessage("\nModelSpace not found.");
                    return;
                }

                // (Optional) Create a circle
                using (Circle c = new Circle(new Point3d(cx, cy, cz), Vector3d.ZAxis, radius))
                {
                    btrMs.AppendEntity(c);
                    acTrans.AddNewlyCreatedDBObject(c, true);
                }

                // Subdivide the arc by angle
                double totalAngle = beta - alpha;
                double step = totalAngle / subdivisions;

                for (int i = 0; i <= subdivisions; i++)
                {
                    double currentAngle = alpha + i * step;
                    double px = cx + radius * Math.Cos(currentAngle);
                    double py = cy + radius * Math.Sin(currentAngle);

                    // Create a DBPoint
                    using (DBPoint dbPt = new DBPoint(new Point3d(px, py, cz)))
                    {
                        btrMs.AppendEntity(dbPt);
                        acTrans.AddNewlyCreatedDBObject(dbPt, true);
                    }

                    // Create a simple DBText label
                    // For clarity, we’ll label i (the index)
                    using (DBText txt = new DBText())
                    {
                        txt.Position = new Point3d(px, py, cz);
                        txt.Height = 2.5; // choose a suitable text height
                        txt.TextString = i.ToString();
                        txt.ColorIndex = 1; // red, optional
                        btrMs.AppendEntity(txt);
                        acTrans.AddNewlyCreatedDBObject(txt, true);
                    }
                }

                acTrans.Commit();
            }

            ed.WriteMessage("\nArc subdivision completed.");
        }
    }
}
