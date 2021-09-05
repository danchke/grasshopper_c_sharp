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
  private void RunScript(double GkPlusQk, double Span, double SecondarySpan, object CLT_DepthTable_2wayTable, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Description, ref object MinSquareColSize)
  {
    var depth = TwoWayLookup(CLT_DepthTable_2wayTable, Span, GkPlusQk);

    var cltMassPerM2 = depth / 1000 * CLTDensity; //kg/m², Mass of CLT slab = dρ
    var gsw = cltMassPerM2 * 9.807 / 1000; //kN/m2, Self weight of CLT slab
    var Vclt = depth / 1000; //m³/m², Volume of clt per m² floor

    string laminations = "";
    if (depth < 101) laminations = "3s";
    else if (depth < 201) laminations = "5s";
    else if (depth < 281) laminations = "7ss";

    var slabArea = Span * SecondarySpan;
    Depth = depth;
    MaterialNames = new string[] { CLTMaterial };
    MaterialUnitWeights = new double[] { cltMassPerM2 };
    MaterialTotalWeights = new double[] { cltMassPerM2 * slabArea };
    Description = string.Format("{0}mm thick CLT slab with {1} laminations", depth, laminations);
  }

  // <Custom additional code> 

  public static double CLTDensity = 420; //kg/m³

  public static string CLTMaterial = "Timber: CLT";

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

  public static double LookupDoubleValue(object tabularObjObj, string searchColumn, double searchCriteria, string resultColumn)
  {
    if (!(tabularObjObj is Dictionary<string, object>))
      throw new Exception("Lookup table must be Dictionary<string, object>. Could not be cast.");
    Dictionary<string, object> tabularObj = (Dictionary<string, object>) tabularObjObj;

    var searchColObj = tabularObj[searchColumn];
    if (!(searchColObj is List<double>))
      throw new Exception("Search column must be of double type. Could not be cast.");

    var resultColObj = tabularObj[resultColumn];
    if (!(resultColObj is List<double>))
      throw new Exception("Result column must be of string type. Could not be cast.");

    var searchCol = (List<double>) searchColObj;
    var resultCol = (List<double>) resultColObj;

    var rowNo = -1;
    double tolerance = 0.001;
    for (int i = 0; i < searchCol.Count; i++)
    {
      if(searchCol[i] < searchCriteria + tolerance && searchCol[i] > searchCriteria - tolerance)
      {
        rowNo = i;
        break;
      }
    }
    if (rowNo == -1) return double.NaN;

    return resultCol[rowNo];
  }
  // </Custom additional code> 
}