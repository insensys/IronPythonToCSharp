using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace ArcToLinePlugin
{
    public class Arc2LineCommands
    {
        /// <summary>
        /// Статический метод для расчёта расстояния в 3D.
        /// </summary>
        private static double Dist(Point3d a, Point3d b)
        {
            return Math.Sqrt(
                (b.X - a.X) * (b.X - a.X)
                + (b.Y - a.Y) * (b.Y - a.Y)
                + (b.Z - a.Z) * (b.Z - a.Z)
            );
        }

        /// <summary>
        /// Линейная интерполяция (аналог функции interpol из Python).
        /// </summary>
        private static double Interpol(double n1, double n2, double i, double total)
        {
            return (n2 - n1) * i / total + n1;
        }

        /// <summary>
        /// Команда Arc2LineCS, которую нужно вызвать в AutoCAD.
        /// </summary>
        [CommandMethod("Arc2LineCS")]
        public void Arc2LineCS()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Запрашиваем первую точку (Башындагы чекитти танда)
            PromptPointOptions opt1 = new PromptPointOptions("\nБашындагы чекитти танда: ");
            PromptPointResult res1 = ed.GetPoint(opt1);
            if (res1.Status != PromptStatus.OK) return;
            Point3d pnt1 = res1.Value;

            // Запрашиваем вторую точку (Аягындагы чекитти танда)
            PromptPointOptions opt2 = new PromptPointOptions("\nАягындагы чекитти танда: ");
            PromptPointResult res2 = ed.GetPoint(opt2);
            if (res2.Status != PromptStatus.OK) return;
            Point3d pnt2 = res2.Value;

            // Запрашиваем точку на «дуге» (Аркадагы чекитти танда)
            PromptPointOptions optA = new PromptPointOptions("\nАркадагы чекитти танда: ");
            PromptPointResult resA = ed.GetPoint(optA);
            if (resA.Status != PromptStatus.OK) return;
            Point3d pntA = resA.Value;

            // Сколько сегментов делить дугу (арасын канчага майдалайлы?)
            int kancha = 10; // По умолчанию
            PromptIntegerOptions intOpt = new PromptIntegerOptions("\nарасын канчага майдалайлы?: ")
            {
                DefaultValue = 10,
                AllowZero = false,
                AllowNegative = false
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpt);
            if (intRes.Status == PromptStatus.OK)
            {
                kancha = intRes.Value;
            }

            // Запрашиваем 3-ю точку
            PromptPointOptions opt3 = new PromptPointOptions("\nN3 чекитти танда: ");
            PromptPointResult res3 = ed.GetPoint(opt3);
            if (res3.Status != PromptStatus.OK) return;
            Point3d pnt3 = res3.Value;

            // Запрашиваем на сколько делить отрезок pnt2->pnt3 (N2 менем N3 арасын канчага майдалайлы?)
            int kancha2 = 10;
            PromptIntegerOptions intOpt2 = new PromptIntegerOptions("\nN2 менем N3 арасын канчага майдалайлы?: ")
            {
                DefaultValue = 10,
                AllowZero = false,
                AllowNegative = false
            };
            PromptIntegerResult intRes2 = ed.GetInteger(intOpt2);
            if (intRes2.Status == PromptStatus.OK)
            {
                kancha2 = intRes2.Value;
            }

            // Запрашиваем 4-ю точку
            PromptPointOptions opt4 = new PromptPointOptions("\nN4 чекитти танда: ");
            PromptPointResult res4 = ed.GetPoint(opt4);
            if (res4.Status != PromptStatus.OK) return;
            Point3d pnt4 = res4.Value;

            // Теперь повторим логику вычисления центра окружности (x0, y0) для трёх точек pnt1, pnt2, pntA
            // Аналог Python-кода
            double x1 = pnt1.X, y1 = pnt1.Y;
            double x2 = pnt2.X, y2 = pnt2.Y;
            double x3 = pntA.X, y3 = pntA.Y;
            double z = pnt1.Z; // Предположим, Z у всех точек одинаков

            // Формула из кода
            double denom = (x1 * y2 - x1 * y3 - y1 * x2 + y1 * x3 - x3 * y2 + y3 * x2);
            if (Math.Abs(denom) < 1e-9)
            {
                ed.WriteMessage("\nНевозможно вычислить центр окружности – плохие точки?");
                return;
            }
            double x0 = 0.5 *
                        (-y1 * x2 * x2 + y3 * x2 * x2 - y2 * y2 * y1 + y2 * y1 * y1 + y1 * x3 * x3 +
                         y3 * y3 * y1 - x1 * x1 * y3 + x1 * x1 * y2 - y3 * y1 * y1 +
                         y2 * y2 * y3 - x3 * x3 * y2 - y2 * y3 * y3)
                        / denom;
            double y0 = -0.5 *
                        (x1 * x1 * x2 - x1 * x1 * x3 - x1 * x2 * x2 - x1 * y2 * y2 +
                         x1 * x3 * x3 + x1 * y3 * y3 + y1 * y1 * x2 - y1 * y1 * x3 -
                         x3 * x3 * x2 + x3 * x2 * x2 + x3 * y2 * y2 - y3 * y3 * x2)
                        / denom;

            // Радиус
            Point3d center = new Point3d(x0, y0, z);
            double R = Dist(pnt1, center);

            // Углы alpha/beta (аналог из Python)
            double alpha = Math.Atan2(y1 - y0, x1 - x0);
            double betta = Math.Atan2(y2 - y0, x2 - x0);

            // Корректировка углов (Python-код добавляет math.pi, если x<0)
            // В C# достаточно использовать Atan2, но сохраним логику:
            if ((x1 - x0) < 0.0) alpha += Math.PI;
            if ((x2 - x0) < 0.0) betta += Math.PI;

            // Разбивка дуги на "kancha" сегментов
            double kadam = (betta - alpha) / kancha;

            // Создаём двумерный массив P[j][i] (j=0..kancha2, i=0..kancha), где храним точки
            // В C# можно хранить в List<List<Point3d>>, но для наглядности – массив.
            Point3d[,] P = new Point3d[kancha2 + 1, kancha + 1];

            // Заполняем первую "полосу" (j=0) точками дуги
            for (int i = 0; i <= kancha; i++)
            {
                double angle = alpha + kadam * i;
                double xx = x0 + R * Math.Cos(angle);
                double yy = y0 + R * Math.Sin(angle);
                P[0, i] = new Point3d(xx, yy, z);
            }

            // Конечные точки (python: P[kancha2][kancha] = pnt3, P[kancha2][0] = pnt4)
            // Здесь pnt2 -> pnt3 и pnt4 – для "верхней" полосы.
            // Соответственно, j=kancha2, i=kancha => pnt3
            P[kancha2, kancha] = pnt3;
            // j=kancha2, i=0 => pnt4
            P[kancha2, 0] = pnt4;

            // Интерполяция вдоль j=kancha2, i=1..(kancha-1)
            for (int i = 1; i < kancha; i++)
            {
                double xinterp = Interpol(pnt4.X, pnt3.X, i, kancha);
                double yinterp = Interpol(pnt4.Y, pnt3.Y, i, kancha);
                double zinterp = Interpol(pnt4.Z, pnt3.Z, i, kancha);
                P[kancha2, i] = new Point3d(xinterp, yinterp, zinterp);
            }

            // Интерполяция внутри "прямоугольника" (0..kancha2, 0..kancha)
            // По j=1..kancha2-1 и i=0..kancha
            for (int i = 0; i <= kancha; i++)
            {
                for (int j = 1; j < kancha2; j++)
                {
                    double xinterp = Interpol(P[0, i].X, P[kancha2, i].X, j, kancha2);
                    double yinterp = Interpol(P[0, i].Y, P[kancha2, i].Y, j, kancha2);
                    double zinterp = Interpol(P[0, i].Z, P[kancha2, i].Z, j, kancha2);
                    P[j, i] = new Point3d(xinterp, yinterp, zinterp);
                }
            }

            // Создаём 3DFace в AutoCAD для каждого "квадратика" сетки
            using (Transaction acTrans = doc.TransactionManager.StartTransaction())
            {
                BlockTable bt = acTrans.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                for (int i = 0; i < kancha; i++)
                {
                    for (int j = 0; j < kancha2; j++)
                    {
                        // 4 угла "квадрата" P[j,i], P[j,i+1], P[j+1,i+1], P[j+1,i]
                        Point3d p1 = P[j, i];
                        Point3d p2 = P[j, i + 1];
                        Point3d p3 = P[j + 1, i + 1];
                        Point3d p4 = P[j + 1, i];

                        //var face = new 3dFace(p1, p2, p3, p4, true, true, true, true);
                        Face face = new Face(); // или DB3dFace face = new DB3dFace();
                        face.SetVertexAt(0, p1);
                        face.SetVertexAt(1, p2);
                        face.SetVertexAt(2, p3);
                        face.SetVertexAt(3, p4);

                        //face.SetEdgeVisAt(0, true);
                        //face.SetEdgeVisAt(1, true);
                        //face.SetEdgeVisAt(2, true);
                        //face.SetEdgeVisAt(3, true);

                        btr.AppendEntity(face);
                        acTrans.AddNewlyCreatedDBObject(face, true);
                        
                    }
                }

                acTrans.Commit();
            }

            ed.WriteMessage("\nГотово! Сетка 3DFace создана.");
        }
    }
}
