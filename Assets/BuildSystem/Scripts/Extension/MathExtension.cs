using UnityEngine;

public static class MathExtension
    {
        #region Methods 

        public static Bounds GetChildsBounds(this GameObject target)
        {
            MeshRenderer[] renders = target.GetComponentsInChildren<MeshRenderer>();

            Quaternion currentRotation = target.transform.rotation;

            Vector3 currentScale = target.transform.localScale;

            target.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            target.transform.localScale = Vector3.one;

            Bounds resultBounds = new Bounds(target.transform.position, Vector3.zero);

            foreach (Renderer item in renders)
            {
                resultBounds.Encapsulate(item.bounds);
            }

            Vector3 relativeCenter = resultBounds.center - target.transform.position;

            resultBounds.center = relativeCenter;

            resultBounds.size = resultBounds.size;

            target.transform.rotation = currentRotation;

            target.transform.localScale = currentScale;

            return resultBounds;
        }

        public static Bounds ConvertBoundsToWorld(this Transform transform, Bounds localBounds)
        {
            return new Bounds(transform.TransformPoint(localBounds.center), new Vector3(localBounds.size.x * transform.localScale.x,
                localBounds.size.y * transform.localScale.y,
                localBounds.size.z * transform.localScale.z));
        }

        public static float ConvertToGrid(float gridSize, float gridOffset, float axis)
        {
            return Mathf.Round(axis) * gridSize + gridOffset;
        }

        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            value.z = Mathf.Clamp(value.z, min.z, max.z);
            return value;
        }

        #endregion
    }