using System.Collections.Generic;
using UnityEngine;

    public static class PhysicExtension
    {
        #region Fields

        public const int MAX_ALLOC_COUNT = 128;

        #endregion

        #region Methods

        public static void SetLayerRecursively(this GameObject go, LayerMask layer)
        {
            if (go == null)
            {
                return;
            }

            go.layer = ToLayer(layer.value);

            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (go.transform.GetChild(i) == null)
                {
                    continue;
                }

                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
            }
        }

        public static int ToLayer(int bitmask)
        {
            int Result = bitmask > 0 ? 0 : 31;

            while (bitmask > 1)
            {
                int v = bitmask >> 1;
                bitmask = v;

                Result++;
            }

            return Result;
        }

        private static Collider[] SphereCache = new Collider[MAX_ALLOC_COUNT];
        public static T[] GetNeighborsTypeBySphere<T>(Vector3 position, float size, LayerMask layer, QueryTriggerInteraction query = QueryTriggerInteraction.UseGlobal)
        {
            SphereCache = new Collider[MAX_ALLOC_COUNT];
            int ColliderCount = Physics.OverlapSphereNonAlloc(position, size, SphereCache, layer, query);

            List<T> Types = new List<T>();

            for (int i = 0; i < ColliderCount; i++)
            {
                T Type = SphereCache[i].GetComponentInParent<T>();

                if (Type != null)
                {
                    if (Type is T)
                    {
                        if (!Types.Contains(Type))
                        {
                            Types.Add(Type);
                        }
                    }
                }
            }

            return Types.ToArray();
        }

        private static Collider[] BoxCache = new Collider[MAX_ALLOC_COUNT];
        public static T[] GetNeighborsTypeByBox<T>(Vector3 position, Vector3 size, Quaternion rotation, LayerMask layer, QueryTriggerInteraction query = QueryTriggerInteraction.UseGlobal)
        {
            bool InitQueries = Physics.queriesHitTriggers;

            Physics.queriesHitTriggers = true;

            BoxCache = new Collider[MAX_ALLOC_COUNT];
            int ColliderCount = Physics.OverlapBoxNonAlloc(position, size, BoxCache, rotation, layer, query);

            Physics.queriesHitTriggers = InitQueries;

            List<T> Types = new List<T>();

            for (int i = 0; i < ColliderCount; i++)
            {
                T Type = BoxCache[i].GetComponentInParent<T>();

                if (Type != null)
                {
                    if (Type is T)
                    {
                        if (!Types.Contains(Type))
                        {
                            Types.Add(Type);
                        }
                    }
                }
            }

            return Types.ToArray();
        }

        #endregion
    }