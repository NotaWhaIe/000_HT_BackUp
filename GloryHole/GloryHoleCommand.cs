﻿/*
 * Copyright (c) <2023> <Misharev Evgeny>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
 *    in the documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the <organization> nor the names of its contributors may be used to endorse or promote products derived 
 *    from this software without specific prior written permission.
 * 4. Redistributions are not allowed to be sold, in whole or in part, for any compensation of any kind.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Contact: <citrusbim@gmail.com> or <https://web.telegram.org/k/#@MisharevEvgeny>
 */
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GloryHole
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GloryHoleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            //Получение доступа к Selection
            Selection sel = commandData.Application.ActiveUIDocument.Selection; 
            //Общие параметры размеров
            Guid intersectionPointWidthGuid = new Guid("8f2e4f93-9472-4941-a65d-0ac468fd6a5d");
            Guid intersectionPointHeightGuid = new Guid("da753fe3-ecfa-465b-9a2c-02f55d0c2ff1");
            Guid intersectionPointThicknessGuid = new Guid("293f055d-6939-4611-87b7-9a50d0c1f50e");
            Guid intersectionPointDiameterGuid = new Guid("9b679ab7-ea2e-49ce-90ab-0549d5aa36ff");

            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            List<Level> docLvlList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .ToList();

            List<RevitLinkInstance> revitLinkInstanceList = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            List<FamilySymbol> intersectionFamilySymbolList = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilySymbol>()
                .Where(fs => fs.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased)
                .ToList();

            GloryHoleWPF gloryHoleWPF = new GloryHoleWPF(revitLinkInstanceList, intersectionFamilySymbolList);
            gloryHoleWPF.ShowDialog();
            if (gloryHoleWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            List<RevitLinkInstance> selectedRevitLinkInstance = gloryHoleWPF.SelectedRevitLinkInstances;
            if (selectedRevitLinkInstance.Count() == 0)
            {
                TaskDialog.Show("Ravit", "Связанный файл не найден!");
                return Result.Cancelled;
            }

            FamilySymbol intersectionWallRectangularFamilySymbol = gloryHoleWPF.IntersectionWallRectangularFamilySymbol;
            if(intersectionWallRectangularFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для прямоугольного отверстия в стене! Загрузите семейство \"Пересечение_Стена_Прямоугольное\"!");
                return Result.Cancelled;
            }
            FamilySymbol intersectionWallRoundFamilySymbol = gloryHoleWPF.IntersectionWallRoundFamilySymbol;
            if (intersectionWallRoundFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для круглого отверстия в стене! Загрузите семейство \"Пересечение_Стена_Круглое\"!");
                return Result.Cancelled;
            }
            FamilySymbol intersectionFloorRectangularFamilySymbol = gloryHoleWPF.IntersectionFloorRectangularFamilySymbol;
            if (intersectionFloorRectangularFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для прямоугольного отверстия в плите! Загрузите семейство \"Пересечение_Плита_Прямоугольное\"!");
                return Result.Cancelled;
            }
            FamilySymbol intersectionFloorRoundFamilySymbol = gloryHoleWPF.IntersectionFloorRoundFamilySymbol;
            if (intersectionFloorRoundFamilySymbol == null)
            {
                TaskDialog.Show("Ravit", "Не найден тип для круглого отверстия в плите! Загрузите семейство \"Пересечение_Плита_Круглое\"!");
                return Result.Cancelled;
            }

            double pipeSideClearance = gloryHoleWPF.PipeSideClearance * 2 / 304.8;
            double pipeTopBottomClearance = gloryHoleWPF.PipeTopBottomClearance * 2 / 304.8;
            double ductSideClearance = gloryHoleWPF.DuctSideClearance * 2 / 304.8;
            double ductTopBottomClearance = gloryHoleWPF.DuctTopBottomClearance * 2 / 304.8;
            double cableTraySideClearance = gloryHoleWPF.CableTraySideClearance * 2 / 304.8;
            double cableTrayTopBottomClearance = gloryHoleWPF.CableTrayTopBottomClearance * 2 / 304.8;

            string holeShapeButtonName = gloryHoleWPF.HoleShapeButtonName;
            string roundHolesPositionButtonName = gloryHoleWPF.RoundHolesPositionButtonName;
            double roundHoleSizesUpIncrement = gloryHoleWPF.RoundHoleSizesUpIncrement;
            double roundHolePosition = gloryHoleWPF.RoundHolePositionIncrement;
            double additionalToThickness = 20 / 304.8;
            bool combineHoles = gloryHoleWPF.CombineHoles;

            //Получение трубопроводов, воздуховодов и кабельных лотков
            List<Pipe> pipesList = new List<Pipe>();
            List<Duct> ductsList = new List<Duct>();
            List<CableTray> cableTrayList = new List<CableTray>();
            //Выбор трубы, воздуховода или кабельного лотка
            PipeDuctCableTraySelectionFilter pipeDuctCableTraySelectionFilter = new PipeDuctCableTraySelectionFilter();
            IList<Reference> pipeDuctRefList = null;
            try
            {
                pipeDuctRefList = sel.PickObjects(ObjectType.Element, pipeDuctCableTraySelectionFilter, "Выберите трубу, воздуховод или кабельный лоток!");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            foreach (Reference refElem in pipeDuctRefList)
            {
                if (doc.GetElement(refElem).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_PipeCurves))
                {
                    pipesList.Add((doc.GetElement(refElem) as Pipe));
                }
                else if (doc.GetElement(refElem).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_DuctCurves))
                {
                    ductsList.Add((doc.GetElement(refElem)) as Duct);
                }
                else if (doc.GetElement(refElem).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_CableTray))
                {
                    cableTrayList.Add((doc.GetElement(refElem)) as CableTray);
                }
            }

            List<FamilyInstance> intersectionWallRectangularList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionWallRectangularCombineList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionWallRoundList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionFloorRectangularCombineList = new List<FamilyInstance>();


            using (TransactionGroup tg = new TransactionGroup(doc))
            {
                tg.Start("Задание на отверстия");
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Создание болванок");
                    ActivateFamilySymbols(intersectionWallRectangularFamilySymbol
                        , intersectionWallRoundFamilySymbol
                        , intersectionFloorRectangularFamilySymbol
                        , intersectionFloorRoundFamilySymbol);
                    foreach (RevitLinkInstance linkInst in selectedRevitLinkInstance)
                    {
                        Options opt = new Options();
                        opt.ComputeReferences = true;
                        opt.DetailLevel = ViewDetailLevel.Fine;
                        Document linkDoc = linkInst.GetLinkDocument();
                        Transform transform = linkInst.GetTotalTransform();

                        //Получение стен из связанного файла
                        List<Wall> wallsInLinkList = new FilteredElementCollector(linkDoc)
                            .OfCategory(BuiltInCategory.OST_Walls)
                            .OfClass(typeof(Wall))
                            .WhereElementIsNotElementType()
                            .Cast<Wall>()
                            .Where(w => w.CurtainGrid == null)
                            .ToList();
                        //Получение перекрытий из связанного файла
                        List<Floor> floorsInLinkList = new FilteredElementCollector(linkDoc)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .OfClass(typeof(Floor))
                            .WhereElementIsNotElementType()
                            .Cast<Floor>()                           
                            .ToList();

                        //Обработка стен
                        foreach (Wall wall in wallsInLinkList)
                        {
                            Level lvl = GetClosestBottomWallLevel(docLvlList, linkDoc, wall);
                            GeometryElement geomElem = wall.get_Geometry(opt);
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                Solid geomSolid = geomObj as Solid;
                                if (null != geomSolid)
                                {
                                    Solid transformGeomSolid = SolidUtils.CreateTransformed(geomSolid, transform);
                                    foreach (Pipe pipe in pipesList)
                                    {
                                        Curve pipeCurve = (pipe.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(pipeCurve, scio);
                                        if (intersection.SegmentCount > 0)
                                        {
                                            if (holeShapeButtonName == "radioButton_HoleShapeRectangular")
                                            {
                                                XYZ wallOrientation = wall.Orientation;
                                                double pipeDiameter = Math.Round(pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble(), 6);
                                                double intersectionPointHeight = RoundUpToIncrement(pipeDiameter + pipeTopBottomClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);
                                                double a = Math.Round((wallOrientation.AngleTo((pipeCurve as Line).Direction)) * (180 / Math.PI), 6);

                                                if (a > 90 && a < 180)
                                                {
                                                    a = (180 - a) * (Math.PI / 180);
                                                }
                                                else
                                                {
                                                    a = a * (Math.PI / 180);
                                                }
                                                double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                                double delta2 = Math.Abs((pipeDiameter / 2) / Math.Cos(a));
                                                if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                                XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2) - (intersectionPointHeight / 2) * XYZ.BasisZ;

                                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                                {
                                                    originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                                }
                                                else
                                                {
                                                    originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                                }

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                     , intersectionWallRectangularFamilySymbol
                                                     , lvl
                                                     , StructuralType.NonStructural) as FamilyInstance;
                                                if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                                {
                                                    Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                    double rotationAngle = 0;
                                                    if (Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                                                    {
                                                        rotationAngle = -wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    else
                                                    {
                                                        rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                                }

                                                double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + pipeSideClearance, roundHoleSizesUpIncrement);
                                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                                intersectionWallRectangularList.Add(intersectionPoint);
                                                intersectionWallRectangularCombineList.Add(intersectionPoint);
                                            }
                                            else
                                            {
                                                XYZ wallOrientation = wall.Orientation;
                                                double pipeDiameter = Math.Round(pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble(), 6);
                                                double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);
                                                double a = Math.Round((wallOrientation.AngleTo((pipeCurve as Line).Direction)) * (180 / Math.PI), 6);

                                                if (a > 90 && a < 180)
                                                {
                                                    a = (180 - a) * (Math.PI / 180);
                                                }
                                                else
                                                {
                                                    a = a * (Math.PI / 180);
                                                }
                                                double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                                double delta2 = Math.Abs((pipeDiameter / 2) / Math.Cos(a));
                                                if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                                XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2);

                                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                                {
                                                    originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                                }
                                                else
                                                {
                                                    originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                                }

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                     , intersectionWallRoundFamilySymbol
                                                     , lvl
                                                     , StructuralType.NonStructural) as FamilyInstance;
                                                if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                                {
                                                    Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                    double rotationAngle = 0;
                                                    if (Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                                                    {
                                                        rotationAngle = -wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    else
                                                    {
                                                        rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                                }

                                                double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + pipeSideClearance, roundHoleSizesUpIncrement);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                                intersectionPoint.get_Parameter(intersectionPointDiameterGuid).Set(intersectionPointWidth);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                                intersectionWallRoundList.Add(intersectionPoint);
                                            }
                                        }
                                    }

                                    foreach (Duct duct in ductsList)
                                    {
                                        Curve ductCurve = (duct.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(ductCurve, scio);
                                        if (intersection.SegmentCount > 0)
                                        {
                                            XYZ wallOrientation = wall.Orientation;
                                            if (duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM) != null)
                                            {
                                                if (holeShapeButtonName == "radioButton_HoleShapeRectangular")
                                                {
                                                    double ductDiameter = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble(), 6);

                                                    double intersectionPointHeight = RoundUpToIncrement(ductDiameter + ductTopBottomClearance, roundHoleSizesUpIncrement);
                                                    double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);

                                                    double a = Math.Round((wallOrientation.AngleTo((ductCurve as Line).Direction)) * (180 / Math.PI), 6);

                                                    if (a > 90 && a < 180)
                                                    {
                                                        a = (180 - a) * (Math.PI / 180);
                                                    }
                                                    else
                                                    {
                                                        a = a * (Math.PI / 180);
                                                    }
                                                    double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                                    double delta2 = Math.Abs((ductDiameter / 2) / Math.Cos(a));
                                                    if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                                    XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                    XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                    XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2) - (intersectionPointHeight / 2) * XYZ.BasisZ;

                                                    if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                                    {
                                                        originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                                    }
                                                    else
                                                    {
                                                        originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                                    }

                                                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                        , intersectionWallRectangularFamilySymbol
                                                        , lvl
                                                        , StructuralType.NonStructural) as FamilyInstance;
                                                    if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                                    {
                                                        Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                        double rotationAngle = 0;
                                                        if (Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                                                        {
                                                            rotationAngle = -wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                        }
                                                        else
                                                        {
                                                            rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                        }
                                                        ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                                    }

                                                    double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + ductSideClearance, roundHoleSizesUpIncrement);
                                                    intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                                    intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);

                                                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                    intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                                    intersectionWallRectangularList.Add(intersectionPoint);
                                                    intersectionWallRectangularCombineList.Add(intersectionPoint);
                                                }
                                                else
                                                {
                                                    double ductDiameter = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble(), 6);
                                                    double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);
                                                    double a = Math.Round((wallOrientation.AngleTo((ductCurve as Line).Direction)) * (180 / Math.PI), 6);

                                                    if (a > 90 && a < 180)
                                                    {
                                                        a = (180 - a) * (Math.PI / 180);
                                                    }
                                                    else
                                                    {
                                                        a = a * (Math.PI / 180);
                                                    }
                                                    double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                                    double delta2 = Math.Abs((ductDiameter / 2) / Math.Cos(a));
                                                    if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                                    XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                    XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                    XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2);

                                                    if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                                    {
                                                        originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                                    }
                                                    else
                                                    {
                                                        originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                                    }

                                                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                        , intersectionWallRoundFamilySymbol
                                                        , lvl
                                                        , StructuralType.NonStructural) as FamilyInstance;
                                                    if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                                    {
                                                        Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                        double rotationAngle = 0;
                                                        if (Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                                                        {
                                                            rotationAngle = -wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                        }
                                                        else
                                                        {
                                                            rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                        }
                                                        ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                                    }

                                                    double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + ductSideClearance, roundHoleSizesUpIncrement);
                                                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                                    intersectionPoint.get_Parameter(intersectionPointDiameterGuid).Set(intersectionPointWidth);

                                                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                    intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                                    intersectionWallRoundList.Add(intersectionPoint);
                                                }

                                            }
                                            else
                                            {
                                                double ductHeight = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble(), 6);
                                                double ductWidth = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble(), 6);
                                                double intersectionPointHeight = RoundUpToIncrement(ductHeight + ductTopBottomClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);

                                                double a = Math.Round((wallOrientation.AngleTo((ductCurve as Line).Direction)) * (180 / Math.PI), 6);

                                                if (a > 90 && a < 180)
                                                {
                                                    a = (180 - a) * (Math.PI / 180);
                                                }
                                                else
                                                {
                                                    a = a * (Math.PI / 180);
                                                }

                                                double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                                double delta2 = Math.Abs((ductWidth / 2) / Math.Cos(a));
                                                if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                                XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2) - (intersectionPointHeight / 2) * XYZ.BasisZ;

                                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                                {
                                                    originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                                }
                                                else
                                                {
                                                    originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                                }

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                    , intersectionWallRectangularFamilySymbol
                                                    , lvl
                                                    , StructuralType.NonStructural) as FamilyInstance;
                                                if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                                {
                                                    Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                    double rotationAngle = 0;
                                                    if(Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ),6) * (180 / Math.PI) < 180)
                                                    {
                                                        rotationAngle = - wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    else
                                                    {
                                                        rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                    }
                                                    ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                                }

                                                double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + ductSideClearance, roundHoleSizesUpIncrement);
                                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                                intersectionWallRectangularList.Add(intersectionPoint);
                                                intersectionWallRectangularCombineList.Add(intersectionPoint);
                                            }
                                        }
                                    }

                                    foreach (CableTray cableTray in cableTrayList)
                                    {
                                        Curve cableTrayCurve = (cableTray.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(cableTrayCurve, scio);
                                        if (intersection.SegmentCount > 0)
                                        {
                                            XYZ wallOrientation = wall.Orientation;
                                            double cableTrayHeight = Math.Round(cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble(), 6);
                                            double cableTrayWidth = Math.Round(cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble(), 6);
                                            double intersectionPointHeight = RoundUpToIncrement(cableTrayHeight + cableTrayTopBottomClearance, roundHoleSizesUpIncrement);
                                            double intersectionPointThickness = RoundUpToIncrement(wall.Width + additionalToThickness, 10);

                                            double a = Math.Round((wallOrientation.AngleTo((cableTrayCurve as Line).Direction)) * (180 / Math.PI), 6);
                                            if (a > 90 && a < 180)
                                            {
                                                a = (180 - a) * (Math.PI / 180);
                                            }
                                            else
                                            {
                                                a = a * (Math.PI / 180);
                                            }

                                            double delta1 = Math.Abs((wall.Width / 2) * Math.Tan(a));
                                            double delta2 = Math.Abs((cableTrayWidth / 2) / Math.Cos(a));
                                            if (delta1 >= 9.84251968504 || delta2 >= 9.84251968504) continue;

                                            XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                            XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                            XYZ originIntersectionCurve = ((intersectionCurveStartPoint + intersectionCurveEndPoint) / 2) - (intersectionPointHeight / 2) * XYZ.BasisZ;

                                            if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                            {
                                                originIntersectionCurve = new XYZ(RoundToIncrement(originIntersectionCurve.X, roundHolePosition), RoundToIncrement(originIntersectionCurve.Y, roundHolePosition), RoundToIncrement(originIntersectionCurve.Z, roundHolePosition) - lvl.Elevation);
                                            }
                                            else
                                            {
                                                originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation);
                                            }

                                            FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                , intersectionWallRectangularFamilySymbol
                                                , lvl
                                                , StructuralType.NonStructural) as FamilyInstance;
                                            if (Math.Round(wallOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                            {
                                                Line rotationLine = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                double rotationAngle = 0;
                                                if (Math.Round(wallOrientation.AngleOnPlaneTo(intersectionPoint.FacingOrientation, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                                                {
                                                    rotationAngle = -wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                }
                                                else
                                                {
                                                    rotationAngle = wallOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                                }
                                                ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, rotationAngle);
                                            }

                                            double intersectionPointWidth = RoundUpToIncrement(delta1 * 2 + delta2 * 2 + cableTraySideClearance, roundHoleSizesUpIncrement);
                                            intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                            intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                                            intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);

                                            intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                            intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(originIntersectionCurve.Z);
                                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(originIntersectionCurve.Z);
                                            intersectionWallRectangularList.Add(intersectionPoint);
                                            intersectionWallRectangularCombineList.Add(intersectionPoint);
                                        }
                                    }
                                }
                            }
                        }

                        //Обработка перекрытий
                        foreach (Floor floor in floorsInLinkList)
                        {
                            GeometryElement geomElem = floor.get_Geometry(opt);
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                Solid geomSolid = geomObj as Solid;
                                if (null != geomSolid)
                                {
                                    Solid transformGeomSolid = SolidUtils.CreateTransformed(geomSolid, transform);
                                    foreach (Pipe pipe in pipesList)
                                    {
                                        Curve pipeCurve = (pipe.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(pipeCurve, scio);

                                        if (intersection.SegmentCount > 0)
                                        {
                                            if (holeShapeButtonName == "radioButton_HoleShapeRectangular")
                                            {
                                                double pipeDiameter = Math.Round(pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble(), 6);
                                                double intersectionPointHeight = RoundUpToIncrement(pipeDiameter + pipeTopBottomClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointWidth = RoundUpToIncrement(pipeDiameter + pipeSideClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);

                                                XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                XYZ originIntersectionCurve = null;

                                                if (intersectionCurveStartPoint.Z > intersectionCurveEndPoint.Z)
                                                {
                                                    originIntersectionCurve = intersectionCurveStartPoint;
                                                }
                                                else
                                                {
                                                    originIntersectionCurve = intersectionCurveEndPoint;
                                                }

                                                Level lvl = GetClosestFloorLevel(docLvlList, linkDoc, floor);
                                                originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation + 50 / 304.8);

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                , intersectionFloorRectangularFamilySymbol
                                                , lvl
                                                , StructuralType.NonStructural) as FamilyInstance;
                                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                                intersectionFloorRectangularCombineList.Add(intersectionPoint);
                                            }
                                            else
                                            {
                                                double pipeDiameter = Math.Round(pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble(), 6);
                                                double intersectionPointWidth = RoundUpToIncrement(pipeDiameter + pipeSideClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);

                                                XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                                XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                                XYZ originIntersectionCurve = null;

                                                if (intersectionCurveStartPoint.Z > intersectionCurveEndPoint.Z)
                                                {
                                                    originIntersectionCurve = intersectionCurveStartPoint;
                                                }
                                                else
                                                {
                                                    originIntersectionCurve = intersectionCurveEndPoint;
                                                }

                                                Level lvl = GetClosestFloorLevel(docLvlList, linkDoc, floor);
                                                originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation + 50 / 304.8);

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                , intersectionFloorRoundFamilySymbol
                                                , lvl
                                                , StructuralType.NonStructural) as FamilyInstance;
                                                intersectionPoint.get_Parameter(intersectionPointDiameterGuid).Set(intersectionPointWidth);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                            }

                                        }
                                    }

                                    foreach (Duct duct      in ductsList)
                                    {
                                        Curve ductCurve = (duct.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(ductCurve, scio);

                                        if (intersection.SegmentCount > 0)
                                        {
                                            XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                            XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                            XYZ originIntersectionCurve = null;

                                            if (intersectionCurveStartPoint.Z > intersectionCurveEndPoint.Z)
                                            {
                                                originIntersectionCurve = intersectionCurveStartPoint;
                                            }
                                            else
                                            {
                                                originIntersectionCurve = intersectionCurveEndPoint;
                                            }

                                            Level lvl = GetClosestFloorLevel(docLvlList, linkDoc, floor);
                                            originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation + 50 / 304.8);

                                            if (duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM) != null)
                                            {
                                                if (holeShapeButtonName == "radioButton_HoleShapeRectangular")
                                                {
                                                    double ductDiameter = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble(), 6);
                                                    double intersectionPointHeight = RoundUpToIncrement(ductDiameter + ductTopBottomClearance, roundHoleSizesUpIncrement);
                                                    double intersectionPointWidth = RoundUpToIncrement(ductDiameter + ductSideClearance, roundHoleSizesUpIncrement);
                                                    double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);

                                                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                        , intersectionFloorRectangularFamilySymbol
                                                        , lvl
                                                        , StructuralType.NonStructural) as FamilyInstance;
                                                    intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                                    intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                                    intersectionFloorRectangularCombineList.Add(intersectionPoint);
                                                }
                                                else
                                                {
                                                    double ductDiameter = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble(), 6);
                                                    double intersectionPointWidth = RoundUpToIncrement(ductDiameter + ductSideClearance, roundHoleSizesUpIncrement);
                                                    double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);

                                                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                        , intersectionFloorRoundFamilySymbol
                                                        , lvl
                                                        , StructuralType.NonStructural) as FamilyInstance;
                                                    intersectionPoint.get_Parameter(intersectionPointDiameterGuid).Set(intersectionPointWidth);
                                                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                                }
                                            }
                                            else
                                            {
                                                double ductHeight = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble(), 6);
                                                double ductWidth = Math.Round(duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble(), 6);
                                                double intersectionPointHeight = RoundUpToIncrement(ductHeight + ductTopBottomClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointWidth = RoundUpToIncrement(ductWidth + ductSideClearance, roundHoleSizesUpIncrement);
                                                double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);
                                                double ductRotationAngle = GetAngleFromMEPCurve(duct as MEPCurve);

                                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                    , intersectionFloorRectangularFamilySymbol
                                                    , lvl
                                                    , StructuralType.NonStructural) as FamilyInstance;
                                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                                intersectionFloorRectangularCombineList.Add(intersectionPoint);

                                                if (ductRotationAngle != 0)
                                                {
                                                    double xxx = Math.Round(ductRotationAngle, 6) * (180 / Math.PI);
                                                    //double rotationAngle = 0;
                                                    //if (Math.Round(ductRotationAngle, 6) * (180 / Math.PI) < 180)
                                                    //{
                                                    //    rotationAngle = ductRotationAngle;
                                                    //}
                                                    //else
                                                    //{
                                                    //    rotationAngle = -ductRotationAngle;
                                                    //}

                                                    Line rotationAxis = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                    ElementTransformUtils.RotateElement(doc , intersectionPoint.Id , rotationAxis , ductRotationAngle);
                                                }
                                            }
                                        }
                                    }

                                    foreach (CableTray cableTray in cableTrayList)
                                    {
                                        Curve cableTrayCurve = (cableTray.Location as LocationCurve).Curve;
                                        SolidCurveIntersectionOptions scio = new SolidCurveIntersectionOptions();
                                        SolidCurveIntersection intersection = transformGeomSolid.IntersectWithCurve(cableTrayCurve, scio);

                                        if (intersection.SegmentCount > 0)
                                        {
                                            XYZ intersectionCurveStartPoint = intersection.GetCurveSegment(0).GetEndPoint(0);
                                            XYZ intersectionCurveEndPoint = intersection.GetCurveSegment(0).GetEndPoint(1);
                                            XYZ originIntersectionCurve = null;

                                            if (intersectionCurveStartPoint.Z > intersectionCurveEndPoint.Z)
                                            {
                                                originIntersectionCurve = intersectionCurveStartPoint;
                                            }
                                            else
                                            {
                                                originIntersectionCurve = intersectionCurveEndPoint;
                                            }

                                            Level lvl = GetClosestFloorLevel(docLvlList, linkDoc, floor);
                                            originIntersectionCurve = new XYZ(originIntersectionCurve.X, originIntersectionCurve.Y, originIntersectionCurve.Z - lvl.Elevation + 50 / 304.8);

                                            double cableTrayHeight = Math.Round(cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble(), 6);
                                            double cableTrayWidth = Math.Round(cableTray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble(), 6);
                                            double intersectionPointHeight = RoundUpToIncrement(cableTrayHeight + cableTrayTopBottomClearance, roundHoleSizesUpIncrement);
                                            double intersectionPointWidth = RoundUpToIncrement(cableTrayWidth + cableTraySideClearance, roundHoleSizesUpIncrement);
                                            double intersectionPointThickness = RoundToIncrement(floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() + 100 / 304.8, 10);
                                            double cableTrayRotationAngle = GetAngleFromMEPCurve(cableTray as MEPCurve);

                                            FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(originIntersectionCurve
                                                , intersectionFloorRectangularFamilySymbol
                                                , lvl
                                                , StructuralType.NonStructural) as FamilyInstance;
                                            intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                            intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                            intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                            intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);
                                            intersectionFloorRectangularCombineList.Add(intersectionPoint);

                                            if (cableTrayRotationAngle != 0)
                                            {
                                                Line rotationAxis = Line.CreateBound(originIntersectionCurve, originIntersectionCurve + 1 * XYZ.BasisZ);
                                                ElementTransformUtils.RotateElement(doc
                                                    , intersectionPoint.Id
                                                    , rotationAxis
                                                    , cableTrayRotationAngle);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    t.Commit();
                }

                //Объединение отверстий в многослойной стене
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Объединение соостных отверстий");
                    while (intersectionWallRectangularList.Count != 0)
                    {
                        List<FamilyInstance> intersectionWallRectangularForGruppingList = new List<FamilyInstance>();
                        intersectionWallRectangularForGruppingList.Add(intersectionWallRectangularList[0]);
                        intersectionWallRectangularList.RemoveAt(0);

                        List<FamilyInstance> tmpIntersectionWallRectangularList = intersectionWallRectangularList.ToList();
                        for (int i = 0; i < intersectionWallRectangularForGruppingList.Count; i++)
                        {
                            FamilyInstance firstIntersectionPoint = intersectionWallRectangularForGruppingList[i];
                            Curve firstIntersectionPointCurve = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, firstIntersectionPoint);
                            for (int j = 0; j < tmpIntersectionWallRectangularList.Count; j++)
                            {
                                FamilyInstance secondIntersectionPoint = tmpIntersectionWallRectangularList[j];
                                Curve secondIntersectionPointCurve = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, secondIntersectionPoint);
                                SetComparisonResult intersectResult = firstIntersectionPointCurve.Intersect(secondIntersectionPointCurve);
                                if (intersectResult == SetComparisonResult.Equal)
                                {
                                    if (firstIntersectionPoint.get_Parameter(intersectionPointWidthGuid).AsDouble()
                                        == secondIntersectionPoint.get_Parameter(intersectionPointWidthGuid).AsDouble() &&
                                        firstIntersectionPoint.get_Parameter(intersectionPointHeightGuid).AsDouble()
                                        == secondIntersectionPoint.get_Parameter(intersectionPointHeightGuid).AsDouble())
                                    {
                                        intersectionWallRectangularForGruppingList.Add(secondIntersectionPoint);
                                        tmpIntersectionWallRectangularList.Remove(secondIntersectionPoint);
                                        i = 0;
                                        j = 0;
                                    }
                                }
                            }
                        }

                        if (intersectionWallRectangularForGruppingList.Count > 1)
                        {
                            FamilyInstance p1 = null;
                            FamilyInstance p2 = null;
                            double distance = -10000000;
                            foreach (FamilyInstance point1 in intersectionWallRectangularForGruppingList)
                            {
                                foreach (FamilyInstance point2 in intersectionWallRectangularForGruppingList)
                                {
                                    double tmpDistance = (point1.Location as LocationPoint).Point.DistanceTo((point2.Location as LocationPoint).Point);
                                    if (point1 != point2 && distance < tmpDistance)
                                    {
                                        distance = tmpDistance;
                                        p1 = point1;
                                        p2 = point2;
                                    }
                                }
                            }

                            XYZ centerPoint = ((p1.Location as LocationPoint).Point + (p2.Location as LocationPoint).Point) / 2;
                            XYZ newP1 = null;
                            XYZ newP2 = null;

                            Curve curveP1 = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, p1);
                            Curve curveP2 = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, p2);
                            if (curveP1.GetEndPoint(0).DistanceTo(centerPoint) > curveP1.GetEndPoint(1).DistanceTo(centerPoint))
                            {
                                newP1 = curveP1.GetEndPoint(0);
                            }
                            else newP1 = curveP1.GetEndPoint(1);

                            if (curveP2.GetEndPoint(0).DistanceTo(centerPoint) > curveP2.GetEndPoint(1).DistanceTo(centerPoint))
                            {
                                newP2 = curveP2.GetEndPoint(0);
                            }
                            else newP2 = curveP2.GetEndPoint(1);

                            XYZ newCenterPoint = (newP1 + newP2) / 2;

                            FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                                , intersectionWallRectangularFamilySymbol
                                , doc.GetElement(p1.LevelId) as Level
                                , StructuralType.NonStructural) as FamilyInstance;

                            if (Math.Round(p1.FacingOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                            {
                                Line rotationLine = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                                ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, p1.FacingOrientation.AngleTo(intersectionPoint.FacingOrientation));
                            }

                            intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(p1.get_Parameter(intersectionPointHeightGuid).AsDouble());
                            double intersectionPointThickness = RoundUpToIncrement(newP1.DistanceTo(newP2), 10);
                            intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                            intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(p1.get_Parameter(intersectionPointWidthGuid).AsDouble());

                            intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set(p1.get_Parameter(heightOfBaseLevelGuid).AsDouble());
                            intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(p1.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(p1.get_Parameter(levelOffsetGuid).AsDouble());

                            foreach (FamilyInstance forDel in intersectionWallRectangularForGruppingList)
                            {
                                doc.Delete(forDel.Id);
                                intersectionWallRectangularList.Remove(forDel);
                                intersectionWallRectangularCombineList.Remove(forDel);
                            }
                            intersectionWallRectangularCombineList.Add(intersectionPoint);
                        }
                        else
                        {
                            intersectionWallRectangularList.Remove(intersectionWallRectangularForGruppingList[0]);
                        }
                    }

                    while (intersectionWallRoundList.Count != 0)
                    {
                        List<FamilyInstance> intersectionWallRoundForGruppingList = new List<FamilyInstance>();
                        intersectionWallRoundForGruppingList.Add(intersectionWallRoundList[0]);
                        intersectionWallRoundList.RemoveAt(0);

                        List<FamilyInstance> tmpIntersectionWallRoundList = intersectionWallRoundList.ToList();
                        for (int i = 0; i < intersectionWallRoundForGruppingList.Count; i++)
                        {
                            FamilyInstance firstIntersectionPoint = intersectionWallRoundForGruppingList[i];
                            Curve firstIntersectionPointCurve = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, firstIntersectionPoint);
                            for (int j = 0; j < tmpIntersectionWallRoundList.Count; j++)
                            {
                                FamilyInstance secondIntersectionPoint = tmpIntersectionWallRoundList[j];
                                Curve secondIntersectionPointCurve = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, secondIntersectionPoint);
                                SetComparisonResult intersectResult = firstIntersectionPointCurve.Intersect(secondIntersectionPointCurve);
                                if (intersectResult == SetComparisonResult.Equal)
                                {
                                    if (firstIntersectionPoint.get_Parameter(intersectionPointDiameterGuid).AsDouble()
                                        == secondIntersectionPoint.get_Parameter(intersectionPointDiameterGuid).AsDouble())
                                    {
                                        intersectionWallRoundForGruppingList.Add(secondIntersectionPoint);
                                        tmpIntersectionWallRoundList.Remove(secondIntersectionPoint);
                                        i = 0;
                                        j = 0;
                                    }
                                }
                            }
                        }

                        if (intersectionWallRoundForGruppingList.Count > 1)
                        {
                            FamilyInstance p1 = null;
                            FamilyInstance p2 = null;
                            double distance = -10000000;
                            foreach (FamilyInstance point1 in intersectionWallRoundForGruppingList)
                            {
                                foreach (FamilyInstance point2 in intersectionWallRoundForGruppingList)
                                {
                                    double tmpDistance = (point1.Location as LocationPoint).Point.DistanceTo((point2.Location as LocationPoint).Point);
                                    if (point1 != point2 && distance < tmpDistance)
                                    {
                                        distance = tmpDistance;
                                        p1 = point1;
                                        p2 = point2;
                                    }
                                }
                            }

                            XYZ centerPoint = ((p1.Location as LocationPoint).Point + (p2.Location as LocationPoint).Point) / 2;
                            XYZ newP1 = null;
                            XYZ newP2 = null;

                            Curve curveP1 = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, p1);
                            Curve curveP2 = GetCurveFromIntersectionPoint(intersectionPointThicknessGuid, p2);
                            if (curveP1.GetEndPoint(0).DistanceTo(centerPoint) > curveP1.GetEndPoint(1).DistanceTo(centerPoint))
                            {
                                newP1 = curveP1.GetEndPoint(0);
                            }
                            else newP1 = curveP1.GetEndPoint(1);

                            if (curveP2.GetEndPoint(0).DistanceTo(centerPoint) > curveP2.GetEndPoint(1).DistanceTo(centerPoint))
                            {
                                newP2 = curveP2.GetEndPoint(0);
                            }
                            else newP2 = curveP2.GetEndPoint(1);

                            XYZ newCenterPoint = (newP1 + newP2) / 2;

                            FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                                , intersectionWallRoundFamilySymbol
                                , doc.GetElement(p1.LevelId) as Level
                                , StructuralType.NonStructural) as FamilyInstance;

                            if (Math.Round(p1.FacingOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                            {
                                Line rotationLine = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                                ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, p1.FacingOrientation.AngleTo(intersectionPoint.FacingOrientation));
                            }

                            double intersectionPointThickness = RoundUpToIncrement(newP1.DistanceTo(newP2), 10);
                            intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);
                            intersectionPoint.get_Parameter(intersectionPointDiameterGuid).Set(p1.get_Parameter(intersectionPointDiameterGuid).AsDouble());

                            intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set(p1.get_Parameter(heightOfBaseLevelGuid).AsDouble());
                            intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(p1.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble());
                            intersectionPoint.get_Parameter(levelOffsetGuid).Set(p1.get_Parameter(levelOffsetGuid).AsDouble());

                            foreach (FamilyInstance forDel in intersectionWallRoundForGruppingList)
                            {
                                doc.Delete(forDel.Id);
                                intersectionWallRoundList.Remove(forDel);
                            }
                        }
                        else
                        {
                            intersectionWallRoundList.Remove(intersectionWallRoundForGruppingList[0]);
                        }
                    }
                    t.Commit();
                }

                //Объединение пересекающихся прямоугольных отверстий
                if (combineHoles)
                {
                    using (Transaction t = new Transaction(doc))
                    {
                        t.Start("Объединение пересекающихся отверстий");

                        Options opt = new Options();
                        opt.ComputeReferences = true;
                        opt.DetailLevel = ViewDetailLevel.Fine;
                        
                        while (intersectionWallRectangularCombineList.Count != 0)
                        {
                            List<FamilyInstance> intersectionWallRectangularSolidIntersectCombineList = new List<FamilyInstance>
                            {
                                intersectionWallRectangularCombineList[0]
                            };

                            intersectionWallRectangularCombineList.RemoveAt(0);

                            List<FamilyInstance> tmpIntersectionWallRectangularSolidIntersectCombineList = intersectionWallRectangularCombineList.ToList();
                            for (int i = 0; i < intersectionWallRectangularSolidIntersectCombineList.Count; i++)
                            {
                                FamilyInstance firstIntersectionPoint = intersectionWallRectangularSolidIntersectCombineList[i];
                                Solid firstIntersectionPointSolid = null;
                                GeometryElement firstIntersectionPointGeomElem = firstIntersectionPoint.get_Geometry(opt);
                                foreach (GeometryObject geomObj in firstIntersectionPointGeomElem)
                                {
                                    GeometryInstance instance = geomObj as GeometryInstance;
                                    if (null != instance)
                                    {
                                        GeometryElement instanceGeometryElement = instance.GetInstanceGeometry();
                                        foreach (GeometryObject o in instanceGeometryElement)
                                        {
                                            Solid solid = o as Solid;
                                            if (solid != null && solid.Volume != 0)
                                            {
                                                firstIntersectionPointSolid = solid;
                                                break;
                                            }
                                        }
                                    }
                                }

                                for (int j = 0; j < tmpIntersectionWallRectangularSolidIntersectCombineList.Count; j++)
                                {
                                    FamilyInstance secondIntersectionPoint = tmpIntersectionWallRectangularSolidIntersectCombineList[j];
                                    Solid secondIntersectionPointSolid = null;
                                    GeometryElement secondIntersectionPointGeomElem = secondIntersectionPoint.get_Geometry(opt);
                                    foreach (GeometryObject geomObj in secondIntersectionPointGeomElem)
                                    {
                                        GeometryInstance instance = geomObj as GeometryInstance;
                                        if (null != instance)
                                        {
                                            GeometryElement instanceGeometryElement = instance.GetInstanceGeometry();
                                            foreach (GeometryObject o in instanceGeometryElement)
                                            {
                                                Solid solid = o as Solid;
                                                if (solid != null && solid.Volume != 0)
                                                {
                                                    secondIntersectionPointSolid = solid;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    double unionvolume = BooleanOperationsUtils.ExecuteBooleanOperation(firstIntersectionPointSolid
                                        , secondIntersectionPointSolid, BooleanOperationsType.Intersect).Volume;

                                    if (unionvolume > 0)
                                    {
                                        intersectionWallRectangularSolidIntersectCombineList.Add(secondIntersectionPoint);
                                        tmpIntersectionWallRectangularSolidIntersectCombineList.Remove(secondIntersectionPoint);
                                        i = 0;
                                        j = 0;
                                    }
                                }
                            }

                            if (intersectionWallRectangularSolidIntersectCombineList.Count > 1)
                            {
                                List<XYZ> pointsList = new List<XYZ>();
                                double intersectionPointThickness = 0;

                                foreach (FamilyInstance intPount in intersectionWallRectangularSolidIntersectCombineList)
                                {
                                    XYZ originPoint = (intPount.Location as LocationPoint).Point;

                                    XYZ downLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation;
                                    pointsList.Add(downLeftPoint);

                                    XYZ downRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate();
                                    pointsList.Add(downRightPoint);

                                    XYZ upLeftPoint = originPoint + ((intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation)
                                        + intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() * XYZ.BasisZ;
                                    pointsList.Add(upLeftPoint);

                                    XYZ upRightPoint = originPoint + ((intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate())
                                        + intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() * XYZ.BasisZ;
                                    pointsList.Add(upRightPoint);

                                    if (intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble() > intersectionPointThickness)
                                    {
                                        intersectionPointThickness = intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble();
                                    }
                                }

                                //Найти центр спроецировать точки на одну отметку и померить расстояние
                                double maxHorizontalDistance = 0;
                                double maxVerticalDistance = 0;
                                XYZ pointP1 = null;
                                XYZ pointP2 = null;
                                XYZ pointP3 = null;
                                XYZ pointP4 = null;
                                foreach (XYZ p1 in pointsList)
                                {
                                    foreach(XYZ p2 in pointsList)
                                    {
                                        if(new XYZ(p1.X,p1.Y,0).DistanceTo(new XYZ(p2.X, p2.Y, 0)) > maxHorizontalDistance)
                                        {
                                            maxHorizontalDistance = new XYZ(p1.X, p1.Y, 0).DistanceTo(new XYZ(p2.X, p2.Y, 0));
                                            pointP1 = p1;
                                            pointP2 = p2;
                                        }

                                        if (new XYZ(0, 0, p1.Z).DistanceTo(new XYZ(0, 0, p2.Z)) > maxVerticalDistance)
                                        {
                                            maxVerticalDistance = new XYZ(0, 0, p1.Z).DistanceTo(new XYZ(0, 0, p2.Z));
                                            pointP3 = p1;
                                            pointP4 = p2;
                                        }
                                    }
                                }
                                XYZ midPointLeftRight = (pointP1 + pointP2) / 2;
                                XYZ midPointUpDown = (pointP3 + pointP4) / 2;
                                XYZ centroidIntersectionPoint = new XYZ(midPointLeftRight.X, midPointLeftRight.Y, midPointUpDown.Z);

                                List<XYZ> combineDownLeftPointList = new List<XYZ>();
                                List<XYZ> combineDownRightPointList = new List<XYZ>();
                                List<XYZ> combineUpLeftPointList = new List<XYZ>();
                                List<XYZ> combineUpRightPointList = new List<XYZ>();

                                XYZ pointFacingOrientation = intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation;
                                XYZ pointHandOrientation = intersectionWallRectangularSolidIntersectCombineList.First().HandOrientation;

                                foreach (XYZ p in pointsList)
                                {
                                    XYZ vectorToPoint = (p - centroidIntersectionPoint).Normalize();

                                    //Нижний левый угол
                                    if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineDownLeftPointList.Add(p);
                                    }

                                    //Нижний правый угол
                                    if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineDownRightPointList.Add(p);
                                    }

                                    //Верхний левый угол
                                    if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineUpLeftPointList.Add(p);
                                    }

                                    //Верхний правый угол
                                    if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineUpRightPointList.Add(p);
                                    }
                                }

                                List<XYZ> maxRightPointList = new List<XYZ>();
                                maxRightPointList.AddRange(combineDownRightPointList);
                                maxRightPointList.AddRange(combineUpRightPointList);
                                double maxRightDistance = -1000000;
                                foreach (XYZ p in pointsList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxRightDistance)
                                    {
                                        maxRightDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                List<XYZ> maxLeftPointList = new List<XYZ>();
                                maxLeftPointList.AddRange(combineDownLeftPointList);
                                maxLeftPointList.AddRange(combineUpLeftPointList);
                                double maxLeftDistance = -1000000;
                                foreach (XYZ p in pointsList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation.Negate()).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxLeftDistance)
                                    {
                                        maxLeftDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                double minZ = 10000000000;
                                XYZ minZPoint = null;
                                double maxZ = -10000000000;
                                XYZ maxZPoint = null;

                                foreach (XYZ p in pointsList)
                                {
                                    if (p.Z < minZ)
                                    {
                                        minZ = p.Z;
                                        minZPoint = p;
                                    }
                                    if (p.Z > maxZ)
                                    {
                                        maxZ = p.Z;
                                        maxZPoint = p;
                                    }
                                }

                                double intersectionPointHeight = RoundUpToIncrement(maxZPoint.Z - minZPoint.Z, roundHoleSizesUpIncrement);
                                double intersectionPointWidth = RoundUpToIncrement(maxLeftDistance + maxRightDistance, roundHoleSizesUpIncrement);

                                XYZ newCenterPoint = null;
                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                {
                                    newCenterPoint = new XYZ(RoundToIncrement(centroidIntersectionPoint.X, roundHolePosition)
                                        , RoundToIncrement(centroidIntersectionPoint.Y, roundHolePosition)
                                        , RoundToIncrement((centroidIntersectionPoint.Z - intersectionPointHeight / 2)
                                        - (doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level).Elevation, roundHolePosition));
                                }
                                else
                                {
                                    newCenterPoint = new XYZ(centroidIntersectionPoint.X
                                        , centroidIntersectionPoint.Y
                                        , (centroidIntersectionPoint.Z - intersectionPointHeight / 2)
                                        - (doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level).Elevation);
                                }

                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                                    , intersectionWallRectangularFamilySymbol
                                    , doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level
                                    , StructuralType.NonStructural) as FamilyInstance;

                                if (Math.Round(intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                                {
                                    Line rotationLine = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation.AngleTo(intersectionPoint.FacingOrientation));
                                }

                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                                intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(newCenterPoint.Z);
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(newCenterPoint.Z);

                                foreach (FamilyInstance forDel in intersectionWallRectangularSolidIntersectCombineList)
                                {
                                    doc.Delete(forDel.Id);
                                    intersectionWallRectangularCombineList.Remove(forDel);
                                }
                            }
                            else
                            {
                                intersectionWallRectangularCombineList.Remove(intersectionWallRectangularSolidIntersectCombineList[0]);
                            }
                        }

                        while (intersectionFloorRectangularCombineList.Count != 0)
                        {
                            List<FamilyInstance> intersectionFloorRectangularSolidIntersectCombineList = new List<FamilyInstance>();
                            intersectionFloorRectangularSolidIntersectCombineList.Add(intersectionFloorRectangularCombineList[0]);
                            intersectionFloorRectangularCombineList.RemoveAt(0);

                            List<FamilyInstance> tmpIntersectionFloorRectangularSolidIntersectCombineList = intersectionFloorRectangularCombineList.ToList();
                            for (int i = 0; i < intersectionFloorRectangularSolidIntersectCombineList.Count; i++)
                            {
                                FamilyInstance firstIntersectionPoint = intersectionFloorRectangularSolidIntersectCombineList[i];
                                Solid firstIntersectionPointSolid = null;
                                GeometryElement firstIntersectionPointGeomElem = firstIntersectionPoint.get_Geometry(opt);
                                foreach (GeometryObject geomObj in firstIntersectionPointGeomElem)
                                {
                                    GeometryInstance instance = geomObj as GeometryInstance;
                                    if (null != instance)
                                    {
                                        GeometryElement instanceGeometryElement = instance.GetInstanceGeometry();
                                        foreach (GeometryObject o in instanceGeometryElement)
                                        {
                                            Solid solid = o as Solid;
                                            if (solid != null && solid.Volume != 0)
                                            {
                                                firstIntersectionPointSolid = solid;
                                                break;
                                            }
                                        }
                                    }
                                }
                                for (int j = 0; j < tmpIntersectionFloorRectangularSolidIntersectCombineList.Count; j++)
                                {
                                    FamilyInstance secondIntersectionPoint = tmpIntersectionFloorRectangularSolidIntersectCombineList[j];
                                    Solid secondIntersectionPointSolid = null;
                                    GeometryElement secondIntersectionPointGeomElem = secondIntersectionPoint.get_Geometry(opt);
                                    foreach (GeometryObject geomObj in secondIntersectionPointGeomElem)
                                    {
                                        GeometryInstance instance = geomObj as GeometryInstance;
                                        if (null != instance)
                                        {
                                            GeometryElement instanceGeometryElement = instance.GetInstanceGeometry();
                                            foreach (GeometryObject o in instanceGeometryElement)
                                            {
                                                Solid solid = o as Solid;
                                                if (solid != null && solid.Volume != 0)
                                                {
                                                    secondIntersectionPointSolid = solid;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    double unionvolume = BooleanOperationsUtils.ExecuteBooleanOperation(firstIntersectionPointSolid
                                        , secondIntersectionPointSolid, BooleanOperationsType.Intersect).Volume;

                                    if (unionvolume > 0)
                                    {
                                        intersectionFloorRectangularSolidIntersectCombineList.Add(secondIntersectionPoint);
                                        tmpIntersectionFloorRectangularSolidIntersectCombineList.Remove(secondIntersectionPoint);
                                        i = 0;
                                        j = 0;
                                    }
                                }
                            }

                            if (intersectionFloorRectangularSolidIntersectCombineList.Count > 1)
                            {
                                List<XYZ> pointsList = new List<XYZ>();
                                double intersectionPointThickness = 0;

                                foreach (FamilyInstance intPount in intersectionFloorRectangularSolidIntersectCombineList)
                                {
                                    XYZ originPoint = (intPount.Location as LocationPoint).Point;

                                    XYZ downLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate()
                                        + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation.Negate();
                                    pointsList.Add(downLeftPoint);

                                    XYZ downRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation
                                        + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation.Negate();
                                    pointsList.Add(downRightPoint);

                                    XYZ upLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate()
                                        + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation;
                                    pointsList.Add(upLeftPoint);

                                    XYZ upRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation
                                        + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation;
                                    pointsList.Add(upRightPoint);

                                    if (intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble() > intersectionPointThickness)
                                    {
                                        intersectionPointThickness = intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble();
                                    }
                                }
                                double maxX  = pointsList.Max(p => p.X);
                                double minX = pointsList.Min(p => p.X);
                                double maxY = pointsList.Max(p => p.Y);
                                double minY = pointsList.Min(p => p.Y);
                                XYZ centroidIntersectionPoint = new XYZ((maxX + minX)/2,(maxY + minY) /2, pointsList.First().Z);

                                List<XYZ> combineDownLeftPointList = new List<XYZ>();
                                List<XYZ> combineDownRightPointList = new List<XYZ>();
                                List<XYZ> combineUpLeftPointList = new List<XYZ>();
                                List<XYZ> combineUpRightPointList = new List<XYZ>();

                                XYZ pointFacingOrientation = intersectionFloorRectangularSolidIntersectCombineList.First().FacingOrientation;
                                XYZ pointHandOrientation = intersectionFloorRectangularSolidIntersectCombineList.First().HandOrientation;
                                Level pointLevel = doc.GetElement(intersectionFloorRectangularSolidIntersectCombineList.First().LevelId) as Level;
                                double pointLevelElevation = pointLevel.Elevation;
                                foreach (XYZ p in pointsList)
                                {
                                    XYZ vectorToPoint = (p - centroidIntersectionPoint).Normalize();

                                    //Нижний левый угол
                                    if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineDownLeftPointList.Add(p);
                                    }

                                    //Нижний правый угол
                                    if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineDownRightPointList.Add(p);
                                    }

                                    //Верхний левый угол
                                    if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineUpLeftPointList.Add(p);
                                    }

                                    //Верхний правый угол
                                    if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                                        && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                                    {
                                        combineUpRightPointList.Add(p);
                                    }
                                }

                                List<XYZ> maxUpPointList = new List<XYZ>();
                                maxUpPointList.AddRange(combineUpLeftPointList);
                                maxUpPointList.AddRange(combineUpRightPointList);
                                double maxUpDistance = -1000000;
                                foreach (XYZ p in maxUpPointList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointFacingOrientation).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxUpDistance)
                                    {
                                        maxUpDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                List<XYZ> maxDownPointList = new List<XYZ>();
                                maxDownPointList.AddRange(combineDownLeftPointList);
                                maxDownPointList.AddRange(combineDownRightPointList);
                                double maxDownDistance = -1000000;
                                foreach (XYZ p in maxDownPointList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointFacingOrientation.Negate()).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxDownDistance)
                                    {
                                        maxDownDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                List<XYZ> maxRightPointList = new List<XYZ>();
                                maxRightPointList.AddRange(combineDownRightPointList);
                                maxRightPointList.AddRange(combineUpRightPointList);
                                double maxRightDistance = -1000000;
                                foreach (XYZ p in maxRightPointList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxRightDistance)
                                    {
                                        maxRightDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                List<XYZ> maxLeftPointList = new List<XYZ>();
                                maxLeftPointList.AddRange(combineDownLeftPointList);
                                maxLeftPointList.AddRange(combineUpLeftPointList);
                                double maxLeftDistance = -1000000;
                                foreach (XYZ p in maxLeftPointList)
                                {
                                    XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation.Negate()).Project(p).XYZPoint;
                                    if (x.DistanceTo(centroidIntersectionPoint) > maxLeftDistance)
                                    {
                                        maxLeftDistance = x.DistanceTo(centroidIntersectionPoint);
                                    }
                                }

                                double intersectionPointHeight = RoundUpToIncrement(maxUpDistance + maxDownDistance, roundHoleSizesUpIncrement);
                                double intersectionPointWidth = RoundUpToIncrement(maxLeftDistance + maxRightDistance, roundHoleSizesUpIncrement);

                                XYZ newCenterPoint = null;
                                if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                                {
                                    newCenterPoint = new XYZ(RoundToIncrement(centroidIntersectionPoint.X, roundHolePosition)
                                        , RoundToIncrement(centroidIntersectionPoint.Y, roundHolePosition)
                                        , RoundToIncrement(centroidIntersectionPoint.Z
                                        - pointLevelElevation, roundHolePosition));
                                }
                                else
                                {
                                    newCenterPoint = new XYZ(centroidIntersectionPoint.X
                                        , centroidIntersectionPoint.Y
                                        , centroidIntersectionPoint.Z - pointLevelElevation);
                                }

                                FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                                    , intersectionFloorRectangularFamilySymbol
                                    , pointLevel
                                    , StructuralType.NonStructural) as FamilyInstance;
                                intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                                intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                                intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                                intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set(pointLevelElevation);
                                intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);

                                double rotationAngle = pointFacingOrientation.AngleTo(intersectionPoint.FacingOrientation);
                                if (rotationAngle != 0)
                                {
                                    Line rotationAxis = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(doc
                                        , intersectionPoint.Id
                                        , rotationAxis
                                        , rotationAngle);
                                }

                                foreach (FamilyInstance forDel in intersectionFloorRectangularSolidIntersectCombineList)
                                {
                                    doc.Delete(forDel.Id);
                                    intersectionFloorRectangularCombineList.Remove(forDel);
                                }
                            }
                            else
                            {
                                intersectionFloorRectangularCombineList.Remove(intersectionFloorRectangularSolidIntersectCombineList[0]);
                            }

                        }
                        t.Commit();
                    }
                }
                tg.Assimilate();
            }

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            TaskDialog.Show("Время работы", elapsedTime.TotalSeconds.ToString() + " сек.");

            return Result.Succeeded;
        }

        private static Curve GetCurveFromIntersectionPoint(Guid intersectionPointThicknessGuid, FamilyInstance familyInstanceIntersectionPoint)
        {
            XYZ intersectionPointLocation = (familyInstanceIntersectionPoint.Location as LocationPoint).Point;
            XYZ curveStartPoint = intersectionPointLocation - (familyInstanceIntersectionPoint
                .get_Parameter(intersectionPointThicknessGuid).AsDouble() / 2 * familyInstanceIntersectionPoint.FacingOrientation);
            XYZ curveEndPoint = intersectionPointLocation + (familyInstanceIntersectionPoint
                .get_Parameter(intersectionPointThicknessGuid).AsDouble() / 2 * familyInstanceIntersectionPoint.FacingOrientation);
            Curve curve = Line.CreateBound(curveStartPoint, curveEndPoint) as Curve;
            return curve;
        }
        private static Level GetClosestBottomWallLevel(List<Level> docLvlList, Document linkDoc, Wall wall)
        {
            Level lvl = null;
            double linkWallLevelElevation = (linkDoc.GetElement(wall.LevelId) as Level).Elevation;
            double heightDifference = 10000000000;
            foreach (Level docLvl in docLvlList)
            {
                double tmpHeightDifference = Math.Abs(Math.Round(linkWallLevelElevation, 6) - Math.Round(docLvl.Elevation, 6));
                if (tmpHeightDifference < heightDifference)
                {
                    heightDifference = tmpHeightDifference;
                    lvl = docLvl;
                }
            }
            return lvl;
        }
        private static Level GetClosestFloorLevel(List<Level> docLvlList, Document linkDoc, Floor floor)
        {
            Level lvl = null;
            double linkFloorLevelElevation = (linkDoc.GetElement(floor.LevelId) as Level).Elevation;
            double heightDifference = 10000000000;
            foreach (Level docLvl in docLvlList)
            {
                double tmpHeightDifference = Math.Abs(Math.Round(linkFloorLevelElevation, 6) - Math.Round(docLvl.Elevation, 6));
                if (tmpHeightDifference < heightDifference)
                {
                    heightDifference = tmpHeightDifference;
                    lvl = docLvl;
                }
            }
            return lvl;
        }
        private static void ActivateFamilySymbols(FamilySymbol intersectionWallRectangularFamilySymbol
            , FamilySymbol intersectionWallRoundFamilySymbol
            , FamilySymbol intersectionFloorRectangularFamilySymbol
            , FamilySymbol intersectionFloorRoundFamilySymbol)
        {
            if (intersectionWallRectangularFamilySymbol != null)
            {
                intersectionWallRectangularFamilySymbol.Activate();
            }
            if (intersectionWallRoundFamilySymbol != null)
            {
                intersectionWallRoundFamilySymbol.Activate();
            }
            if (intersectionFloorRectangularFamilySymbol != null)
            {
                intersectionFloorRectangularFamilySymbol.Activate();
            }
            if (intersectionFloorRoundFamilySymbol != null)
            {
                intersectionFloorRoundFamilySymbol.Activate();
            }
        }
        private double GetAngleFromMEPCurve(MEPCurve curve)
        {
            foreach (Connector c in curve.ConnectorManager.Connectors)
            {
                double rotationAngle = 0;
                if (Math.Round(c.CoordinateSystem.BasisY.AngleOnPlaneTo(XYZ.BasisY, XYZ.BasisZ), 6) * (180 / Math.PI) < 180)
                {
                    rotationAngle = -c.CoordinateSystem.BasisY.AngleTo(XYZ.BasisY);
                }
                else
                {
                    rotationAngle = c.CoordinateSystem.BasisY.AngleTo(XYZ.BasisY);
                }
                return rotationAngle;
            }
            return 0;
        }
        private double RoundToIncrement(double x, double m)
        {
            return (Math.Round((x * 304.8) / m) * m) / 304.8;
        }
        private double RoundUpToIncrement(double x, double m)
        {
            return (((int)Math.Ceiling(x * 304.8 / m)) * m) / 304.8;
        }
    }
}
