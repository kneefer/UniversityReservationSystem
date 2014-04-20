﻿using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Controls;
using System;

namespace MonthCalendar
{
    public partial class MonthCalendarControl : UserControl
    {
	    internal DateTime _DisplayStartDate = DateTime.Now.AddDays(-1 * (DateTime.Now.Day - 1));
	    private int _displayMonth;
	    private int _displayYear;
	    private System.Globalization.Calendar sysCal;

	    private List<Appointment> _monthAppointments;
	    public event DisplayMonthChangedEventHandler DisplayMonthChanged;
	    public delegate void DisplayMonthChangedEventHandler(MonthChangedEventArgs e);
	    public event DayBoxDoubleClickedEventHandler DayBoxDoubleClicked;
	    public delegate void DayBoxDoubleClickedEventHandler(NewAppointmentEventArgs e);
	    public event AppointmentDblClickedEventHandler AppointmentDblClicked;
	    public delegate void AppointmentDblClickedEventHandler(int appointmentId);

        public MonthCalendarControl()
        {
            InitializeComponent();

    	    _displayMonth = _DisplayStartDate.Month;
	        _displayYear = _DisplayStartDate.Year;
	        sysCal = Thread.CurrentThread.CurrentUICulture.Calendar;

            BuildCalendarUi();
        }

	    public DateTime DisplayStartDate {
		    get { return _DisplayStartDate; }
		    set {
			    _DisplayStartDate = value;
			    _displayMonth = _DisplayStartDate.Month;
			    _displayYear = _DisplayStartDate.Year;
		    }
	    }

	    public List<Appointment> MonthAppointments {
		    get { return _monthAppointments; }
		    set {
			    _monthAppointments = value;
			    BuildCalendarUi();
		    }
	    }

	    private void MonthView_Loaded(object sender, RoutedEventArgs e)
	    {
		    //-- Want to have the calendar show up, even if no appoints are assigned 
		    //   Note - in my own app, appointments are loaded by a backgroundWorker thread to avoid a laggy UI
		    if (_monthAppointments == null)
			    BuildCalendarUi();
	    }

	    private void BuildCalendarUi()
	    {
		    int iDaysInMonth = sysCal.GetDaysInMonth(_DisplayStartDate.Year, _DisplayStartDate.Month);
		    int iOffsetDays = Convert.ToInt32(Enum.ToObject(typeof(DayOfWeek), _DisplayStartDate.DayOfWeek)) - 1 /* without -1 for US calendar */;
            if (iOffsetDays < 0) iOffsetDays = 6;
		    int iWeekCount = 0;
		    var weekRowCtrl = new WeekOfDaysControl();

		    MonthViewGrid.Children.Clear();
		    AddRowsToMonthGrid(iDaysInMonth, iOffsetDays);
            MonthYearLabel.Content = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.GetMonthName(_displayMonth) + " " + _displayYear;

		    for (int i = 1; i <= iDaysInMonth; i++) {
			    if ((i != 1) && Math.IEEERemainder((i + iOffsetDays - 1), 7) == 0) {
				    //-- add existing weekrowcontrol to the monthgrid
				    Grid.SetRow(weekRowCtrl, iWeekCount);
				    MonthViewGrid.Children.Add(weekRowCtrl);
				    //-- make a new weekrowcontrol
				    weekRowCtrl = new WeekOfDaysControl();
			        iWeekCount++;
			    }

			    //-- load each weekrow with a DayBoxControl whose label is set to day number
			    var dayBox = new DayBoxControl {DayNumberLabel = {Content = i.ToString(CultureInfo.InvariantCulture)}, Tag = i};
		        dayBox.MouseDoubleClick += DayBox_DoubleClick;

			    //-- customize daybox for today:
			    if ((new DateTime(_displayYear, _displayMonth, i)) == DateTime.Today) {
				    dayBox.DayLabelRowBorder.Background = (Brush)dayBox.TryFindResource("TodayGradient");
			        dayBox.DayAppointmentsStack.Background = new SolidColorBrush(Color.FromArgb(87,188,206,125));
			    }

			    //-- for design mode, add appointments to random days for show...
			    if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) {
				    if (Math.Round(1d) < 0.25) {
					    var apt = new DayBoxAppointmentControl {DisplayText = {Text = "Apt on " + i + "th"}};
				        dayBox.DayAppointmentsStack.Children.Add(apt);
				    }

			    } else if (_monthAppointments != null) {
				    //-- Compiler warning about unpredictable results if using i (the iterator) in lambda, the 
				    //   "hint" suggests declaring another var and set equal to iterator var
				    int iday = i;
				    var aptInDay = _monthAppointments.FindAll(apt => Convert.ToDateTime(apt.StartTime).Day == iday);
				    foreach (Appointment a in aptInDay) {
					    var apt = new DayBoxAppointmentControl {DisplayText = {Text = a.Subject}, Tag = a.AppointmentId};
				        apt.MouseDoubleClick += Appointment_DoubleClick;
					    dayBox.DayAppointmentsStack.Children.Add(apt);
				    }

			    }

			    Grid.SetColumn(dayBox, (i - (iWeekCount * 7)) + iOffsetDays);
			    weekRowCtrl.WeekRowGrid.Children.Add(dayBox);
		    }
		    Grid.SetRow(weekRowCtrl, iWeekCount);
		    MonthViewGrid.Children.Add(weekRowCtrl);
	    }

