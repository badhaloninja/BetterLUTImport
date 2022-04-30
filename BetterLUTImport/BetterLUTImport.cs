using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Threading.Tasks;
using CodeX;
using BaseX;

namespace BetterLUTImport
{
    public class BetterLUTImport : NeosMod
    {
        public override string Name => "BetterLUTImport";
        public override string Author => "badhaloninja";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/badhaloninja/BetterLUTImport";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.badhaloninja.BetterLUTImport");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ImageImporter), "ImportLUT")]
        class ImageImporter_ImportLUT_Patch
        {
            public static bool Prefix(string path, Slot targetSlot)
            {
                NewLUTImport(path, targetSlot).ConfigureAwait(false);
                return false;
            }

            private static async Task NewLUTImport(string path, Slot targetSlot)
            {
                await new ToBackground(); // To Background thread
                var bitmap2D = Bitmap2D.Load(path, false, false);
                Uri uri;
                if (bitmap2D.Size.x != 1024 || bitmap2D.Size.y < 32) return;
                var texture = new Bitmap3D(32, 32, 32, TextureFormat.RGBA32, false);
                for (int y1=0; y1 < 32; ++y1)
                {
                    for (int x1 =0; x1< 1024; ++x1)
                    {
                        int num1 = x1 % 32;
                        int num2 = x1 / 32;

                        int x2 = num1;
                        int z = num2;
                        color pixel = bitmap2D.GetPixel(x1, y1);
                        texture.SetPixel(x2, y1, z, in pixel);
                    }
                }

                uri = await targetSlot.Engine.LocalDB.SaveAssetAsync(texture).ConfigureAwait(false);

                await new ToWorld(); // To Locking thread

                var lutTex = targetSlot.AttachComponent<StaticTexture3D>();
                lutTex.URL.Value = uri;

                var lutMat = targetSlot.AttachComponent<LUT_Material>();
                lutMat.LUT.Target = lutTex;

                MaterialOrb.ConstructMaterialOrb(lutMat, targetSlot);
                lutMat.GetGizmo();
            }

        }
    }
}