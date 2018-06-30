﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensions.RaftShim.Credentials;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Extensibility.UserDirectories;
using Inedo.Security;

namespace Inedo.BuildMaster.Extensions.RaftShim.Operations
{
    [ScriptNamespace("RaftShim", PreferUnqualified = true)]
    [Tag("rafts")]
    public abstract class RaftOperationBase : ExecuteOperation, IHasCredentials<RaftCredentials>
    {
        protected RaftRepository Raft
        {
            get
            {
                var credentials = this.TryGetCredentials() ?? throw new InvalidOperationException("Raft not found.");
                var raft = credentials.Raft ?? throw new InvalidOperationException("Raft not configured.");
                raft.RaftName = this.CredentialName;
                return raft;
            }
        }
        protected async Task<IUserDirectoryUser> GetExecutionCreatorAsync(int executionId)
        {
            var executionData = await new DB.Context(false).Executions_GetExecutionAsync(executionId);
            return executionData == null ? null : UserContext.Current.CurrentUserDirectory.TryGetUser(executionData.CreatedBy_User_Name);
        }

        protected async Task<bool> RaftItemsEqualAsync(RaftRepository raft1, RaftRepository raft2, RaftItemType itemType, string itemName)
        {
            // This relies on raft items (usually) being pretty small. If that's no longer the case, we need to read the files in chunks instead.
            var contents = await Task.WhenAll(
                getItemContentsAsync(raft1),
                getItemContentsAsync(raft2)
            );

            return contents[0].SequenceEqual(contents[1]);

            async Task<byte[]> getItemContentsAsync(RaftRepository raft)
            {
                using (var stream = await raft.OpenRaftItemAsync(itemType, itemName, FileMode.Open, FileAccess.Read))
                using (var memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    return memory.ToArray();
                }
            }
        }

        public abstract string CredentialName { get; set; }
    }
}
