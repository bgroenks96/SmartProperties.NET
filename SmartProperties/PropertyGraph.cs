/*
 * MIT License
 * 
 * Copyright (c) 2017 Brian Groenke
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

﻿
namespace SmartProperties
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;

    using static SmartProperties.PropertyGraph;

    /// <summary>
    /// A map of property names to linked property nodes that form a directional
    /// graph. Cyclic edges are permitted; self-edges are not.
    /// </summary>
    internal sealed class PropertyGraph : Dictionary<string, PropertyNode>
    {
        private PropertyGraph()
        {
        }

        public static PropertyGraph BuildFor(Type type)
        {
            var graph = new PropertyGraph();
            var allProperties = type.GetRuntimeProperties().ToList();
			foreach (var dependent in allProperties.Where(HasAttribute))
			{
                Initialize(dependent, graph, allProperties);
			}
            return graph;
        }

        /// <summary>
        /// Initialize the specified property in 'graph' given a total list of
        /// available properties in 'allProperties.' Dependency references that
        /// do not exist in 'allProperties' and duplicates are silently ignored.
        /// </summary>
        /// <returns>The initialize.</returns>
        /// <param name="property">Property.</param>
        /// <param name="graph">Graph.</param>
        /// <param name="allProperties">All properties.</param>
        private static PropertyNode Initialize(PropertyInfo property, PropertyGraph graph, IList<PropertyInfo> allProperties)
        {
			PropertyNode node;
            if (graph.ContainsKey(property.Name))
            {
                node = graph[property.Name];
            }
            else
            {
                node = new PropertyNode(property.Name);
                graph[property.Name] = node;
				var dependencyAttr = property.GetCustomAttribute<DependsOnAttribute>();
				if (dependencyAttr == null)
				{
					return node;
				}

				foreach (var propName in dependencyAttr.Properties)
				{
                    var dependency = allProperties.FirstOrDefault(p => propName.Equals(p.Name));
					if (dependency == null)
					{
						continue;
					}

					var dependencyNode = Initialize(dependency, graph, allProperties);
					dependencyNode.AddDependent(node);
				}
            }

            return node;
        }

		private static bool HasAttribute(PropertyInfo p)
		{
			return p.GetCustomAttributes().Any(a => a is DependsOnAttribute);
		}

        public override string ToString()
        {
            return string.Format("[{0}]", string.Join(" ", this.Values));
        }

        internal class PropertyNode
        {
            private readonly HashSet<PropertyNode> dependents;

            internal PropertyNode(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }
                this.Name = name;
                this.dependents = new HashSet<PropertyNode>();
            }

            internal string Name { get; }

            internal IEnumerable<PropertyNode> GetDependents()
            {
                return dependents;
            }

            internal void AddDependent(PropertyNode node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }
                if (node.Equals(this))
                {
                    throw new ArgumentException("A property cannot be dependent on itself.");
                }
                this.dependents.Add(node);
            }

            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this.Name.Equals((obj as PropertyNode)?.Name);
            }

            public override string ToString()
            {
                return string.Format("{0}({1})", this.Name, string.Join(",", this.dependents));
            }
        }
    }
}
