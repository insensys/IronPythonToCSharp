using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace EdgePoint01
{
    public class EdgePoints
    {
        // Атрибут указывает, что метод - это AutoCAD-команда
        [CommandMethod("CollectPoints")]
        public void CollectPointsMethod()
        {
            // Текущий документ и редактор
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Запрос выбора объектов
            PromptSelectionResult selRes = ed.GetSelection();
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            // Начинаем транзакцию
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                // Список для координат
                var pointList = new System.Collections.Generic.List<Point3d>();

                // Перебираем выбранные объекты
                foreach (SelectedObject selObj in selRes.Value)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // DBPoint
                    if (ent is DBPoint dbp)
                    {
                        pointList.Add(dbp.Position);
                    }
                    // Line
                    else if (ent is Line ln)
                    {
                        pointList.Add(ln.StartPoint);
                        pointList.Add(ln.EndPoint);
                    }
                    // 3D Face
                    else if (ent is Face face)
                    {
                        pointList.Add(face.GetVertexAt(0));
                        pointList.Add(face.GetVertexAt(1));
                        pointList.Add(face.GetVertexAt(2));
                        pointList.Add(face.GetVertexAt(3));
                    }
                }

                // Чтобы вставлять новые объекты, переводим ModelSpace в режим "ForWrite"
                BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Создаём в модели точки на основе собранных координат
                foreach (Point3d pt in pointList)
                {
                    DBPoint newPoint = new DBPoint(pt);
                    ms.AppendEntity(newPoint);
                    tr.AddNewlyCreatedDBObject(newPoint, true);
                }

                // Коммитим транзакцию
                tr.Commit();
            }

            ed.WriteMessage($"\nСоздано точек: {selRes.Value.Count} (DBPoint).");
        }
    }
}
