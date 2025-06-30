#region Usings
using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
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
		string floorType,
		object length,
		object width,
		ref object subtractAdd)
  {
    // Convert inputs
    double widthVal  = ToDouble(width);
    double lengthVal = ToDouble(length);
    double tol       = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;
    var breps = new List<Brep>();

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
        var bl   = new Point3d(cx - archWidth/2.0, 0, 0);
        var br   = new Point3d(cx + archWidth/2.0, 0, 0);
        var tl   = new Point3d(cx - archWidth/2.0, 0, colHeight);
        var tr   = new Point3d(cx + archWidth/2.0, 0, colHeight);
        var apex = new Point3d(cx,                   0, colHeight + archWidth/2.0);

        var curves = new[]{
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
          if (extr != null) breps.Add(extr.ToBrep(true));
        }
      }

      // Y-direction arches
      for (int j = 0; j < bayCountY; j++)
      {
        double cy = offset + j * step;
        var bl   = new Point3d(0, cy - archWidth/2.0, 0);
        var br   = new Point3d(0, cy + archWidth/2.0, 0);
        var tl   = new Point3d(0, cy - archWidth/2.0, colHeight);
        var tr   = new Point3d(0, cy + archWidth/2.0, colHeight);
        var apex = new Point3d(0, cy,                   colHeight + archWidth/2.0);

        var curves = new[]{
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
          if (extr != null) breps.Add(extr.ToBrep(true));
        }
      }
    }

    else if (floorType == "colonnade")
    {
      double radius = wallThickness * 0.5;
      double height = colHeight + archWidth * 0.5 + 0.5;
      double baseH  = height * 0.1;
      double capH   = baseH;
      double shaftH = height - baseH - capH;
      double shaftR = radius * 0.8;
      double capR   = radius * 0.9;

      // Along X edge
      double xStart = radius;
      double xEnd   = widthVal - radius;
      double segX   = bayCountX > 0 ? (xEnd - xStart) / bayCountX : 0;
      double yPos   = radius;
      for (int i = 0; i <= bayCountX; i++)
      {
        double x = xStart + segX * i;
        AddColumnAt(breps, new Point3d(x, yPos, 0), radius, baseH, shaftR, shaftH, capR, capH);
      }

      // Along Y edge
      double yStart = radius;
      double yEnd   = lengthVal - radius;
      double segY   = bayCountY > 0 ? (yEnd - yStart) / bayCountY : 0;
      double xPos   = radius;
      for (int j = 0; j <= bayCountY; j++)
      {
        double y = yStart + segY * j;
        AddColumnAt(breps, new Point3d(xPos, y, 0), radius, baseH, shaftR, shaftH, capR, capH);
      }
    }

    else if (floorType == "solid")
    {
      // Random window pattern arrays
      bool[] winX = new bool[bayCountX], winY = new bool[bayCountY];
      double[] hX = new double[bayCountX], wX = new double[bayCountX];
      double[] hY = new double[bayCountY], wY = new double[bayCountY];
      var rand = new Random();
      for (int i = 0, j = bayCountX - 1; i <= j; i++, j--)
      {
        bool f = rand.Next(2) == 1;
        winX[i] = winX[j] = f;
        hX[i] = hX[j] = 0.5 + rand.NextDouble() * (colHeight - 1);
        wX[i] = wX[j] = 0.5 + (archWidth + colWidth)/4 * rand.NextDouble();
      }
      for (int i = 0, j = bayCountY - 1; i <= j; i++, j--)
      {
        bool f = rand.Next(2) == 1;
        winY[i] = winY[j] = f;
        hY[i] = hY[j] = 0.5 + rand.NextDouble() * (colHeight - 1);
        wY[i] = wY[j] =  0.5 + (archWidth + colWidth)/4 * rand.NextDouble();
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
        if (joined != null && joined.Length > 0)
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
        if (joined != null && joined.Length > 0)
        {
          var prof = joined[0];
          var extr = Extrusion.Create(prof, widthVal, true);
          if (extr != null) breps.Add(extr.ToBrep(true));
        }
      }
    }

    try { subtractAdd = Brep.CreateBooleanUnion(breps, tol); }
    catch { subtractAdd = breps.ToArray(); }
  }

  private void AddColumnAt(
    List<Brep> breps, Point3d basePt, double r,
    double baseH, double shaftR, double shaftH,
    double capR, double capH)
  {
    breps.Add(new Cylinder(new Circle(new Plane(basePt, Vector3d.ZAxis), r), baseH).ToBrep(true, true));
    breps.Add(new Cylinder(
      new Circle(new Plane(basePt + Vector3d.ZAxis * baseH, Vector3d.ZAxis), shaftR),
      shaftH
    ).ToBrep(true, true));
    breps.Add(new Cylinder(
      new Circle(new Plane(basePt + Vector3d.ZAxis * (baseH + shaftH), Vector3d.ZAxis), capR),
      capH
    ).ToBrep(true, true));
  }

  private double ToDouble(object o)
  {
    if (o is double d) return d;
    if (o is int i)    return i;
    return Convert.ToDouble(o);
  }
}
