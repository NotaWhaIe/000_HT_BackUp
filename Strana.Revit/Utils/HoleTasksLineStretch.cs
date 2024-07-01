using Autodesk.Revit.DB;

using FirstRevitPlugin.FailuresProcessing;

using Strana.Revit.HoleTask.Extensions.RevitElement;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Strana.Revit.HoleTask.Extensions.RevitElement.HoleTasksGetter;

namespace Strana.Revit.HoleTask.Utils
{
    internal class HoleTasksLineStretch
    {
        internal void StretchLinesAllHoleTask(Document doc)
        {
            using (var t = new Transaction(doc, "Stretch Lines All Hole Task"))
            {
                TransactionHandler.SetWarningResolver(t);
                t.Start();

                List<FamilyInstance> intersectionWallRectangularCombineList = new List<FamilyInstance>(
                    CollectFamilyInstances.Instance.FamilyInstance1);

                foreach (var holeTask in intersectionWallRectangularCombineList)
                {
                    Location loc = holeTask.Location;
                    if (loc is LocationPoint locPoint && locPoint.Point != null)
                    {
                        XYZ intersectionCurveCenter = locPoint.Point;

                        double upperLevelElevation = ElevationOfNearestUpperLevel(doc, intersectionCurveCenter);
                        if (upperLevelElevation != -1)
                        {
                            holeTask.LookupParameter("Отметка этажа над заданием").Set(upperLevelElevation );
                        }

                        double lowerLevelElevation = ElevationOfNearestLowerLevel(doc, intersectionCurveCenter);
                        if (lowerLevelElevation != -1)
                        {
                            holeTask.LookupParameter("Отметка этажа под заданием").Set(lowerLevelElevation);
                        }

                        double zeroLevelElevation = DistanceFromZeroElevationLevelToFamilyInstance(doc,
                            intersectionCurveCenter);
                        if (zeroLevelElevation != -1)
                        {
                            if (holeTask?.LookupParameter("ADSK_Отверстие_Отметка от нуля") is { IsReadOnly: false }
                            parameter) parameter.Set(zeroLevelElevation);
                        }
                    }
                }
                t.Commit();
            }

        }
        public static double ElevationOfNearestUpperLevel(Document doc, XYZ point)
        {
            //var levels = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Level))
            //    .Cast<Level>()
            //    .OrderBy(l => l.Elevation)
            //    .ToList();

            IEnumerable<Level> levels = new List<Level>(CollectFamilyInstances.Instance.Level);


            foreach (var level in levels)
            {
                if (level.Elevation > point.Z)
                {
                    return level.Elevation;
                }
            }
            return -1; // Возвращаем -1, если ближайший верхний уровень не найден
        }
        public static double ElevationOfNearestLowerLevel(Document doc, XYZ point)
        {
            //var levels = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Level))
            //    .Cast<Level>()
            //    .OrderByDescending(l => l.Elevation)
            //    .ToList();

            IEnumerable<Level> levels = new List<Level>(CollectFamilyInstances.Instance.Level);

            foreach (var level in levels)
            {
                if (level.Elevation < point.Z)
                {
                    return level.Elevation;
                }
            }

            return -1; // Возвращаем -1, если ближайший нижний уровень не найден
        }
        public static double DistanceFromZeroElevationLevelToFamilyInstance(Document doc, XYZ intersectionCurveCenter)
        {
            //var levels = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Level))
            //    .Cast<Level>();
            IEnumerable<Level> levels = new List<Level>(CollectFamilyInstances.Instance.Level);
            
            // Ищем уровень с отметкой, равной нулю
            Level zeroElevationLevel = levels.FirstOrDefault(level =>
                Math.Abs(level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble()) < 0.001);

            if (zeroElevationLevel != null)
            {
                // Вычисляем расстояние от нулевого уровня до заданной точки
                return intersectionCurveCenter.Z - zeroElevationLevel.Elevation;
            }
            else
            {
                // Возвращаем -1, если уровень с нулевой отметкой не найден
                return -1;
            }
        }


    }
}
