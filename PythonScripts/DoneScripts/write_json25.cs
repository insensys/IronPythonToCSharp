using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using static System.Net.Mime.MediaTypeNames;

namespace AutoCadToJson
{
    public class write_json25
    {

        [CommandMethod("WRITEJSON")]
        public void WriteJsonData()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Запрос выбора объектов
            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "Выберите объекты для экспорта в JSON: ";
            PromptSelectionResult psr = ed.GetSelection(opts);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nОбъекты не выбраны или выбор отменён.");
                return;
            }

            // Открываем поток в файл d:\acad.json
            string filePath = @"d:\acad.json";
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Имя чертежа
                string docName = doc.Name.Replace('\\', '/');

                writer.Write("{");
                writer.Write("\"name\": \"" + docName + "\"");

                // Преобразуем имя чертежа в массив числовых кодов
                writer.Write(",\"name_ord\":[");
                for (int i = 0; i < docName.Length; i++)
                {
                    if (i > 0) writer.Write(",");
                    writer.Write(((int)docName[i]).ToString());
                }
                writer.Write("]");

                // Переходим к слоям
                writer.Write(",\"layers\":[");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Получаем список слоёв
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt == null)
                    {
                        writer.Write("]}");
                        writer.Close();
                        ed.WriteMessage("\nОшибка при чтении таблицы слоёв.");
                        return;
                    }

                    // Собираем имена всех слоёв
                    var layerNames = new System.Collections.Generic.List<string>();
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (ltr != null)
                            layerNames.Add(ltr.Name);
                    }

                    // Массив выбранных объектов
                    SelectionSet selSet = psr.Value;

                    // Перебираем каждый слой
                    for (int j = 0; j < layerNames.Count; j++)
                    {
                        if (j > 0) writer.Write(",");
                        writer.Write("\n{");
                        writer.Write("\"name\":\"" + layerNames[j] + "\"");

                        // Преобразуем имя слоя в массив числовых кодов
                        writer.Write(",\"name_ord\":[");
                        for (int c = 0; c < layerNames[j].Length; c++)
                        {
                            if (c > 0) writer.Write(",");
                            writer.Write(((int)layerNames[j][c]).ToString());
                        }
                        writer.Write("]");

                        // Точки (DBPoint)
                        writer.Write(",\"points\":[");
                        bool firstPoint = true;

                        // Линии (Line)
                        System.Collections.Generic.List<string> lineData = new System.Collections.Generic.List<string>();

                        // 3D Face (Face)
                        System.Collections.Generic.List<string> faceData = new System.Collections.Generic.List<string>();

                        // Тексты (DBText)
                        System.Collections.Generic.List<string> textData = new System.Collections.Generic.List<string>();

                        // Таблицы (Table)
                        System.Collections.Generic.List<string> tableData = new System.Collections.Generic.List<string>();

                        foreach (SelectedObject selObj in selSet)
                        {
                            if (selObj == null) continue;
                            Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            // Проверяем соответствие слоя
                            if (!ent.Layer.Equals(layerNames[j], StringComparison.OrdinalIgnoreCase))
                                continue;

                            // 1) Точки DBPoint
                            if (ent is DBPoint dbPoint)
                            {
                                if (!firstPoint) writer.Write(",");
                                writer.Write("[");
                                writer.Write(dbPoint.Position.X + "," + dbPoint.Position.Y + "," + dbPoint.Position.Z);
                                writer.Write(",\"" + ent.Handle.ToString() + "\"]\n");
                                firstPoint = false;
                            }
                            // 2) Линии
                            else if (ent is Line line)
                            {
                                StringBuilder sbLine = new StringBuilder();
                                sbLine.Append("[[");
                                sbLine.Append(line.StartPoint.X + "," + line.StartPoint.Y + "," + line.StartPoint.Z);
                                sbLine.Append("],[");
                                sbLine.Append(line.EndPoint.X + "," + line.EndPoint.Y + "," + line.EndPoint.Z);
                                sbLine.Append("],\"");
                                sbLine.Append(ent.Handle.ToString() + "\"]");
                                lineData.Add(sbLine.ToString());
                            }
                            // 3) 3D Face
                            else if (ent is Face face)
                            {
                                StringBuilder sbFace = new StringBuilder();
                                sbFace.Append("[");
                                // Четыре вершины
                                for (int v = 0; v < 4; v++)
                                {
                                    var vertex = face.GetVertexAt((short)v);
                                    if (v > 0) sbFace.Append(",");
                                    sbFace.Append("[");
                                    sbFace.Append(vertex.X + "," + vertex.Y + "," + vertex.Z);
                                    sbFace.Append("]");
                                }
                                sbFace.Append(",\"");
                                sbFace.Append(ent.Handle.ToString() + "\"]");
                                faceData.Add(sbFace.ToString());
                            }
                            // 4) Тексты (DBText)
                            else if (ent is DBText dbtext)
                            {
                                StringBuilder sbText = new StringBuilder();
                                sbText.Append("[");
                                sbText.Append(dbtext.Position.X + "," + dbtext.Position.Y + "," + dbtext.Position.Z);
                                sbText.Append(",\"");
                                sbText.Append(dbtext.TextString.Replace("\"", "\\\"")); // на случай кавычек
                                sbText.Append("\",[");
                                // Массив кодов символов
                                for (int k = 0; k < dbtext.TextString.Length; k++)
                                {
                                    if (k > 0) sbText.Append(",");
                                    sbText.Append(((int)dbtext.TextString[k]).ToString());
                                }
                                sbText.Append("],");
                                sbText.Append(dbtext.Height);
                                sbText.Append("]");
                                textData.Add(sbText.ToString());
                            }
                            // 5) Таблицы (Table)
                            else if (ent is Table acTable)
                            {
                                // Сериализация параметров таблицы
                                StringBuilder sbTable = new StringBuilder();
                                sbTable.Append("{\"NumRows\":");
                                sbTable.Append(acTable.NumRows);
                                sbTable.Append(",\"NumColumns\":");
                                sbTable.Append(acTable.NumColumns);
                                sbTable.Append(",\"Position\":[");
                                sbTable.Append(acTable.Position.X + "," + acTable.Position.Y + "," + acTable.Position.Z);
                                sbTable.Append("],\"Height\":");
                                sbTable.Append(acTable.Height);
                                sbTable.Append(",\"Width\":");
                                sbTable.Append(acTable.Width);
                                sbTable.Append(",\"Rotation\":");
                                sbTable.Append(acTable.Rotation);
                                sbTable.Append(",\"ScaleFactors\":[");
                                sbTable.Append(acTable.ScaleFactors.X + "," + acTable.ScaleFactors.Y + "," + acTable.ScaleFactors.Z);
                                sbTable.Append("],\"Handle\":\"");
                                sbTable.Append(ent.Handle.ToString());
                                sbTable.Append("\",\"data\":[");
                                // Читаем тексты ячеек
                                bool firstCell = true;
                                for (int r = 0; r < acTable.NumRows; r++)
                                {
                                    for (int c = 0; c < acTable.NumColumns; c++)
                                    {
                                        if (!firstCell) sbTable.Append(",");
                                        firstCell = false;
                                        string cellText = acTable.TextString(r, c);
                                        sbTable.Append("\"" + cellText.Replace("\"", "\\\"") + "\",[");
                                        // Коды символов
                                        for (int m = 0; m < cellText.Length; m++)
                                        {
                                            if (m > 0) sbTable.Append(",");
                                            sbTable.Append((int)cellText[m]);
                                        }
                                        sbTable.Append("]");
                                    }
                                }
                                sbTable.Append("]}");
                                tableData.Add(sbTable.ToString());
                            }
                        }

                        // Закрываем секцию points
                        writer.Write("]");

                        // Lines
                        writer.Write(",\"lines\":[");
                        for (int i = 0; i < lineData.Count; i++)
                        {
                            if (i > 0) writer.Write(",");
                            writer.Write("\n" + lineData[i]);
                        }
                        writer.Write("]");

                        // Faces
                        writer.Write(",\"faces\":[");
                        for (int i = 0; i < faceData.Count; i++)
                        {
                            if (i > 0) writer.Write(",");
                            writer.Write("\n" + faceData[i]);
                        }
                        writer.Write("]");

                        // Texts
                        writer.Write(",\"texts\":[");
                        for (int i = 0; i < textData.Count; i++)
                        {
                            if (i > 0) writer.Write(",");
                            writer.Write("\n" + textData[i]);
                        }
                        writer.Write("]");

                        // Tables
                        writer.Write(",\"tables\":[");
                        for (int i = 0; i < tableData.Count; i++)
                        {
                            if (i > 0) writer.Write(",");
                            writer.Write("\n" + tableData[i]);
                        }
                        writer.Write("]");

                        // Завершаем объект слоя
                        writer.Write("}");
                    } // Конец цикла по слоям

                    tr.Commit();
                } // using Transaction

                // Закрываем JSON
                writer.Write("]}");
            } // using StreamWriter

            ed.WriteMessage("\nЭкспорт JSON выполнен. Файл: " + filePath);
        }
    }    
}
