using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace SumLinesByLayers
{
    public class SumLayersCommands : IExtensionApplication
    {
        // Метод, который вызывается при загрузке плагина
        public void Initialize()
        {
            // Можно ничего не делать или вывести приветствие в консоль
        }

        // Метод, который вызывается при выгрузке плагина
        public void Terminate()
        {
            // Можно освободить ресурсы, если нужно
        }

        /// <summary>
        /// Основная команда, аналог Python-скрипта.
        /// </summary>
        [CommandMethod("SUM_LAYERS_COMMAND")]
        public void SumLayersCommand()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Запрос на выбор объектов
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\nВыберите объекты для суммирования:";
            PromptSelectionResult psr = ed.GetSelection(opts);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nОтмена или ничего не выбрано.");
                return;
            }

            // Списки для накопления результатов по слоям
            // (слой -> сумма длин, сумма площадей, кол-во 3dFace, кол-во Line, кол-во Point и т. д.)
            List<string> layerNames = new List<string>();
            List<double> sumLen = new List<double>();
            List<double> sumArea = new List<double>();
            List<int> sumLines = new List<int>();
            List<int> sumFaces = new List<int>();
            List<int> sumPoints = new List<int>();

            // Для вычисления в транзакции
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Перебираем все выбранные объекты
                SelectionSet ss = psr.Value;
                foreach (SelectedObject selObj in ss)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Проверяем тип объекта
                    if (ent is Line lineObj)
                    {
                        // Длина линии
                        double linelen = (lineObj.EndPoint - lineObj.StartPoint).Length;
                        // Ищем в списках нужный слой
                        int idx = layerNames.IndexOf(ent.Layer);
                        if (idx < 0)
                        {
                            // Добавляем новый слой
                            layerNames.Add(ent.Layer);
                            sumLines.Add(1);
                            sumLen.Add(linelen);

                            sumFaces.Add(0);
                            sumPoints.Add(0);
                            sumArea.Add(0.0);
                        }
                        else
                        {
                            // Прибавляем к уже существующим данным
                            sumLines[idx] += 1;
                            sumLen[idx] += linelen;
                        }
                    }
                    else if (ent is DBPoint)
                    {
                        int idx = layerNames.IndexOf(ent.Layer);
                        if (idx < 0)
                        {
                            layerNames.Add(ent.Layer);

                            sumLines.Add(0);
                            sumLen.Add(0.0);

                            sumFaces.Add(0);
                            sumArea.Add(0.0);
                            sumPoints.Add(1);
                        }
                        else
                        {
                            sumPoints[idx] += 1;
                        }
                    }
                    else if (ent is Face faceObj)
                    {
                        // 3dFace задается 4 вершинами (вершины могут дублироваться).
                        // Логика вычисления площади: разбиваем 4-угольник на 2 треугольника.
                        // Треугольник 1: вершины [0,1,2], треугольник 2: [0,2,3].

                        // Координаты вершин:
                        Point3d v0 = faceObj.GetVertexAt(0);
                        Point3d v1 = faceObj.GetVertexAt(1);
                        Point3d v2 = faceObj.GetVertexAt(2);
                        Point3d v3 = faceObj.GetVertexAt(3);

                        double s1 = TriangleArea(v0, v1, v2);
                        double s2 = TriangleArea(v0, v2, v3);
                        double faceArea = s1 + s2;

                        int idx = layerNames.IndexOf(ent.Layer);
                        if (idx < 0)
                        {
                            layerNames.Add(ent.Layer);

                            sumLines.Add(0);
                            sumLen.Add(0.0);

                            sumPoints.Add(0);

                            sumFaces.Add(1);
                            sumArea.Add(faceArea);
                        }
                        else
                        {
                            sumFaces[idx] += 1;
                            sumArea[idx] += faceArea;
                        }
                    }
                }

                tr.Commit();
            }

            // Подсчёт общих итогов
            double allLen = 0.0;
            double allArea = 0.0;
            int allLines = 0;
            int allFaces = 0;
            int allPoints = 0;

            for (int i = 0; i < layerNames.Count; i++)
            {
                allLen += sumLen[i];
                allLines += sumLines[i];
                allArea += sumArea[i];
                allFaces += sumFaces[i];
                allPoints += sumPoints[i];
            }

            // Вывести в консоль итоги (если нужно)
            ed.WriteMessage("\nИтоги по слоям:");
            for (int i = 0; i < layerNames.Count; i++)
            {
                ed.WriteMessage(
                    $"\n{i + 1}. Слой: {layerNames[i]} | " +
                    $"Сумма длин (Line): {sumLen[i]:F2} | " +
                    $"Кол-во Line: {sumLines[i]} | " +
                    $"Сумма площадей (Face): {sumArea[i]:F2} | " +
                    $"Кол-во Face: {sumFaces[i]} | " +
                    $"Кол-во Point: {sumPoints[i]}"
                );
            }

            ed.WriteMessage(
                $"\n\nИТОГО: общая длина={allLen:F2}, " +
                $"всего Line={allLines}, общая площадь={allArea:F2}, " +
                $"всего Face={allFaces}, всего Point={allPoints}\n"
            );

            // Запрос точки вставки таблицы от пользователя
            PromptPointOptions ppo = new PromptPointOptions("\nВыберите точку вставки таблицы:");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d basePoint = ppr.Value;

            // Создание таблицы
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Откроем пространствo модели для записи
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Размеры таблицы: (строк = кол-во слоев + 2-3), (столбцов = 8 для примера)
                int rowsCount = layerNames.Count + 3;
                int colsCount = 8;

                // Создаём Table
                using (Table table = new Table())
                {
                    table.TableStyle = db.Tablestyle;
                    table.NumRows = rowsCount;
                    table.NumColumns = colsCount;

                    // Зададим высоту/ширину ячеек
                    table.SetRowHeight(10);
                    for (int c = 0; c < colsCount; c++)
                    {
                        table.SetColumnWidth(c, 50);
                    }

                    // Установим позицию
                    table.Position = basePoint;

                    // Заголовок
                    table.MergeCells(CellRange.Create(table, 0, 0, 0, colsCount - 1));
                    table.SetTextHeight(0, 0, 2.5);
                    table.SetTextString(0, 0, "Суммарные показатели по выделенным объектам");

                    // Шапка таблицы
                    table.SetTextString(1, 1, "Слой");
                    table.SetTextString(1, 2, "Сумма длин Line");
                    table.SetTextString(1, 3, "Кол-во Line");
                    table.SetTextString(1, 4, "Сумма площадей Face");
                    table.SetTextString(1, 5, "Кол-во Face");
                    table.SetTextString(1, 7, "Кол-во Point");

                    // Заполняем строки по слоям
                    for (int i = 0; i < layerNames.Count; i++)
                    {
                        int rowIndex = i + 2;

                        table.SetTextString(rowIndex, 0, (i + 1).ToString());
                        table.SetTextString(rowIndex, 1, layerNames[i]);
                        table.SetTextString(rowIndex, 2, sumLen[i].ToString("F2"));
                        table.SetTextString(rowIndex, 3, sumLines[i].ToString());
                        table.SetTextString(rowIndex, 4, sumArea[i].ToString("F2"));
                        table.SetTextString(rowIndex, 5, sumFaces[i].ToString());
                        table.SetTextString(rowIndex, 7, sumPoints[i].ToString());
                    }

                    // Строка "ИТОГО"
                    int totalRowIndex = layerNames.Count + 2;
                    table.SetTextString(totalRowIndex, 1, "ИТОГО");
                    table.SetTextString(totalRowIndex, 2, allLen.ToString("F2"));
                    table.SetTextString(totalRowIndex, 3, allLines.ToString());
                    table.SetTextString(totalRowIndex, 4, allArea.ToString("F2"));
                    table.SetTextString(totalRowIndex, 5, allFaces.ToString());
                    table.SetTextString(totalRowIndex, 7, allPoints.ToString());

                    // Добавляем таблицу в модель
                    btr.AppendEntity(table);
                    tr.AddNewlyCreatedDBObject(table, true);
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Вспомогательная функция для вычисления площади треугольника по трем точкам (3D).
        /// </summary>
        private double TriangleArea(Point3d p0, Point3d p1, Point3d p2)
        {
            // Вектор p0->p1
            Vector3d v1 = p1.GetVectorTo(p0);
            // Вектор p0->p2
            Vector3d v2 = p2.GetVectorTo(p0);
            // Площадь = 0.5 * |v1 x v2|
            double crossLen = v1.CrossProduct(v2).Length;
            return 0.5 * crossLen;
        }
    }
}
