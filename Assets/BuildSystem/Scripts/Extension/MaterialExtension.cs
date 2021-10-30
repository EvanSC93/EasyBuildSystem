using System.Collections.Generic;
using UnityEngine;

    public static class MaterialExtension
    {
        #region Methods

        public static void ChangeAllMaterialsColorInChildren(this GameObject go, Renderer[] renderers, Color color)
        {
            Renderer[] Renderers = go.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i] != null)
                {
                    for (int x = 0; x < Renderers[i].materials.Length; x++)
                    {
                        Renderers[i].materials[x].SetColor("_BaseColor", color);
                    }
                }
            }
        }

        public static void ChangeAllMaterialsInChildren(this GameObject go, Renderer[] renderers, Material material)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer temp = renderers[i];
                if (temp != null)
                {
                    Material[] materials = new Material[temp.sharedMaterials.Length];

                    for (int x = 0; x < temp.sharedMaterials.Length; x++)
                    {
                        materials[x] = material;
                    }

                    temp.sharedMaterials = materials;
                }
            }
        }

        public static void ChangeAllMaterialsInChildren(this GameObject go, Renderer[] renderers, Dictionary<Renderer, Material[]> materials)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] CacheMaterials = renderers[i].sharedMaterials;

                for (int c = 0; c < CacheMaterials.Length; c++)
                {
                    CacheMaterials[c] = materials[renderers[i]][c];
                }

                renderers[i].materials = CacheMaterials;
            }
        }

        #endregion
    }