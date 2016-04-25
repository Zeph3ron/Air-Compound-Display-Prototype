using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.Mvc;
using DotNet.Highcharts;
using DotNet.Highcharts.Enums;
using DotNet.Highcharts.Helpers;
using DotNet.Highcharts.Options;
using EnvironmentalDisplay.EF;
using EnvironmentalDisplay.Models;

namespace EnvironmentalDisplay.Controllers
{
    public class ChartController : Controller
    {
        private readonly DbMandatoryEnvironmentEntities db = new DbMandatoryEnvironmentEntities();

        public ActionResult Index()
        {
            ViewBag.LocationId = new SelectList(db.Locations, "LocationId", "LocationName");
            ViewBag.ChartTypes = new List<String> {"Zoomable", "Line Showing Limits" };
            return View();
        }

        public ActionResult Redirector(int? locationId, DateTime? dateFrom, DateTime? dateTo,
            string compoundName, string chartType)
        {
            if (chartType == "Zoomable")
            {
                return RedirectToAction("TimeSeriesZoomable", new {locationId = locationId, dateFrom = dateFrom, dateTo = dateTo, compoundName = compoundName});
            }
            else
            {
                return RedirectToAction("SplineWithPlotBands", new { locationId = locationId, dateFrom = dateFrom, dateTo = dateTo, compoundName = compoundName });
            }
        }



        #region ChartMethods

        /// <summary>
        /// This method generates a zoomable time series chart, based on a compound, date and a location.
        /// </summary>
        /// <param name="locationId">Location to get measurments from</param>
        /// <param name="dateFrom">The from date</param>
        /// <param name="dateTo">The to date</param>
        /// <param name="compoundName">The name of the compound to view</param>
        /// <returns></returns>
        public ActionResult TimeSeriesZoomable(int? locationId, DateTime? dateFrom, DateTime? dateTo,
            string compoundName)
        {
            var allSetupsNeeded =
                db.Setups
                    .Where(
                        setup =>
                            setup.Compound.CompoundName == compoundName && setup.Location.LocationId == locationId &&
                            (setup.DateOfMeasurement >= dateFrom &&
                             setup.DateOfMeasurement <= dateTo));

            var testSetup = allSetupsNeeded.First();
            var locationName = testSetup.Location.LocationName;
            ViewBag.ChartTitle = "Amount of '" + testSetup.Compound.CompoundName + "' from: '" + dateFrom.Value.Day +
                                 "/" + dateFrom.Value.Month + "/" +
                                 dateFrom.Value.Year + "' to '" + dateTo.Value.Day + "/" + dateTo.Value.Month + "/" +
                                 dateTo.Value.Year + "' at '" + locationName + "'.";

            var resultsDates =
                allSetupsNeeded.Select(x => new DateResult {DateTime = x.DateOfMeasurement.Value, Result = x.Results})
                    .ToList();
            var correctResultDates = FillInMissingDates(resultsDates, dateTo.Value);

            var chart = new Highcharts("chart")
                .SetOptions(new GlobalOptions {Global = new Global {UseUTC = false}})
                .InitChart(new Chart {ZoomType = ZoomTypes.X, SpacingRight = 20})
                .SetTitle(new Title {Text = ""})
                .SetSubtitle(new Subtitle {Text = "Click and drag in the plot area to zoom in"})
                .SetXAxis(new XAxis
                {
                    Type = AxisTypes.Datetime,
                    Title = new XAxisTitle {Text = "Date of Measurement"}
                })
                .SetYAxis(new YAxis
                {
                    Title = new YAxisTitle {Text = "Amount of Ozon in " + testSetup.Unit.UnitName},
                    Min = 0.6,
                    StartOnTick = false,
                    EndOnTick = false
                })
                .SetTooltip(new Tooltip {Shared = true})
                .SetLegend(new Legend {Enabled = false})
                .SetPlotOptions(new PlotOptions
                {
                    Area = new PlotOptionsArea
                    {
                        FillColor = new BackColorOrGradient(new Gradient
                        {
                            LinearGradient = new[] {0, 0, 0, 300},
                            Stops = new object[,] {{0, "rgb(116, 116, 116)"}, {1, Color.Gold}}
                        }),
                        LineWidth = 1,
                        Marker = new PlotOptionsAreaMarker
                        {
                            Enabled = false,
                            States = new PlotOptionsAreaMarkerStates
                            {
                                Hover = new PlotOptionsAreaMarkerStatesHover
                                {
                                    Enabled = true,
                                    Radius = 5
                                }
                            }
                        },
                        Shadow = false,
                        States = new PlotOptionsAreaStates {Hover = new PlotOptionsAreaStatesHover {LineWidth = 1}},
                        PointInterval = ((24*3600*1000)/48),
                        PointStart = new PointStart(dateFrom.Value)
                    }
                })
                .SetSeries(new Series
                {
                    Type = ChartTypes.Area,
                    Name = testSetup.Unit.UnitName,
                    Data = new Data(ToObjectArrayResult(correctResultDates.Select(x => x.Result)))
                });
            return View(chart);
        }


