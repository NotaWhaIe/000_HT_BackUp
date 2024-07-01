using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FirstRevitPlugin.FailuresProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Strana.Revit.HoleTask.Utils;
using System.Windows.Media.Media3D;
using System.Windows;
using static Strana.Revit.HoleTask.Extensions.RevitElement.HoleTasksGetter;

namespace Strana.Revit.HoleTask.ElementCollections
{
    public class SabFamilyInstenceCollections
    {
        public static void GetFamilyInstenceCollections(Document doc)
        {
            var nestedFamilyNames = new List<string>
            {
                "(Отв_Задание)_Стены_Прямоугольное",
                "(Отв_Задание)_Перекрытия_Прямоугольное"
            };

            using (Transaction trans = new Transaction(doc, "Копирование вложенных семейств с параметрами"))
            {
                TransactionHandler.SetWarningResolver(trans);
                trans.Start();

                IEnumerable<Level> levels = new List<Level>(CollectFamilyInstances.Instance.Level);
                IEnumerable<FamilyInstance> allInstances = new List<FamilyInstance>(CollectFamilyInstances.Instance.FamilyInstance0);

                foreach (var instance in allInstances)
                {
                    var dependentIds = instance.GetSubComponentIds();
                    foreach (ElementId id in dependentIds)
                    {
                        FamilyInstance nestedInstance = doc.GetElement(id) as FamilyInstance;
                        if (nestedInstance != null && nestedFamilyNames.Contains(nestedInstance.Symbol.FamilyName))
                        {
                            LocationPoint locationPoint = nestedInstance.Location as LocationPoint;

                            if (locationPoint != null)
                            {
                                Level chosenLevel = ChooseLevel(levels, locationPoint.Point.Z);

                                if (chosenLevel != null)
                                {
                                    double offset = locationPoint.Point.Z - chosenLevel.Elevation;


                                    if (CanPlaceFamilyInstanceAtLocation(allInstances, locationPoint.Point, nestedFamilyNames))
                                    {
                                        FamilyInstance newInstance = CreateFamilyInstanceWithLevel(doc, nestedInstance, locationPoint.Point, chosenLevel, offset);
                                        CopyParameters(nestedInstance, newInstance);

                                        double thickness = nestedInstance.LookupParameter("Ширина").AsDouble();
                                        double width = nestedInstance.LookupParameter("Глубина").AsDouble();

                                        HoleTaskGridDelta delta = GridRoundUpDimension.DeltaHoleTaskToGrids(doc, locationPoint.Point, thickness, width, 0);
                                        double O1 = UnitUtils.ConvertToInternalUnits(delta.DeltaGridNumber, UnitTypeId.Millimeters);
                                        double Oa = UnitUtils.ConvertToInternalUnits(delta.deltaGridLetter, UnitTypeId.Millimeters);
                                        HoleTaskCreator.MoveFamilyInstance(newInstance, O1, "X");
                                        ///сдвинуть семейство по оси фУ в верх, от оси и А
                                        HoleTaskCreator.MoveFamilyInstance(newInstance, Oa, "Y");

                                        newInstance.LookupParameter(":Назначение отверстия")?.Set(nestedInstance.LookupParameter(":Назначение отверстия").AsString());
                                        newInstance.LookupParameter(":Примечание")?.Set(nestedInstance.LookupParameter(":Примечание").AsString());
                                        newInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(GlobalParameters.Date);
                                        newInstance.LookupParameter("SD_Версия задания")?.Set(GlobalParameters.UserName);
                                        GlobalParameters.SetScriptCreationMethod(newInstance);
                                    }
                                }
                            }
                        }
                    }
                }
                trans.Commit();
            }

        }
        public static Level ChooseLevel(IEnumerable<Level> levels, double zPoint)
        {
            Level closestLevelBelow = levels.LastOrDefault(l => l.Elevation < zPoint);
            Level closestLevelAbove = levels.FirstOrDefault(l => l.Elevation > zPoint);
            return closestLevelBelow ?? closestLevelAbove;
        }

        public static FamilyInstance CreateFamilyInstanceWithLevel(Document doc, FamilyInstance originalInstance, XYZ point, Level level, double offset)
        {
            FamilySymbol symbol = originalInstance.Symbol;
            FamilyInstance newInstance = doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural);
            Parameter elevationParam = newInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
            if (elevationParam != null && elevationParam.IsReadOnly == false)
            {
                elevationParam.Set(offset);
            }
            return newInstance;
        }

        public static bool CanPlaceFamilyInstanceAtLocation(IEnumerable<FamilyInstance> familyInstances, XYZ location, List<string> familyNames)
        {
            const double tolerance = 0.01; // Небольшой допуск для сравнения координат

            // Перебираем все экземпляры семейств
            foreach (FamilyInstance fi in familyInstances)
            {
                XYZ existingLocation = (fi.Location as LocationPoint)?.Point;

                // Проверяем, находится ли экземпляр семейства в заданных координатах с учетом допуска
                if (existingLocation != null && existingLocation.IsAlmostEqualTo(location, tolerance))
                {
                    // Проверяем, соответствует ли имя семейства заданным именам и является ли семейство родительским
                    if (familyNames.Contains(fi.Symbol.Family.Name) && fi.SuperComponent == null)
                    {
                        // Проверяем, имеет ли семейство вложенные компоненты
                        var subComponentIds = fi.GetSubComponentIds();
                        if (!subComponentIds.Any())
                        {
                            // Если у семейства нет вложенных компонентов, возвращаем false, 
                            // т.к. семейство уже существует в заданных координатах
                            return false;
                        }
                    }
                }
            }

            // Если в указанных координатах нет семейства с заданным именем и без вложенных компонентов, возвращаем true
            return true;
        }

        public static void CopyParameters(FamilyInstance originalInstance, FamilyInstance newInstance)
        {
            var parameterNames = new List<string> { "Глубина", "Ширина", "Высота" };

            foreach (string paramName in parameterNames)
            {
                Parameter originalParam = originalInstance.LookupParameter(paramName);
                Parameter newParam = newInstance.LookupParameter(paramName);

                // Проверяем, существуют ли параметры и не является ли параметр нового экземпляра только для чтения
                if (originalParam != null && newParam != null && !newParam.IsReadOnly)
                {
                    // Копируем значение из оригинального экземпляра в новый
                    double value = originalParam.AsDouble();
                    newParam.Set(value);
                }
            }
        }
    }
}

