using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(double GkPlusQk, double Span, double SecondarySpan, object FlatSlab_Depth_2wayTable, object FlatSlab_Reinforcement_2wayTable, object FlatSlab_MinColumnSize_2wayTable, string ConcMatierial, string SteelMatierial, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Description, ref object MinSquareColSize)
  {
    var MaxSpan = Math.Max(Span, SecondarySpan);

    double depth = TwoWayLookup(FlatSlab_Depth_2wayTable, MaxSpan, GkPlusQk);
    Depth = depth;
    var concreteWeightPerM2 = (depth / 1000d) * ConcDensity;

    var steelWeightLookup = TwoWayLookup(FlatSlab_Reinforcement_2wayTable, MaxSpan, GkPlusQk);
    var steelWeightPerM2 = steelWeightLookup * 1.1;

    MinSquareColSize = TwoWayLookup(FlatSlab_MinColumnSize_2wayTable, MaxSpan, GkPlusQk);

    var slabArea = Span * SecondarySpan;
    MaterialNames = new string[] { ConcMatierial, SteelMatierial };
    MaterialTotalWeights = new double[] { concreteWeightPerM2 * slabArea, steelWeightPerM2 * slabArea };
    MaterialUnitWeights = new double[] { concreteWeightPerM2, steelWeightPerM2 };
    Description = string.Format("{0}mm thick {1} flat slab with {2}kg/m3 steel reinforcement", Depth, ConcMatierial.Substring(10), steelWeightPerM2);
  }

  // <Custom additional code> 
  public static double ConcDensity = 2450;

  public static double TwoWayLookup(object TwoWayLookup, double IndexValue, double HeaderValue)
  {
    var TwoWayLookupObj = (object[]) TwoWayLookup;

    var headerNo = ListLookup_LowerThan((List<double>) TwoWayLookupObj[0], HeaderValue);
    if (headerNo == -1) return double.NaN;

    var indexNo = ListLookup_LowerThan((List<double>) TwoWayLookupObj[1], IndexValue);
    if (indexNo == -1) return double.NaN;

    return ((double[,]) TwoWayLookupObj[2])[indexNo, headerNo];
  }

  private static int ListLookup_LowerThan(List<double> lookupList, double lookupValue)
  {
    for (int i = 0; i < lookupList.Count; i++)
    {
      if (lookupList[i] >= lookupValue)
        return i;
    }
    return -1;
  }
  // </Custom additional code> 
}