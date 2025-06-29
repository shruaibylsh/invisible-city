// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object brepInput, ref object report)
    {
        var brep = (brepInput as GH_Brep)?.Value ?? brepInput as Brep;
        if (brep == null)
        {
            report = "Invalid input - provide a Brep";
            return;
        }

        string orientationReport = "";
        int incorrectFaces = 0;
        double tol = RhinoDocument.ModelAbsoluteTolerance;

        foreach (var face in brep.Faces)
        {
            var amp = AreaMassProperties.Compute(face);
            if (amp == null) continue;

            var center = amp.Centroid;
            var normal = face.NormalAt(face.Domain(0).Mid, face.Domain(1).Mid);
            normal.Unitize();

            // Test point slightly offset along normal
            var testPoint = center + normal * 0.1;
            bool isInside = brep.IsPointInside(testPoint, tol, false);

            if (isInside)
            {
                incorrectFaces++;
                orientationReport += $"Face {face.FaceIndex}: Normal points INTO solid (incorrect)\n";
            }
            else
            {
                orientationReport += $"Face {face.FaceIndex}: Normal points OUT (correct)\n";
            }
        }

        report = $"ORIENTATION REPORT:\n" +
                 $"Total faces: {brep.Faces.Count}\n" +
                 $"Incorrect faces: {incorrectFaces}\n\n" +
                 orientationReport;
    }
}