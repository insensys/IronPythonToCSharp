using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System;

namespace ArmLayers2
{
    public class ArmLayersCommand
    {
        [CommandMethod("ArmLayers02")]
        public static void RunArmLayers02()
        {
            // Получаем текущий документ и базовые объекты
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Открываем транзакцию
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Получаем таблицу слоёв
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                // Перебираем все записи (слои) в таблице
                foreach (ObjectId layerId in layerTable)
                {
                    // Получаем слой для чтения
                    LayerTableRecord layerRec = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);

                    // Смотрим имя слоя
                    string layerName = layerRec.Name;
                    ed.WriteMessage($"\nСлой: {layerName}");

                    // Разбиваем имя на части по символу "_"
                    string[] parts = layerName.Split('_');
                    if (parts.Length == 3)
                    {
                        ed.WriteMessage($"\nЧасти имени: {string.Join(",", parts)}");

                        // Проверяем, что первая часть ровно 3 символа
                        if (parts[0].Length == 3)
                        {
                            // Пробуем считать вторую часть как число (диаметр)
                            if (int.TryParse(parts[1], out int diam))
                            {
                                ed.WriteMessage($"\nДиаметр: {diam}");

                                // Переводим слой в режим на запись и меняем цвет
                                layerRec.UpgradeOpen();
                                layerRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 3);
                            }
                        }
                    }

                    ed.WriteMessage("\n");
                }

                // Фиксируем изменения
                tr.Commit();
            }
        }
    }


}
