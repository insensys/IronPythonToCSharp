using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

// Пространства имён могут немного отличаться в зависимости от версии AutoCAD.

namespace Equalizer04
{
    public class Equalizer
    {
        // ================================
        // 1) Вспомогательные функции
        // ================================

        /// <summary>
        /// Вычисление 3D расстояния между двумя точками (x,y,z)
        /// </summary>
        private static double Dist(double[] a, double[] b)
        {
            double dx = b[0] - a[0];
            double dy = b[1] - a[1];
            double dz = b[2] - a[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Простая функция для создания массива вида (x,y,z)
        /// </summary>
        private static double[] MakeArray3d(double x, double y, double z)
        {
            return new double[] { x, y, z };
        }

        // ================================
        // 2) Основная команда
        // ================================

        [CommandMethod("EQUALIZER")]
        public void Equalizer04Command()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Список всех 3D-точек, полученных из выбранных объектов
            List<double[]> allPoints = new List<double[]>();

            // ---------------------------------
            // 2.1. Просим пользователя выбрать объекты
            // ---------------------------------
            PromptSelectionOptions selOpt = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите точки, линии, 3D Face и т.п.: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpt);

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            SelectionSet sset = selRes.Value;

            // ---------------------------------
            // 2.2. Извлекаем координаты точек из выбранных объектов
            // ---------------------------------
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject sobj in sset)
                {
                    if (sobj == null) continue;
                    Entity ent = tr.GetObject(sobj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Если это DBPoint
                    if (ent is DBPoint dbp)
                    {
                        allPoints.Add(MakeArray3d(dbp.Position.X, dbp.Position.Y, dbp.Position.Z));
                    }
                    // Если это Line
                    else if (ent is Line ln)
                    {
                        allPoints.Add(MakeArray3d(ln.StartPoint.X, ln.StartPoint.Y, ln.StartPoint.Z));
                        allPoints.Add(MakeArray3d(ln.EndPoint.X, ln.EndPoint.Y, ln.EndPoint.Z));
                    }
                    // Если это Face (старый AutoCAD 3DFace)
                    else if (ent is Face face)
                    {
                        // Face имеет 4 вершины
                        for (int i = 0; i < 4; i++)
                        {
                            var v = face.GetVertexAt((short)i);
                            allPoints.Add(MakeArray3d(v.X, v.Y, v.Z));
                        }
                    }
                    // Можно расширять для других типов (Polyline, Mesh и т.д.)
                }
                tr.Commit();
            }

            // ---------------------------------
            // 2.3. Запрашиваем у пользователя 2 точки в пространстве
            // ---------------------------------
            var pntRes1 = ed.GetPoint("\nУкажите первую точку (pnt1): ");
            if (pntRes1.Status != PromptStatus.OK) return;
            var pnt1 = pntRes1.Value;  // это Point3d

            var pntRes2 = ed.GetPoint("\nУкажите вторую точку (pnt2): ");
            if (pntRes2.Status != PromptStatus.OK) return;
            var pnt2 = pntRes2.Value;  // это Point3d

            double[] arr1 = MakeArray3d(pnt1.X, pnt1.Y, pnt1.Z);
            double[] arr2 = MakeArray3d(pnt2.X, pnt2.Y, pnt2.Z);
            double dist12 = Dist(arr1, arr2);

            // ---------------------------------
            // 2.4. Ищем промежуточные точки, которые лежат на отрезке pnt1->pnt2
            // ---------------------------------
            double eps = 0.001; // "pushinka" в оригинале
            List<double[]> selectedBetween = new List<double[]>();

            foreach (var p in allPoints)
            {
                double A = Dist(arr1, p);   // расстояние pnt1 -> p
                double B = Dist(p, arr2);   // расстояние p -> pnt2
                double sum = A + B;

                // Проверяем, что сумма A+B примерно равна dist12 => значит точка на одной прямой
                if (Math.Abs(dist12 - sum) < eps)
                {
                    // берем точку, но исключаем саму pnt1 или pnt2, если хотим
                    if (A > eps && B > eps)
                    {
                        selectedBetween.Add(p);
                    }
                }
            }

            // Сортируем промежуточные точки по расстоянию от pnt1
            selectedBetween = selectedBetween
                .OrderBy(p => Dist(arr1, p))
                .ToList();

            // Убираем дубликаты, которые близко друг к другу
            List<double[]> midPoints = new List<double[]>();
            foreach (var candidate in selectedBetween)
            {
                if (midPoints.Count == 0)
                {
                    midPoints.Add(candidate);
                }
                else
                {
                    var last = midPoints[midPoints.Count - 1];
                    if (Dist(last, candidate) > eps)
                    {
                        midPoints.Add(candidate);
                    }
                }
            }

            // Число найденных промежуточных точек + 2 (pnt1 и pnt2) => кол-во сегментов
            int kancha = midPoints.Count + 1;

            ed.WriteMessage($"\nМежду этими точками всего сегментов: {kancha}.");

            // ---------------------------------
            // 2.5. Запрашиваем у пользователя ещё 2 точки (pnt3, pnt4) + число разбиений
            // ---------------------------------
            var pntRes3 = ed.GetPoint("\nУкажите третью точку (pnt3): ");
            if (pntRes3.Status != PromptStatus.OK) return;
            var pnt3 = pntRes3.Value;

