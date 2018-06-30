using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.BuildMaster.Extensions.RaftShim
{
    internal sealed partial class ApplicationShimRaftRepository : RaftRepository
    {
        public ApplicationShimRaftRepository(int? applicationId)
        {
            this.ApplicationId = applicationId;
        }

        public int? ApplicationId { get; }

        public override bool IsReadOnly => false;

        public override Task<IEnumerable<string>> GetProjectsAsync(bool recursive)
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public override Task<RaftRepository> GetProjectScopedRaftRepositoryAsync(string project)
        {
            throw new NotSupportedException();
        }

        public override async Task<IEnumerable<RaftItem>> GetRaftItemsAsync()
        {
            return (await Task.WhenAll(from RaftItemType type in Enum.GetValues(typeof(RaftItemType))
                                       select this.GetRaftItemsAsync(type))).SelectMany(items => items);
        }

        public override Task<IEnumerable<RaftItem>> GetRaftItemsAsync(RaftItemType type)
        {
            switch (type)
            {
                case RaftItemType.ServerConfigurationPlan:
                case RaftItemType.RoleConfigurationPlan:
                case RaftItemType.OrchestrationPlan:
                    break;
                case RaftItemType.Module:
                    return this.FindPlansAsync(Domains.PlanTypes.Template);
                case RaftItemType.Script:
                    return this.FindScriptsAsync();
                case RaftItemType.File:
                    break;
                case RaftItemType.DeploymentPlan:
                    return this.FindPlansAsync(Domains.PlanTypes.Deployment);
                case RaftItemType.TextTemplate:
                    return this.FindTextTemplatesAsync();
                case RaftItemType.Pipeline:
                    return this.FindPipelinesAsync();
                case RaftItemType.DeploymentSetTemplate:
                    break;
                default:
                    throw new NotImplementedException("Unhandled raft item type: " + type);
            }
            return Task.FromResult(Enumerable.Empty<RaftItem>());
        }

        public override Task<Stream> OpenRaftItemAsync(RaftItemType type, string name, FileMode fileMode, FileAccess fileAccess, string version)
        {
            if (version != null)
                throw new NotSupportedException("This raft does not support versioning.");

            if (fileAccess == FileAccess.Read)
                return this.OpenRaftItemReadAsync(type, name, fileMode, version);

            throw new NotImplementedException();
        }
    }
}
