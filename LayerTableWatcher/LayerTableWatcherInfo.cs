using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace LayerTableWatcher
{
    public class LayerTableWatcherInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "LayerTableWatcher";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a3181b18-5a4e-4783-9e9b-ab86f0087209");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
