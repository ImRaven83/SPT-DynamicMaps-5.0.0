using System.Collections.Generic;
using Diz.Binding;

namespace DynamicMaps.Utils
{
    /// <summary>
    /// Copies il2cpp collections into managed lists, so they can be iterated after the il2cpp side changes
    /// </summary>
    public static class Il2CppExtensions
    {
        public static List<T> ToManaged<T>(this BindableList<T> list)
        {
            var result = new List<T>();
            if (list?.List == null)
            {
                return result;
            }

            foreach (var item in list.List)
            {
                result.Add(item);
            }

            return result;
        }

        public static List<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var result = new List<T>();
            if (list == null)
            {
                return result;
            }

            foreach (var item in list)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
