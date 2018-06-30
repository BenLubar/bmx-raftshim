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
    [DisplayName("Export to Raft")]
    [Description("")]
    [ScriptAlias("Export-Raft")]
    public sealed class ExportRaftOperation : ScopedRaftOperationBase
    {
        [DisplayName("Delete missing items")]
        [ScriptAlias("DeleteMissing")]
        [DefaultValue(true)]
        public bool DeleteMissing { get; set; } = true;

        protected override async Task ExecuteRaftAsync(IOperationExecutionContext context, RaftRepository actualRaft, RaftRepository raftShim)
        {
            var actualItems = await actualRaft.GetRaftItemsAsync();
            var shimItems = await raftShim.GetRaftItemsAsync();
            var actualLookup = actualItems.ToLookup(i => (i.ItemType, i.ItemName));
            var shimLookup = shimItems.ToLookup(i => (i.ItemType, i.ItemName));
            bool any = false;
            if (this.DeleteMissing)
            {
                foreach (var item in actualItems)
                {
                    if (shimLookup.Contains((item.ItemType, item.ItemName)))
                    {
                        continue;
                    }

                    any = true;
                    this.LogInformation($"Deleting {item.ItemType} {item.ItemName}, which is present in the raft but not locally.");
                    await actualRaft.DeleteRaftItemAsync(item.ItemType, item.ItemName);
                }
            }

            foreach (var item in shimItems)
            {
                var actualItem = actualLookup[(item.ItemType, item.ItemName)].FirstOrDefault();
                if (actualItem != null && (!item.ItemSize.HasValue || item.ItemSize == actualItem.ItemSize))
                {
                    if (await this.RaftItemsEqualAsync(actualRaft, raftShim, item.ItemType, item.ItemName))
                    {
                        continue;
                    }
                }

                this.LogInformation($"Exporting {item.ItemType} {item.ItemName} to the raft. ({(item.ItemSize.HasValue ? AH.FormatSize(item.ItemSize.Value) : "unknown size")}, last modified {item.LastWriteTime}{AH.ConcatNE(" by ", item.ModifiedByUser)})");
                any = true;
                using (var input = await raftShim.OpenRaftItemAsync(item.ItemType, item.ItemName, FileMode.Open, FileAccess.Read))
                using (var output = await actualRaft.OpenRaftItemAsync(item.ItemType, item.ItemName, FileMode.Create, FileAccess.Write))
                {
                    await input.CopyToAsync(output);
                }
            }

            if (any)
            {
                this.LogDebug("Committing changes...");
                await actualRaft.CommitAsync(await this.GetExecutionCreatorAsync(context.ExecutionId));
            }
        }


        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Export to raft ", new Hilite(config[nameof(this.CredentialName)])),
                this.GetLongDescription(config)
            );
        }
    }
}
