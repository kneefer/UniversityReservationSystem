﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace UniversityReservationSystem.Interface.Helpers
{
    public class ReservationsMultiBindingConverter : IMultiValueConverter 
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Cast<int>().All(value => value != 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
