using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace C01opora
{
    public class C01oporaCommand
    {
        // A small tolerance for matching the Z-level
        private const double Pushinka = 0.01;

        [CommandMethod("C01")]
        public void MarkPointsNearZ()
        {
            // Get the active document, database, and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt the user to select objects
            PromptSelectionOptions selOpts = new PromptSelectionOptions();
            selOpts.MessageForAdding = "\nSelect objects:";
            PromptSelectionResult selRes = ed.GetSelection(selOpts);

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo objects selected or selection canceled.");
                return;
            }

            // Collect geometry points from the selection
            List<Point3d> allPoints = new List<Point3d>();

            // Start a transaction to read geometry
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in selRes.Value)
                {
                    if (selObj == null) continue;

                    Entity ent = trans.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Collect line endpoints
                    if (ent is Line line)
                    {
                        allPoints.Add(line.StartPoint);
                        allPoints.Add(line.EndPoint);
                    }
                    // Collect face vertices
                    else if (ent is Face face)
                    {
                        // Each face has four corners
                        for (int i = 0; i < 4; i++)
                        {
                            allPoints.Add(face.GetVertexAt((short)i));
                        }
                    }
                }

                trans.Commit();
            }

            // Prompt the user for a 3D point (reference point)
            PromptPointOptions ppo = new PromptPointOptions("\nPick a reference point:");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nNo reference point selected.");
                return;
            }

            // Get the reference point's Z
            double refZ = ppr.Value.Z;

            // Now, create new points in the drawing for each coordinate 
            // near the chosen Z-level within the Pushinka tolerance
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Point3d pt in allPoints)
                {
                    if (Math.Abs(pt.Z - refZ) < Pushinka)
                    {
                        // Create a DBPoint at this coordinate
                        DBPoint dbpt = new DBPoint(pt);
                        btr.AppendEntity(dbpt);
                        trans.AddNewlyCreatedDBObject(dbpt, true);
                    }
                }

                trans.Commit();
            }

            ed.WriteMessage("\nPoints added near the selected Z-level.");
        }
    }
}
