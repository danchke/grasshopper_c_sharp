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
  private void RunScript(double NEd, int NoStoreys, double Height, double MinColSqSize, System.Object SteelColumn_UCProperties_LookupTable, double MaxUtilisation, ref object MaterialNames, ref object MaterialUnitWeights, ref object MaterialTotalWeights, ref object Depth, ref object Width, ref object Description, ref object SteelSection)
  {
    var SteelBeam_UBProperties = (Dictionary<string, object>) SteelColumn_UCProperties_LookupTable; //To copy/paste into grasshopper

    //Find Lcr value for table lookup
    double LcrValue = double.NaN; //m, Buckling Length assuming Pin - Pin end restraints
    string LcrStr = ""; //Lcr for table lookup
    for (int i = 0; i < LcrValueList.Count; i++)
    {
      if(Height <= LcrValueList[i])
      {
        LcrValue = LcrValueList[i];
        LcrStr = LcrStrList[i];
        break;
      }
    }

    //var MaxUtilisation = 0.8; //UR, Maximum axial utilisation permitted, to allow for eccentricity moment
    var NbzRdreq = NEd / MaxUtilisation; //kN, Minimum minor axis buckling resistance

    //Column headers: Section,Weight,Depth,IYY,Mcrdy
    var sections = (List<string>) SteelBeam_UBProperties["Section"];
    var weights = (List<double>) SteelBeam_UBProperties["Weight"];
    var depths = (List<double>) SteelBeam_UBProperties["Depth"];
    var widths = (List<double>) SteelBeam_UBProperties["Width"];
    var areas = (List<double>) SteelBeam_UBProperties["Area"];
    var bucklingLoads = (List<double>) SteelBeam_UBProperties[LcrStr];

    //Selecting the lowest weight passing section
    double minWeight = double.MaxValue;
    int minWeightRow = -1;
    //string lowestWeightSection = null;
    //double lowestWeightDepth = double.NaN;
    //double lowestWeightWidth = double.NaN;
    //string utilisations = "";

    for (int i = 0; i < sections.Count; i++)
    {
      var axialComp = ((areas[i] * 1e-4) * (fy * 1000) / gamma_m0);
      var flexuralBuckling = bucklingLoads[i];
      var util_axialComp = NbzRdreq / axialComp;
      var util_flexuralBuckling = NbzRdreq / flexuralBuckling;

      if ( util_axialComp > 1 || util_flexuralBuckling > 1 ) continue;

      if (weights[i] < minWeight)
      {
        minWeight = weights[i];
        minWeightRow = i;
        //utilisations = string.Format("Moment util: {0}, Deflec util: {1} (I_min_g:{2}, I_min_q:{3})", util_mrd, util_ireq, I_ming, I_minq);
      }
    }

    if (minWeightRow == -1)
    {
      MaterialNames = new string[] { "Steel: Section Open (UK)" };
      MaterialUnitWeights = new double[] { double.NaN };
      MaterialTotalWeights = new double[] { double.NaN };
      SteelSection = "";
      Description = string.Format("{0}UB S355 steel column", SteelSection);
      Depth = double.NaN;
      Width = double.NaN;
      return;
    }

    MaterialNames = new string[] { "Steel: Section Open (UK)" };
    MaterialUnitWeights = new double[] { minWeight };
    MaterialTotalWeights = new double[] { minWeight * Height };
    SteelSection = sections[minWeightRow];
    Description = string.Format("{0}UB S355 steel column", SteelSection);
    Depth = depths[minWeightRow];
    Width = widths[minWeightRow];

  }

  // <Custom additional code> 

  public static List<double> LcrValueList = new List<double> { 1, 1.5, 2, 2.5, 3, 3.5, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
  public static List<string> LcrStrList = new List<string> { "1", "1.5", "2", "2.5", "3", "3.5", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14" };

  public static double fy = 355;
  public static double gamma_m0 = 1; //Partial Factor for resistance

  // </Custom additional code> 
}