        /// <summary>
        /// This method generates a Spline chart with Plot bands that also shows the limits allowed for the compound.
        /// </summary>
        /// <param name="locationId">Location to get measurments from</param>
        /// <param name="dateFrom">The from date</param>
        /// <param name="dateTo">The to date</param>
        /// <param name="compoundName">The name of the compound to view</param>
        /// <returns></returns>
        public ActionResult SplineWithPlotBands(int? locationId, DateTime? dateFrom, DateTime? dateTo,
            string compoundName)
        {
            var allSetupsNeeded =
                db.Setups
                    .Where(
                        setup =>
                            setup.Compound.CompoundName == compoundName && setup.Location.LocationId == locationId &&
                            (setup.DateOfMeasurement >= dateFrom &&
                             setup.DateOfMeasurement <= dateTo));

            var testSetup = allSetupsNeeded.First();
            var locationName = testSetup.Location.LocationName;
            ViewBag.ChartTitle = "Amount of '" + testSetup.Compound.CompoundName + "' from: '" + dateFrom.Value.Day +
                                 "/" + dateFrom.Value.Month + "/" +
                                 dateFrom.Value.Year + "' to '" + dateTo.Value.Day + "/" + dateTo.Value.Month + "/" +
                                 dateTo.Value.Year + "' at '" + locationName + "'.";
            var resultsDates =
                allSetupsNeeded.Select(x => new DateResult {DateTime = x.DateOfMeasurement.Value, Result = x.Results})
                    .ToList();
            var correctResultDates = FillInMissingDates(resultsDates, dateTo.Value);
            var correctResults = correctResultDates.Select(c => c.Result).ToList();

            var chart = new Highcharts("chart")
                .InitChart(new Chart {DefaultSeriesType = ChartTypes.Spline})
                .SetTitle(new Title {Text = ""})
                .SetXAxis(new XAxis {Type = AxisTypes.Datetime})
                .SetYAxis(new YAxis
                {
                    Title = new YAxisTitle {Text = "Amount of Ozon in " + testSetup.Unit.UnitName},
                    Min = 0,
                    MinorGridLineWidth = 0,
                    GridLineWidth = 0,
                    PlotBands = GetPlotbands(compoundName)
                })
                .SetTooltip(new Tooltip
                {
                    Formatter =
                        "function() { return ''+ Highcharts.dateFormat('%e. %b %Y, %H:00', this.x) +': '+ this.y +' "+testSetup.Unit.UnitName+"'; }"
                })
                .SetPlotOptions(new PlotOptions
                {
                    Spline = new PlotOptionsSpline
                    {
                        LineWidth = 4,
                        PointInterval = ((24*3600*1000)/48),
                        PointStart = new PointStart(dateFrom.Value),
                        States = new PlotOptionsSplineStates {Hover = new PlotOptionsSplineStatesHover {LineWidth = 5}},
                        Marker = new PlotOptionsSplineMarker
                        {
                            Enabled = false,
                            States = new PlotOptionsSplineMarkerStates
                            {
                                Hover = new PlotOptionsSplineMarkerStatesHover
                                {
                                    Enabled = true,
                                    Radius = 5,
                                    LineWidth = 1
                                }
                            }
                        }
                    }
                })
                .SetSeries(new[] {GetSeries(compoundName, correctResults)})
                .SetNavigation(new Navigation {MenuItemStyle = "fontSize: '10px'"});
            return View(chart);
        }

