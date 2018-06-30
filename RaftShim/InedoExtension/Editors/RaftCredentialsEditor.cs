using System;
using System.Linq;
using Inedo.BuildMaster.Extensions.RaftShim.Credentials;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Serialization;
using Inedo.Web;
using Inedo.Web.Controls;
using Inedo.Web.Editors;

namespace Inedo.BuildMaster.Extensions.RaftShim.Editors
{
    public sealed class RaftCredentialsEditor : CredentialsEditor
    {
        private Type RaftType { get; set; }
        private ExtensionEditor RaftEditor { get; set; }
        private int? EnvironmentId { get; set; }
        private SelectList TypeSelect { get; set; }
        private SimpleVirtualCompositeControl Form { get; } = new SimpleVirtualCompositeControl();

        public override void BindToInstance(object instance)
        {
            var credential = (RaftCredentials)instance;
            this.EnvironmentId = credential.EnvironmentId;
            using (var raft = credential.Raft)
            {
                this.RaftType = raft?.GetType();
                if (this.RaftType == null)
                {
                    this.RaftEditor = null;
                }
                else
                {
                    this.RaftEditor = RaftRepositoryEditor.GetEditor(this.RaftType, this.EnvironmentId);
                    this.RaftEditor.BindToInstance(raft);
                }
            }
            this.ResetForm();
        }

        public override void WriteToInstance(object instance)
        {
            var credential = (RaftCredentials)instance;
            if (this.RaftType == null)
            {
                credential.RaftData = null;
            }
            else
            {
                using (var raft = (RaftRepository)Activator.CreateInstance(this.RaftType))
                {
                    this.RaftEditor.WriteToInstance(raft);
                    credential.RaftData = Persistence.SerializeToPersistedObjectXml(raft);
                }
            }
        }

        protected override ISimpleControl CreateEditorControl()
        {
            var selected = HttpContextThatWorksOnLinux.Current?.Request?.Form?["raft-type"];
            this.RaftType = selected == null ? null : Internals.RaftTypes.Select(rt => rt.type).FirstOrDefault(t => t.FullName + "," + t.Assembly.GetName().Name == selected);
            this.RaftType = this.RaftType ?? Internals.RaftTypes.FirstOrDefault().type;
            this.RaftEditor = null;
            if (this.RaftType != null)
            {
                this.RaftEditor = RaftRepositoryEditor.GetEditor(this.RaftType, this.EnvironmentId);
                this.RaftEditor.BindToInstance(Activator.CreateInstance(this.RaftType));
            }

            return this.Form;
        }

        private void ResetForm()
        {
            this.TypeSelect = new SelectList(
                from rt in Internals.RaftTypes
                select new SelectListItem(
                    rt.name + AH.ConcatNE(" - ", rt.description),
                    rt.type.FullName + "," + rt.type.Assembly.GetName().Name,
                    this.RaftType == rt.type,
                    rt.extension?.Name
                )
            )
            {
                Attributes = { ["name"] = "raft-type" },
                AutoPostBack = true
            };

            this.Form.Controls.Clear();
            this.Form.Controls.Add(new SlimFormField("Raft Type:", this.TypeSelect));
            if (this.RaftEditor != null)
            {
                this.Form.Controls.Add(this.RaftEditor.EditorControl);
            }
        }
    }
}
