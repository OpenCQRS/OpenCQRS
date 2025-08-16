using System;
using System.Reflection;

namespace OpenCqrs.Utilities
{
    /// <summary>
    /// https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
    /// </summary>
    public static class Magic
    {
        public static Func<T, object, object> MagicMethod<T>(MethodInfo method) where T : class
        {
            // First fetch the generic form
            MethodInfo genericHelper = typeof(Magic).GetMethod("MagicMethodHelper",
                BindingFlags.Static | BindingFlags.NonPublic);

            // Now supply the type arguments
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod
                (typeof(T), method.GetParameters()[0].ParameterType, method.ReturnType);

            // Now call it. The null argument is because it's a static method.
            object ret = constructedHelper.Invoke(null, new object[] { method });

            // Cast the result to the right kind of delegate and return it
            return (Func<T, object, object>)ret;
        }

        private static Func<TTarget, object, object> MagicMethodHelper<TTarget, TParam, TReturn>(MethodInfo method)
            where TTarget : class
        {
            // Convert the slow MethodInfo into a fast, strongly typed, open delegate
            Func<TTarget, TParam, TReturn> func = (Func<TTarget, TParam, TReturn>)Delegate.CreateDelegate
                (typeof(Func<TTarget, TParam, TReturn>), method);

            // Now create a more weakly typed delegate which will call the strongly typed one
            Func<TTarget, object, object> ret = (TTarget target, object param) => func(target, (TParam)param);
            return ret;
        }
    }
}