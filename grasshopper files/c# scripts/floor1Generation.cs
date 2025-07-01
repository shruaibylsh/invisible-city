#region Usings
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(
		object floorEnvelope,
		List<object> subtractAdd,
		object floorType,
		ref object floorWall)
    {
        // Gather inputs
        var envelopes = ToBreps(floorEnvelope);
        var subs       = ToBreps(subtractAdd);
        double tol     = RhinoDocument.ModelAbsoluteTolerance;

        // Pre-union subtractors once
        Brep[] unionSubs = subs.Count > 0
            ? Brep.CreateBooleanUnion(subs, tol) ?? subs.ToArray()
            : Array.Empty<Brep>();

        var pieces = new List<Brep>();

        if (string.Equals(floorType as string, "colonnade", StringComparison.OrdinalIgnoreCase))
        {
            // just pass through the columns
            pieces.AddRange(unionSubs);
        }
        else
        {
            // subtract (if any) from each envelope
            foreach (var env in envelopes)
            {
                if (unionSubs.Length > 0)
                {
                    var diff = Brep.CreateBooleanDifference(new[] { env }, unionSubs, tol);
                    if (diff != null && diff.Length > 0)
                    {
                        pieces.AddRange(diff);
                        continue;
                    }
                }
                pieces.Add(env);
            }
        }

        // final union of everything
        Brep[] finalUnion = Brep.CreateBooleanUnion(pieces, tol) ?? pieces.ToArray();

        // pack into a single tree branch
        var tree = new GH_Structure<IGH_Goo>();
        var path = new GH_Path(0);
        foreach (var b in finalUnion)
            tree.Append(new GH_Brep(b), path);

        floorWall = tree;
    }

    // Flatten any Brep, Brep[], or collection of Breps into a List<Brep>
    private List<Brep> ToBreps(object input)
    {
        var list = new List<Brep>();
        switch (input)
        {
            case Brep b:            list.Add(b);                      break;
            case Brep[] ba:         list.AddRange(ba);                break;
            case IEnumerable ie:
                foreach (var x in ie)
                    if (x is Brep br) list.Add(br);
                break;
        }
        return list;
    }
}
