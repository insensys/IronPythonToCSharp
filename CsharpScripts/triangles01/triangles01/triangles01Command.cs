using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

public class triangles01Command
{
    [CommandMethod("AddTriangle")]
    public void AddTriangle()
    {
        Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        Editor ed = doc.Editor;

        // Prompt for three points
        PromptPointResult pprA = ed.GetPoint("\nSpecify point A: ");
        if (pprA.Status != PromptStatus.OK) return;

        PromptPointResult pprB = ed.GetPoint("\nSpecify point B: ");
        if (pprB.Status != PromptStatus.OK) return;

        PromptPointResult pprC = ed.GetPoint("\nSpecify point C: ");
        if (pprC.Status != PromptStatus.OK) return;

        Point3d A = pprA.Value;
        Point3d B = pprB.Value;
        Point3d C = pprC.Value;

        using (Transaction tr = doc.TransactionManager.StartTransaction())
        {
            BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            // Create a triangular 3D face
            Face face = new Face(
                A, // Corner 1
                B, // Corner 2
                C, // Corner 3
                A, // Corner 4 (repeat corner 1 for a triangle)
                true,  // edge 0 is visible
                true,  // edge 1 is visible
                true,  // edge 2 is visible
                true   // edge 3 is visible
            );

            // Add the 3D face to Model Space
            btr.AppendEntity(face);
            tr.AddNewlyCreatedDBObject(face, true);

            tr.Commit();
        }
    
    }
}