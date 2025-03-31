using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace pointin4
{
    public class pointin4Command
    {
        // Атрибут CommandMethod сообщает AutoCAD, что это команда,
        // которую можно вызвать в командной строке после NETLOAD.
        [CommandMethod("PointBetween4")]
        public static void PointBetweenFour()
        {
            // 1. Получаем доступ к текущему документу и редактору.
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 2. Последовательно запрашиваем 4 точки у пользователя.
            var ppo = new PromptPointOptions("\nВыберите первую точку:");
            var pprA = ed.GetPoint(ppo);
            if (pprA.Status != PromptStatus.OK) return;
            Point3d A = pprA.Value;

            ppo.Message = "\nВыберите вторую точку:";
            var pprB = ed.GetPoint(ppo);
            if (pprB.Status != PromptStatus.OK) return;
            Point3d B = pprB.Value;

            ppo.Message = "\nВыберите третью точку:";
            var pprC = ed.GetPoint(ppo);
            if (pprC.Status != PromptStatus.OK) return;
            Point3d C = pprC.Value;

            ppo.Message = "\nВыберите четвертую точку:";
            var pprD = ed.GetPoint(ppo);
            if (pprD.Status != PromptStatus.OK) return;
            Point3d D = pprD.Value;

            // 3. Выполняем некий расчет. Ниже — условный пример,
            // повторяющий логику "point_between_4_points".
            // Реальную формулу подставьте свою.
            Point3d result = ComputeIntersection(A, B, C, D);

            // 4. Добавляем вычисленную точку в модель.
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                using (DBPoint dbPoint = new DBPoint(result))
                {
                    btr.AppendEntity(dbPoint);
                    tr.AddNewlyCreatedDBObject(dbPoint, true);
                }

                tr.Commit();
            }

            ed.WriteMessage($"\nНовая точка добавлена по координатам: X={result.X:F2}, Y={result.Y:F2}, Z={result.Z:F2}");
        }

        // Пример функции, повторяющей логику 'point_between_4_points'
        private static Point3d ComputeIntersection(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            // Ниже — пример, как можно внедрить ту самую формулу из Python,
            // если она нужна. Или же замените своим расчетом.
            // Здесь, для примера, просто берем среднее 4х точек:
            double x = (A.X + B.X + C.X + D.X) / 4.0;
            double y = (A.Y + B.Y + C.Y + D.Y) / 4.0;
            double z = (A.Z + B.Z + C.Z + D.Z) / 4.0;
            return new Point3d(x, y, z);
        }
    }
}
