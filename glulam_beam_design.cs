using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;

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
  private void RunScript(double Wgk , double Wqk, double Span, double VariableDeflLimit, double Timber_InstantaneousDeflLimit, double Timber_LongTermDeflLimit, double MinNaturalFreq, System.Object Kmod_LookupTable, System.Object GlulamSizes_LookupTable, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Width, ref object Description, ref object SteelSection)
  {
    var wgd = Wgk * 1.35; //kN/m, Permanent Line Load (factored)
    var wgk = 2.06517741; //kN/m, Permanent Line Load (unfactored)
    var wqd = Wqk * 1.5; //kN/m, Variable Line load (factored)
    var wqk = 3.5; //kN/m, Variable Line load (unfactored)
    var wgdPlusWqd = wgd + wqd; //kN/m, Total factored line load
    var kmod = LookupDoubleValue(Kmod_LookupTable, "ServiceClass", ServiceClass, LoadDurationClass); //Strength modification based on load duration

    double kdef = 0;
    if (ServiceClass == 1) kdef = 0.6;
    else if (ServiceClass == 2) kdef = 0.8;
    else if (ServiceClass == 3) kdef = 2;

    var gamma_m = 1.25; //Material safety factor, T2.3 EC5
    var Med = ( wgdPlusWqd * Math.Pow(Span, 2)) / 8; //kNm, Med=wl2/8
    var Ved = wgd + wqd * Span / 2; //kN, Ved=wl/2

    //Deflection check
    var delta_g = Math.Pow((17.8 / MinNaturalFreq ), 2); //mm, Allowable permanent deflection, dg=(17.8/F)2
    var delta_q = (Span * 1000) / VariableDeflLimit; //mm, Allowable variable deflection, dq= L/[user input]
    var delta_inst = (Span * 1000) / Timber_InstantaneousDeflLimit; //mm, Allowable total instantaneous deflection, dinst= L/250
    var delta_LT = (Span * 1000) / Timber_LongTermDeflLimit; //mm, Allowable long term deflection, dLT= L/200
    var Iming = (5 * wgk * Math.Pow(Span, 4)) / (384 * (E / 1e-6) * (delta_g / 1000)) * 1e8; //cm4, Min second moment of area, Imin,g = 5wgl4/384Edg
    var Iminq = (5 * wqk * Math.Pow(Span, 4)) / (384 * (E / 1e-6) * (delta_q / 1000)) * 1e8; //cm4, Min second moment of area, Imin,q= 5wql4/384Edq
    var Imininst = (((5 * wgk * Math.Pow(Span, 4)) / (384 * (E / 1e-6)) + ((5 * wqk * Math.Pow(Span, 4)) / (384 * (E / 1e-6)))) / (delta_inst / 1000)) * 1e8;
    //cm4, Min second moment of area, Imin,Inst= ((5wgl4/384E)+(5wql4/384E))/dinst
    var Y2 = 0.3; //Appropriate for office and residential
    var IminLT = (((1 + kdef) * (5 * wgk * Math.Pow(Span, 4)) / (384 * (E / 1e-6)) + ((1 + (Y2 * kdef)) * (5 * wqk * Math.Pow(Span, 4)) / (384 * (E / 1e-6)))) / (delta_LT / 1000)) * 1e8; //cm4, Min second moment of area, Imin,Inst = ((1 + kdef)(5wgl4 / 384E) + (1 + kdef.Y2)(5wql4 / 384E)) / dLt
    var Ireq = Enumerable.Max(new double[] { Iming, Iminq, Imininst, IminLT }); //cm4
    var dreq_I = Math.Ceiling(Math.Pow(24 * (Ireq * 1e4), 0.25)); //mm, Assuming b:d ratio limited to 1:2 to avoid LTB check

    //Moment design
    var Mcrd_req = Med; //kNm
    var fbd = fmk * kmod / gamma_m; //N/mm2
    var Wel_min = (Mcrd_req * 1e6) / fbd; //mm3, Mc,rd,req /fb,d
    var dreq_Wel = Math.Ceiling(Math.Pow(12 * Wel_min, 1 / 3)); //mm, b:d ratio 1:2, wpl=bd2/6 = d3/12
    var MaxShearUtil = 0.7; //Maximum shear utilisation to account for connection design (Engineering judgement to limit utilisation at this stage)

    //Shear design
    var fvdmax = MaxShearUtil * (fvk * kmod * ksys) / gamma_m; //N/mm2, fv,d = fv,k.Kmod.Ksys/gm , T3.1,EC5
    var Areq = 1.5 * (Ved * 1e3) / fvdmax; //mm2, from fv,d=3/2. V/A
    var dreq_Shr = Math.Ceiling(Math.Sqrt(2 * (Areq / kcr))); //mm, A=b.d.kcr, assuming b:d=1:2, therefore b=d/2 and A=(d^2/2).kcr

    //Max section depth required
    var dreq_max = Enumerable.Max(new double[] { dreq_I, dreq_I, dreq_I }); //mm, Minimum depth of section required

    var glulamSizes_LookupTable = (Dictionary<string,object>) GlulamSizes_LookupTable;
    var widths = (List<double>) glulamSizes_LookupTable["Width"];
    var depths = (List<double>) glulamSizes_LookupTable["Depth"];

    double width = double.NaN;
    double depth = double.NaN;
    for (int i = 0; i < widths.Count; i++)
    {
      var iWidth = widths[i];
      var iDepth = depths[i];

      var iArea = iWidth * iDepth; //mm2
      var iIvalue = iWidth * iDepth * iDepth * iDepth / 12 / 10000;
      var iWel = iWidth * iDepth * iDepth / 6;

      var util_ireq = Ireq / iIvalue;
      var util_Areq = Areq / iArea;
      var util_maxDepth = dreq_max / iDepth;

      if (util_ireq > 1 || util_Areq > 1 || util_maxDepth > 1) continue;

      width = iWidth;
      depth = iDepth;
      break;
    }

    double weightPerM = width * depth / 1000000 * GlulamDensity; //kg/m, Glulam Beam weight

    MaterialNames = new string[] { GlulamMaterial };
    MaterialUnitWeights = new double[] { weightPerM };
    MaterialTotalWeights = new double[] { weightPerM * Span };
    Description = string.Format("{0} x {1}mm GL28h glulam beam", width, depth);
    Depth = depth;
    Width = width;
  }

  // <Custom additional code> 

  public static double E = 12.5; //kN/mm2, Young's Modulus of Glulam (parallel)
  public static double fmk = 28; //N/mm2, Characteristic Bending Strength
  public static double fvk = 3.5; //N/mm2, Characteristic Shear Strength
  public static double ksys = 1; //
  public static double kcr = 0.67; //To account for shear cracking
  public static int ServiceClass = 1; //Service Class (i.e. 1 (warm), 2 (cold), 3 (external))
  public static string LoadDurationClass = "MediumTerm";
  public static double GlulamDensity = 420;
  public static string GlulamMaterial = "Timber: Glulam";


  public static double LookupDoubleValue(object tabularObjObj, string searchColumn, double searchCriteria, string resultColumn)
  {
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