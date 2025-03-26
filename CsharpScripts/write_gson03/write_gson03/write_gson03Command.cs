using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Text;

namespace Ewrite_gson03
{
    public class write_gson03Command
    {
        [CommandMethod("ExportToGson")]
        public static void ExportToGsonMethod()
        {
            // Текущий документ, редактор и база данных
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Шаг 1. Запрос выбора объектов
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "Выберите объекты для экспорта в JSON:";
            PromptSelectionResult res = ed.GetSelection(opts);
            if (res.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nОтмена или ошибка выбора объектов.");
                return;
            }

            // Шаг 2. Открываем (создаём) JSON-файл
            string outputPath = @"D:\acadgson.json";
            using (StreamWriter sw = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                // Имя документа, заменим слэши для совместимости
                string docName = doc.Name.Replace('\\', '/');

                // Начинаем формировать JSON-структуру
                sw.WriteLine("{");
                sw.WriteLine($"  \"name\": \"{docName}\",");

                // Записываем name_ord (ASCII коды имени файла)
                sw.Write("  \"name_ord\": [");
                for (int i = 0; i < docName.Length; i++)
                {
                    if (i > 0) sw.Write(",");
                    sw.Write(((int)docName[i]).ToString());
                }
                sw.WriteLine("],");

                sw.WriteLine("  \"layers\": [");

                // Шаг 3. Соберём все слои
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Список ID выбранных объектов
                    ObjectId[] selectedIds = res.Value.GetObjectIds();

                    // Получим таблицу слоёв
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    bool firstLayer = true;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                        string layerName = layerRecord.Name;

                        // Начинаем описание слоя в JSON
                        if (!firstLayer) sw.WriteLine("    ,");
                        firstLayer = false;

                        sw.WriteLine("    {");
                        sw.WriteLine($"      \"name\": \"{layerName}\",");
                        sw.Write("      \"name_ord\": [");
                        for (int i = 0; i < layerName.Length; i++)
                        {
                            if (i > 0) sw.Write(",");
                            sw.Write(((int)layerName[i]).ToString());
                        }
                        sw.WriteLine("],");

                        // Шаг 4. Для каждого слоя собираем разные типы объектов:
                        // Точки, Линии, 3D-лица, Тексты, Таблицы
                        // Для удобства разберём по под-массивам

                        // 4.1 Точки
                        sw.WriteLine("      \"points\": [");
                        bool firstPoint = true;
                        foreach (ObjectId id in selectedIds)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            // Проверяем, что это DBPoint и что слой совпадает
                            if (ent is DBPoint pointEnt && pointEnt.Layer == layerName)
                            {
                                if (!firstPoint) sw.Write(",");
                                firstPoint = false;

                                // Позиция точки
                                var pos = pointEnt.Position;
                                sw.WriteLine();
                                sw.Write("        {");
                                sw.Write($"\"position\":[{pos.X},{pos.Y},{pos.Z}],");
                                sw.Write($"\"handle\":\"{ent.Handle}\"");
                                sw.Write("}");
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("      ],");

                        // 4.2 Линии
                        sw.WriteLine("      \"lines\": [");
                        bool firstLine = true;
                        foreach (ObjectId id in selectedIds)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            if (ent is Line lineEnt && lineEnt.Layer == layerName)
                            {
                                if (!firstLine) sw.Write(",");
                                firstLine = false;

                                sw.WriteLine();
                                sw.Write("        {");
                                // Координаты начала и конца
                                var sp = lineEnt.StartPoint;
                                var ep = lineEnt.EndPoint;
                                sw.Write($"\"start\":[{sp.X},{sp.Y},{sp.Z}],");
                                sw.Write($"\"finish\":[{ep.X},{ep.Y},{ep.Z}],");
                                sw.Write($"\"handle\":\"{ent.Handle}\"");
                                sw.Write("}");
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("      ],");

                        // 4.3 3D-лица (Face)
                        sw.WriteLine("      \"faces\": [");
                        bool firstFace = true;
                        foreach (ObjectId id in selectedIds)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            if (ent is Face faceEnt && faceEnt.Layer == layerName)
                            {
                                if (!firstFace) sw.Write(",");
                                firstFace = false;

                                sw.WriteLine();
                                sw.Write("        {\"vertices\":[");
                                // faceEnt.GetVertexAt(i) - 4 вершины (0..3)
                                for (int iVert = 0; iVert < 4; iVert++)
                                {
                                    var v = faceEnt.GetVertexAt((short)iVert);
                                    if (iVert > 0) sw.Write(",");
                                    sw.Write($"[{v.X},{v.Y},{v.Z}]");
                                }
                                sw.Write("],");
                                sw.Write($"\"handle\":\"{ent.Handle}\"");
                                sw.Write("}");
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("      ],");

                        // 4.4 Тексты (DBText)
                        sw.WriteLine("      \"texts\": [");
                        bool firstText = true;
                        foreach (ObjectId id in selectedIds)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            if (ent is DBText textEnt && textEnt.Layer == layerName)
                            {
                                if (!firstText) sw.Write(",");
                                firstText = false;

                                var pos = textEnt.Position;
                                string txt = textEnt.TextString;
                                double height = textEnt.Height;

                                sw.WriteLine();
                                sw.Write("        [");
                                sw.Write($"{pos.X},{pos.Y},{pos.Z},");
                                sw.Write($"\"{txt}\",[");
                                // Символьные коды текста
                                for (int i = 0; i < txt.Length; i++)
                                {
                                    if (i > 0) sw.Write(",");
                                    sw.Write(((int)txt[i]).ToString());
                                }
                                sw.Write($"],{height}");
                                sw.Write("]");
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("      ],");

                        // 4.5 Таблицы (Table)
                        sw.WriteLine("      \"tables\": [");
                        bool firstTable = true;
                        foreach (ObjectId id in selectedIds)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            if (ent is Table tableEnt && tableEnt.Layer == layerName)
                            {
                                if (!firstTable) sw.Write(",");
                                firstTable = false;

                                // Сериализуем информацию о таблице
                                sw.WriteLine();
                                sw.Write("        {");
                                sw.Write($"\"NumRows\":{tableEnt.NumRows},");
                                sw.Write($"\"NumColumns\":{tableEnt.NumColumns},");
                                var pos = tableEnt.Position;
                                sw.Write($"\"Position\":[{pos.X},{pos.Y},{pos.Z}],");
                                sw.Write($"\"Height\":{tableEnt.Height},");
                                sw.Write($"\"Width\":{tableEnt.Width},");
                                sw.Write($"\"Rotation\":{tableEnt.Rotation},");
                                var scale = tableEnt.ScaleFactors;
                                sw.Write($"\"ScaleFactors\":[{scale.X},{scale.Y},{scale.Z}],");
                                sw.Write($"\"Handle\":\"{ent.Handle}\"");
                                sw.Write(",\"data\":[");

                                // Перебираем ячейки таблицы
                                bool firstCell = true;
                                for (int r = 0; r < tableEnt.NumRows; r++)
                                {
                                    for (int c = 0; c < tableEnt.NumColumns; c++)
                                    {
                                        if (!firstCell) sw.Write(",");
                                        firstCell = false;

                                        string cellText = tableEnt.TextString(r, c);
                                        sw.Write($"\"{cellText}\",[");
                                        for (int k = 0; k < cellText.Length; k++)
                                        {
                                            if (k > 0) sw.Write(",");
                                            sw.Write(((int)cellText[k]).ToString());
                                        }
                                        sw.Write("]");
                                    }
                                }
                                sw.Write("]}");
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("      ]");

                        // Завершение описания слоя
                        sw.Write("    }");
                    }

                    tr.Commit();
                }

                // Закрываем массив "layers"
                sw.WriteLine();
                sw.WriteLine("  ]");
                // Закрываем корневой объект
                sw.WriteLine("}");
            }

            ed.WriteMessage($"\nФайл {outputPath} успешно записан.");
        }
    }
}
