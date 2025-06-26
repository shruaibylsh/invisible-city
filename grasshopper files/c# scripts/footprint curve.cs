// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

private void RunScript(
		string footprint,
		double length,
		double width,
		double insetLength,
		double insetWidth,
		ref object a)
    {
        // Tolerance for boolean operations
        double tol = Rhino.RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? Rhino.RhinoMath.UnsetValue;

        // 1. Create the outer rectangle
        var outer = new Rectangle3d(Plane.WorldXY, width, length).ToNurbsCurve();

        // 2. Prepare result list for footprint curves
        List<Curve> result = new List<Curve>();

        // 3. Compute footprint based on type
        switch (footprint)
        {
            case "Rectangle":
                result.Add(outer);
                break;

            case "L":
                {
                    var notch = new Rectangle3d(Plane.WorldXY, insetWidth, insetLength).ToNurbsCurve();
                    notch.Translate(new Vector3d(width - insetWidth, length - insetLength, 0));
                    var diffs = Curve.CreateBooleanDifference(outer, notch, tol);
                    if (diffs != null) result.AddRange(diffs);
                }
                break;

            case "C":
                {
                    var notch = new Rectangle3d(Plane.WorldXY, insetWidth, insetLength).ToNurbsCurve();
                    notch.Translate(new Vector3d((width - insetWidth) / 2.0, length - insetLength, 0));
                    var diffs = Curve.CreateBooleanDifference(outer, notch, tol);
                    if (diffs != null)
                        result.AddRange(diffs);
                }
                break;

            case "Courtyard":
                {
                    var hole = new Rectangle3d(Plane.WorldXY, insetWidth, insetLength).ToNurbsCurve();
                    hole.Translate(new Vector3d((width - insetWidth) / 2.0, (length - insetLength) / 2.0, 0));
                    var diffs = Curve.CreateBooleanDifference(outer, hole, tol);
                    if (diffs != null)
                        result.AddRange(diffs);
                }
                break;

            default:
                result.Add(outer);
                break;
        }

        // 4. Output handling
        // Preserve both outer and hole for courtyard
        if (footprint == "Courtyard")
        {
            a = result.Count > 0 ? (object)result : null;
            return;
        }

        // For other types, if only one curve, output it
        if (result.Count == 1)
        {
            a = result[0];
            return;
        }

        // If multiple segments, try to join
        if (result.Count > 1)
        {
            var joined = Curve.JoinCurves(result, tol);
            if (joined != null && joined.Length > 0)
            {
                a = joined[0];
                return;
            }
        }

        // Fallback: null if nothing
        a = null;
    }
}
