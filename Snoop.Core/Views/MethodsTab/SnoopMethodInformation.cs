// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;
using System.Collections.Generic;
using System.Reflection;

public class SnoopMethodInformation : IComparable, IEquatable<SnoopMethodInformation>
{
    public string MethodName { get; }

    public MethodInfo MethodInfo { get; }

    public SnoopMethodInformation(MethodInfo methodInfo)
    {
        this.MethodInfo = methodInfo;
        this.MethodName = methodInfo.Name;
    }

    #region IComparable Members

    public int CompareTo(object? obj)
    {
        return string.Compare(this.MethodName, (obj as SnoopMethodInformation)?.MethodName, StringComparison.Ordinal);
    }

    #endregion

    public override string ToString()
    {
        return this.MethodName;
    }

    public IList<SnoopParameterInformation> GetParameters(Type declaringType)
    {
        var parameterInfos = this.MethodInfo.GetParameters();

        var parametersToReturn = new List<SnoopParameterInformation>();

        foreach (var parameterInfo in parameterInfos)
        {
            var snoopParameterInfo = new SnoopParameterInformation(parameterInfo, declaringType);
            parametersToReturn.Add(snoopParameterInfo);
        }

        return parametersToReturn;
    }

    #region IEquatable<SnoopMethodInformation> Members

    public bool Equals(SnoopMethodInformation? other)
    {
        if (other is null)
        {
            return false;
        }

        if (other.MethodName != this.MethodName)
        {
            return false;
        }

        if (other.MethodInfo.ReturnType != this.MethodInfo.ReturnType)
        {
            return false;
        }

        var thisParameterInfos = this.MethodInfo.GetParameters();
        var otherParameterInfos = other.MethodInfo.GetParameters();

        if (thisParameterInfos.Length != otherParameterInfos.Length)
        {
            return false;
        }

        for (var i = 0; i < thisParameterInfos.Length; i++)
        {
            var thisParameterInfo = thisParameterInfos[i];
            var otherParameterInfo = otherParameterInfos[i];

            if (ParameterInfosEqual(thisParameterInfo, otherParameterInfo) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterInfosEqual(ParameterInfo parm1, ParameterInfo parm2)
    {
        if (parm1.Name?.Equals(parm2.Name, StringComparison.Ordinal) != true)
        {
            return false;
        }

        if (parm1.ParameterType != parm2.ParameterType)
        {
            return false;
        }

        return parm1.Position == parm2.Position;
    }

    #endregion
}