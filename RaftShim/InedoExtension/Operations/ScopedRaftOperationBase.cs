using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.BuildMaster.Extensions.RaftShim.Operations
{
    public abstract class ScopedRaftOperationBase : RaftOperationBase
    {
        protected IEnumerable<(int id, string name)> Applications =>
            from a in DB.Applications_GetApplications(null, false)
            join n in this.ApplicationNames ?? Enumerable.Empty<string>() on a.Application_Name equals n
            select (a.Application_Id, n);

        [Category("Scope")]
        [DisplayName("Global items")]
        [ScriptAlias("IncludeGlobal")]
        [DefaultValue(true)]
        public bool IncludeGlobal { get; set; } = true;

        [Category("Scope")]
        [DisplayName("Application names")]
        [ScriptAlias("ApplicationNames")]
        public IEnumerable<string> ApplicationNames { get; set; }

        public sealed override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            await this.BeforeExecuteAsync(context);

            using (var raft = this.Raft)
            {
                bool any = false;
                if (this.IncludeGlobal)
                {
                    any = true;
                    this.LogDebug("Processing global items...");
                    using (var bmRaft = new ApplicationShimRaftRepository(null))
                    {
                        await this.ExecuteRaftAsync(context, raft, bmRaft);
                    }
                }
                foreach (var (id, name) in this.Applications)
                {
                    any = true;
                    this.LogDebug($"Processing application: {name}");
                    using (var bmRaft = new ApplicationShimRaftRepository(id))
                    {
                        using (var scopedRaft = await raft.GetProjectScopedRaftRepositoryAsync(name))
                        {
                            await this.ExecuteRaftAsync(context, scopedRaft, bmRaft);
                        }
                    }
                }
                if (!any)
                {
                    this.LogWarning("No applications were selected, and global items are not included.");
                }
            }

            await this.AfterExecuteAsync(context);
        }

        protected abstract Task ExecuteRaftAsync(IOperationExecutionContext context, RaftRepository actualRaft, RaftRepository raftShim);
        protected virtual Task BeforeExecuteAsync(IOperationExecutionContext context) => InedoLib.NullTask;
        protected virtual Task AfterExecuteAsync(IOperationExecutionContext context) => InedoLib.NullTask;

        protected RichDescription GetLongDescription(IOperationConfiguration config)
        {
            var description = new RichDescription();
            var applicationNames = config[nameof(ApplicationNames)].AsEnumerable();
            if (bool.TryParse(AH.CoalesceString(config[nameof(IncludeGlobal)].ToString(), bool.TrueString), out bool includeGlobal) && includeGlobal)
            {
                description.AppendContent("with ", new Hilite("global"), " items");
                if (applicationNames.Any())
                {
                    description.AppendContent(" and items from ", new ListHilite(applicationNames));
                }
            }
            else
            {
                description.AppendContent("with items from ", new ListHilite(applicationNames));
            }
            return description;
        }
    }
}
