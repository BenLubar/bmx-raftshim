using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.RaftRepositories;

namespace Inedo.BuildMaster.Extensions.RaftShim
{
    // This class is full of terrible hacks. Don't try this at home.
    internal static class Internals
    {
        public static InedoExtensionsManager ExtensionsManager
        {
            get
            {
                var extensionsManager = Type.GetType("Inedo.BuildMaster.Extensibility.ExtensionsManager,BuildMaster");
                var waitForInitializationAsync = extensionsManager.GetMethod("WaitForInitializationAsync", BindingFlags.Public | BindingFlags.Static);
                var task = (Task)waitForInitializationAsync.Invoke(null, new object[0]);
                var taskResult = (Task<InedoExtensionsManager>)task;
                return taskResult.Result();
            }
        }

        public static IEnumerable<(Type type, string name, string description, InedoExtension extension)> RaftTypes =>
            from extension in ExtensionsManager.GetExtensions()
            from type in extension.Assembly.GetExportedTypes()
            where type.IsSubclassOf(typeof(RaftRepository)) && !type.IsAbstract
            let name = AhReflection.GetCustomAttribute<DisplayNameAttribute>(type)?.DisplayName
            let description = AhReflection.GetCustomAttribute<DescriptionAttribute>(type)?.Description
            select (type, name, description, extension);
    }
}
