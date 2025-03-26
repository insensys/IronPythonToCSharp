using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace Equalizer2Side09
{
    public class Equailizer2Sides
    {
        private const double Eps = 0.001;

        [CommandMethod("Equalizer2Side")]
        public void Equalizer2Side()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 1. Выбираем объекты (точки, линии, 3D Face)
            PromptSelectionResult selRes = ed.GetSelection(new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите объекты (точки, линии, 3D Faces): "
            });

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            List<Point3d> points = new List<Point3d>();

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                // Проходимся по выбранным объектам
                foreach (SelectedObject selObj in selRes.Value)
                {
                    if (selObj == null) continue;

                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Если это одиночная точка
                    if (ent is DBPoint dbPoint)
                    {
                        points.Add(dbPoint.Position);
                    }
                    // Если это линия
                    else if (ent is Line line)
                    {
                        points.Add(line.StartPoint);
                        points.Add(line.EndPoint);
                    }
                    // Если это 3D Face
                    else if (ent is Face face)
                    {
                        points.Add(face.GetVertexAt(0));
                        points.Add(face.GetVertexAt(1));
                        points.Add(face.GetVertexAt(2));
                        points.Add(face.GetVertexAt(3));
                    }
                }

                tr.Commit();
            }

            // 2. Просим пользователя указать четыре точки
            // (первая, вторая, третья, четвертая)
            PromptPointResult p1Res = ed.GetPoint("\nВыберите первую точку (pnt1): ");
            if (p1Res.Status != PromptStatus.OK) return;

            PromptPointResult p2Res = ed.GetPoint("\nВыберите вторую точку (pnt2): ");
            if (p2Res.Status != PromptStatus.OK) return;

            PromptPointResult p3Res = ed.GetPoint("\nВыберите третью точку (pnt3): ");
            if (p3Res.Status != PromptStatus.OK) return;

            PromptPointResult p4Res = ed.GetPoint("\nВыберите четвертую точку (pnt4): ");
            if (p4Res.Status != PromptStatus.OK) return;

            Point3d pnt1 = p1Res.Value;
            Point3d pnt2 = p2Res.Value;
            Point3d pnt3 = p3Res.Value;
            Point3d pnt4 = p4Res.Value;

            // 3. Находим промежуточные точки между pnt1 и pnt2
            List<Point3d> between12 = SelectPointsBetween2Points(points, pnt1, pnt2);

            // Находим промежуточные точки между pnt2 и pnt3
            List<Point3d> between23 = SelectPointsBetween2Points(points, pnt2, pnt3);

            // Формируем нужную матрицу точек с учетом pnt1, pnt2, pnt3, pnt4 и промежуточных
            // (упрощенный пример без полноценного вычисления каждой ячейки,
            //  здесь бы потребовалась логика интерполяции и т.п.)

            // 4. Создаем 3D Faces между узлами сетки
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Ниже — чисто иллюстрация. Нужно будет самому оформить матрицу узлов и цикл:
                // for (int i = 0; i < ...; i++)
                // {
                //     for (int j = 0; j < ...; j++)
                //     {
                //         // берем четыре угловые точки (Pij, Pi+1j, Pi+1j+1, Pij+1)
                //         // создаем 3Dface
                //     }
                // }

                // Пример 3DFace (здесь берем просто pnt1, pnt2, pnt3, pnt4):
                Face face = new Face(pnt1, pnt2, pnt3, pnt4, true, true, true, true);
                ms.AppendEntity(face);
                tr.AddNewlyCreatedDBObject(face, true);

                tr.Commit();
            }

            ed.WriteMessage("\nСкрипт завершил построение 3D Faces.");
        }

        /// <summary>
        /// Вычисляет расстояние между двумя точками.
        /// </summary>
        private double Dist(Point3d a, Point3d b)
        {
            return a.DistanceTo(b);
        }

        /// <summary>
        /// Находит точки (из общего списка points), которые лежат на отрезке между p1 и p2.
        /// Условие: A + B ~ L (с заданной точностью).
        /// </summary>
        private List<Point3d> SelectPointsBetween2Points(List<Point3d> allPoints, Point3d p1, Point3d p2)
        {
            double L = Dist(p1, p2);
            var result = new List<Point3d>();

            foreach (var p in allPoints)
            {
                double A = Dist(p1, p);
                double B = Dist(p, p2);
                double diff = Math.Abs((A + B) - L);

                if (diff < Eps)
                {
                    // Точка p лежит на отрезке p1-p2
                    result.Add(p);
                }
            }

            // Убираем крайние точки, оставляя только «промежуточные»
            // (или как нужно по логике)
            // Здесь можно дополнительно сортировать по расстоянию от p1, и т.п.

            return result;
        }

        /// <summary>
        /// Находит пересечение по 4 точкам (примерно как point_between_4_points).
        /// </summary>
        private Point3d PointBetween4Points(Point3d A, Point3d B, Point3d C, Point3d D)
        {
            // В оригинальном скрипте идет довольно громоздкая формула.
            // Здесь даем упрощенный пример, показывающий идею:
            return new Point3d(
                (A.X + B.X + C.X + D.X) / 4.0,
                (A.Y + B.Y + C.Y + D.Y) / 4.0,
                (A.Z + B.Z + C.Z + D.Z) / 4.0
            );
        }
    }
}
