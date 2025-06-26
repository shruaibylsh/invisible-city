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
    private void RunScript(
		double colWidth,
		double archWidth,
		double wallThickness,
		int bayCountX,
		int bayCountY,
		double colHeight,
		string floor1Type,
		object length,
		object width,
		ref object a)
    {
        // Collect output Breps
        var breps = new List<Brep>();
        double tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;

        // Common dimension conversions
        double widthVal  = ToDouble(width);
        double lengthVal = ToDouble(length);

        if (floor1Type == "arcade")
        {
            // Arcade logic: unchanged
            double extrudeY = -lengthVal;
            double extrudeX =  widthVal;
            double step     = archWidth + colWidth;
            double offset   = wallThickness + colWidth * 0.5 + archWidth * 0.5;

            // X-direction arcade openings (extrude along Y)
            for (int i = 0; i < bayCountX; i++)
            {
                double cx = offset + i * step;
                var bl   = new Point3d(cx - archWidth/2.0, 0, 0);
                var br   = new Point3d(cx + archWidth/2.0, 0, 0);
                var tl   = new Point3d(cx - archWidth/2.0, 0, colHeight);
                var tr   = new Point3d(cx + archWidth/2.0, 0, colHeight);
                var apex = new Point3d(cx, 0, colHeight + archWidth/2.0);

                var curves = new List<Curve>
                {
                    new Arc(tl, apex, tr).ToNurbsCurve(),
                    new Line(br, tr).ToNurbsCurve(),
                    new Line(bl, br).ToNurbsCurve(),
                    new Line(tl, bl).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined != null && joined.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, extrudeY, true);
                    if (extr != null)
                        breps.Add(extr.ToBrep(true));
                }
            }

            // Y-direction arcade openings (extrude along X)
            for (int j = 0; j < bayCountY; j++)
            {
                double cy = offset + j * step;
                var bl   = new Point3d(0, cy - archWidth/2.0, 0);
                var br   = new Point3d(0, cy + archWidth/2.0, 0);
                var tl   = new Point3d(0, cy - archWidth/2.0, colHeight);
                var tr   = new Point3d(0, cy + archWidth/2.0, colHeight);
                var apex = new Point3d(0, cy, colHeight + archWidth/2.0);

                var curves = new List<Curve>
                {
                    new Arc(tl, apex, tr).ToNurbsCurve(),
                    new Line(br, tr).ToNurbsCurve(),
                    new Line(bl, br).ToNurbsCurve(),
                    new Line(tl, bl).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined != null && joined.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, extrudeX, true);
                    if (extr != null)
                        breps.Add(extr.ToBrep(true));
                }
            }
        }
        else if (floor1Type == "colonnade")
        {
            // Only update colonnade positions; arcade code remains as above
            double radius    = wallThickness * 0.5;
            double height    = colHeight + archWidth * 0.5 + 0.5;

            // Place columns along X: from (½wt,½wt,0) to (width-½wt,½wt,0)
            double xStart = wallThickness * 0.5;
            double xEnd   = widthVal - wallThickness * 0.5;
            double yPos   = wallThickness * 0.5;
            double segX   = (xEnd - xStart) / bayCountX;
            for (int i = 0; i <= bayCountX; i++)
            {
                double x = xStart + segX * i;
                var center = new Point3d(x, yPos, 0);
                AddTuscanColumn(breps, center, radius, height, tol);
            }

            // Place columns along Y: from (½wt,½wt,0) to (½wt,length-½wt,0)
            double yStart = wallThickness * 0.5;
            double yEnd   = lengthVal - wallThickness * 0.5;
            double xPos   = wallThickness * 0.5;
            double segY   = (yEnd - yStart) / bayCountY;
            for (int j = 0; j <= bayCountY; j++)
            {
                double y = yStart + segY * j;
                var center = new Point3d(xPos, y, 0);
                AddTuscanColumn(breps, center, radius, height, tol);
            }
        }
        else if (floor1Type == "solid")
        {
            // TODO: implement solid building logic
        }

        a = breps.ToArray();
    }

    private void AddTuscanColumn(List<Brep> breps, Point3d baseCenter, double radius, double height, double tol)
    {
        // Tuscan proportions: base 10%, shaft 80%, capital 10%
        double baseH  = height * 0.1;
        double capH   = height * 0.1;
        double shaftH = height - baseH - capH;
        double shaftR = radius * 0.8;
        double capR   = radius * 0.9;

        // Base
        var baseC = new Circle(new Plane(baseCenter, Vector3d.ZAxis), radius);
        breps.Add(new Cylinder(baseC, baseH).ToBrep(true, true));
        // Shaft
        var shaftC = new Circle(
            new Plane(baseCenter + Vector3d.ZAxis * baseH, Vector3d.ZAxis), shaftR);
        breps.Add(new Cylinder(shaftC, shaftH).ToBrep(true, true));
        // Capital
        var capC   = new Circle(
            new Plane(baseCenter + Vector3d.ZAxis * (baseH + shaftH), Vector3d.ZAxis), capR);
        breps.Add(new Cylinder(capC, capH).ToBrep(true, true));
    }

    private double ToDouble(object o)
    {
        if (o is double d) return d;
        if (o is int    i) return i;
        try { return Convert.ToDouble(o); } catch { return 0.0; }
    }
}

