using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

//[assembly: CommandClass(typeof(invisible01Command))]

public class invisible01tsCommand
{
    [CommandMethod("HIDE_OBJECTS")] // The command name to type in AutoCAD
    public void HideSelectedObjects()
    {
        // Get the active document, editor, and database
        Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        Editor ed = doc.Editor;
        Database db = doc.Database;

        // Set up selection options
        PromptSelectionOptions opts = new PromptSelectionOptions
        {
            MessageForAdding = "\nSelect objects to hide: "
        };

        // Prompt the user to select objects
        PromptSelectionResult psr = ed.GetSelection(opts);
        if (psr.Status == PromptStatus.OK)
        {
            // Begin a transaction to modify entities
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                SelectionSet ss = psr.Value;
                foreach (SelectedObject selObj in ss)
                {
                    if (selObj == null) continue;

                    // Get the entity for write
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        // If it is a DBPoint, Line, or Face, set it invisible
                        if (ent is DBPoint || ent is Line || ent is Face)
                        {
                            ent.Visible = false;
                        }
                    }
                }
                // Commit changes
                tr.Commit();
            }

            ed.WriteMessage("\nSelected DBPoint/Line/Face objects are now invisible.");
        }
        else
        {
            ed.WriteMessage("\nNo objects selected.");
        }
    }
}
