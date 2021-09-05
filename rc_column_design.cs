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
  private void RunScript(double NEd, int NoStoreys, double Height, double MinColSqSize, double ReinforcementRatio, System.Object RCColumn_ColumnSize_2wayTable, string ConcMatierial, string SteelMatierial, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Width, ref object Description)
  {
    double colSize = TwoWayLookup(RCColumn_ColumnSize_2wayTable, ReinforcementRatio, NEd);
    double SqSize = Math.Max(MinColSqSize, colSize); //Adjust column size for minimum punching shear

    var ConcVol = (SqSize / 1000) * (SqSize / 1000) * Height; //m3, Volumn of column
    var ConcWeight = ConcDensity * ConcVol; //kg, Mass of concrete
    var Asreq = (SqSize * SqSize) * ReinforcementRatio; //mm2/m, Assuming reinforcement ratio specified above
    var SteelVol = (Asreq / 1e6) * Height; //m3, Volume of steel/column
    var SteelWeight = SteelVol * SteelDensity; //kg, Mass of longitudinal bars

    Width = SqSize;
    Depth = SqSize;
    MaterialNames = new string[] { ConcMatierial, SteelMatierial };
    MaterialTotalWeights = new double[] { ConcWeight, SteelWeight };
    MaterialUnitWeights = new double[] { ConcWeight / Height, SteelWeight / Height };
    Description = string.Format("{0}mm square {1} column with {2}% reinforcement ratio", SqSize, ConcMatierial.Substring(10), ReinforcementRatio * 100);

  }

  // <Custom additional code> 

  public const double ConcDensity = 2450;
  public const double SteelDensity = 7850;

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