	    private void AddRowsToMonthGrid(int daysInMonth, int offSetDays)
	    {
		    MonthViewGrid.RowDefinitions.Clear();
		    var rowHeight = new GridLength(60, GridUnitType.Star);

		    int endOffSetDays = 7 - (Convert.ToInt32(Enum.ToObject(typeof(DayOfWeek), _DisplayStartDate.AddDays(daysInMonth - 1).DayOfWeek)) /* + 1 for US calendar format */);

		    for (int i = 1; i <= Convert.ToInt32((daysInMonth + offSetDays + endOffSetDays) / 7); i++) {
			    dynamic rowDef = new RowDefinition();
			    rowDef.Height = rowHeight;
			    MonthViewGrid.RowDefinitions.Add(rowDef);
		    }
	    }

	    private void UpdateMonth(int monthsToAdd)
	    {
		    var ev = new MonthChangedEventArgs {OldDisplayStartDate = _DisplayStartDate};
	        DisplayStartDate = _DisplayStartDate.AddMonths(monthsToAdd);
		    ev.NewDisplayStartDate = _DisplayStartDate;
		    if (DisplayMonthChanged != null) {
			    DisplayMonthChanged(ev);
		    }
            BuildCalendarUi();
	    }

	    #region UI Event Handlers

        private void MonthGoPrev_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateMonth(-1);
        }

        private void MonthGoNext_OnClick(object sender, RoutedEventArgs e)
	    {
		    UpdateMonth(1);
	    }

	    private void Appointment_DoubleClick(object sender, MouseButtonEventArgs e)
	    {
		    if (e.Source is DayBoxAppointmentControl) 
            {
			    if (((DayBoxAppointmentControl)e.Source).Tag != null) 
                {
				    //-- You could put your own call to your appointment-displaying code or whatever here..
				    if (AppointmentDblClicked != null) 
					    AppointmentDblClicked(Convert.ToInt32(((DayBoxAppointmentControl)e.Source).Tag));
			    }
			    e.Handled = true;
		    }
	    }

	    private void DayBox_DoubleClick(object sender, MouseButtonEventArgs e)
	    {
		    //-- call to FindVisualAncestor to make sure they didn't click on existing appointment (in which case,
		    //   that appointment window is already opened by handler Appointment_DoubleClick)

		    if (e.Source is DayBoxControl && Utilities.FindVisualAncestor(typeof(DayBoxAppointmentControl), (Visual)e.OriginalSource) == null) {
			    var ev = new NewAppointmentEventArgs();
			    if (((DayBoxControl)e.Source).Tag != null) {
				    ev.StartDate = new DateTime(_displayYear, _displayMonth, Convert.ToInt32(((DayBoxControl)e.Source).Tag), 10, 0, 0);
				    ev.EndDate = Convert.ToDateTime(ev.StartDate).AddHours(2);
			    }
			    if (DayBoxDoubleClicked != null) {
				    DayBoxDoubleClicked(ev);
			    }
			    e.Handled = true;
		    }
	    }

	    public void MonthView()
	    {
		    Loaded += MonthView_Loaded;
	    }

	    #endregion
    }

    public struct MonthChangedEventArgs
    {
	    public DateTime OldDisplayStartDate;
	    public DateTime NewDisplayStartDate;
    }

    public struct NewAppointmentEventArgs
    {
	    public DateTime? StartDate;
	    public DateTime? EndDate;
	    public int? CandidateId;
	    public int? RequirementId;
    }

    public static class Utilities
    {
	    //-- Many thanks to Bea Stollnitz, on whose blog I found the original C# version of below in a drag-drop helper class... 
	    public static FrameworkElement FindVisualAncestor(Type ancestorType, Visual visual)
	    {
		    while ((visual != null && !ancestorType.IsInstanceOfType(visual))) 
			    visual = (Visual)VisualTreeHelper.GetParent(visual);
		    return (FrameworkElement)visual;
	    }
    }
}