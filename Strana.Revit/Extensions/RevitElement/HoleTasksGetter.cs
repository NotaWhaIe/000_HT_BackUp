using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// В данном классе изменить код, передав this.doc во все методы
namespace Strana.Revit.HoleTask.Extensions.RevitElement
{
    public class HoleTasksGetter
    {
        private readonly Document doc;

        public HoleTasksGetter(Document doc)
        {
            this.doc = doc;
        }

        
        public static void FilterFamilyInstancesToList(IEnumerable<FamilyInstance> collector, string familyName,
            List<FamilyInstance> list, string parameterName, string parameterValue)
        {
            foreach (FamilyInstance fi in collector)
            {
                if (fi.Name == familyName && fi.SuperComponent == null)
                {
                    Parameter param = fi.LookupParameter(parameterName);
                    if (param != null && param.StorageType == StorageType.String && param.AsString() == parameterValue)
                    {
                        var subComponentIds = fi.GetSubComponentIds();
                        if (!subComponentIds.Any())
                        {
                            list.Add(fi);
                        }
                    }
                }
            }
        }
        public class CollectFamilyInstances
        {
            private static CollectFamilyInstances _instance;
            private IEnumerable<FamilyInstance> _list0 = Enumerable.Empty<FamilyInstance>();
            private IEnumerable<FamilyInstance> _list1 = Enumerable.Empty<FamilyInstance>();
            private IEnumerable<FamilyInstance> _list2 = Enumerable.Empty<FamilyInstance>();
            private IEnumerable<Level> _list3 = Enumerable.Empty<Level>();
            private IEnumerable<Grid> _list4 = Enumerable.Empty<Grid>();

            public static CollectFamilyInstances Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new CollectFamilyInstances();
                    }
                    return _instance;
                }
            }

            public IEnumerable<FamilyInstance> FamilyInstance0 => _list0;
            public IEnumerable<FamilyInstance> FamilyInstance1 => _list1;
            public IEnumerable<FamilyInstance> FamilyInstance2 => _list2;
            public IEnumerable<Level> Level => _list3;
            public IEnumerable<Grid> Grid => _list4;

            private CollectFamilyInstances() { }

            public void AddToListFamilyInstances(Document doc, string familyName1, string familyName2)
            {
                List<FamilyInstance> temporaryList0 = new List<FamilyInstance>();
                List<FamilyInstance> temporaryList1 = new List<FamilyInstance>();
                List<FamilyInstance> temporaryList2 = new List<FamilyInstance>();

                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>();

                foreach (FamilyInstance fi in collector)
                {
                    temporaryList0.Add(fi);
                    if (fi.SuperComponent == null && !fi.GetSubComponentIds().Any())
                    {
                        if (fi.Symbol.FamilyName == familyName1)
                        {
                            temporaryList1.Add(fi);
                        }
                        else if (fi.Symbol.FamilyName == familyName2)
                        {
                            temporaryList2.Add(fi);
                        }
                    }
                }

                _list0 = temporaryList0;
                _list1 = temporaryList1;
                _list2 = temporaryList2;
            }

            public void AddToListLevels(Document doc)
            {
                List<Level> temporaryList = new List<Level>();
                temporaryList.AddRange(new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation));

                _list3 = temporaryList;
            }

            public void AddToListGrids(Document doc)
            {
                List<Grid> temporaryList = new List<Grid>();
                temporaryList.AddRange(new FilteredElementCollector(doc)
                    .OfClass(typeof(Grid))
                    .Cast<Grid>());

                _list4 = temporaryList;
            }
             
            public void ClearDataFamilyInstance()
            {
                _list0 = Enumerable.Empty<FamilyInstance>();
                _list1 = Enumerable.Empty<FamilyInstance>();
                _list2 = Enumerable.Empty<FamilyInstance>();
            }

            public void ClearDataLevel()
            {
                _list3 = Enumerable.Empty<Level>();
            }

            public void ClearDataGrid()
            {
                _list4 = Enumerable.Empty<Grid>();
            }
        }
    }
}
