using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.Extensibility.RaftRepositories;
using Inedo.Serialization;
using Newtonsoft.Json;

namespace Inedo.BuildMaster.Extensions.RaftShim
{
    partial class ApplicationShimRaftRepository
    {
        private Task<Stream> OpenRaftItemReadAsync(RaftItemType type, string name, FileMode fileMode, string version)
        {
            if (fileMode != FileMode.Open)
            {
                throw new NotSupportedException("FileMode must be Open for reading raft items.");
            }

            switch (type)
            {
                case RaftItemType.ServerConfigurationPlan:
                case RaftItemType.RoleConfigurationPlan:
                case RaftItemType.OrchestrationPlan:
                    break;
                case RaftItemType.Module:
                    return this.OpenPlanReadAsync(Domains.PlanTypes.Module, name);
                case RaftItemType.Script:
                    return this.OpenScriptReadAsync(name);
                case RaftItemType.File:
                    break;
                case RaftItemType.DeploymentPlan:
                    return this.OpenPlanReadAsync(Domains.PlanTypes.Deployment, name);
                case RaftItemType.TextTemplate:
                    return this.OpenTextTemplateReadAsync(name);
                case RaftItemType.Pipeline:
                    return this.OpenPipelineReadAsync(name);
                case RaftItemType.DeploymentSetTemplate:
                    break;
                default:
                    throw new NotImplementedException("Unhandled raft item type: " + type);
            }
            throw new NotSupportedException("Unsupported raft item type: " + type);
        }

        private async Task<Stream> OpenPlanReadAsync(string planType, string name)
        {
            var plan = await new DB.Context(false).Plans_GetPlanByNameAsync(this.ApplicationId, name, planType);
            if (plan != null && plan.Application_Id == this.ApplicationId && plan.Active_Indicator)
                return new MemoryStream(plan.Plan_Bytes, false);

            throw new FileNotFoundException();
        }

        private async Task<Stream> OpenScriptReadAsync(string name)
        {
            var script = await new DB.Context(false).ScriptAssets_GetScriptByNameAsync(name, this.ApplicationId);
            if (script != null && script.Application_Id == this.ApplicationId)
                return new MemoryStream(script.Script_Text, false);

            throw new FileNotFoundException();
        }

        private async Task<Stream> OpenTextTemplateReadAsync(string name)
        {
            var tmpl = await new DB.Context(false).TextTemplates_GetTemplateByNameAsync(this.ApplicationId, name);
            if (tmpl != null && tmpl.Application_Id == this.ApplicationId)
                return new MemoryStream(tmpl.Template_Bytes, false);

            throw new FileNotFoundException();
        }

        private async Task<Stream> OpenPipelineReadAsync(string name)
        {
            var pipeline = (await new DB.Context(false).Pipelines_GetPipelinesAsync(this.ApplicationId))
                .FirstOrDefault(p => p.Application_Id == this.ApplicationId && p.Pipeline_Name == name && p.Active_Indicator);

            if (pipeline != null)
            {
                var instance = Persistence.DeserializeFromPersistedObjectXml(pipeline.Pipeline_Configuration);
                var json = JsonConvert.SerializeObject(instance, Formatting.Indented);
                var bytes = InedoLib.UTF8Encoding.GetBytes(json);
                return new MemoryStream(bytes, false);
            }

            throw new FileNotFoundException();
        }
    }
}
