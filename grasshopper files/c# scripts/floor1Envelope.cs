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
		object footprintCurve,
		double wallThickness,
		double floorHeight,
		string floorType,
		ref object floorEnvelope)
    {
        // 1. Cast input to a Rhino curve
        var outer = footprintCurve as Rhino.Geometry.Curve;
        if (outer == null)
        {
            floorEnvelope = null;
            return;
        }

        // 2. Only do work if floorType is "arcade"
        if (!string.Equals(floorType, "arcade", StringComparison.OrdinalIgnoreCase))
        {
            floorEnvelope = null;
            return;
        }

        double tol = RhinoDocument.ModelAbsoluteTolerance;

        // 3. If the input is a polyline that isn't closed, close it
        if (!outer.IsClosed)
        {
            Rhino.Geometry.Polyline pl;
            if (outer.TryGetPolyline(out pl))
            {
                if (!pl.IsClosed) pl.Add(pl[0]);
                outer = new Rhino.Geometry.PolylineCurve(pl);
            }
        }

        // 4. Offset inward by wallThickness
        var inners = outer.Offset(
            Rhino.Geometry.Plane.WorldXY,
            -wallThickness,
            tol,
            Rhino.Geometry.CurveOffsetCornerStyle.Sharp);
        if (inners == null || inners.Length == 0)
        {
            floorEnvelope = null;
            return;
        }
        var inner = inners[0];

        // 5. Ensure the inner curve is closed too
        if (!inner.IsClosed)
        {
            Rhino.Geometry.Polyline pl2;
            if (inner.TryGetPolyline(out pl2))
            {
                if (!pl2.IsClosed) pl2.Add(pl2[0]);
                inner = new Rhino.Geometry.PolylineCurve(pl2);
            }
        }

        // 6. Make a planar Brep ring between outer & inner
        var planar = Rhino.Geometry.Brep.CreatePlanarBreps(
            new Rhino.Geometry.Curve[] { outer, inner },
            tol);
        if (planar == null || planar.Length == 0)
        {
            floorEnvelope = null;
            return;
        }
        var region = planar[0];

        // 7. Build a spine line straight up from the region's centroid
        var bbox = region.GetBoundingBox(true);
        var bottom = bbox.Center;
        var top    = bbox.Center;
        top.Z += floorHeight;
        var spine  = new Rhino.Geometry.LineCurve(bottom, top);

        // 8. Extrude that single planar face along the spine (caps both ends)
        var face      = region.Faces[0];
        var extruded  = face.CreateExtrusion(spine, true);

        floorEnvelope = extruded;  
    }
}
