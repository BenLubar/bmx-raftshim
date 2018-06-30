using System;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.BuildMaster.Extensions.RaftShim
{
    partial class ApplicationShimRaftRepository
    {
        public override async Task DeleteRaftItemAsync(RaftItemType type, string name)
        {
            using (var db = new DB.Context())
            {
                Tables.Plans_Extended plan;
                Tables.ScriptAssets script;
                Tables.TextTemplates tmpl;
                Tables.Pipelines pipeline;

                db.BeginTransaction();

                switch (type)
                {
                    case RaftItemType.ServerConfigurationPlan:
                    case RaftItemType.RoleConfigurationPlan:
                    case RaftItemType.OrchestrationPlan:
                        break;
                    case RaftItemType.Module:
                        plan = await db.Plans_GetPlanByNameAsync(this.ApplicationId, name, Domains.PlanTypes.Template);
                        if (plan == null || plan.Application_Id != this.ApplicationId)
                            return;

                        await db.Plans_DeletePlanAsync(plan.Plan_Id);
                        db.CommitTransaction();
                        return;
                    case RaftItemType.Script:
                        script = await db.ScriptAssets_GetScriptByNameAsync(name, this.ApplicationId);
                        if (script == null || script.Application_Id != this.ApplicationId)
                            return;

                        await db.ScriptAssets_DeleteScriptAsync(script.ScriptAsset_Id);
                        db.CommitTransaction();
                        return;
                    case RaftItemType.File:
                        break;
                    case RaftItemType.DeploymentPlan:
                        plan = await db.Plans_GetPlanByNameAsync(this.ApplicationId, name, Domains.PlanTypes.Deployment);
                        if (plan == null || plan.Application_Id != this.ApplicationId)
                            return;

                        await db.Plans_DeletePlanAsync(plan.Plan_Id);
                        db.CommitTransaction();
                        return;
                    case RaftItemType.TextTemplate:
                        tmpl = await db.TextTemplates_GetTemplateByNameAsync(this.ApplicationId, name);
                        if (tmpl == null || tmpl.Application_Id != this.ApplicationId)
                            return;

                        await db.TextTemplates_DeleteTemplateAsync(tmpl.TextTemplate_Id);
                        db.CommitTransaction();
                        return;
                    case RaftItemType.Pipeline:
                        pipeline = (await db.Pipelines_GetPipelinesAsync(this.ApplicationId))
                            .FirstOrDefault(p => p.Application_Id == this.ApplicationId && p.Pipeline_Name == name);
                        if (pipeline == null)
                            return;

                        await db.Pipelines_DeletePipelineAsync(pipeline.Pipeline_Id);
                        db.CommitTransaction();
                        return;
                    case RaftItemType.DeploymentSetTemplate:
                        break;
                    default:
                        throw new NotImplementedException("Unhandled raft item type: " + type);
                }
            }
            throw new NotSupportedException("Unsupported raft item type: " + type);
        }
    }
}
