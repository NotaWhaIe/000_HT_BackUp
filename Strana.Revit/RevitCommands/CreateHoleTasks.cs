using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;

using FirstRevitPlugin.FailuresProcessing;

using Strana.Revit.HoleTask.ElementCollections;
using Strana.Revit.HoleTask.Extension.RevitElement;
using Strana.Revit.HoleTask.Extensions;
using Strana.Revit.HoleTask.Extensions.RevitElement;
using Strana.Revit.HoleTask.FailuresProcessing;
using Strana.Revit.HoleTask.Utils;
using Strana.Revit.HoleTask.ViewModel;

using static Strana.Revit.HoleTask.Extensions.RevitElement.HoleTasksGetter;

namespace Strana.Revit.HoleTask.RevitCommands
{
    [Transaction(TransactionMode.Manual)]
    public class CreateHoleTasks : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            MepElementSelector.UIDocument = uidoc;
            Document doc = uidoc.Document;
            docsaver.doc = doc;
            string userName = commandData.Application.Application.Username.ToString();
            GlobalParameters.UserName = userName;

            HoleTaskView taskView = new(doc);
            taskView.ShowDialog();
            
            CollectFamilyInstances.Instance.AddToListFamilyInstances(doc,
                "(Отв_Задание)_Стены_Прямоугольное", "(Отв_Задание)_Перекрытия_Прямоугольное");
            CollectFamilyInstances.Instance.AddToListLevels(doc);

            IEnumerable<FamilyInstance> intersectionRectangularWall = new List<FamilyInstance>(CollectFamilyInstances.Instance.FamilyInstance1);
            IEnumerable<FamilyInstance> intersectionRectangularFloor = new List<FamilyInstance>(CollectFamilyInstances.Instance.FamilyInstance2);

            GlobalParameters.ExistingTaskWall = intersectionRectangularWall;
            GlobalParameters.ExistingTaskFloor = intersectionRectangularFloor;
            GlobalParameters.OldTasksWall = intersectionRectangularWall.Count().ToString();
            GlobalParameters.OldTasksFloor = intersectionRectangularFloor.Count().ToString();

            if (taskView.ShouldExecuteProgram)
            {
                using (var gt = new TransactionGroup(doc, "HoleTasks"))
                {
                    gt.Start();
                    
                    SabFamilyInstenceCollections.GetFamilyInstenceCollections(doc);

                    var viewModel = taskView.DataContext as HoleTaskViewModel;
                    if (viewModel != null)
                    {
                        var selectedLinkNames = viewModel.GetSelectedLinks()
                            .Select(link =>
                            {
                                var index = link.Name.LastIndexOf('.');
                                return index == -1 ? link.Name : link.Name.Substring(0, index);
                            })
                            .ToList();
                        
                        var linkInstances = new FilteredElementCollector(doc)
                            .OfClass(typeof(RevitLinkInstance))
                            .Cast<RevitLinkInstance>();

                        foreach (RevitLinkInstance linkInstance in linkInstances)
                        {
                            Document linkDoc = linkInstance.GetLinkDocument();
                            if (linkDoc != null && selectedLinkNames.Contains(linkDoc.Title))
                            {
                                linkInstance.CreateHoleTasksByCurrentLink();
                            }
                        }
                    }
                     
                    CollectFamilyInstances.Instance.ClearDataFamilyInstance();
                    CollectFamilyInstances.Instance.AddToListFamilyInstances(doc,
                        "(Отв_Задание)_Стены_Прямоугольное", "(Отв_Задание)_Перекрытия_Прямоугольное");
                    IEnumerable<FamilyInstance> intersectionWallRectangularCombineList01 = new List<FamilyInstance>(CollectFamilyInstances.Instance.FamilyInstance1);
                    IEnumerable<FamilyInstance> intersectionFloorRectangularCombineList02 = new List<FamilyInstance>(CollectFamilyInstances.Instance.FamilyInstance2);

                    new HoleTasksLineStretch().StretchLinesAllHoleTask(doc);

                    double createdTasksWall = intersectionWallRectangularCombineList01.Count() - intersectionRectangularWall.Count();
                    GlobalParameters.СreatedTasksWall = (createdTasksWall > 0 ? createdTasksWall : 0).ToString();
                    double createdTasksFloor = intersectionFloorRectangularCombineList02.Count() - intersectionRectangularFloor.Count();
                    GlobalParameters.СreatedTasksFloor = (createdTasksFloor > 0 ? createdTasksFloor : 0).ToString();
                    double deletedTasks = -((createdTasksFloor < 0 ? createdTasksFloor : 0) + (createdTasksWall < 0 ? createdTasksWall : 0));
                    GlobalParameters.DeletedTasks = deletedTasks.ToString();

                    gt.Assimilate();
                }
            }

            stopwatch.Stop();
            var taskStatistics = new TaskStatistics();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00} мин. {1:00} сек.", ts.Minutes, ts.Seconds);
            taskStatistics.ShowTaskStatistics(elapsedTime);

            CollectFamilyInstances.Instance.ClearDataFamilyInstance();
            GlobalParameters.ResetParameters();

            return Result.Succeeded;
        }
        public static List<ElementId> GetFamilyInstanceIds(List<FamilyInstance> rectangularCombineList)
        {
            List<ElementId> sortedIds = rectangularCombineList
                .Select(instance => instance.Id)
                .OrderBy(id => id.IntegerValue)
                .ToList();

            return sortedIds;
        }
    }
    
    public static class docsaver
    {
        public static Document doc { get; set; }
    }
}