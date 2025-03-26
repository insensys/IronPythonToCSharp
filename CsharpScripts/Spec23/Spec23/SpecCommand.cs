using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Spec23
{
    public class SpecCommand
    {
        // Массивы диаметров (мм) и удельных масс (кг/м)
        private static readonly double[] diam =
        {
            3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40, 45, 50, 55, 60, 70, 80
        };

        private static readonly double[] ves =
        {
            0.055, 0.098, 0.154, 0.222, 0.395, 0.617, 0.888, 1.21, 1.58, 2.0,
            2.47, 2.98, 3.85, 4.83, 6.31, 7.99, 9.87, 12.48, 15.41, 18.65,
            22.19, 30.21, 39.46
        };

        // Соответствие класса стали и ГОСТ
        private static readonly Dictionary<string, string> klassGost = new Dictionary<string, string>
        {
            { "BI", "6727-80" },
            { "BpI", "6727-80" },
            { "AI", "5781-82" },
            { "AIII", "5781-82" }
        };

        [CommandMethod("SPEC23")]
        public void CreateSpecTables()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Попросим пользователя выбрать объекты
            PromptSelectionOptions opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите объекты (линии, соответствующие арматуре): "
            };

            PromptSelectionResult selRes = ed.GetSelection(opts);
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Списки для накопления данных
                List<double> sumLen = new List<double>();         // суммарная длина по каждой позиции
                List<int> sumLines = new List<int>();            // кол-во линий по каждой позиции
                List<List<double>> posDiamLens = new List<List<double>>(); // отдельные длины для детального анализа

                // В этом списке храним [поз, диаметр, класс_арматуры], чтобы связать с sumLen/sumLines
                List<string[]> posDiams = new List<string[]>();

                // Пройдем по выбранным объектам
                foreach (SelectedObject selObj in selRes.Value)
                {
                    if (selObj == null) continue;
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent is Line line)
                    {
                        // Длина линии
                        double dx = line.EndPoint.X - line.StartPoint.X;
                        double dy = line.EndPoint.Y - line.StartPoint.Y;
                        double dz = line.EndPoint.Z - line.StartPoint.Z;
                        double linelen = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                        // Предположим, что слой имеет формат: "Поз_Диаметр_КлассАрм" (например, "main_12_AIII")
                        string layerName = ent.Layer;
                        string[] splitData = layerName.Split('_');
                        if (splitData.Length < 3) continue;

                        // Пример: pozName = "main", diamStr = "12", klassStr = "AIII"
                        string pozName = splitData[0];
                        string diamStr = splitData[1];
                        string klassStr = splitData[2];

                        // Сопоставим строку с нашим массивом diam
                        int indexDiam = -1;
                        for (int i = 0; i < diam.Length; i++)
                        {
                            // Сравнение по строке; при необходимости используйте Convert.ToInt32, если диаметр целый
                            if (Math.Abs(diam[i] - Convert.ToDouble(diamStr)) < 0.0001)
                            {
                                indexDiam = i;
                                break;
                            }
                        }
                        if (indexDiam < 0) continue; // диаметр не найден в массиве

                        // Если длина > 11700, предполагаем, что арматура будет в прутках по ~11.7м + припуски
                        // (как в оригинале python: linelen // 11700, +8*d для вязки и т.п.)
                        // Но здесь можно просто повторить логику: если > 11700, учитываем добавку
                        // (примерно как в исходном коде)
                        if (linelen > 11700.0)
                        {
                            // Кол-во целых прутков
                            double kratno = Math.Floor(linelen / 11700.0);
                            // Добавляем припуск (8*d)
                            linelen += 8.0 * diam[indexDiam];
                        }

                        // Найдём, есть ли такая позиция (pozName, diamStr, klassStr) в нашем списке
                        int foundPos = -1;
                        for (int j = 0; j < posDiams.Count; j++)
                        {
                            // Сравниваем по всем трем полям
                            if (posDiams[j][0] == pozName &&
                                posDiams[j][1] == diamStr &&
                                posDiams[j][2] == klassStr)
                            {
                                foundPos = j;
                                break;
                            }
                        }

                        if (foundPos == -1)
                        {
                            // Добавляем новую позицию
                            posDiams.Add(new string[] { pozName, diamStr, klassStr });
                            sumLines.Add(1);
                            sumLen.Add(linelen);

                            List<double> newListLens = new List<double>();
                            newListLens.Add(linelen);
                            posDiamLens.Add(newListLens);
                        }
                        else
                        {
                            sumLines[foundPos] += 1;
                            sumLen[foundPos] += linelen;
                            posDiamLens[foundPos].Add(linelen);
                        }
                    }
                }

                // После сбора данных создадим первую спецификационную таблицу
                // Запросим точку вставки у пользователя
                PromptPointResult ppr1 = ed.GetPoint("\nУкажите точку вставки спецификации: ");
                if (ppr1.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nТочка не выбрана. Прерывание.");
                    tr.Commit();
                    return;
                }
                Point3d basePoint1 = ppr1.Value;

                // Начинаем работу с модельным пространством
                // Создаем Table в модели
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                using (Table table = new Table())
                {
                    table.TableStyle = db.Tablestyle;
                    // Простейший вариант: количество строк = sumLen.Count + 6, колонок = 6
                    table.SetSize(sumLen.Count + 6, 6);
                    table.SetRowHeight(10.0);
                    table.SetColumnWidth(30.0);

                    // Координаты вставки
                    table.Position = basePoint1;

                    // Заполним заголовки
                    table.Cells[0, 0].TextString = "Спецификация";
                    // Примерно как в python-коде
                    table.Cells[1, 0].TextString = "Марка поз";
                    table.Cells[1, 1].TextString = "Обозначение";
                    table.Cells[1, 2].TextString = "Наименование";
                    table.Cells[1, 3].TextString = "Кол.";
                    table.Cells[1, 4].TextString = "Масса ед. (кг)";
                    table.Cells[1, 5].TextString = "Примечание";

                    table.Cells[3, 2].TextString = "Детали";

                    // Дальше заполняем строки
                    int irow = 4;

                    // Сортируем posDiams/layers по слою, как в python-коде, но у нас нет точной сортировки слоев,
                    // поэтому можно просто выводить в порядке накопления.
                    // Если нужно, можно отсортировать по [0], [1] и т.д.

                    // Для удобства создадим копию индексов
                    List<int> sortedIndexes = new List<int>();
                    for (int i = 0; i < posDiams.Count; i++) sortedIndexes.Add(i);

                    // Если нужно сортировать по (pozName, diamStr), раскомментируйте пример:
                    // sortedIndexes.Sort((a, b) => {
                    //     int comparePoz = posDiams[a][0].CompareTo(posDiams[b][0]);
                    //     if (comparePoz != 0) return comparePoz;
                    //     // если одинаковая позиция, сравним диаметр
                    //     double dA = Convert.ToDouble(posDiams[a][1]);
                    //     double dB = Convert.ToDouble(posDiams[b][1]);
                    //     return dA.CompareTo(dB);
                    // });

                    // Заполняем строки таблицы
                    for (int idx = 0; idx < sortedIndexes.Count; idx++)
                    {
                        int j = sortedIndexes[idx];
                        string pozName = posDiams[j][0];
                        string diamStr = posDiams[j][1];
                        string klassStr = posDiams[j][2];

                        // Проверим диаметр в массиве
                        int dIndex = -1;
                        for (int i = 0; i < diam.Length; i++)
                        {
                            if (Math.Abs(diam[i] - Convert.ToDouble(diamStr)) < 0.0001)
                            {
                                dIndex = i;
                                break;
                            }
                        }
                        if (dIndex < 0) continue; // не найден

                        // Проверим класс -> ГОСТ
                        string gostVal = klassGost.ContainsKey(klassStr) ? $"ГОСТ {klassGost[klassStr]}" : "ГОСТ ???";

                        // Проверяем все длины (если в python-коде был анализ на +-5мм)
                        double firstLen = posDiamLens[j][0];
                        bool allEqual = true;
                        foreach (double dl in posDiamLens[j])
                        {
                            if (Math.Abs(dl - firstLen) > 5.0)
                            {
                                allEqual = false;
                                break;
                            }
                        }

                        // Заполняем ячейки
                        table.Cells[irow, 0].TextString = pozName;
                        table.Cells[irow, 1].TextString = gostVal;
                        // Наименование: "Ø12AIII L=1000(пм)" - пример
                        table.Cells[irow, 2].TextString = $"%%c{diamStr}{klassStr} L=1000(п.м.)";

                        // Кол. (п.м.) = sumLen / 1000
                        double totalLenM = sumLen[j] / 1000.0;
                        table.Cells[irow, 3].TextString = totalLenM.ToString("0.0");

                        double massOneMeter = ves[dIndex]; // удельная масса (кг/м)
                        table.Cells[irow, 4].TextString = massOneMeter.ToString("0.###");

                        // Масса всей позиции
                        double m = massOneMeter * Math.Round(totalLenM, 1);
                        table.Cells[irow, 5].TextString = m.ToString("0.###");

                        irow++;
                    }

                    // Добавляем таблицу в чертеж
                    btr.AppendEntity(table);
                    tr.AddNewlyCreatedDBObject(table, true);
                }

                // Теперь вторая таблица (Ведомость расхода стали)
                PromptPointResult ppr2 = ed.GetPoint("\nУкажите точку вставки ведомости: ");
                if (ppr2.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nТочка не выбрана. Прерывание.");
                    tr.Commit();
                    return;
                }
                Point3d basePoint2 = ppr2.Value;

                // В python-коде собиралась классификация по классам (AI, AIII и т.д.), объединяли диаметры
                // Повторим логику.

                // 1) Собираем уникальные классы
                List<string> klassList = new List<string>();
                // Для каждого класса - список диаметров
                List<List<int>> klassDiamList = new List<List<int>>();
                // И параллельно нам нужна сумма масс
                // Но сначала аккуратно нужно собрать всю информацию
                // posDiams[j] = [poz, diamStr, klassStr], sumLen[j], mass
                // В python-коде отдельно хранился mass, можно вычислить заново.

                // Соберем словарь <(klassStr), List<(diamInt, mass)>>
                Dictionary<string, List<Tuple<int, double>>> dictKlass = new Dictionary<string, List<Tuple<int, double>>>();

                for (int j = 0; j < posDiams.Count; j++)
                {
                    string kstr = posDiams[j][2];
                    int dIndex = -1;
                    for (int i = 0; i < diam.Length; i++)
                    {
                        if (Math.Abs(diam[i] - Convert.ToDouble(posDiams[j][1])) < 0.0001)
                        {
                            dIndex = i;
                            break;
                        }
                    }
                    if (dIndex < 0) continue;

                    double totalLenM = sumLen[j] / 1000.0;
                    double totalMass = ves[dIndex] * totalLenM;

                    int diamVal = (int)diam[dIndex];

                    if (!dictKlass.ContainsKey(kstr))
                        dictKlass[kstr] = new List<Tuple<int, double>>();

                    dictKlass[kstr].Add(Tuple.Create(diamVal, totalMass));
                }

                // Теперь для каждого класса объединим (суммируем) массу по каждому диаметру
                // Ключ: диаметр, Значение: суммарная масса
                Dictionary<string, Dictionary<int, double>> finalKlassMass = new Dictionary<string, Dictionary<int, double>>();

                foreach (var kvp in dictKlass)
                {
                    string kstr = kvp.Key;
                    finalKlassMass[kstr] = new Dictionary<int, double>();
                    foreach (var dmm in kvp.Value)
                    {
                        int dVal = dmm.Item1;
                        double mVal = dmm.Item2;
                        if (!finalKlassMass[kstr].ContainsKey(dVal))
                            finalKlassMass[kstr][dVal] = 0.0;

                        finalKlassMass[kstr][dVal] += mVal;
                    }
                }

                // Подсчитаем общее число (кг) на каждый класс
                // и общее количество разных диаметров
                double totalOverall = 0.0;
                List<string> sortedKlasses = new List<string>(finalKlassMass.Keys);
                sortedKlasses.Sort(); // сортируем по названию класса (AI, AIII, BI, BpI и т.д.)

                // Посчитаем, сколько всего столбцов нам нужно в ведомости
                // По логике python-кода: на каждый класс идет блок: [n диаметров] + 1 столбец "Итого"
                // В конце еще 2 столбца: "Всего", "Общий расход" (в python это было чуть иначе).
                // Для простоты можем не идеально воспроизводить, а сделать приблизительно.

                // Считаем общее кол-во диаметров во всех классах + кол-во "итого" столбцов.
                int diamCountAll = 0;
                foreach (var k in sortedKlasses)
                {
                    diamCountAll += finalKlassMass[k].Count;
                    // +1 на "итого" по классу
                }
                //  +2 на "Всего" и "Общий расход" — (или как-то так)
                int colCount = diamCountAll + sortedKlasses.Count + 1;

                using (Table table2 = new Table())
                {
                    table2.TableStyle = db.Tablestyle;
                    table2.SetSize(7, colCount);
                    table2.Position = basePoint2;
                    table2.SetRowHeight(10.0);
                    table2.SetColumnWidth(30.0);

                    // Заполним шапку
                    table2.Cells[0, 0].TextString = "Ведомость расхода стали, кг";
                    // Примерно как в python-коде
                    // (Расставим ячейки упрощенно)

                    table2.Cells[1, 0].TextString = "Марки элементов";
                    table2.Cells[1, 1].TextString = "Изделия арматурные";

                    // и т.д. Ниже можно проставить MergeCells при желании.

                    // Теперь последовательно заполняем диаметр/итоги
                    int currentCol = 1;
                    // Оставим ячейку [1,1] как есть, начнем вставлять классы/диаметры с [3, currentCol]
                    int rowDiamHeaders = 5;
                    int rowDiamValues = 6;

                    // Перебираем классы
                    foreach (var kstr in sortedKlasses)
                    {
                        // Считаем итого по данному классу
                        double sumForClass = 0.0;

                        // Возьмем словарь для класса
                        var diamDict = finalKlassMass[kstr];
                        // Отсортируем по возрастанию диаметра
                        List<int> sortedDiamList = new List<int>(diamDict.Keys);
                        sortedDiamList.Sort();

                        // Пишем заголовок класса (например, "AI", "AIII")
                        // В одну из строк (например, row=3). 
                        // Или сделаем так: table2.Cells[3, currentCol] = kstr;
                        table2.Cells[3, currentCol].TextString = kstr;

                        // Пытаемся найти ГОСТ
                        string gostText = klassGost.ContainsKey(kstr) ? $"ГОСТ {klassGost[kstr]}" : "ГОСТ ???";
                        table2.Cells[4, currentCol].TextString = gostText;

                        int startColForThisClass = currentCol;
                        foreach (int dVal in sortedDiamList)
                        {
                            double massVal = diamDict[dVal];
                            sumForClass += massVal;

                            // Диаметр пишем в rowDiamHeaders
                            currentCol++;
                            table2.Cells[rowDiamHeaders, currentCol].TextString = $"∅{dVal}";
                            table2.Cells[rowDiamValues, currentCol].TextString = massVal.ToString("0.##");
                        }

                        // Доп. столбец "Итого" по классу
                        currentCol++;
                        table2.Cells[rowDiamHeaders, currentCol].TextString = "Итого";
                        table2.Cells[rowDiamValues, currentCol].TextString = sumForClass.ToString("0.##");

                        totalOverall += sumForClass;

                        // можно сделать MergeCells над заголовками класса, если нужно
                        // table2.MergeCells(3,3, startColForThisClass, currentCol);

                    }

                    // В конце делаем столбец "Всего"
                    currentCol++;
                    table2.Cells[rowDiamHeaders, currentCol].TextString = "Всего";
                    table2.Cells[rowDiamValues, currentCol].TextString = totalOverall.ToString("0.##");

                    // Добавим таблицу в чертеж
                    btr.AppendEntity(table2);
                    tr.AddNewlyCreatedDBObject(table2, true);
                }

                tr.Commit();
            }

            ed.WriteMessage("\nСпецификация успешно создана.");
        }
    }
}
