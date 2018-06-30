using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.ExecutionEngine;

namespace Inedo.BuildMaster.Extensions.RaftShim
{
    partial class ApplicationShimRaftRepository
    {
        public override async Task<IReadOnlyDictionary<RuntimeVariableName, string>> GetVariablesAsync()
        {
            return (from v in await new DB.Context(false).Variables_GetVariablesAccessibleFromScopeAsync(Application_Id: this.ApplicationId, IncludeSystemVariables_Indicator: !this.ApplicationId.HasValue)
                    where !v.Sensitive_Indicator
                    let type = AH.Switch<string, RuntimeValueType>(v.ValueType_Code)
                        .Case(Domains.VariableValueType.Scalar, RuntimeValueType.Scalar)
                        .Case(Domains.VariableValueType.Vector, RuntimeValueType.Vector)
                        .Case(Domains.VariableValueType.Map, RuntimeValueType.Map)
                        .End()
                    select (key: new RuntimeVariableName(v.Variable_Name, type), value: InedoLib.UTF8Encoding.GetString(v.Variable_Value))).ToDictionary(kv => kv.key, kv => kv.value);
        }

        public override Task SetVariableAsync(RuntimeVariableName name, string value)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteVariableAsync(RuntimeVariableName name)
        {
            throw new NotImplementedException();
        }
    }
}
