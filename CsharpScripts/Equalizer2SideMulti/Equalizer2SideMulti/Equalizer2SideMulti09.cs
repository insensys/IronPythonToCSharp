using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace Equalizer2SideMulti
{
    public class Equalizer2SideMulti09
    {
        // Вспомогательная функция для 3D-расстояния
        private double Dist(Point3d a, Point3d b)
        {
            return a.DistanceTo(b);
        }

        // Выбирает точки из общего списка 'allPoints', которые лежат на отрезке [p1, p2]
        // с учётом малой погрешности pushinka.
        private List<Point3d> SelectPointsBetweenTwoPoints(Point3d p1, Point3d p2, List<Point3d> allPoints, double pushinka = 0.001)
        {
            double L = Dist(p1, p2);
            var selected = new List<Point3d>();

            foreach (var p in allPoints)
            {
                double A = Dist(p1, p);
                double B = Dist(p, p2);
                double diff = L - A - B;
                if (Math.Abs(diff) < pushinka)
                    selected.Add(p);
            }

            // Дополнительно сортируем/убираем дубликаты для получения "промежуточных" точек
            selected.Sort((a, b) => Dist(p1, a).CompareTo(Dist(p1, b)));

            // Убираем точки, которые совпадают с самими p1, p2 (или близки) если нужно
            var result = new List<Point3d>();
            foreach (var pt in selected)
            {
                if (Dist(pt, p1) > pushinka && Dist(pt, p2) > pushinka)
                    result.Add(pt);
            }

            return result;
        }

        // Находит "точку пересечения" или аналогичную интерполяцию между четырьмя точками (A,B,C,D).
        // В оригинальном скрипте – уравнение плоскостей, но тут можно оставить упрощённую логику.
        private Point3d PointBetween4Points(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            // Ниже – простой линейный интерполятор. Можно заменить на решение системы, если нужно строго пересечение.
            // Для упрощения берём среднее между интерполяциями A–B и C–D.
            Point3d midAB = new Point3d((A.X + B.X) / 2.0, (A.Y + B.Y) / 2.0, (A.Z + B.Z) / 2.0);
            Point3d midCD = new Point3d((C.X + D.X) / 2.0, (C.Y + D.Y) / 2.0, (C.Z + D.Z) / 2.0);
            return new Point3d((midAB.X + midCD.X) / 2.0, (midAB.Y + midCD.Y) / 2.0, (midAB.Z + midCD.Z) / 2.0);
        }

        // Основной метод команды
        [CommandMethod("EqualizerGrid")]
        public void EqualizerGridCommand()
        {
            // Инициализация
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Список всех координат (точек), извлечённых из выделения
            List<Point3d> allPoints = new List<Point3d>();

            // 1) Просим пользователя выбрать объекты
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nВыберите объекты для извлечения точек:";
            PromptSelectionResult res = ed.GetSelection(opts);

            if (res.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            // 2) Заходим в транзакцию и вытаскиваем точки из точек/линий/3DFaces
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in res.Value)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is DBPoint dbPt)
                    {
                        allPoints.Add(dbPt.Position);
                    }
                    else if (ent is Line ln)
                    {
                        allPoints.Add(ln.StartPoint);
                        allPoints.Add(ln.EndPoint);
                    }
                    else if (ent is Face face3d)
                    {
                        // Face имеет 4 вершины
                        for (short i = 0; i < 4; i++)
                        {
                            var vertex = face3d.GetVertexAt(i);
                            allPoints.Add(vertex);
                        }
                    }
                    // Другие типы – по необходимости
                }

                tr.Commit();
            }

            // 3) Просим пользователя выбрать 4 ключевых точки
            Point3d pnt1 = GetPointFromUser(ed, "\nУкажите первую точку (pnt1): ");
            Point3d pnt2 = GetPointFromUser(ed, "\nУкажите вторую точку (pnt2): ");
            Point3d pnt3 = GetPointFromUser(ed, "\nУкажите третью точку (pnt3): ");
            Point3d pnt4 = GetPointFromUser(ed, "\nУкажите четвёртую точку (pnt4): ");

            // 4) Находим промежуточные точки на отрезках (pnt1–pnt2) и (pnt2–pnt3)
            var midpoints12 = SelectPointsBetweenTwoPoints(pnt1, pnt2, allPoints);
            var midpoints23 = SelectPointsBetweenTwoPoints(pnt2, pnt3, allPoints);

            // Получаем "сколько" сегментов
            int count1 = midpoints12.Count + 1;
            int count2 = midpoints23.Count + 1;

            // 5) Формируем "матрицу" P размера (count2+1) x (count1+1)
            //    P[j][i], где j идёт по одному направлению (pnt2->pnt3), i по другому (pnt1->pnt2)
            Point3d[,] P = new Point3d[count2 + 1, count1 + 1];

            // Заполнение крайних значений (углы)
            P[0, 0] = pnt1;                    // левый нижний
            P[0, count1] = pnt2;              // правый нижний
            P[count2, count1] = pnt3;         // правый верхний
            P[count2, 0] = pnt4;              // левый верхний

            // Заполняем промежуточные точки по нижней границе (pnt1 -> pnt2)
            for (int i = 1; i < count1; i++)
            {
                P[0, i] = midpoints12[i - 1];
            }

            // Аналогично по правой границе (pnt2 -> pnt3)
            for (int j = 1; j < count2; j++)
            {
                P[j, count1] = midpoints23[j - 1];
            }

            // Интерполяция по верхней (pnt4->pnt3) и левой (pnt1->pnt4) границам – упрощённо
            // Можно делать более точную интерполяцию, как в оригинале.
            for (int i = 1; i < count1; i++)
            {
                P[count2, i] = LinearInterpolate(P[count2, 0], P[count2, count1], i, count1);
            }
            for (int j = 1; j < count2; j++)
            {
                P[j, 0] = LinearInterpolate(P[0, 0], P[count2, 0], j, count2);
            }

            // 6) Заполняем внутренние точки (PointBetween4Points)
            for (int j = 1; j < count2; j++)
            {
                for (int i = 1; i < count1; i++)
                {
                    P[j, i] = PointBetween4Points(P[0, i],    // A
                                                   P[count2, i],  // B
                                                   P[j, 0],    // C
                                                   P[j, count1]); // D
                }
            }

            // 7) Строим 3D Face сетку
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Перебираем "ячейки" сетки (count1 x count2), для каждой создаём 3D Face
                for (int j = 0; j < count2; j++)
                {
                    for (int i = 0; i < count1; i++)
                    {
                        // Четыре вершины ячейки
                        Point3d pA = P[j, i];
                        Point3d pB = P[j, i + 1];
                        Point3d pC = P[j + 1, i + 1];
                        Point3d pD = P[j + 1, i];

                        Face face3d = new Face(pA, pB, pC, pD, true, true, true, true);
                        btr.AppendEntity(face3d);
                        tr.AddNewlyCreatedDBObject(face3d, true);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage($"\nГенерация сетки завершена. Всего ячеек: {count1 * count2}");
        }

        // Функция линейной интерполяции (1D) между двумя точками
        private Point3d LinearInterpolate(Point3d p1, Point3d p2, int step, int maxStep)
        {
            double t = (double)step / (double)maxStep;
            return new Point3d(
                p1.X + (p2.X - p1.X) * t,
                p1.Y + (p2.Y - p1.Y) * t,
                p1.Z + (p2.Z - p1.Z) * t
            );
        }

        // Упрощённая функция ввода точки пользователем
        private Point3d GetPointFromUser(Editor ed, string promptMsg)
        {
            PromptPointOptions ppo = new PromptPointOptions(promptMsg);
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status == PromptStatus.OK)
            {
                return ppr.Value;
            }
            // Если пользователь отменил ввод, вернём (0,0,0) или выбросим исключение
            return new Point3d(0, 0, 0);
        }
    }
}
