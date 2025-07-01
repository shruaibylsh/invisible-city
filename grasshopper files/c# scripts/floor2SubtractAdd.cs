#region Usings
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    /// <summary>
    /// Main entry: generates arches, columns, or solid windows based on floorType,
    /// then unions all geometry into subtractAdd, and lifts it by floorHeight + floorThickness.
    /// </summary>
    private void RunScript(
		double colWidth,
		double archWidth,
		double wallThickness,
		int bayCountX,
		int bayCountY,
		double colHeight,
		string floorType,
		object length,
		object width,
		double floorHeight,
		double floorThickness,
		ref object subtractAdd)
    {
        // Convert inputs
        double lengthVal = ToDouble(length);
        double widthVal  = ToDouble(width);
        double tol       = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;
        var breps        = new List<Brep>();

        // ---- ARCADE: extruded arches along X & Y edges ----
        if (floorType == "arcade")
        {
            double extrudeY = -lengthVal;
            double extrudeX =  widthVal;
            double step     = colWidth + archWidth;
            double offset   = wallThickness + 0.5 * colWidth + 0.5 * archWidth;

            // X-direction arches
            for (int i = 0; i < bayCountX; i++)
            {
                double cx = offset + i * step;
                var bl   = new Point3d(cx - archWidth/2, 0, 0);
                var br   = new Point3d(cx + archWidth/2, 0, 0);
                var tl   = new Point3d(cx - archWidth/2, 0, colHeight);
                var tr   = new Point3d(cx + archWidth/2, 0, colHeight);
                var apex = new Point3d(cx, 0, colHeight + archWidth/2);

                var curves = new[] {
                    new Arc(tl, apex, tr).ToNurbsCurve(),
                    new Line(br, tr).ToNurbsCurve(),
                    new Line(bl, br).ToNurbsCurve(),
                    new Line(tl, bl).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined?.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, extrudeY, true);
                    if (extr != null) breps.Add(extr.ToBrep(true));
                }
            }

            // Y-direction arches
            for (int j = 0; j < bayCountY; j++)
            {
                double cy = offset + j * step;
                var bl   = new Point3d(0, cy - archWidth/2, 0);
                var br   = new Point3d(0, cy + archWidth/2, 0);
                var tl   = new Point3d(0, cy - archWidth/2, colHeight);
                var tr   = new Point3d(0, cy + archWidth/2, colHeight);
                var apex = new Point3d(0, cy, colHeight + archWidth/2);

                var curves = new[] {
                    new Arc(tl, apex, tr).ToNurbsCurve(),
                    new Line(br, tr).ToNurbsCurve(),
                    new Line(bl, br).ToNurbsCurve(),
                    new Line(tl, bl).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined?.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, extrudeX, true);
                    if (extr != null) breps.Add(extr.ToBrep(true));
                }
            }
        }

        // ---- COLONNADE: columns with square+circle tiers, duplicated on opposite wall faces ----
        else if (floorType == "colonnade")
        {
            double radius = wallThickness * 0.5;
            double height = colHeight + archWidth * 0.5 + 0.5;

            // Along X-edge (front/back)
            double xStart = radius;
            double xEnd   = widthVal - radius;
            double segX   = bayCountX > 0 ? (xEnd - xStart) / bayCountX : 0;
            double yPos   = radius;
            for (int i = 0; i <= bayCountX; i++)
            {
                double x = xStart + segX * i;
                AddColumnAt(breps, new Point3d(x, yPos, 0), radius, height);
                AddColumnAt(breps, new Point3d(x, yPos + (lengthVal - wallThickness), 0), radius, height);
            }

            // Along Y-edge (left/right)
            double yStart = radius;
            double yEnd   = lengthVal - radius;
            double segY   = bayCountY > 0 ? (yEnd - yStart) / bayCountY : 0;
            double xPos   = radius;
            for (int j = 0; j <= bayCountY; j++)
            {
                double y = yStart + segY * j;
                AddColumnAt(breps, new Point3d(xPos, y, 0), radius, height);
                AddColumnAt(breps, new Point3d(xPos + (widthVal - wallThickness), y, 0), radius, height);
            }
        }

        // ---- SOLID: random windows punched through solid walls ----
        else if (floorType == "solid")
        {
            bool[] winX = new bool[bayCountX], winY = new bool[bayCountY];
            double[] hX = new double[bayCountX], wX = new double[bayCountX];
            double[] hY = new double[bayCountY], wY = new double[bayCountY];
            var rand = new Random();
            for (int i = 0, j = bayCountX - 1; i <= j; i++, j--)
            {
                bool f = rand.Next(2) == 1;
                winX[i] = winX[j] = f;
                hX[i] = hX[j] = 0.5 + rand.NextDouble() * (colHeight - 1);
                wX[i] = wX[j] = 0.5 + (archWidth + colWidth) / 4 * rand.NextDouble();
            }
            for (int i = 0, j = bayCountY - 1; i <= j; i++, j--)
            {
                bool f = rand.Next(2) == 1;
                winY[i] = winY[j] = f;
                hY[i] = hY[j] = 0.5 + rand.NextDouble() * (colHeight - 1);
                wY[i] = wY[j] = 0.5 + (archWidth + colWidth) / 4 * rand.NextDouble();
            }

            double off = wallThickness + 0.5 * (colWidth + archWidth);

            // X-facing windows
            for (int i = 0; i < bayCountX; i++)
            {
                if (!winX[i]) continue;
                double t = bayCountX > 1 ? i / (double)(bayCountX - 1) : 0;
                var ctr = new Point3d(off + t * (widthVal - 2 * off), 0, 0.85);
                var pts = new[] {
                    ctr - Vector3d.XAxis * (wX[i]/2),
                    ctr + Vector3d.XAxis * (wX[i]/2),
                    ctr + Vector3d.XAxis * (wX[i]/2) + Vector3d.ZAxis * hX[i],
                    ctr - Vector3d.XAxis * (wX[i]/2) + Vector3d.ZAxis * hX[i]
                };
                var curves = new[] {
                    new Line(pts[0], pts[1]).ToNurbsCurve(),
                    new Line(pts[1], pts[2]).ToNurbsCurve(),
                    new Line(pts[2], pts[3]).ToNurbsCurve(),
                    new Line(pts[3], pts[0]).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined?.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, -lengthVal, true);
                    if (extr != null) breps.Add(extr.ToBrep(true));
                }
            }

            // Y-facing windows
            for (int j = 0; j < bayCountY; j++)
            {
                if (!winY[j]) continue;
                double t = bayCountY > 1 ? j / (double)(bayCountY - 1) : 0;
                var ctr = new Point3d(0, off + t * (lengthVal - 2 * off), 0.85);
                var pts = new[] {
                    ctr - Vector3d.YAxis * (wY[j]/2),
                    ctr + Vector3d.YAxis * (wY[j]/2),
                    ctr + Vector3d.YAxis * (wY[j]/2) + Vector3d.ZAxis * hY[j],
                    ctr - Vector3d.YAxis * (wY[j]/2) + Vector3d.ZAxis * hY[j]
                };
                var curves = new[] {
                    new Line(pts[0], pts[1]).ToNurbsCurve(),
                    new Line(pts[1], pts[2]).ToNurbsCurve(),
                    new Line(pts[2], pts[3]).ToNurbsCurve(),
                    new Line(pts[3], pts[0]).ToNurbsCurve()
                };
                var joined = Curve.JoinCurves(curves, tol);
                if (joined?.Length > 0)
                {
                    var prof = joined[0];
                    var extr = Extrusion.Create(prof, widthVal, true);
                    if (extr != null) breps.Add(extr.ToBrep(true));
                }
            }
        }

        // ---- FINAL UNION ----
        try
        {
            subtractAdd = Brep.CreateBooleanUnion(breps, tol);
        }
        catch
        {
            subtractAdd = breps.ToArray();
        }

        // ---- TRANSLATE UNIONED GEOMETRY UP BY floorHeight + floorThickness ----
        double dz = floorHeight + floorThickness;
        var xform = Transform.Translation(0, 0, dz);
        if (subtractAdd is Brep[] arr)
        {
            foreach (var b in arr)
                b.Transform(xform);

            subtractAdd = arr;
        }
    }

    /// <summary>
    /// Builds one column at basePt with:
    /// top-down: square cap (0.025H), circular cap (0.025H),
    /// shaft (0.90H), circular base (0.025H), square base (0.025H).
    /// </summary>
    private void AddColumnAt(
        List<Brep> breps,
        Point3d basePt,
        double r,
        double height)
    {
        double sqBaseH   = 0.025 * height;
        double circBaseH = 0.025 * height;
        double shaftH    = 0.90  * height;
        double circCapH  = 0.025 * height;
        double sqCapH    = 0.025 * height;

        // --- Square extrusion cap (top) ---
        var capSqPlane = new Plane(
            basePt + Vector3d.ZAxis * (sqBaseH + circBaseH + shaftH + circCapH),
            Vector3d.ZAxis
        );
        var capSqBox = new Box(
            capSqPlane,
            new Interval(-r, r),
            new Interval(-r, r),
            new Interval(0, sqCapH)
        );
        breps.Add(Brep.CreateFromBox(capSqBox));

        // --- Circular extrusion cap ---
        breps.Add(
            new Cylinder(
                new Circle(
                    new Plane(
                        basePt + Vector3d.ZAxis * (sqBaseH + circBaseH + shaftH),
                        Vector3d.ZAxis
                    ),
                    r
                ),
                circCapH
            ).ToBrep(true, true)
        );

        // --- Shaft ---
        breps.Add(
            new Cylinder(
                new Circle(
                    new Plane(
                        basePt + Vector3d.ZAxis * (sqBaseH + circBaseH),
                        Vector3d.ZAxis
                    ),
                    r * 0.8
                ),
                shaftH
            ).ToBrep(true, true)
        );

        // --- Circular extrusion base ---
        breps.Add(
            new Cylinder(
                new Circle(
                    new Plane(
                        basePt + Vector3d.ZAxis * sqBaseH,
                        Vector3d.ZAxis
                    ),
                    r
                ),
                circBaseH
            ).ToBrep(true, true)
        );

        // --- Square extrusion base (bottom) ---
        var baseSqPlane = new Plane(basePt, Vector3d.ZAxis);
        var baseSqBox   = new Box(
            baseSqPlane,
            new Interval(-r, r),
            new Interval(-r, r),
            new Interval(0, sqBaseH)
        );
        breps.Add(Brep.CreateFromBox(baseSqBox));
    }

    /// <summary>
    /// Helper: convert int or double input to double.
    /// </summary>
    private double ToDouble(object o)
    {
        if (o is double d) return d;
        if (o is int i)    return i;
        return Convert.ToDouble(o);
    }
}
