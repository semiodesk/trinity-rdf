using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity
{
    public static class ResourceExtensions
    {
        /// <summary>
        /// Executes a breath first search on the property of the resource and returns all
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(this IResource resource, T relation, int depth = int.MaxValue) where T : IResource
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a breath first search on the property of the resource and returns all
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(this IResource resource, IEnumerable<T> relation, int depth = int.MaxValue) where T : IResource
        {
            throw new NotImplementedException();
        }
    }
}
