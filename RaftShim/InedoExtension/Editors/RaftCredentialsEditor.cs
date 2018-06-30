using System;
using System.Linq;
using Inedo.BuildMaster.Extensions.RaftShim.Credentials;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Serialization;
using Inedo.Web.Controls;
using Inedo.Web.Editors;

namespace Inedo.BuildMaster.Extensions.RaftShim.Editors
{
    public sealed class RaftCredentialsEditor : CredentialsEditor
    {
        private Type RaftType { get; set; }
        private ExtensionEditor RaftEditor { get; set; }
        private int? EnvironmentId { get; set; }

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
            var typeSelect = new SelectList(
                from rt in Internals.RaftTypes
                select new SelectListItem(
                    rt.name + AH.ConcatNE(" - ", rt.description),
                    rt.type.AssemblyQualifiedName,
                    this.RaftType == rt.type,
                    rt.extension?.Name
                )
            ) { AutoPostBack = true };

            var form = new SimpleVirtualCompositeControl(
                new SlimFormField("Raft Type:", typeSelect)
            );
            if (this.RaftEditor != null)
            {
                form.Controls.Add(this.RaftEditor.EditorControl);
            }

            typeSelect.SelectedValueChanged += (s, e) =>
            {
                this.RaftType = Internals.RaftTypes.Select(rt => rt.type).FirstOrDefault(t => t.AssemblyQualifiedName == typeSelect.SelectedValue);
                if (this.RaftEditor != null)
                {
                    form.Controls.Remove(this.RaftEditor.EditorControl);
                }
                this.RaftEditor = null;
                if (this.RaftType != null)
                {
                    this.RaftEditor = RaftRepositoryEditor.GetEditor(this.RaftType, this.EnvironmentId);
                    this.RaftEditor.BindToInstance(Activator.CreateInstance(this.RaftType));
                    form.Controls.Add(this.RaftEditor.EditorControl);
                }
            };

            return form;
        }
    }
}
