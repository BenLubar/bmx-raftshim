using System.ComponentModel;
using Inedo.BuildMaster.Extensions.RaftShim.Editors;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMaster.Extensions.RaftShim.Credentials
{
    [SlimSerializable]
    [DisplayName("Raft Shim")]
    [Description("A raft for use with operations from the Raft Shim extension.")]
    [ScriptAlias("Raft")]
    [Tag("rafts")]
    [CustomEditor(typeof(RaftCredentialsEditor))]
    public sealed class RaftCredentials : ResourceCredentials
    {
        [Required]
        [Persistent]
        public string RaftData { get; set; }

        public RaftRepository Raft => this.RaftData == null ? null : (RaftRepository)Persistence.DeserializeFromPersistedObjectXml(this.RaftData);

        public override RichDescription GetDescription()
        {
            return new RichDescription("Raft: ", this.Raft?.GetDescription() ?? new RichDescription("[unknown]"));
        }

        public override int GetHashCode() => this.Raft?.GetHashCode() ?? 0;
        public override bool Equals(object obj) => (obj is RaftCredentials cred) && Equals(this.Raft, cred.Raft);
        public override string ToString() => this.Raft?.ToString() ?? nameof(RaftCredentials);
    }
}
