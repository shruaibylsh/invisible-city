// Grasshopper Script Instance
#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(
		List<object> footprintCurve,
		double floorHeight,
		double floorThickness,
		double edgeOffset,
		string footprint,
		double wallThickness,
		ref object ceiling)
    {
        if (footprintCurve == null || footprintCurve.Count == 0)
        {
            ceiling = null;
            return;
        }

        // common translation and tolerance
        var move = Transform.Translation(0, 0, floorHeight);
        double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

        // extract a sample curve to get a plane
        Curve sample = footprintCurve.OfType<Curve>().FirstOrDefault();
        Plane pl;
        if (sample == null || !sample.TryGetPlane(out pl))
            pl = Plane.WorldXY;

        if (footprint == "Courtyard")
        {
            var bases = footprintCurve.OfType<Curve>().Take(2).ToArray();
            if (bases.Length < 2) { ceiling = null; return; }

            // move, offset (out/in), extrude, boolean diff
            Curve c1 = bases[0].DuplicateCurve(); c1.Transform(move);
            Curve c2 = bases[1].DuplicateCurve(); c2.Transform(move);

            Curve o1 = c1.Offset(pl,  edgeOffset,  tol, CurveOffsetCornerStyle.Sharp)?.FirstOrDefault();
            Curve o2 = c2.Offset(pl, -edgeOffset-wallThickness,  tol, CurveOffsetCornerStyle.Sharp)?.FirstOrDefault();
            if (o1 == null || o2 == null) { ceiling = null; return; }

            Brep b1 = Extrusion.Create(o1, floorThickness, true).ToBrep(true);
            Brep b2 = Extrusion.Create(o2, floorThickness, true).ToBrep(true);

            var diff = Brep.CreateBooleanDifference(new[]{b1}, new[]{b2}, tol);
            ceiling = (diff != null && diff.Length > 0) ? diff[0] : null;
        }
        else
        {
            // non-courtyard: move→offset outward→extrude each curve
            var breps = new List<Brep>();
            foreach (var crv in footprintCurve.OfType<Curve>())
            {
                Curve c = crv.DuplicateCurve(); 
                c.Transform(move);

                Curve off = c.Offset(pl, edgeOffset, tol, CurveOffsetCornerStyle.Sharp)?.FirstOrDefault();
                if (off == null) continue;

                var ex = Extrusion.Create(off, floorThickness, true);
                if (ex != null) breps.Add(ex.ToBrep(true));
            }

            ceiling = breps.Count > 0 ? breps : null;
        }
    }
}
