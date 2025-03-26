using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

public class ArcToLineCommands
{
    // Команда для создания дуги, окружности и 3D-сетки (PolyFaceMesh)
    [CommandMethod("Arc2Line02CS")]
    public void Arc2Line02CS()
    {
        Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        Editor ed = doc.Editor;
        Database db = doc.Database;

        // Функция-утилита для расчёта расстояния между двумя точками
        double Dist(Point3d a, Point3d b) => a.DistanceTo(b);

        // Линейная интерполяция
        double Interpol(double n1, double n2, double kaisy, double kancha3)
        {
            return (n2 - n1) * (kaisy / kancha3) + n1;
        }

        // 1) Запрашиваем точки (pnt1, pnt2, pntA, pnt3, pnt4) и целые числа (kancha, kancha2)
        PromptPointOptions ppo = new PromptPointOptions("\nВыберите начальную точку (pnt1): ");
        PromptPointResult ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return;
        Point3d pnt1 = ppr.Value;

        ppo.Message = "\nВыберите конечную точку (pnt2): ";
        ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return;
        Point3d pnt2 = ppr.Value;

        ppo.Message = "\nВыберите точку на дуге (pntA): ";
        ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return;
        Point3d pntA = ppr.Value;

        PromptIntegerOptions pio = new PromptIntegerOptions("\nСколько сегментов по дуге (kancha)? ");
        PromptIntegerResult pir = ed.GetInteger(pio);
        if (pir.Status != PromptStatus.OK) return;
        int kancha = pir.Value;

        ppo.Message = "\nВыберите точку N3 (pnt3): ";
        ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return;
        Point3d pnt3 = ppr.Value;

        pio.Message = "\nСколько сегментов между N2 и N3 (kancha2)? ";
        pir = ed.GetInteger(pio);
        if (pir.Status != PromptStatus.OK) return;
        int kancha2 = pir.Value;

        ppo.Message = "\nВыберите точку N4 (pnt4): ";
        ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return;
        Point3d pnt4 = ppr.Value;

        // 2) Рассчитываем центр окружности (x0, y0) и радиус (R)
        double x1 = pnt1.X, y1 = pnt1.Y;
        double x2 = pnt2.X, y2 = pnt2.Y;
        double x3 = pntA.X, y3 = pntA.Y;
        double z = pnt1.Z; // предполагаем, что все точки в одной плоскости

        // Формула из Python-скрипта
        double denom = (x1 * y2 - x1 * y3 - y1 * x2 + y1 * x3 - x3 * y2 + y3 * x2);
        if (Math.Abs(denom) < 1e-9)
        {
            ed.WriteMessage("\nНевозможно вычислить центр окружности (точки коллинеарны).");
            return;
        }

        double x0 = 0.5 * (
            -y1 * x2 * x2 + y3 * x2 * x2 - y2 * y2 * y1 + y2 * y1 * y1 + y1 * x3 * x3 +
            y3 * y3 * y1 - x1 * x1 * y3 + x1 * x1 * y2 - y3 * y1 * y1 + y2 * y2 * y3 -
            x3 * x3 * y2 - y2 * y3 * y3) / denom;

        double y0 = -0.5 * (
            x1 * x1 * x2 - x1 * x1 * x3 - x1 * x2 * x2 - x1 * y2 * y2 + x1 * x3 * x3 +
            x1 * y3 * y3 + y1 * y1 * x2 - y1 * y1 * x3 - x3 * x3 * x2 + x3 * x2 * x2 +
            x3 * y2 * y2 - y3 * y3 * x2) / denom;

        Point3d centerPt = new Point3d(x0, y0, z);
        double R = Dist(pnt1, centerPt);

        // Углы для pnt1 и pnt2
        double alpha = Math.Atan2(y1 - y0, x1 - x0);
        double beta = Math.Atan2(y2 - y0, x2 - x0);

        // 3) Внутри транзакции добавляем окружность и создаём PolyFaceMesh
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            // 3a) Создаём окружность
            Circle circle = new Circle(centerPt, Vector3d.ZAxis, R);
            btr.AppendEntity(circle);
            tr.AddNewlyCreatedDBObject(circle, true);

            // Вычислим массив Point3d P[kancha2+1, kancha+1]
            double stepAngle = (beta - alpha) / kancha;
            Point3d[,] P = new Point3d[kancha2 + 1, kancha + 1];

            // Заполняем "верхний ряд" (j=0, i=0..kancha) - дуга
            for (int i = 0; i <= kancha; i++)
            {
                double angle = alpha + stepAngle * i;
                double px = x0 + R * Math.Cos(angle);
                double py = y0 + R * Math.Sin(angle);
                P[0, i] = new Point3d(px, py, z);
            }

            // Заполняем нижний ряд (j=kancha2, i=0..kancha) - это линия от pnt4 до pnt3
            //   - pnt4 => i=0
            //   - pnt3 => i=kancha
            //   и промежуточные точки
            P[kancha2, 0] = new Point3d(pnt4.X, pnt4.Y, pnt4.Z);
            P[kancha2, kancha] = new Point3d(pnt3.X, pnt3.Y, pnt3.Z);

            for (int i = 1; i < kancha; i++)
            {
                double px = Interpol(pnt4.X, pnt3.X, i, kancha);
                double py = Interpol(pnt4.Y, pnt3.Y, i, kancha);
                double pz = Interpol(pnt4.Z, pnt3.Z, i, kancha);
                P[kancha2, i] = new Point3d(px, py, pz);
            }

            // Заполняем промежуточные "ряды" (j=1..kancha2-1, i=0..kancha)
            // интерполируем между P[0, i] и P[kancha2, i]
            for (int i = 0; i <= kancha; i++)
            {
                Point3d topPt = P[0, i];
                Point3d bottomPt = P[kancha2, i];
                for (int j = 1; j < kancha2; j++)
                {
                    double px = Interpol(topPt.X, bottomPt.X, j, kancha2);
                    double py = Interpol(topPt.Y, bottomPt.Y, j, kancha2);
                    double pz = Interpol(topPt.Z, bottomPt.Z, j, kancha2);
                    P[j, i] = new Point3d(px, py, pz);
                }
            }

            // 3b) Создаём один PolyFaceMesh
            using (PolyFaceMesh pfm = new PolyFaceMesh())
            {
                btr.AppendEntity(pfm);
                tr.AddNewlyCreatedDBObject(pfm, true);

                // Чтобы формировать FaceRecord, нужно знать индексы (1-based) вершин.
                // Мы добавим по одной вершине для каждой P[j,i]. 
                // Храним индексы в массиве "idx" такого же размера, чтобы потом сослаться на них в FaceRecord.

                short[,] idx = new short[kancha2 + 1, kancha + 1];
                short vertexCounter = 0;

                // Сначала добавим все вершины
                for (int j = 0; j <= kancha2; j++)
                {
                    for (int i = 0; i <= kancha; i++)
                    {
                        vertexCounter++;
                        // Создаём PolyFaceMeshVertex
                        PolyFaceMeshVertex vtx = new PolyFaceMeshVertex(P[j, i]);
                        pfm.AppendVertex(vtx);
                        tr.AddNewlyCreatedDBObject(vtx, true);
                        idx[j, i] = vertexCounter;
                    }
                }

                // Теперь для каждой "ячейки" (4-х точек) создаём FaceRecord
                // В Python: for i in range(0, kancha): for j in range(0, kancha2):
                for (int j = 0; j < kancha2; j++)
                {
                    for (int i = 0; i < kancha; i++)
                    {
                        // Вершины (j,i), (j,i+1), (j+1,i+1), (j+1,i)
                        short i1 = idx[j, i];
                        short i2 = idx[j, i + 1];
                        short i3 = idx[j + 1, i + 1];
                        short i4 = idx[j + 1, i];

                        FaceRecord face = new FaceRecord(i1, i2, i3, i4);
                        pfm.AppendFaceRecord(face);
                        tr.AddNewlyCreatedDBObject(face, true);
                    }
                }
            }

            tr.Commit();
        }

        // 4) Выводим результат в командную строку
        ed.WriteMessage($"\nЦентр окружности: ({x0:F3}, {y0:F3}, {z:F3}), радиус R={R:F3}.");
        ed.WriteMessage("\nОбработка завершена. Созданы окружность и сетка (PolyFaceMesh).\n");
    }
}
