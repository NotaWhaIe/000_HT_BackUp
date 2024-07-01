using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Strana.Revit.HoleTask.RevitCommands
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Test : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            if (DateTime.Now.Minute % 5 == 0)
            {
                ShowCompliment();
            }
            
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Проверка марок перемычек, окон и витражей");

                // Проверяем, существуют ли элементы категории Structural Framing на виде
                bool hasStructuralFraming = new FilteredElementCollector(doc, doc.ActiveView.Id)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Any();
                
                IEnumerable<Element> filteredElements;
                
                if (!hasStructuralFraming)
                {
                    filteredElements = new FilteredElementCollector(doc)
                        .WherePasses(new ElementMulticlassFilter(new List<Type> { typeof(Wall), typeof(FamilyInstance) }))
                        .Where(e => e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows ||
                                    (e is Wall w && w.WallType.Kind == WallKind.Curtain));
                }
                else
                {
                    filteredElements = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .WhereElementIsNotElementType();
                }

                OverrideGraphicSettings clearSettings = new OverrideGraphicSettings(); // Сброс настроек

                foreach (Element elem in filteredElements)
                {
                    var tags = new FilteredElementCollector(doc, doc.ActiveView.Id)
                        .OfClass(typeof(IndependentTag))
                        .Cast<IndependentTag>()
                        .Where(t => t.TaggedLocalElementId == elem.Id);

                    Parameter param = DetermineParameter(doc, elem);

                    if (tags.Any() && param != null && !string.IsNullOrWhiteSpace(param.AsString()))
                    {
                        doc.ActiveView.SetElementOverrides(elem.Id, clearSettings); // Сброс переопределения графики
                    }
                    else if (!tags.Any())
                    {
                        OverrideGraphicSettings colorSettings = DetermineColorSettings(elem);
                        doc.ActiveView.SetElementOverrides(elem.Id, colorSettings); // Применение цвета
                    }
                    else if (param == null || string.IsNullOrWhiteSpace(param.AsString()))
                    {
                        OverrideGraphicSettings colorSettings = DetermineColorSettings(elem);
                        doc.ActiveView.SetElementOverrides(elem.Id, colorSettings); // Применение цвета при неустановленном параметре
                    }
                }

                tx.Commit();
                return Result.Succeeded;
            }
        }

        private void ShowCompliment()
        {
            string[] compliments = new string[]
            {
                "Сегодня ты настоящий мастер Revit!",
                "Твои проекты вдохновляют!",
                "Отличный день для архитектурных шедевров!",
                "Твои идеи изменят мир!",
                "Ты превращаешь пиксели в магию!",
                "Твоё внимание к деталям просто невероятно!",
                "Каждая линия, которую ты рисуешь, создаёт историю.",
                "Архитектура начинается, когда ты берёшь в руки Revit.",
                "Твой проект выглядит лучше, чем кофе по утрам!",
                "Это видение! Твоя работа вдохновляет коллег.",
                 "Ты строишь мечты. Продолжай в том же духе!",
                "Ты настоящий волшебник в мире Revit!",
                "Твоё мастерство делает мир красивее!",
                "Твори дальше, архитектурный гений!",
                 "Ты создаёшь не просто здания, ты создаёшь искусство!",
                "Твоя работа в Revit столь же впечатляюща, как и твоя креативность!",
                "Ты помогаешь миру видеть не только здания, но и их душу!",
                "Твой дизайн вдохновляет новые поколения архитекторов!",
                "Твой вклад в архитектуру делает этот мир лучше!",
                "Ты и Revit — идеальная команда!",
                "Ты воплощаешь мечты в жизнь с каждым новым проектом!",
                "Твои проекты говорят сами за себя — ты гений!",
                "Твои здания будто оживают на экране!",
                "Твой стиль уникален и неповторим, как ты сам!",
                "Ты мастер света и тени в каждом проекте!",
                "Твои решения удивительно элегантны и функциональны.",
                "Ты делаешь сложное простым и изящным!",
                "Ты даришь людям не просто пространство, а мечту о будущем.",
                "Твои проекты — это поэзия в мире архитектуры.",
                "Ты несешь красоту в этот мир с каждым своим чертежом.",
                "Твой подход к проектированию изменяет архитектурные традиции.",
                "Ты делаешь мир лучше, один проект за раз.",
                "Твои работы вдохновляют на создание чего-то великолепного.",
                "Ты всегда найдешь решение, каким бы сложным оно ни казалось.",
                "Ты превосходно владеешь искусством создания пространства.",
                "Ты умеешь думать настолько креативно, что это поражает.",
                "Твой талант приносит радость и удовлетворение людям.",
                "Каждый твой проект — это шедевр.",
                "Ты настоящий инноватор в архитектуре.",
                "Твои здания словно говорят: 'Добро пожаловать домой'.",
                "Твой творческий подход заставляет каждый угол сверкать новизной.",
                "Ты как волшебник, превращающий бетон и стекло в живую историю.",
                "Ты всегда на шаг впереди в архитектурных тенденциях."         
            };

            Random random = new Random();
            string compliment = compliments[random.Next(compliments.Length)];
            TaskDialog.Show("Комплимент", compliment);
        }

        private Parameter DetermineParameter(Document doc, Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
            {
                return elem.LookupParameter("ADSK_Марка");
            }
            else if (elem is Wall wall && wall.WallType.Kind == WallKind.Curtain)
            {
                return wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            }
            else if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows)
            {
                return doc.GetElement(elem.GetTypeId())?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK);
            }
            return null;
        }

        private OverrideGraphicSettings DetermineColorSettings(Element elem)
        {
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
            {
                settings.SetProjectionLineColor(new Color(255, 0, 255)); // Пурпурный
                settings.SetCutLineColor(new Color(255, 0, 255));
            }
            else
            {
                settings.SetProjectionLineColor(new Color(255, 0, 0)); // Красный
                settings.SetCutLineColor(new Color(255, 0, 0));
            }
            return settings;
        }
    }
}
