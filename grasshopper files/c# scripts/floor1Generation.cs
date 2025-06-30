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
		object subtractAdd,
		object floorType,
		ref object floorWall)
    {
        var envelopes = ExtractBreps(floorEnvelope);
        var subs      = ExtractBreps(subtractAdd);
        double tol   = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoMath.UnsetValue;

        Brep[] unionSubs;
        try { unionSubs = Brep.CreateBooleanUnion(subs.ToArray(), tol); }
        catch { unionSubs = subs.ToArray(); }

        string type = (floorType as string)?.ToLowerInvariant();
        var result = new List<Brep>();

        if (type == "colonnade")
        {
            result.AddRange(unionSubs);
        }
        
        else
        {
            var toSubtract = subs.Count > 0 ? subs[0] : null;
            foreach (var env in envelopes)
            {
                if (toSubtract != null)
                {
                    try
                    {
                        var diffs = Brep.CreateBooleanDifference(new[]{ env }, new[]{ toSubtract }, tol);
                        if (diffs?.Length > 0) { result.Add(diffs[0]); continue; }
                    }
                    catch {}
                }
                result.Add(env);
            }
        }

        var tree = new GH_Structure<IGH_Goo>();
        var path = new GH_Path(0);
        foreach (var brep in result)
            tree.Append(new GH_Brep(brep), path);

        floorWall = tree;
    }

    private List<Brep> ExtractBreps(object input)
    {
        var list = new List<Brep>();
        switch (input)
        {
            case Brep b:
                list.Add(b);
                break;
            case Brep[] ba:
                list.AddRange(ba);
                break;
            case IEnumerable ie:
                foreach (var item in ie)
                    if (item is Brep br)
                        list.Add(br);
                break;
        }
        return list;
    }
}
