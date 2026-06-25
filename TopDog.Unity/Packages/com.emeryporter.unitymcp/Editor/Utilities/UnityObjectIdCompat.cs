using UnityEditor;
using UnityEngine;

namespace UnityMCP.Editor.Utilities
{
    /// <summary>Unity 6.5 deprecates GetInstanceID / InstanceIDToObject as errors (CS0619).</summary>
    internal static class UnityObjectIdCompat
    {
        internal static int GetObjectInstanceId(Object obj)
        {
            if (obj == null)
            {
                return 0;
            }

#if UNITY_6000_5_OR_NEWER
            return unchecked((int)EntityId.ToULong(obj.GetEntityId()));
#else
            return obj.GetInstanceID();
#endif
        }

        internal static Object InstanceIdToObject(int instanceId)
        {
#if UNITY_6000_5_OR_NEWER
            return EditorUtility.EntityIdToObject(EntityId.FromULong(unchecked((ulong)instanceId)));
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

        /// <summary>True when an object reference slot is null but still points at a missing object id.</summary>
        internal static bool HasStaleObjectReference(SerializedProperty property)
        {
#if UNITY_6000_5_OR_NEWER
            return property.objectReferenceEntityIdValue != EntityId.None;
#else
            return property.objectReferenceInstanceIDValue != 0;
#endif
        }
    }

    internal static class UnityObjectIdCompatExtensions
    {
        internal static int GetMcpInstanceId(this Object obj) => UnityObjectIdCompat.GetObjectInstanceId(obj);
    }
}
