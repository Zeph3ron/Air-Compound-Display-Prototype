using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNet.Highcharts.Helpers;
using DotNet.Highcharts.Options;
using EnvironmentalDisplay.EF;

namespace EnvironmentalDisplay.Models
{
    public static class ChartDataModels
    {
        public static object[] ReturnSomething(List<Setup> setups)
        {
            
            return setups.Select(s => new { A = s.Location.LocationName, B = s.Compound.CompoundName}).ToArray();
        }
    }
}