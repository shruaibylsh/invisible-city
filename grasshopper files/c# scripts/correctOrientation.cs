// Grasshopper Script Instance
#region Usings
using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object extrudedShape, ref object correctedShape)
    {
        // 1. Cast input to a Brep
        var brep = extrudedShape as Brep;
        if (brep == null)
        {
            correctedShape = null;
            return;
        }

        // 2. Duplicate for modification
        var fixedBrep = brep.DuplicateBrep();
        bool needsFlip = false;
        double tol = RhinoDocument.ModelAbsoluteTolerance;

        // 3. Check each face: if any normal points inside, mark for flip
        foreach (var face in fixedBrep.Faces)
        {
            var amp = AreaMassProperties.Compute(face);
            if (amp == null) continue;

            var center = amp.Centroid;
            var normal = face.NormalAt(face.Domain(0).Mid, face.Domain(1).Mid);
            normal.Unitize();

            var testPoint = center + normal * 0.1;
            if (fixedBrep.IsPointInside(testPoint, tol, false))
            {
                needsFlip = true;
                break;
            }
        }

        // 4. Flip whole Brep if needed
        if (needsFlip)
            fixedBrep.Flip();

        // 5. Output corrected Brep
        correctedShape = fixedBrep;
    }
}
