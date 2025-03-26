using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace equalizermult04
{
    public class equalizermult04Command
    {
        // Небольшая погрешность для сравнения расстояний
        private const double epsilon = 0.001;

        // Функция расстояния между двумя точками
        private double Dist(Point3d a, Point3d b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Поиск промежуточных точек между pnt1 и pnt2 из общего списка
        private List<Point3d> SelectPointsBetween2Points(
            Point3d pnt1,
            Point3d pnt2,
            List<Point3d> allPoints)
        {
            double baseLen = Dist(pnt1, pnt2);
            List<Point3d> selected = new List<Point3d>();

            foreach (Point3d p in allPoints)
            {
                // Проверяем, лежит ли точка p почти на отрезке [pnt1, pnt2]
                double distA = Dist(pnt1, p);
                double distB = Dist(p, pnt2);
                double diff = baseLen - distA - distB;

                if (Math.Abs(diff) < epsilon)
                {
                    selected.Add(p);
                }
            }

            // Сортируем точки по расстоянию от pnt1
            selected.Sort((p1, p2) => Dist(pnt1, p1).CompareTo(Dist(pnt1, p2)));

            // Убираем слишком близкие дубликаты
            List<Point3d> unique = new List<Point3d>();
            foreach (Point3d p in selected)
            {
                if (unique.Count == 0 ||
                    Dist(p, unique[unique.Count - 1]) > epsilon)
                {
                    unique.Add(p);
                }
            }

            return unique;
        }

        // Простая линейная интерполяция
        private double Interp(double n1, double n2, double k, double total)
        {
            // (n2 - n1) * (k / total) + n1
            return (n2 - n1) * (k / total) + n1;
        }

        [CommandMethod("EQUALIZERMULT04")]
        public void RunCommand()
        {
            // Текущий документ и редактор
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Список всех собранных точек
            List<Point3d> allPoints = new List<Point3d>();

            // 1) Выбор объектов, сбор точек
            PromptSelectionOptions selOpts = new PromptSelectionOptions();
            selOpts.MessageForAdding = "\nВыберите объекты (DBPoint, Line, Face)...";

            PromptSelectionResult selRes = ed.GetSelection(selOpts);
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                SelectionSet ss = selRes.Value;
                foreach (SelectedObject selObj in ss)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Если это точка
                    if (ent is DBPoint dbp)
                    {
                        allPoints.Add(dbp.Position);
                    }
                    // Если это линия
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }
                    // Если это 3D Face
                    else if (ent is Face fc)
                    {
                        allPoints.Add(fc.GetVertexAt(0));
                        allPoints.Add(fc.GetVertexAt(1));
                        allPoints.Add(fc.GetVertexAt(2));
                        allPoints.Add(fc.GetVertexAt(3));
                    }
                }
                tr.Commit();
            }

            // 2) Просим пользователя указать первую и вторую точку (pnt1, pnt2)
            PromptPointResult ppr1 = ed.GetPoint("\nУкажите первую точку (pnt1): ");
            if (ppr1.Status != PromptStatus.OK) return;
            Point3d pnt1 = ppr1.Value;

            PromptPointResult ppr2 = ed.GetPoint("\nУкажите вторую точку (pnt2): ");
            if (ppr2.Status != PromptStatus.OK) return;
            Point3d pnt2 = ppr2.Value;

            // Находим промежуточные точки между pnt1 и pnt2
            List<Point3d> midpoints = SelectPointsBetween2Points(pnt1, pnt2, allPoints);

            // Количество «сегментов» на этом отрезке в итоге будет midpoints.Count+1
            int kancha = midpoints.Count + 1;
            ed.WriteMessage($"\nМежду pnt1 и pnt2 получено {midpoints.Count} промежуточных точек.");

            // 3) Третья точка (pnt3) и количество разбиений
            PromptPointResult ppr3 = ed.GetPoint("\nУкажите третью точку (pnt3): ");
            if (ppr3.Status != PromptStatus.OK) return;
            Point3d pnt3 = ppr3.Value;

            PromptIntegerResult pir = ed.GetInteger("\nСколько раз поделить отрезок pnt2–pnt3?: ");
            if (pir.Status != PromptStatus.OK) return;
            int kancha2 = pir.Value;

            // 4) Четвёртая точка (pnt4)
            PromptPointResult ppr4 = ed.GetPoint("\nУкажите четвёртую точку (pnt4): ");
            if (ppr4.Status != PromptStatus.OK) return;
            Point3d pnt4 = ppr4.Value;

            // 5) Создаём двумерный массив P (размер: (kancha2+1) x (kancha+1))
            //    в котором будем хранить координаты новых точек
            Point3d[,] P = new Point3d[kancha2 + 1, kancha + 1];

            // Заполняем верхнюю строку (j=0)
            // Первая строка идёт по отрезку pnt1->pnt2, включая midpoints
            P[0, 0] = pnt1;
            P[0, kancha - 1] = (midpoints.Count > 0) ? midpoints[midpoints.Count - 1] : pnt1;
            // Но аккуратнее — чуть ниже мы их переопределим. Проще задать циклом:

            // Запишем первую строку, используя midpoints
            // pnt1 -> midpoints -> pnt2
            // midpoints упорядочены, потому в ячейках 1..midpoints.Count будут промежуточные точки
            if (midpoints.Count > 0)
            {
                for (int i = 0; i < midpoints.Count; i++)
                {
                    P[0, i + 1] = midpoints[i];
                }
            }
            // Последняя колонка (pnt2)
            P[0, kancha - 1] = pnt2;

            // Заполняем нижнюю строку (j=kancha2) => pnt4->pnt3 (или наоборот, в зависимости от логики)
            // В оригинальном скрипте pnt4 [kancha2,0], pnt3 [kancha2,kancha]
            P[kancha2, 0] = pnt4;
            P[kancha2, kancha - 1] = pnt3;

            // Линейная интерполяция по горизонтали
            for (int col = 1; col < kancha - 1; col++)
            {
                // col меняется от 1 до (kancha-2)
                // P[kancha2, col] = линейная интерполяция между pnt4 и pnt3
                double t = (double)col / (double)(kancha - 1);
                double newX = Interp(pnt4.X, pnt3.X, col, kancha - 1);
                double newY = Interp(pnt4.Y, pnt3.Y, col, kancha - 1);
                double newZ = Interp(pnt4.Z, pnt3.Z, col, kancha - 1);

                P[kancha2, col] = new Point3d(newX, newY, newZ);
            }

            // Теперь вертикальная интерполяция между верхней и нижней строкой
            for (int col = 0; col < kancha; col++)
            {
                Point3d topPt = P[0, col];
                Point3d botPt = P[kancha2, col];
                for (int row = 1; row < kancha2; row++)
                {
                    double frac = (double)row / (double)kancha2;
                    double newX = Interp(topPt.X, botPt.X, row, kancha2);
                    double newY = Interp(topPt.Y, botPt.Y, row, kancha2);
                    double newZ = Interp(topPt.Z, botPt.Z, row, kancha2);
                    P[row, col] = new Point3d(newX, newY, newZ);
                }
            }

            // 6) Создаём 3D Face на каждой «ячейке» сетки
            //    идём по строкам 0..(kancha2-1) и столбцам 0..(kancha-1)
            using (Transaction trFaces = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trFaces.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trFaces.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                for (int row = 0; row < kancha2; row++)
                {
                    for (int col = 0; col < kancha - 1; col++)
                    {
                        Point3d p1 = P[row, col];
                        Point3d p2 = P[row, col + 1];
                        Point3d p3 = P[row + 1, col + 1];
                        Point3d p4 = P[row + 1, col];

                        Face face = new Face(p1, p2, p3, p4, true, true, true, true);
                        btr.AppendEntity(face);
                        trFaces.AddNewlyCreatedDBObject(face, true);
                    }
                }
                trFaces.Commit();
            }

            ed.WriteMessage("\nГотово! Созданы 3D-лица по сетке.");
        }
    }
}
