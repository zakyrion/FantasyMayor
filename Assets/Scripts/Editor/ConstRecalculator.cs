using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConstRecalculator
{
    static ConstRecalculator()
    {
    }

    private static string GetFilePath(Type attributeType)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes(attributeType, true);

                if (attributes.Any())
                {
                    var codeBase = assembly.CodeBase;
                    var uri = new UriBuilder(codeBase);
                    var path = Uri.UnescapeDataString(uri.Path);
                    return Path.GetDirectoryName(path);
                }
            }
        }

        return null;
    }

    private static void RecalculateConsts()
    {
        // var text = File.ReadAllText(file);
        // text = text.Replace("public const float TriangleSegmentSize = 1;", "public const float TriangleSegmentSize = 0.5f;");
        // File.WriteAllText(file, text);
        // var hexDataLayer = new HexDataLayer();
        // var hexVectors = hexDataLayer.HexVectors;
        // var hexVectorUtil = new HexVectorUtil();
        // hexVectorUtil.TriangleSegmentSize = 1;
        // var hexVector = new HexVector();
        // hexVector.WorldPosition = new UnityEngine.Vector3(0, 0, 0);
        // var int2 = hexVectorUtil.CalculateGridPosition(hexVector.WorldPosition);
        // var int2_1 = hexVectorUtil.CalculateGridPosition(new UnityEngine.Rect());
        // var int2_2 = hexVectorUtil.Neighbour(0);
        // var float3 = hexVectorUtil.CalculateWorldPosition(new Unity.Mathematics.int2());
    }
}