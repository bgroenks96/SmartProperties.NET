
namespace SmartProperties
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Property)]
    public class DependsOnAttribute : Attribute
    {
        public DependsOnAttribute(params string[] parentPropertyNames)
        {
            this.Properties = parentPropertyNames;
        }

        public IEnumerable<string> Properties { get; }
    }
}
