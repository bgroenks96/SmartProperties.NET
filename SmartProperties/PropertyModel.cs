
namespace SmartProperties
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Represents a directional event propagation graph for any object that extends
    /// <see cref="INotifyPropertyChanged"/>. Dependency graphs are built once (lazily)
    /// per-type on the first call to <see cref="Create"/>. <see cref="PropertyModel"/>
    /// maintains a registered handler for the host object's <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// event until 1) both the host object and <see cref="PropertyModel"/> are made
    /// available for finalization or 2) <see cref="Dispose"/> is called.
    /// </summary>
    public sealed class PropertyModel : IDisposable
    {
        private static Dictionary<Type, PropertyGraph> typeGraphCache = new Dictionary<Type, PropertyGraph>();

        private readonly INotifyPropertyChanged host;

        private readonly PropertyGraph graph;

        private readonly Action<string> onPropertyChanged;

        private bool ignoreEvents = false;

        private PropertyModel(INotifyPropertyChanged host, PropertyGraph graph, Action<string> onPropertyChanged)
        {
            this.host = host;
            this.graph = graph;
            this.onPropertyChanged = onPropertyChanged;
            this.host.PropertyChanged += this.OnHostPropertyChanged;
        }

        public static PropertyModel Create(INotifyPropertyChanged host, Action<string> onPropertyChanged)
        {
            var type = host.GetType();
            PropertyGraph graph;
            if (typeGraphCache.ContainsKey(type))
            {
                graph = typeGraphCache[type];
            }
            else
            {
                graph = PropertyGraph.BuildFor(type);
                typeGraphCache[type] = graph;
            }
            return new PropertyModel(host, graph, onPropertyChanged);
        }

		public void Dispose()
		{
            this.host.PropertyChanged -= this.OnHostPropertyChanged;
		}

        private void OnHostPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // Ignore event if 1) propagation is in progress or 2) the name is null/empty.
            if (this.ignoreEvents || string.IsNullOrEmpty(args.PropertyName))
            {
                return;
            }

            try
            {
				this.ignoreEvents = true;
				PropertyGraph.PropertyNode node;
				if (this.graph.TryGetValue(args.PropertyName, out node))
				{
					this.PropagatePropertyChanged(node);
				}
            }
            finally
            {
				this.ignoreEvents = false;
            }
        }

        // Fires property changed events exactly once for all descendents of 'property'.
        // The property graph is traversed breadth-first to ensure that all dependent
        // properties are called only once the corresponding property changed event for
        // each of its dependencies has been called first.
        private void PropagatePropertyChanged(PropertyGraph.PropertyNode property)
        {
            var closedSet = new HashSet<PropertyGraph.PropertyNode>();
            var visitQueue = new Queue<PropertyGraph.PropertyNode>();
            closedSet.Add(property);
            foreach (var dependent in property.GetDependents())
            {
                visitQueue.Enqueue(dependent);
            }
            while (visitQueue.Any())
            {
                var next = visitQueue.Dequeue();
                if (closedSet.Contains(next))
                {
                    continue;
                }

				closedSet.Add(next);
                this.FirePropertyChanged(next.Name);
                foreach (var dependent in next.GetDependents().Where(d => !closedSet.Contains(d)))
				{
					visitQueue.Enqueue(dependent);
				}
            }
        }

        private void FirePropertyChanged(string propertyName)
        {
            this.onPropertyChanged(propertyName);
        }
    }
}
