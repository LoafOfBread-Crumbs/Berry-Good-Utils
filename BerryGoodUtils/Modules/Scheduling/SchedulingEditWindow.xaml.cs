using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BerryGoodUtils.Models;

namespace BerryGoodUtils.Modules.Scheduling;

public partial class SchedulingEditWindow : Window
{
    public CustomerSchedule Schedule { get; }
    private readonly ObservableCollection<Customer> _customers;
    private ObservableCollection<DateTime> _specificDates;

    public SchedulingEditWindow(CustomerSchedule schedule, ObservableCollection<Customer> customers)
    {
        InitializeComponent();
        Schedule = schedule;
        _customers = customers;
        _specificDates = new ObservableCollection<DateTime>(schedule.SpecificDates.OrderBy(d => d.Date));

        tbTitle.Text = string.IsNullOrWhiteSpace(schedule.Title) ? "New Schedule" : "Edit Schedule";

        cmbCustomer.ItemsSource = _customers.OrderBy(c => c.Name).ToList();
        cmbCustomer.SelectedItem = _customers.FirstOrDefault(c => c.Id == schedule.CustomerId);

        txtTitle.Text = schedule.Title;
        txtDescription.Text = schedule.Description;
        txtLocation.Text = schedule.Location;

        dpStartDate.SelectedDate = schedule.StartDateTime.Date;
        txtStartTime.Text = schedule.StartDateTime.ToString("hh:mm tt");
        dpEndDate.SelectedDate = schedule.EndDateTime.Date;
        txtEndTime.Text = schedule.EndDateTime.ToString("hh:mm tt");

        cmbRecurrence.ItemsSource = new[]
        {
            ScheduleRecurrenceType.None,
            ScheduleRecurrenceType.Weekly,
            ScheduleRecurrenceType.BiWeekly,
            ScheduleRecurrenceType.Monthly,
            ScheduleRecurrenceType.Yearly,
            ScheduleRecurrenceType.CustomInterval,
            ScheduleRecurrenceType.SpecificDates
        };
        cmbRecurrence.SelectedItem = schedule.RecurrenceType;

        cmbCustomIntervalUnit.ItemsSource = new[]
        {
            CustomIntervalUnit.Days,
            CustomIntervalUnit.Weeks,
            CustomIntervalUnit.Months,
            CustomIntervalUnit.Years
        };
        cmbCustomIntervalUnit.SelectedItem = schedule.CustomIntervalUnit;
        txtCustomIntervalValue.Text = schedule.CustomIntervalValue.ToString();

        lbSpecificDates.ItemsSource = _specificDates;
        txtReminder.Text = schedule.ReminderMinutesBefore.ToString();

        UpdatePanels();
    }

    private void CmbRecurrence_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePanels();
    }

    private void UpdatePanels()
    {
        var type = cmbRecurrence.SelectedItem as ScheduleRecurrenceType? ?? ScheduleRecurrenceType.None;
        gridCustomInterval.Visibility = type == ScheduleRecurrenceType.CustomInterval ? Visibility.Visible : Visibility.Collapsed;
        panelSpecificDates.Visibility = type == ScheduleRecurrenceType.SpecificDates ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddSpecificDate_Click(object sender, RoutedEventArgs e)
    {
        if (dpSpecificDate.SelectedDate is not DateTime date)
            return;

        if (!_specificDates.Contains(date.Date))
            _specificDates.Add(date.Date);

        _specificDates = new ObservableCollection<DateTime>(_specificDates.OrderBy(d => d.Date));
        lbSpecificDates.ItemsSource = _specificDates;
    }

    private void RemoveSpecificDate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not DateTime date)
            return;

        _specificDates.Remove(date.Date);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var customer = cmbCustomer.SelectedItem as Customer;
        if (customer == null)
        {
            MessageBox.Show("Please select a customer.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var title = txtTitle.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Please enter a title.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (dpStartDate.SelectedDate is not DateTime startDate || dpEndDate.SelectedDate is not DateTime endDate)
        {
            MessageBox.Show("Please select start and end dates.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseTime(txtStartTime.Text, out var startTime))
        {
            MessageBox.Show("Start time is not valid. Use format like 08:00 AM.", "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseTime(txtEndTime.Text, out var endTime))
        {
            MessageBox.Show("End time is not valid. Use format like 09:00 AM.", "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var recurrenceType = cmbRecurrence.SelectedItem as ScheduleRecurrenceType? ?? ScheduleRecurrenceType.None;
        if (recurrenceType == ScheduleRecurrenceType.SpecificDates && !_specificDates.Any())
        {
            MessageBox.Show("Please add at least one specific date.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtReminder.Text, out var reminderMinutes) || reminderMinutes < 0)
        {
            MessageBox.Show("Reminder must be a non-negative number of minutes.", "Invalid Reminder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var startDateTime = startDate.Date.Add(startTime);
        var endDateTime = endDate.Date.Add(endTime);
        if (endDateTime < startDateTime)
        {
            MessageBox.Show("End time must be after start time.", "Invalid Time Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Schedule.CustomerId = customer.Id;
        Schedule.Title = title;
        Schedule.Description = txtDescription.Text?.Trim() ?? string.Empty;
        Schedule.Location = txtLocation.Text?.Trim() ?? string.Empty;
        Schedule.StartDateTime = startDateTime;
        Schedule.EndDateTime = endDateTime;
        Schedule.RecurrenceType = recurrenceType;
        Schedule.ReminderMinutesBefore = reminderMinutes;

        if (recurrenceType == ScheduleRecurrenceType.CustomInterval)
        {
            if (!int.TryParse(txtCustomIntervalValue.Text, out var intervalValue) || intervalValue < 1)
            {
                MessageBox.Show("Custom interval must be 1 or greater.", "Invalid Interval", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Schedule.CustomIntervalValue = intervalValue;
            Schedule.CustomIntervalUnit = cmbCustomIntervalUnit.SelectedItem as CustomIntervalUnit? ?? CustomIntervalUnit.Weeks;
        }

        if (recurrenceType == ScheduleRecurrenceType.SpecificDates)
        {
            Schedule.SpecificDates = _specificDates.Select(d => d.Date).ToList();
        }
        else if (Schedule.RecurrenceType != ScheduleRecurrenceType.SpecificDates)
        {
            Schedule.SpecificDates.Clear();
        }

        Schedule.UpdatedAt = DateTime.Now;
        DialogResult = true;
        Close();
    }

    private static bool TryParseTime(string? input, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();
        if (DateTime.TryParseExact(input, ["h:mm tt", "hh:mm tt", "H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
