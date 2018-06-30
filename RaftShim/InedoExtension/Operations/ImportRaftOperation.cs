using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.BuildMaster.Extensions.RaftShim.Operations
{
    [DisplayName("Import from Raft")]
    [Description("")]
    [ScriptAlias("Import-Raft")]
    public sealed class ImportRaftOperation : ScopedRaftOperationBase
    {
        [DisplayName("Delete missing items")]
        [ScriptAlias("DeleteMissing")]
        [DefaultValue(false)]
        public bool DeleteMissing { get; set; } = false;

        protected override async Task ExecuteRaftAsync(IOperationExecutionContext context, RaftRepository actualRaft, RaftRepository raftShim)
        {
            var actualItems = await actualRaft.GetRaftItemsAsync();
            var shimItems = await raftShim.GetRaftItemsAsync();
            var actualLookup = actualItems.ToLookup(i => (i.ItemType, i.ItemName));
            var shimLookup = shimItems.ToLookup(i => (i.ItemType, i.ItemName));
            bool any = false;
            if (this.DeleteMissing)
            {
                foreach (var item in shimItems)
                {
                    if (actualLookup.Contains((item.ItemType, item.ItemName)))
                    {
                        continue;
                    }

                    any = true;
                    this.LogInformation($"Deleting {item.ItemType} {item.ItemName}, which is present locally but not in the raft.");
                    await raftShim.DeleteRaftItemAsync(item.ItemType, item.ItemName);
                }
            }

            foreach (var item in actualItems)
            {
                var shimItem = shimLookup[(item.ItemType, item.ItemName)].FirstOrDefault();
                if (shimItem != null && (!item.ItemSize.HasValue || item.ItemSize == shimItem.ItemSize))
                {
                    if (await this.RaftItemsEqualAsync(actualRaft, raftShim, item.ItemType, item.ItemName))
                    {
                        continue;
                    }
                }

                this.LogInformation($"Importing {item.ItemType} {item.ItemName} from the raft. ({(item.ItemSize.HasValue ? AH.FormatSize(item.ItemSize.Value) : "unknown size")}, last modified {item.LastWriteTime}{AH.ConcatNE(" by ", item.ModifiedByUser)})");
                any = true;
                using (var input = await actualRaft.OpenRaftItemAsync(item.ItemType, item.ItemName, FileMode.Open, FileAccess.Read))
                using (var output = await raftShim.OpenRaftItemAsync(item.ItemType, item.ItemName, FileMode.Create, FileAccess.Write))
                {
                    await input.CopyToAsync(output);
                }
            }

            if (any)
            {
                this.LogDebug("Committing changes...");
                await raftShim.CommitAsync(await this.GetExecutionCreatorAsync(context.ExecutionId));
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Import from raft ", new Hilite(config[nameof(this.CredentialName)])),
                this.GetLongDescription(config)
            );
        }
    }
}
