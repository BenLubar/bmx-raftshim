using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inedo.BuildMaster.Data;
using Inedo.Web.Controls;
using Inedo.Web.Editors.PropertyEditors;

namespace Inedo.BuildMaster.Extensions.RaftShim.Operations
{
    public sealed class ApplicationNamesEditor : PropertyEditor
    {
        public new TagTextBox EditorControl => (TagTextBox)base.EditorControl;

        public ApplicationNamesEditor(PropertyInfo property) : base(property, new TagTextBox())
        {
            this.EditorControl.Tags = DB.Applications_GetApplications(null, false).Select(a => a.Application_Name).ToArray();
            this.EditorControl.AllowTagCreation = false;
        }

        protected override void BindToControl(object instance)
        {
            this.EditorControl.SelectedTags.Clear();
            var applications = (IEnumerable<string>)this.Property.GetValue(instance, null);
            if (applications != null)
                this.EditorControl.SelectedTags.AddRange(applications);
        }

        protected override void WriteToInstance(object instance)
        {
            this.Property.SetValue(instance, this.EditorControl.SelectedTags.ToArray());
        }

        protected override string GetRawValue()
        {
            return "@(" + string.Join(", ", this.EditorControl.SelectedTags) + ")";
        }
    }
}
