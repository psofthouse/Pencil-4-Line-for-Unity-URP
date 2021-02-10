using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Pcl4Editor
{
    public class URPVersionWindow : VersionWindow
    {
        [MenuItem("Pencil+ 4/About URP", false, 3)]
        private static void Open()
        {
            OpenWithPackageManifestGUID("34dbcecc0c31fa441a77e650ead61639");
        }
    }
}