#region Usings
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(
		object floor1Envelope,
		object floor1SubtractAdd,
		object floor1Type,
		ref object a)
    {
        // 1. Extract Brep lists from inputs
        var envList = ExtractBreps(floor1Envelope);
        var subList = ExtractBreps(floor1SubtractAdd);

        double tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoMath.UnsetValue;

        // 2. Union all subtractAdd breps
        Brep[] unionSub;
        try
        {
            unionSub = Brep.CreateBooleanUnion(subList.ToArray(), tol);
        }
        catch
        {
            unionSub = subList.ToArray();
        }

        // 3. Branch by floor1Type
        string typeStr = floor1Type as string;
        Brep[] result = null;

        if (string.Equals(typeStr, "arcade", StringComparison.OrdinalIgnoreCase))
        {
            // Combine envelope and unionSub
            var combined = new List<Brep>();
            combined.AddRange(envList);
            if (unionSub != null)
                combined.AddRange(unionSub);

            // Boolean union of envelope and subtractAdd
            try
            {
                result = Brep.CreateBooleanUnion(combined.ToArray(), tol);
            }
            catch
            {
                result = combined.ToArray();
            }
        }
        else if (string.Equals(typeStr, "colonnade", StringComparison.OrdinalIgnoreCase))
        {
            // Only output unioned subtractAdd
            result = unionSub;
        }
        else
        {
            // Default: return envelope only
            result = envList.ToArray();
        }

        a = (result != null && result.Length > 0) ? (object)result : null;
    }

    // Helper: extract Brep list from various input types
    private List<Brep> ExtractBreps(object input)
    {
        var breps = new List<Brep>();
        if (input is Brep b)
        {
            breps.Add(b);
        }
        else if (input is Brep[] ba)
        {
            breps.AddRange(ba);
        }
        else if (input is IEnumerable ie)
        {
            foreach (var item in ie)
            {
                if (item is Brep br)
                    breps.Add(br);
            }
        }
        return breps;
    }
}
