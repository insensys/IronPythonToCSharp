using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace LayerColorizer
{
    public class LayerColorizer
    {
        [CommandMethod("ColorArmLayers")]
        public void ColorArmLayers()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<(LayerTableRecord Layer, int Diameter)> selected = new List<(LayerTableRecord, int)>();
            List<int> diameters = new List<int>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId lid in lt)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lid, OpenMode.ForRead);
                    string name = ltr.Name;
                    ed.WriteMessage($"\nLayer: {name}");

                    string[] parts = name.Split('_');
                    if (parts.Length == 3 && parts[0].Length == 3 && int.TryParse(parts[1], out int dia))
                    {
                        selected.Add((ltr, dia));
                        diameters.Add(dia);
                    }
                }

                diameters.Sort();
                int colorIndex = 6;

                foreach (int dia in diameters)
                {
                    foreach (var item in selected)
                    {
                        if (item.Diameter == dia)
                        {
                            ed.WriteMessage($"\nAssigning color to {item.Layer.Name} (D={dia})");

                            if (colorIndex > 0)
                            {
                                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(item.Layer.ObjectId, OpenMode.ForWrite);
                                ltr.Color = Color.FromRgb(255, 0, 0); // красный
                                colorIndex--;
                            }
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nГотово: слои обновлены.");
        }
    }


}
