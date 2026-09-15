using System;

namespace EcsExtensions
{
    // The class adds handlers to C# events of this view; the view knows nothing of its subscriber.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ViewSubscriberAttribute : Attribute
    {
        public Type View { get; }

        public ViewSubscriberAttribute(Type view)
        {
            View = view;
        }
    }
}
