using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Components;
using Rhino;
using Rhino.DocObjects;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace LayerTableEvents
{
    public class LayerTableWatcherComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public LayerTableWatcherComponent()
          : base("LayerTableWatcher", "LayerTableWatcher",
              "Gets the list of layers in the active document per specified events",
              "KieranTimberlake", "Document Info")
        {
            expireSolution = false;
            rhinoDocLayerTableEvent = null;
        }

        private bool expireSolution;
        private EventHandler<Rhino.DocObjects.Tables.LayerTableEventArgs> rhinoDocLayerTableEvent;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Update", "U", "Set this value to true to update the layer table.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Auto-Update", "AU", "If this value is set to true, the component will listen for changes to the layer table based on the toggled events, and automatically update each time something changes. Use with caution - you can create an infinite loop if you create layers downstream based on outputs from this component.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Added", "EA", "Trigger on Added event.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Deleted", "ED", "Trigger on Deleted event.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Modified", "EM", "Trigger on Modified event.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Sorted", "ES", "Trigger on Sorted event.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Current", "EC", "Trigger on Current layer change event.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Layers", "L", "The list of layer names in the active document.", GH_ParamAccess.list);
            pManager.AddTextParameter("Full Layer Paths", "LF", "The list of layer names in the document, including nesting information.", GH_ParamAccess.list);
            pManager.AddColourParameter("Layer Colors", "C", "The colors of the document Layers.", GH_ParamAccess.list);
            pManager.AddTextParameter("Linetypes", "LT", "The list of linetypes associated with the document layers.", GH_ParamAccess.list);
            pManager.AddTextParameter("Material Names", "M", "The list of material names associated with the document layers.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Visible", "V", "True if layer is visible.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Print Width", "PW", "The print widths associated with the document layers.", GH_ParamAccess.list);
            pManager.AddColourParameter("Print Color", "PC", "The print color of the layer.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Locked", "LL", "True if layer is locked.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Expanded", "LE", "True if layer is expanded.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            RemoveEvents();

            bool update = false;
            bool autoUpdate = false;
            bool eventAdded = true;
            bool eventDeleted = true;
            bool eventModified = true;
            bool eventSorted = false;
            bool eventCurrent = false;

            if (!DA.GetData(0, ref update)) return;
            if (!DA.GetData(1, ref autoUpdate)) return;
            if (!DA.GetData(2, ref eventAdded)) return;
            if (!DA.GetData(3, ref eventDeleted)) return;
            if (!DA.GetData(4, ref eventModified)) return;
            if (!DA.GetData(5, ref eventSorted)) return;
            if (!DA.GetData(6, ref eventCurrent)) return;

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            Rhino.DocObjects.Tables.LayerTable layerTable = doc.Layers;
            Rhino.DocObjects.Tables.MaterialTable materialTable = doc.Materials;
            Rhino.DocObjects.Tables.LinetypeTable linetypeTable = doc.Linetypes;

            List<string> name = new List<string>();
            List<string> fullPath = new List<string>();
            List<Color> color = new List<Color>();
            List<string> linetype = new List<string>();
            List<string> material = new List<string>();
            List<bool> visible = new List<bool>();
            List<double> printwidth = new List<double>();
            List<Color> printcolor = new List<Color>();
            List<bool> locked = new List<bool>();
            List<bool> expanded = new List<bool>();

            foreach (Layer layer in layerTable)
            {
                if (!layer.IsDeleted)
                {
                    name.Add(layer.Name);
                    fullPath.Add(layer.FullPath);
                    color.Add(layer.Color);
                    linetype.Add(linetypeTable.FindIndex(layer.LinetypeIndex).Name);
                    material.Add(layer.RenderMaterial?.Name);
                    visible.Add(layer.IsVisible);
                    printwidth.Add(layer.PlotWeight);
                    printcolor.Add(layer.PlotColor);
                    locked.Add(layer.IsLocked);
                    expanded.Add(layer.IsExpanded);
                }
            }

            DA.SetDataList(0, name);
            DA.SetDataList(1, fullPath);
            DA.SetDataList(2, color);
            DA.SetDataList(3, linetype);
            DA.SetDataList(4, material);
            DA.SetDataList(5, visible);
            DA.SetDataList(6, printwidth);
            DA.SetDataList(7, printcolor);
            DA.SetDataList(8, locked);
            DA.SetDataList(9, expanded);

            rhinoDocLayerTableEvent = (sender, e) => ProcessLayerTableEvent(sender, e, eventAdded, eventDeleted, eventModified, eventSorted, eventCurrent);

            if (autoUpdate) AddEvents();
        }

        /// <summary>
        /// This method will be called when the document that owns this object moves into a different context.
        /// </summary>
        /// <param name="document">Document that owns this object.</param>
        /// <param name="context">The reason for this event.<br/>
        ///     Unknown	 0	Specifies unknown context.This should never be used.<br/>
        ///     None     1	Specifies unset state.This is only used for documents that have never been in a context.<br/>
        ///     Open     2	Indicates the document was created anew from a file.<br/>
        ///     Close    3	Indicates the document has been unloaded from memory.<br/>
        ///     Loaded   4	Indicates the document has been loaded into the Canvas.<br/>
        ///     Unloaded 5	Indicates the document has been unloaded from the Canvas.<br/>
        ///     Lock     6	Indicates the document has been locked. This is only possible for nested documents.<br/>
        ///     Unlock   7	Indicates the document has been unlocked. This is only possible for nested documents.
        /// </param>
        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            RhinoApp.WriteLine("DocumentContextChanged");
            switch (context)
            {
                case GH_DocumentContext.Open:
                    RhinoApp.WriteLine("Open");
                    break;
                case GH_DocumentContext.Close:
                    RhinoApp.WriteLine("Close");
                    RemoveEvents();
                    break;
                case GH_DocumentContext.Loaded:
                    RhinoApp.WriteLine("Loaded");
                    break;
                case GH_DocumentContext.Unloaded:
                    RhinoApp.WriteLine("Unloaded");
                    RemoveEvents();
                    break;
                case GH_DocumentContext.Lock:
                    RhinoApp.WriteLine("Lock");
                    RemoveEvents();
                    break;
                case GH_DocumentContext.Unlock:
                    RhinoApp.WriteLine("Unlock");
                    break;
                default:
                    break;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            RhinoApp.WriteLine("RemovedFromDocument");
            RemoveEvents();
        }

        void AddEvents()
        {
            RhinoApp.WriteLine("AddEvents");
            RhinoDoc.LayerTableEvent += rhinoDocLayerTableEvent;
        }

        void RemoveEvents()
        {
            RhinoApp.WriteLine("RemoveEvents");
            RhinoDoc.LayerTableEvent -= rhinoDocLayerTableEvent;
            RhinoApp.Idle -= RhinoAppIdle;
        }

        private void ProcessLayerTableEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e, Boolean eA, Boolean eD, Boolean eM, Boolean eS, Boolean eC)
        {
            RhinoApp.WriteLine("ProcessLayerTableEvent");
            if (!expireSolution)
            {
                RhinoApp.WriteLine("!expireSolution");
                switch (e.EventType)
                {
                    case Rhino.DocObjects.Tables.LayerTableEventType.Added:
                        if (eA) expireSolution = true;
                        break;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Deleted:
                        if (eD) expireSolution = true;
                        break;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Undeleted:
                        if (eA) expireSolution = true;
                        break;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Modified:
                        if (eM) expireSolution = true;
                        break;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Sorted:
                        if (eS) expireSolution = true;
                        break;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Current:
                        if (eC) expireSolution = true;
                        break;
                    default:
                        break;
                }
                if (expireSolution) RhinoApp.Idle += RhinoAppIdle;
            }

        }

        private void RhinoAppIdle(object sender, EventArgs e)
        {
            RhinoApp.WriteLine("RhinoAppIdle");
            if (expireSolution)
            {
                RhinoApp.WriteLine("expireSolution");
                RhinoApp.Idle -= RhinoAppIdle;
                expireSolution = false;
                ExpireSolution(true);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("30954f8f-bb74-4705-9c6a-50f1d672832e"); }
        }
    }
}
