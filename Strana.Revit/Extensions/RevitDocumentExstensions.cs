// <copyright file="RevitDocumentExstensions.cs" company="Strana">
// Copyright (c) Strana. All rights reserved.
// Licensed under the NC license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace Strana.Revit.Extension
{
    /// <summary>
    /// This exstensions for revit document.
    /// </summary>
    public static class RevitDocumentExstensions
    {
        /// <summary>
        /// Return All Links from document.
        /// </summary>
        /// <param name="doc">Current document.</param>
        /// <returns>Revit links.</returns>
        public static IEnumerable<RevitLinkInstance> GetAllRevitLinkInstances(this Document doc)
        {
            return new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .WhereElementIsNotElementType()
                    .Cast<RevitLinkInstance>()
                    .Where(link => link.GetLinkDocument() != null);
        }

        /// <summary>
        /// Gets all communications on project.
        /// </summary>
        /// <param name="doc">Current document.</param>
        /// <returns>return communacations.</returns>
        public static IEnumerable<Element> Communications(this Document doc)
        {
            Type[] communicationTypes = { typeof(Duct), typeof(Pipe), typeof(CableTray) };

            return communicationTypes
                .SelectMany(type => new FilteredElementCollector(doc)
                    .OfClass(type)
                    .WhereElementIsNotElementType()
                    .ToElements());
        }

        public static List<string> GetAllRevitLinkedFileNames(this Document doc)
        {
            return doc.GetAllRevitLinkInstances().Select(link => link.Name).ToList();
        }
    }
}
