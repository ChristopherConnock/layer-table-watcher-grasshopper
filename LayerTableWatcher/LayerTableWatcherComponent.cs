using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
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
            pManager.AddBooleanParameter("Undeleted", "EU", "Trigger on Undeleted event.", GH_ParamAccess.item, true);
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
            pManager.AddColourParameter("Print Color", "PC", "The print color for the layer.", GH_ParamAccess.list);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool update = false;
            bool autoUpdate = false;
            bool eventAdded = true;
            bool eventDeleted = true;
            bool eventUndeleted = true;
            bool eventModified = true;
            bool eventSorted = false;
            bool eventCurrent = false;

            if (!DA.GetData(0, ref update)) return;
            if (!DA.GetData(1, ref autoUpdate)) return;
            if (!DA.GetData(2, ref eventAdded)) return;
            if (!DA.GetData(3, ref eventDeleted)) return;
            if (!DA.GetData(4, ref eventUndeleted)) return;
            if (!DA.GetData(5, ref eventModified)) return;
            if (!DA.GetData(6, ref eventSorted)) return;
            if (!DA.GetData(7, ref eventCurrent)) return;

            RemoveEvents();

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

            rhinoDocLayerTableEvent = (sender, e) => ProcessLayerTableEvent(sender, e, eventAdded, eventDeleted, eventUndeleted, eventModified, eventSorted, eventCurrent);

            if (autoUpdate) AddEvents();

        }

        void AddEvents()
        {
            RhinoDoc.NewDocument += ToggleExpireSolution;
            RhinoDoc.EndOpenDocument += ToggleExpireSolution;
            RhinoDoc.BeginSaveDocument += ToggleExpireSolution;
            RhinoDoc.CloseDocument += ToggleExpireSolution;

            RhinoDoc.LayerTableEvent += rhinoDocLayerTableEvent;

            RhinoApp.Idle += RhinoAppIdle;
        }

        void RemoveEvents()
        {
            RhinoDoc.NewDocument -= ToggleExpireSolution;
            RhinoDoc.EndOpenDocument -= ToggleExpireSolution;
            RhinoDoc.BeginSaveDocument -= ToggleExpireSolution;
            RhinoDoc.CloseDocument -= ToggleExpireSolution;

            RhinoDoc.LayerTableEvent -= rhinoDocLayerTableEvent;

            RhinoApp.Idle -= RhinoAppIdle;
        }

        private void ToggleExpireSolution(object sender, DocumentEventArgs e)
        {
            expireSolution = true;
        }

        private void ProcessLayerTableEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e, Boolean eA, Boolean eD, Boolean eU, Boolean eM, Boolean eS, Boolean eC)
        {
            if (!expireSolution)
            {
                switch (e.EventType)
                {
                    case Rhino.DocObjects.Tables.LayerTableEventType.Added:
                        if (eA) expireSolution = true;
                        return;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Deleted:
                        if (eD) expireSolution = true;
                        return;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Undeleted:
                        if (eU) expireSolution = true;
                        return;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Modified:
                        if (eM) expireSolution = true;
                        return;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Sorted:
                        if (eS) expireSolution = true;
                        return;
                    case Rhino.DocObjects.Tables.LayerTableEventType.Current:
                        if (eC) expireSolution = true;
                        return;
                    default:
                        return;
                }
            }

        }

        private void RhinoAppIdle(object sender, EventArgs e)
        {
            if (expireSolution)
            {
                ExpireSolution(true);
                expireSolution = false;
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
