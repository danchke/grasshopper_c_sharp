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
  private void RunScript(string csv, ref object LookupTable)
  {
    var lines = csv.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.None);

    var headerRow = lines[0].Split(',').Select(cel => cel.Trim()).ToList();

    //Create columns of strings
    var columnsOfString = new List<List<string>>();
    for (int colNo = 0; colNo < headerRow.Count; colNo++)
    {
      columnsOfString.Add(new List<string>(lines.Length - 1));
    }

    //Populate the columns of string with split text
    for (int rowNo = 0; rowNo < lines.Length - 1; rowNo++)
    {
      var rowTxt = lines[rowNo + 1];//+1 because we are ignoring the header row
      var rowSplit = rowTxt.Split(',');

      for (int colNo = 0; colNo < rowSplit.Length; colNo++)
      {
        columnsOfString[colNo].Add(rowSplit[colNo].Trim());
      }
    }

    var result = new Dictionary<string, object>();

    //Test if the column of string can be cast to doubles
    for (int colNo = 0; colNo < headerRow.Count; colNo++)
    {
      var strCol = columnsOfString[colNo];
      var doubleCol = new List<double>(strCol.Count);
      var parseFailureCount = 0;
      var parseFailureLimit = (int) strCol.Count * 0.2f; //If more than 20% fails, cast as a string instead

      bool IsDoubleColumn = true;

      foreach (var str in strCol)
      {
        double value;
        try
        {
          value = double.Parse(str);
        }
        catch (Exception)
        {
          value = double.NaN;
          parseFailureCount++;
          if(parseFailureCount >= parseFailureLimit)
          {
            IsDoubleColumn = false;
            break;
          }
        }
        doubleCol.Add(value);
      }

      if (IsDoubleColumn)
      {
        result.Add(headerRow[colNo], doubleCol);
      }
      else
      {
        result.Add(headerRow[colNo], strCol);
      }
    }

    LookupTable = new GH_ObjectWrapper (result);
  }

  // <Custom additional code> 

  // </Custom additional code> 
}