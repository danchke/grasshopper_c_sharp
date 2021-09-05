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
  private void RunScript(string MaterialName, double MaterialWeight, object CarbonFactor_LookupTable, object TransportScenario_Dict, ref object CarbonA1A3, ref object SequestrationA1A3, ref object TransportA4, ref object WasteA5, ref object TotalA1A5)
  {
    var CarbonFactorTable = (Dictionary<string, object>) CarbonFactor_LookupTable;
    var TransportScenarioDict = (Dictionary<string, double>) TransportScenario_Dict;

    int carbonFactorRow = FindRow(CarbonFactorTable, "Material", MaterialName);
    if (carbonFactorRow == -1) throw new Exception("Material not found.");

    //Material,Density,A1-A3 Carbon,A1-A3 Sequestration,Transport Scenario,A5w Waste
    double carbonFactor = GetDoubleValue(CarbonFactorTable, "A1-A3 Carbon", carbonFactorRow);
    CarbonA1A3 = carbonFactor * MaterialWeight;

    SequestrationA1A3 = GetDoubleValue(CarbonFactorTable, "A1-A3 Sequestration", carbonFactorRow) * MaterialWeight;

    string transportScenario = GetStrValue(CarbonFactorTable, "Transport Scenario", carbonFactorRow);
    TransportA4 = TransportScenarioDict[transportScenario] * MaterialWeight;

    double wasteFactor = GetDoubleValue(CarbonFactorTable, "A5w Waste", carbonFactorRow);
    double wasteCarbonFactor = wasteFactor * (carbonFactor + 0.005 + 0.013);
    WasteA5 = wasteCarbonFactor * MaterialWeight;

    TotalA1A5 = (double) CarbonA1A3 + (double) SequestrationA1A3 + (double) TransportA4 + (double) WasteA5;
  }

  // <Custom additional code> 
  public static int FindRow(Dictionary<string, object> tabularObj, string searchColumn, string searchCriteria)
  {
    var searchColObj = tabularObj[searchColumn];
    if (!(searchColObj is List<string>))
      throw new Exception("Search column must be of string type. Could not be cast.");

    var searchCol = (List<string>) searchColObj;

    return searchCol.IndexOf(searchCriteria);
  }

  public static int FindRow(Dictionary<string, object> tabularObj, string searchColumn, double searchCriteria)
  {
    var searchColObj = tabularObj[searchColumn];
    if (!(searchColObj is List<double>))
      throw new Exception("Search column must be of double type. Could not be cast.");

    var searchCol = (List<double>) searchColObj;

    return searchCol.IndexOf(searchCriteria);
  }

  public static string GetStrValue(Dictionary<string, object> tabularObj, string resultColumn, int resultRow)
  {
    var resultColObj = tabularObj[resultColumn];
    if (!(resultColObj is List<string>))
      throw new Exception("Result column must be of string type. Could not be cast.");

    var resultCol = (List<string>) resultColObj;

    return resultCol[resultRow];
  }

  public static double GetDoubleValue(Dictionary<string, object> tabularObj, string resultColumn, int resultRow)
  {
    var resultColObj = tabularObj[resultColumn];
    if (!(resultColObj is List<double>))
      throw new Exception("Result column must be of string type. Could not be cast.");

    var resultCol = (List <double>) resultColObj;

    return resultCol[resultRow];
  }
  // </Custom additional code> 
}