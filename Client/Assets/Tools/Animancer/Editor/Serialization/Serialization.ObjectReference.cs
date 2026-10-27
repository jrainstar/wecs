// Serialization // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

#if !UNITY_6000_3_OR_NEWER
using EntityId = System.Int32;
#endif

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Shared File Last Modified: 2026-06-10.
namespace Animancer.Editor
// namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] Various serialization utilities.</summary>
    public partial class Serialization
    {
        /// <summary>[Editor-Only]
        /// <list type="bullet">
        /// <item>In Unity 6.3+: returns the entity ID of the object.</item>
        /// <item>In older versions: returns the instance ID of the object.</item>
        /// </list>
        /// </summary>
        public static EntityId GetEntityId(Object obj)
#if UNITY_6000_3_OR_NEWER
            => obj.GetEntityId();
#else
            => obj.GetInstanceID();
#endif

        /// <summary>[Editor-Only] Returns the object with the specified `entityId`.</summary>
        public static Object EntityIdToObject(EntityId entityId)
#if UNITY_6000_3_OR_NEWER
            => EditorUtility.EntityIdToObject(entityId);
#else
            => EditorUtility.InstanceIDToObject(entityId);
#endif

        /// <summary>[Editor-Only]
        /// Directly serializing an <see cref="UnityEngine.Object"/> reference doesn't always work (such as with scene
        /// objects when entering Play Mode), so this class also serializes their instance ID and uses that if the
        /// direct reference fails.
        /// </summary>
        [Serializable]
        public class ObjectReference
        {
            /************************************************************************************************************************/

            [SerializeField] private Object _Object;
            [SerializeField] private EntityId _EntityID;

            /************************************************************************************************************************/

            /// <summary>The referenced <see cref="SerializedObject"/>.</summary>
            public Object Object
            {
                get
                {
                    Initialize();
                    return _Object;
                }
            }

            /// <summary>The <see cref="GetEntityId"/>.</summary>
            public EntityId EntityID => _EntityID;

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ObjectReference"/> which wraps the specified
            /// <see cref="UnityEngine.Object"/>.
            /// </summary>
            public ObjectReference(Object obj)
            {
                _Object = obj;
                if (obj != null)
                    _EntityID = GetEntityId(obj);
            }

            /************************************************************************************************************************/

            private void Initialize()
            {
                if (_Object == null)
                    _Object = EntityIdToObject(_EntityID);
                else
                    _EntityID = GetEntityId(_Object);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ObjectReference"/> which wraps the specified
            /// <see cref="UnityEngine.Object"/>.
            /// </summary>
            public static implicit operator ObjectReference(Object obj)
                => new(obj);

            /// <summary>Returns the target <see cref="Object"/>.</summary>
            public static implicit operator Object(ObjectReference reference)
                => reference.Object;

            /************************************************************************************************************************/

            /// <summary>Creates a new array of <see cref="ObjectReference"/>s representing the `objects`.</summary>
            public static ObjectReference[] Convert(params Object[] objects)
            {
                var references = new ObjectReference[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                    references[i] = objects[i];
                return references;
            }

            /// <summary>
            /// Creates a new array of <see cref="UnityEngine.Object"/>s containing the target <see cref="Object"/> of each
            /// of the `references`.
            /// </summary>
            public static Object[] Convert(params ObjectReference[] references)
            {
                var objects = new Object[references.Length];
                for (int i = 0; i < references.Length; i++)
                    objects[i] = references[i];
                return objects;
            }

            /************************************************************************************************************************/

            /// <summary>Indicates whether both arrays refer to the same set of objects.</summary>
            public static bool AreSameObjects(ObjectReference[] references, Object[] objects)
            {
                if (references == null)
                    return objects == null;

                if (objects == null)
                    return false;

                if (references.Length != objects.Length)
                    return false;

                for (int i = 0; i < references.Length; i++)
                {
                    if (references[i] != objects[i])
                        return false;
                }

                return true;
            }

            /************************************************************************************************************************/

            /// <summary>Returns a string describing this object.</summary>
            public override string ToString()
                => $"Serialization.ObjectReference [{_EntityID}] {_Object}";

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the `reference` and <see cref="ObjectReference.Object"/> are not null.</summary>
        public static bool IsValid(this ObjectReference reference)
            => reference?.Object != null;

        /************************************************************************************************************************/
    }
}

#endif
