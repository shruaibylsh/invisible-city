// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(
		string footprint,
		double length,
		double width,
		double insetLength,
		double insetWidth,
		double wallThickness,
		string floorType,
		ref object footprintCurve)
    {
        double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        var outer = new Rectangle3d(Plane.WorldXY, width, length).ToNurbsCurve();
        var curves = new List<Curve>();

        if (floorType == "colonnade") 
        {
            footprint = "Rectangle";
        }

        switch (footprint)
        {
            case "L":
            case "C":
            {
                // same inset size for both, but different placement
                var notch = new Rectangle3d(Plane.WorldXY, insetWidth, insetLength).ToNurbsCurve();

                if (footprint == "L")
                {
                    // top notch (along X): unchanged
                    double dx = width - insetWidth;
                    notch.Translate(new Vector3d(dx, length - insetLength, 0));
                }
                else
                {
                    // side notch (along Y): flush to right edge, centered vertically
                    double dx = width - insetWidth;
                    double dy = (length - insetLength) / 2.0;
                    notch.Translate(new Vector3d(dx, dy, 0));
                }

                curves.AddRange(
                    Curve.CreateBooleanDifference(outer, notch, tol)
                    ?? Enumerable.Empty<Curve>());
            }
            break;


            case "Courtyard":
                {
                    var hole = new Rectangle3d(Plane.WorldXY, insetWidth, insetLength).ToNurbsCurve();
                    hole.Translate(new Vector3d((width - insetWidth) / 2.0,
                                                (length - insetLength) / 2.0,
                                                0));
                    hole.Reverse();
                    var diffs = Curve.CreateBooleanDifference(outer, hole, tol);
                    if (diffs != null && diffs.Length > 0)
                    {
                        curves.Add(diffs[0]);
                        var inner = hole.DuplicateCurve();
                        inner.Reverse();
                        curves.Add(inner);
                    }
                }
                break;

            default:
                curves.Add(outer);
                break;
        }

        if (curves.Count == 1 && footprint != "Courtyard")
            footprintCurve = curves[0];
        else if (curves.Count > 0)
            footprintCurve = curves;
        else
            footprintCurve = null;
    }
}
