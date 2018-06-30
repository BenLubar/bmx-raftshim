using System;
using System.Collections.Generic;
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
        private async Task<IEnumerable<RaftItem>> FindPlansAsync(string planType = null)
        {
            return from plan in await new DB.Context(false).Plans_GetAllPlansAsync(this.ApplicationId, planType, !this.ApplicationId.HasValue)
                   where plan.Application_Id == this.ApplicationId
                   where plan.Active_Indicator
                   select new RaftItem(
                       AH.Switch<string, RaftItemType>(plan.PlanType_Code)
                           .Case(Domains.PlanTypes.Deployment, RaftItemType.DeploymentPlan)
                           .Case(Domains.PlanTypes.Template, RaftItemType.Module)
                           .End(),
                       plan.Plan_Name,
                       plan.ModifiedOn_Date,
                       plan.ModifiedBy_User_Name,
                       plan.Plan_Bytes.LongLength,
                       plan.PlanVersion_Id?.ToString()
                   );
        }

        private async Task<IEnumerable<RaftItem>> FindScriptsAsync()
        {
            return from script in await new DB.Context(false).ScriptAssets_GetScriptsAsync(this.ApplicationId)
                   where script.Application_Id == this.ApplicationId
                   select new RaftItem(
                       RaftItemType.Script,
                       script.ScriptAsset_Name,
                       script.ModifiedOn_Date,
                       script.ModifiedBy_User_Name,
                       script.Script_Text.LongLength
                   );
        }

        private async Task<IEnumerable<RaftItem>> FindTextTemplatesAsync()
        {
            return from tmpl in await new DB.Context(false).TextTemplates_GetTemplatesAsync(this.ApplicationId)
                   where tmpl.Application_Id == this.ApplicationId
                   select new RaftItem(
                       RaftItemType.TextTemplate,
                       tmpl.Template_Name,
                       DateTimeOffset.MinValue,
                       null,
                       tmpl.Template_Bytes.LongLength
                   );
        }

        private async Task<IEnumerable<RaftItem>> FindPipelinesAsync()
        {
            return from pipeline in await new DB.Context(false).Pipelines_GetPipelinesAsync(this.ApplicationId)
                   where pipeline.Application_Id == this.ApplicationId
                   where pipeline.Active_Indicator
                   let pipelineJson = JsonConvert.SerializeObject(Persistence.DeserializeFromPersistedObjectXml(pipeline.Pipeline_Configuration), Formatting.Indented)
                   select new RaftItem(
                       RaftItemType.Pipeline,
                       pipeline.Pipeline_Name,
                       pipeline.ModifiedOn_Date,
                       pipeline.ModifiedBy_User_Name,
                       InedoLib.UTF8Encoding.GetByteCount(pipelineJson)
                   );
        }
    }
}
