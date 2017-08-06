# SmartProperties.NET
Extension to the .NET INotifyPropertyChanged API that allows you to build a propagation tree for dependent properties.

A common problem in UI or event-based programming (especially MVVM) is that of _dependent properties_, i.e. those whose values are calculated from other properties in your object.

One solution to this problem is to manually call `OnPropertyChanged` in a property setter on behalf of all its dependencies:

    public string MyProp
    {
      get
      {
        return this.myProp;
      }
      
      set
      {
        this.myProp = value;
        this.OnPropertyChanged(nameof(MyDependentProp));
        this.OnPropertyChanged();
      }
    }
    
This, however, gets unwieldy and hard to follow very quickly.

SmartProperties is a simple tool that allows you to statically declare your property dependencies via _attributes_ and create a `PropertyModel` for your object implementing `INotifyPropertyChanged`.

Basic usage looks like this:

    using SmartProperties;

    // MyObject extends SmartProperties type 'NotifyPropertyChangedBase' which takes care
    // of wiring up your object's PropertyModel for you.
    public class MyObject : NotifyPropertyChangedBase
    {
      public string MyProp
      {
        ... (normal property changed code here)
      }
      
      [DependsOn(nameof(MyProp))]
      public string MyDependentProp
      {
        ... (normal property changed code here)
      }
    }
    
And that's it! Now, whenever `MyProp` fires a `PropertyChanged` event, `OnPropertyChanged` will be invoked for `MyDependentProp` as well. This allows you to build dependency trees for your object properties using simple, easy-to-follow code without all of the boiler plate of managing those dependencies yourself.

It is also guaranteed that each property in the dependency tree **will only be invoked once** during a single property changed event propagation. The order of propagation is _breadth-first_ meaning that properties will only have their changed events fire _after_ each of their dependencies' events have been fired.

## Other usage notes
Since properties will only have their property changed events invoked once, dependency cycles are permitted:

    [DependsOn(nameof(B))]
    public string A
    {
      ...
    }
    
    [DependsOn(nameof(A))]
    public string B
    {
      ...
    }
    
Self-dependencies, however, are not allowed (for hopefully obvious reasons):

    // Will throw exception at runtime
    [DependsOn(nameof(A))]
    public string A
    {
      ...
    }

If you don't want to (or can't) inherit from `NotifyPropertyChangedBase`, you can initialize a `PropertyModel` for your object yourself in the constructor:

    public class MyObject
    {
      public MyObject()
      {
        // OnPropertyChanged should have the standard signature for OnPropertyChanged implementations.
        // 'Create' returns a PropertyModel reference, but this can be discarded unless you wish to
        // explicitly deregister its event delegate via the Dispose() method.
        PropertyModel.Create(this, this.OnPropertyChanged);
      }
    }
    
Note also that `PropertyModel` is NOT thread-safe. It is assumed that your property changed events will be fired in a single-threaded context (which is usually the case). Asynchronous event invocation (especially while propagation is already in progress) will lead to undefined behavior.
