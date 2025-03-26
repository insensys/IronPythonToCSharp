using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace more01
{
    // Класс с командами (CommandMethod) для AutoCAD
    public class more01Command
    {
        // Небольшая "погрешность" для сравнения, как pushinka = 0.001
        private const double EPSILON = 0.001;

        // Аналог dist(a, b) из Python: вычисляем расстояние между двумя точками
        private static double Dist(Point3d p1, Point3d p2)
        {
            return p1.DistanceTo(p2);
        }

        // Аналог select_points_between_2_points(pnt1, pnt2)
        // Фильтрует список `allPoints`, оставляя те, что лежат строго на отрезке [pnt1, pnt2]
        // с учётом небольшой погрешности.
        private static List<Point3d> SelectPointsBetweenTwoPoints(Point3d pnt1, Point3d pnt2, List<Point3d> allPoints)
        {
            double length = Dist(pnt1, pnt2);
            var result = new List<Point3d>();

            // Проверяем, лежит ли каждая точка на отрезке (pnt1, pnt2)
            foreach (var p in allPoints)
            {
                double d1 = Dist(pnt1, p);
                double d2 = Dist(p, pnt2);
                // Если сумма расстояний до концов ≈ длине исходного отрезка – точка лежит на отрезке
                double diff = length - (d1 + d2);
                if (Math.Abs(diff) < EPSILON)
                {
                    result.Add(p);
                }
            }

            // Чтобы избежать повторов и расположить "промежуточные" точки по возрастанию расстояния
            // можно отсортировать по d1:
            result = result
                .Where(pt => Dist(pt, pnt1) > EPSILON && Dist(pt, pnt2) > EPSILON)
                .OrderBy(pt => Dist(pnt1, pt))
                .ToList();

            return result;
        }

        // Аналог point_between_4_points(A,B,C,D) (примерно)
        // В Python он вычисляет некую пересекающую точку.
        // Здесь – условно возвращаем координату пересечения плоскостей и т. п.
        private static Point3d PointBetween4Points(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            // В Python-скрипте был громоздкий формулами подход.
            // Здесь для упрощения оставим "заглушку" – в реальном коде надо повторить логику вычислений.
            // Возвращаем просто центр масс 4 точек, как пример.
            double x = (A.X + B.X + C.X + D.X) / 4.0;
            double y = (A.Y + B.Y + C.Y + D.Y) / 4.0;
            double z = (A.Z + B.Z + C.Z + D.Z) / 4.0;
            return new Point3d(x, y, z);
        }

        [CommandMethod("more91Command")]
        public void MyPythonScriptEquivalent()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. Предложим пользователю выбрать объекты (точки, линии, 3DFace и т. д.)
            var selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите объекты (точки, линии, 3DFace)..."
            };
            var selRes = ed.GetSelection(selOpts);
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано. Отмена.");
                return;
            }

            var selectedIds = selRes.Value.GetObjectIds();

            // Соберём все координаты из выбранных объектов
            var allPoints = new List<Point3d>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selectedIds)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is DBPoint dbp)
                    {
                        allPoints.Add(dbp.Position);
                    }
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }
                    else if (ent is Face face)
                    {
                        // 4 вершины
                        allPoints.Add(face.GetVertexAt(0));
                        allPoints.Add(face.GetVertexAt(1));
                        allPoints.Add(face.GetVertexAt(2));
                        allPoints.Add(face.GetVertexAt(3));
                    }
                    // Можно расширить для других типов объектов
                }
                tr.Commit();
            }

            // 2. Спросим у пользователя pnt1 и pnt2
            var p1res = ed.GetPoint("\nУкажите первую точку (pnt1): ");
            if (p1res.Status != PromptStatus.OK) return;
            Point3d pnt1 = p1res.Value;

            var p2res = ed.GetPoint("\nУкажите вторую точку (pnt2): ");
            if (p2res.Status != PromptStatus.OK) return;
            Point3d pnt2 = p2res.Value;

            // Находим промежуточные точки между pnt1 и pnt2
            var midpoints = SelectPointsBetweenTwoPoints(pnt1, pnt2, allPoints);

            ed.WriteMessage($"\nНайдено промежуточных точек между pnt1 и pnt2: {midpoints.Count}");

            // 3. Создаём (при желании) линии или 3DFace между этими точками
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Для примера: проведём линию от pnt1 до первой промежуточной и т. д.
                Point3d prev = pnt1;
                foreach (var mp in midpoints)
                {
                    Line lineObj = new Line(prev, mp);
                    btr.AppendEntity(lineObj);
                    tr.AddNewlyCreatedDBObject(lineObj, true);
                    prev = mp;
                }
                // И ещё одна линия – до pnt2
                Line lastLine = new Line(prev, pnt2);
                btr.AppendEntity(lastLine);
                tr.AddNewlyCreatedDBObject(lastLine, true);

                tr.Commit();
            }

            // 4. Аналогично считываем pnt3 и pnt4 и строим 3DFace
            var p3res = ed.GetPoint("\nУкажите третью точку (pnt3): ");
            if (p3res.Status != PromptStatus.OK) return;
            Point3d pnt3 = p3res.Value;

            var p4res = ed.GetPoint("\nУкажите четвёртую точку (pnt4): ");
            if (p4res.Status != PromptStatus.OK) return;
            Point3d pnt4 = p4res.Value;

            // Для примера найдём некую промежуточную точку (аналог point_between_4_points)
            Point3d mid4 = PointBetween4Points(pnt1, pnt2, pnt3, pnt4);

            // Создаём 3DFace
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Простой пример 3DFace по 4-м точкам
                Face faceObj = new Face(pnt1, pnt2, pnt3, pnt4, true, true, true, true);
                btr.AppendEntity(faceObj);
                tr.AddNewlyCreatedDBObject(faceObj, true);

                // Или что-то более хитрое, если вам нужно mid4
                // ...

                tr.Commit();
            }

            ed.WriteMessage("\nСкрипт завершён. Новые объекты созданы.");
        }
    }
}
