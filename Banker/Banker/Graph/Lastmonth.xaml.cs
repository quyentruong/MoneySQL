﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;

namespace Banker.Graph
{
    /// <summary>
    /// Interaction logic for Lastmonth.xaml
    /// </summary>
    public partial class Lastmonth : Window
    {

        public Lastmonth(List<KeyValuePair<int, decimal>> valueList)
        {
            InitializeComponent();

            var values = new ChartValues<decimal>();

            Labels = new string[valueList.Count];

            var i = 0;
            foreach (var item in valueList)
            {
                Labels[i] = item.Key.ToString();
                values.Add(item.Value);
                i++;
            }

            SeriesCollection = new SeriesCollection {
                new LineSeries
                {
                    Title = "Monthly Stats",
                    Values = values,
                    LineSmoothness = 0, //0: straight lines, 1: really smooth lines
                    PointGeometry = null,
                    PointForeground = Brushes.Blue
                }
            };

            YFormatter = value => value.ToString("C");

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }
    }

}
