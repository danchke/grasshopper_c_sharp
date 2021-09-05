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
  private void RunScript(double Wgk , double Wqk, double Span, double VariableDeflLimit, double MinNaturalFreq, object UBProperties_LookupTable, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Width, ref object Description, ref object SteelSection)
  {
    var SteelBeam_UBProperties = (Dictionary<string, object>) UBProperties_LookupTable; //To copy/paste into grasshopper

    var MaxDepth = 1500; //mm, Maximum depth of section
    var w_ULS = Wgk * 1.35 + Wqk * 1.5; //kN/m, Total factored line load
    var Med = (w_ULS * Span * Span) / 8; //kNm, Med=wl2/8
    var Ved = w_ULS * Span / 2; //kN, Ved=wl/2
    var E = 205; //kN/mm2, Young's Modulus of steel
    var defl_g = Math.Pow(17.8 / MinNaturalFreq, 2); //mm, allowable permanent deflection, dg=(17.8/F)2
    var defl_q = (Span * 1000) / VariableDeflLimit; //mm, allowable variable deflection, dq= L/360
    var defl_allowable = Math.Min(defl_g, defl_q); //mm, minimum allowable deflection
    var I_ming = (5 * Wgk * Math.Pow(Span, 4)) / (384 * (E / 1e-6) * (defl_g / 1000)) * 1e8; //cm4, Min second moment of area, Imin,g = 5wgl4/384Edg
    var I_minq = (5 * Wqk * Math.Pow(Span, 4)) / (384 * (E / 1e-6) * (defl_q / 1000)) * 1e8; //cm4, Min second moment of area, Imin,q= 5wql4/384Edq
    var I_req = Math.Max(I_ming, I_minq); //cm4
    var M_crdreq = Med; //kNm

    //Column headers: Section,Weight,Depth,IYY,Mcrdy
    var sections = (List<string>) SteelBeam_UBProperties["Section"];
    var weights = (List<double>) SteelBeam_UBProperties["Weight"];
    var depths = (List<double>) SteelBeam_UBProperties["Depth"];
    var iyys = (List<double>) SteelBeam_UBProperties["IYY"];
    var mcrdys = (List<double>) SteelBeam_UBProperties["Mcrdy"];

    //Selecting the lowest weight passing section
    double minWeight = double.MaxValue;
    string lowestWeightSection = null;
    double lowestWeightDepth = double.NaN;
    string utilisations = "";

    for (int i = 0; i < sections.Count; i++)
    {
      var util_ireq = I_req / iyys[i];
      var util_mrd = M_crdreq / mcrdys[i];
      var util_maxDepth = depths[i] / MaxDepth;

      if (util_ireq > 1 || util_mrd > 1 || util_maxDepth > 1) continue;

      if (weights[i] < minWeight)
      {
        minWeight = weights[i];
        lowestWeightSection = sections[i];
        lowestWeightDepth = depths[i];
        utilisations = string.Format("Moment util: {0:F2}, Deflec util: {1:F2} (I_min_g:{2:F0}, I_min_q:{3:F0})", util_mrd, util_ireq, I_ming, I_minq);
      }
    }

    MaterialNames = new string[] { "Steel: Section Open (UK)" };
    MaterialUnitWeights = new double[] { minWeight };
    MaterialTotalWeights = new double[] { minWeight * Span };
    SteelSection = lowestWeightSection;
    Description = string.Format("{0}UB S355 steel column", lowestWeightSection);
    Depth = lowestWeightDepth;
    Width = double.Parse(lowestWeightSection.Split('x')[1]);
    Print(utilisations);
  }

  // <Custom additional code> 
  public static double TwoWayLookup(object[] TwoWayLookupObj, double IndexValue, double HeaderValue)
  {
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