        /// <summary>
        /// This method called through AJAX and is used to fill in the list of available measurements at each location.
        /// </summary>
        /// <param name="locationId">The location to check</param>
        /// <returns></returns>
        public JsonResult Compounds(int locationId)
        {
            var allCompounds =
                db.Setups.Where(setup => setup.LocationId == locationId)
                    .Select(setup => setup.Compound.CompoundName)
                    .Distinct()
                    .ToList();
            return Json(allCompounds);
        }


        /// <summary>
        /// This method generates a series for a specific result set, based on the compound name and results, which is used for the chart.
        /// </summary>
        /// <param name="compoundName">The name of the compound</param>
        /// <param name="correctResultDates">A set of correct results paired with a date</param>
        /// <returns></returns>
        public Series GetSeries(string compoundName, List<string> correctResultDates)
        {
            var series = new Series
            {
                Name = compoundName,
                Data = new Data(ToObjectArrayResult(correctResultDates))
            };
            return series;
        }

        #endregion

        #region Helper Methods 
        /// <summary>
        /// This method generates plotBands that show the user the allowed limits of a specific compound thats being viewed.
        /// </summary>
        /// <param name="compoundName"></param>
        /// <returns></returns>
        private YAxisPlotBands[] GetPlotbands(string compoundName)
        {
            switch (compoundName)
            {
                case "PM10 beta gauge":
                    return new[]
                    {
                        GetPlotBand(0,15,"Lower limit",Color.White),
                        GetPlotBand(15,30,"Higher limit",ColorTranslator.FromHtml("#ECF7FB")),
                    };

                case "PM2.5 beta gauge":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
                case "Carbonmonooxid":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
                case "Nitrogenmonooxid":
                    return new[]
                    {
                        GetPlotBand(0,2,"Test",Color.Aqua),
                    };
                case "NOx'er":
                    return new[]
                    {
                       GetPlotBand(14,15,"Test",Color.White),
                    };
                case "Svovldioxid":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
                case "Ozon":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
                case "PM10":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
                case "PM25":
                    return new[]
                    {
                        GetPlotBand(14,15,"Test",Color.White),
                    };
            }
            return null;
        }

        private YAxisPlotBands GetPlotBand(int from, int to, string text, Color color)
        {
            return new YAxisPlotBands
            {
                From = from,
                To = to,
                Color = color,
                Label = new YAxisPlotBandsLabel
                {
                    Text = text,
                    Style = "color:'#606060'"
                }
            };
        }

        private List<DateResult> FillInMissingDates(List<DateResult> resultsDates, DateTime dateTo)
        {
            var resultsToReturn = new List<DateResult>();
            var dateToCompare = resultsDates[0].DateTime;
            foreach (var resultsDate in resultsDates)
            {
                if (resultsDate.DateTime == dateToCompare)
                {
                    resultsToReturn.Add(resultsDate);
                    dateToCompare = dateToCompare.AddMinutes(30);
                }
                else
                {
                    while (resultsDate.DateTime != dateToCompare && dateToCompare < dateTo)
                    {
                        resultsToReturn.Add(new DateResult {DateTime = dateToCompare, Result = null});
                        dateToCompare = dateToCompare.AddMinutes(30);
                    }
                    resultsToReturn.Add(resultsDate);
                    dateToCompare = dateToCompare.AddMinutes(30);
                }
            }
            return resultsToReturn;
        }

        private string[] ToStringArrayDate(IEnumerable<DateTime> listOfData)
        {
            var numbers = listOfData.Select(s => Convert.ToString(s)).ToList();
            var stringArray = numbers.Cast<string>().ToArray();
            return stringArray;
        }

        private object[] ToObjectArrayResult(IEnumerable<string> listOfData)
        {
            var numbers = listOfData.Select(s => Convert.ToDouble(s)).ToList();
            var object_array = numbers.Cast<object>().ToArray();
            return object_array;
        }

        /// <summary>
        /// This method is used to fill in the missing results from the database, it makes it easier to synchronize the results and dates.
        /// </summary>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        private List<DateTime> GetDatesWithinRange(DateTime dateFrom, DateTime dateTo)
        {
            var datesToReturn = new List<DateTime>();
            var dateToTest = dateFrom;
            while (dateToTest < dateTo)
            {
                datesToReturn.Add(dateToTest);
                dateToTest = dateToTest.AddMinutes(30);
            }
            return datesToReturn;
        }
    }

    #endregion
}