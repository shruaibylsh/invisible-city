// Grasshopper Script Instance
#region Usings
using System;
using Rhino;
using Rhino.Geometry;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(
		object footprintCurve,
		double wallThickness,
		double floorHeight,
		string floorType,
		ref object floorEnvelope)
    {
        floorEnvelope = null;
        if (floorType == "colonnade") return;

        var outer = footprintCurve as Curve;
        if (outer == null) return;

        // close polyline if needed
        if (!outer.IsClosed && outer.TryGetPolyline(out var pl))
        {
            if (!pl.IsClosed) pl.Add(pl[0]);
            outer = new PolylineCurve(pl);
        }

        double tol = RhinoDocument.ModelAbsoluteTolerance;
        var inners = outer.Offset(Plane.WorldXY, -wallThickness, tol, CurveOffsetCornerStyle.Sharp);
        if (inners == null || inners.Length == 0) return;

        var inner = inners[0];
        if (!inner.IsClosed && inner.TryGetPolyline(out var pl2))
        {
            if (!pl2.IsClosed) pl2.Add(pl2[0]);
            inner = new PolylineCurve(pl2);
        }

        var breps = Brep.CreatePlanarBreps(new[] { outer, inner }, tol);
        if (breps == null || breps.Length == 0) return;

        var region = breps[0];
        var c     = region.GetBoundingBox(true).Center;
        var spine = new LineCurve(c, c + Vector3d.ZAxis * floorHeight);

        floorEnvelope = region.Faces[0].CreateExtrusion(spine, true);
    }
}