            var intRes = ed.GetInteger("\nСколько раз разбить между pnt2 и pnt3?: ");
            if (intRes.Status != PromptStatus.OK) return;
            int kancha2 = intRes.Value; // кол-во «строк» или «рядиов» при делении

            var pntRes4 = ed.GetPoint("\nУкажите четвёртую точку (pnt4): ");
            if (pntRes4.Status != PromptStatus.OK) return;
            var pnt4 = pntRes4.Value;

            // ---------------------------------
            // 2.6. Строим 3D Face-сетку (аналог point_matrix + Add3DFace)
            // ---------------------------------
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForWrite
                ) as BlockTableRecord;

                // Подготовим массив точек P размером (kancha2+1) x (kancha+1)
                double[,,] P = new double[kancha2 + 1, kancha + 1, 3];

                // Заполним "верхнюю" строку (j=0) точками pnt1->pnt2
                // midPoints - это промежуточные
                // P[0][0] = pnt1, P[0][kancha] = pnt2
                // и т.д.

                // 1) Запишем pnt1 и pnt2
                P[0, 0, 0] = pnt1.X; P[0, 0, 1] = pnt1.Y; P[0, 0, 2] = pnt1.Z;
                P[0, kancha, 0] = pnt2.X; P[0, kancha, 1] = pnt2.Y; P[0, kancha, 2] = pnt2.Z;

                // 2) Запишем pnt3 и pnt4 в "нижнюю" строку (j = kancha2)
                P[kancha2, kancha, 0] = pnt3.X; P[kancha2, kancha, 1] = pnt3.Y; P[kancha2, kancha, 2] = pnt3.Z;
                P[kancha2, 0, 0] = pnt4.X; P[kancha2, 0, 1] = pnt4.Y; P[kancha2, 0, 2] = pnt4.Z;

                // 3) Заполняем промежуточные точки по «верхней» строке (j=0, i=1..kancha-1)
                for (int i = 1; i < kancha; i++)
                {
                    var mp = midPoints[i - 1];
                    P[0, i, 0] = mp[0];
                    P[0, i, 1] = mp[1];
                    P[0, i, 2] = mp[2];
                }

                // 4) Заполним «нижнюю» строку (j=kancha2) интерполяцией между pnt4 и pnt3
                for (int i = 1; i < kancha; i++)
                {
                    double t = (double)i / (double)kancha;
                    // интерполяция по X
                    P[kancha2, i, 0] = pnt4.X + t * (pnt3.X - pnt4.X);
                    // интерполяция по Y
                    P[kancha2, i, 1] = pnt4.Y + t * (pnt3.Y - pnt4.Y);
                    // интерполяция по Z
                    P[kancha2, i, 2] = pnt4.Z + t * (pnt3.Z - pnt4.Z);
                }

                // 5) Заполняем внутренние точки (по j=1..kancha2-1; i=0..kancha)
                // линейная интерполяция между «верхней» и «нижней» строками
                for (int j = 1; j < kancha2; j++)
                {
                    double tj = (double)j / (double)kancha2;
                    for (int i = 0; i <= kancha; i++)
                    {
                        double xTop = P[0, i, 0];
                        double yTop = P[0, i, 1];
                        double zTop = P[0, i, 2];

                        double xBot = P[kancha2, i, 0];
                        double yBot = P[kancha2, i, 1];
                        double zBot = P[kancha2, i, 2];

                        double xVal = xTop + tj * (xBot - xTop);
                        double yVal = yTop + tj * (yBot - yTop);
                        double zVal = zTop + tj * (zBot - zTop);

                        P[j, i, 0] = xVal;
                        P[j, i, 1] = yVal;
                        P[j, i, 2] = zVal;
                    }
                }

                // 6) Теперь формируем 3D Face для каждой ячейки сетки
                // (kancha2 x kancha) квадратиков
                for (int j = 0; j < kancha2; j++)
                {
                    for (int i = 0; i < kancha; i++)
                    {
                        // 4 вершины квадрата (j,i), (j,i+1), (j+1,i+1), (j+1,i)
                        double[] p1 = { P[j, i, 0], P[j, i, 1], P[j, i, 2] };
                        double[] p2 = { P[j, i + 1, 0], P[j, i + 1, 1], P[j, i + 1, 2] };
                        double[] p3 = { P[j + 1, i + 1, 0], P[j + 1, i + 1, 1], P[j + 1, i + 1, 2] };
                        double[] p4 = { P[j + 1, i, 0], P[j + 1, i, 1], P[j + 1, i, 2] };

                        Face face = new Face(
                            new Autodesk.AutoCAD.Geometry.Point3d(p1[0], p1[1], p1[2]),
                            new Autodesk.AutoCAD.Geometry.Point3d(p2[0], p2[1], p2[2]),
                            new Autodesk.AutoCAD.Geometry.Point3d(p3[0], p3[1], p3[2]),
                            new Autodesk.AutoCAD.Geometry.Point3d(p4[0], p4[1], p4[2]),
                            true, true, true, true
                        );

                        btr.AppendEntity(face);
                        tr.AddNewlyCreatedDBObject(face, true);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nСкрипт завершён. Сетка 3D Face создана.");
        }
    }
}
