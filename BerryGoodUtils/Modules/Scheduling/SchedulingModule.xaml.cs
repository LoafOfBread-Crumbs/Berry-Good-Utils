using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BerryGoodUtils.Core.Scheduling;
using BerryGoodUtils.Models;
using BerryGoodUtils.Services;
using BerryGoodUtils.Services.Auth;
using BerryGoodUtils.Services.Calendar;

namespace BerryGoodUtils.Modules.Scheduling;

public partial class SchedulingModule : UserControl, IUtilityModule
{
    public string ModuleName => "Scheduling";
    public string Description => "Manage recurring customer schedules and sync them to Google Calendar.";
    public string Icon => "📅";
    public UserControl View => this;

    private readonly GoogleCalendarService _calendarService;
    private AppData _appData;
    private DateTime _displayMonth;
    private DateTime _selectedDate;
    private string? _selectedScheduleId;
    private CustomerSchedule? SelectedSchedule => _appData.Schedules.FirstOrDefault(s => s.Id == _selectedScheduleId);
    private readonly List<Border> _dayCells = [];

    public SchedulingModule(GoogleAuthService authService)
    {
        _calendarService = new GoogleCalendarService(authService);
        InitializeComponent();
        _appData = DataService.LoadAppData();
        _displayMonth = DateTime.Now;
        _selectedDate = DateTime.Now.Date;
        InitializeCalendarGrid();
        Loaded += (_, _) => RefreshData();
    }

    private void RefreshData()
    {
        _appData = DataService.LoadAppData();
        BuildCalendarDays();
        RefreshSelectedDate();
        RefreshUpcoming();
        UpdateActionStates();
    }

    private void UpdateActionStates()
    {
        var hasSelection = SelectedSchedule != null;
        btnEdit.IsEnabled = hasSelection;
        btnDelete.IsEnabled = hasSelection;
        btnSync.IsEnabled = hasSelection;
    }

    #region Calendar Grid

    private void InitializeCalendarGrid()
    {
        calendarDaysGrid.Children.Clear();
        calendarClickGrid.Children.Clear();
        _dayCells.Clear();

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var cell = CreateDayCell(row, col);
                _dayCells.Add(cell);
                calendarDaysGrid.Children.Add(cell);

                var clickCell = CreateClickCell(row, col);
                calendarClickGrid.Children.Add(clickCell);
            }
        }
    }

    private Border CreateDayCell(int row, int col)
    {
        var cell = new Border
        {
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(col == 0 ? 0 : 0.5, row == 0 ? 0 : 0.5, 0.5, 0.5),
            Background = Brushes.White,
            Padding = new Thickness(4),
            Cursor = Cursors.Hand
        };

        var panel = new StackPanel();
        var dayText = new TextBlock
        {
            FontSize = 12,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#334155")!,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        panel.Children.Add(dayText);

        var indicatorPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 6, 0, 0)
        };
        panel.Children.Add(indicatorPanel);
        cell.Child = panel;

        cell.Tag = new DayCellTag { DayText = dayText, IndicatorPanel = indicatorPanel };
        return cell;
    }

    private Border CreateClickCell(int row, int col)
    {
        var cell = new Border
        {
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Tag = new Point(row, col)
        };
        cell.MouseLeftButtonUp += CalendarCell_Click;
        return cell;
    }

    private void BuildCalendarDays()
    {
        var firstOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
        var startOffset = (int)firstOfMonth.DayOfWeek;

        tbMonthYear.Text = _displayMonth.ToString("MMMM yyyy");

        var rangeStart = firstOfMonth.AddDays(-startOffset);
        var rangeEnd = rangeStart.AddDays(41);

        var occurrenceLookup = new Dictionary<DateTime, List<CustomerSchedule>>();
        foreach (var schedule in _appData.Schedules)
        {
            var customer = _appData.Customers.FirstOrDefault(c => c.Id == schedule.CustomerId);
            if (customer == null)
                continue;

            foreach (var date in ScheduleOccurrenceCalculator.GetOccurrences(schedule, rangeStart, rangeEnd))
            {
                if (!occurrenceLookup.TryGetValue(date.Date, out var list))
                {
                    list = [];
                    occurrenceLookup[date.Date] = list;
                }
                list.Add(schedule);
            }
        }

        for (int i = 0; i < 42; i++)
        {
            var date = rangeStart.AddDays(i);
            var cell = _dayCells[i];
            var tag = (DayCellTag)cell.Tag;
            var isCurrentMonth = date.Month == _displayMonth.Month;
            var isToday = date.Date == DateTime.Today;
            var isSelected = date.Date == _selectedDate.Date;

            tag.DayText.Text = date.Day.ToString();
            tag.DayText.Foreground = isCurrentMonth
                ? (SolidColorBrush)new BrushConverter().ConvertFrom("#334155")!
                : (SolidColorBrush)new BrushConverter().ConvertFrom("#cbd5e1")!;

            cell.Background = isSelected
                ? (SolidColorBrush)new BrushConverter().ConvertFrom("#eff6ff")!
                : isToday
                    ? (SolidColorBrush)new BrushConverter().ConvertFrom("#fefce8")!
                    : Brushes.White;

            tag.IndicatorPanel.Children.Clear();
            if (occurrenceLookup.TryGetValue(date.Date, out var schedules) && schedules.Count > 0)
            {
                foreach (var _ in schedules.Take(3))
                {
                    tag.IndicatorPanel.Children.Add(new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = (SolidColorBrush)FindResource("BerryBlueBrush"),
                        Margin = new Thickness(2, 0, 2, 0)
                    });
                }
            }
        }
    }

    private void CalendarCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Point point)
            return;

        var firstOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
        var startOffset = (int)firstOfMonth.DayOfWeek;
        var date = firstOfMonth.AddDays(-startOffset).AddDays(point.X * 7 + point.Y);
        _selectedDate = date.Date;
        BuildCalendarDays();
        RefreshSelectedDate();
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        BuildCalendarDays();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(1);
        BuildCalendarDays();
    }

    #endregion

    #region Selected Date / Upcoming

    private void RefreshSelectedDate()
    {
        tbSelectedDate.Text = _selectedDate.ToString("dddd, dd MMMM yyyy");

        var schedules = _appData.Schedules
            .Where(s => ScheduleOccurrenceCalculator.GetOccurrences(s, _selectedDate, _selectedDate).Any())
            .OrderBy(s => s.StartDateTime.TimeOfDay)
            .Select(s => ToListItem(s))
            .ToList();

        icSelectedDateSchedules.ItemsSource = schedules;
        tbNoSelectedSchedules.Visibility = schedules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshUpcoming()
    {
        var upcoming = _appData.Schedules
            .Select(s => new { Schedule = s, Next = ScheduleOccurrenceCalculator.GetNextOccurrence(s) })
            .Where(x => x.Next.HasValue)
            .OrderBy(x => x.Next)
            .Select(x => ToListItem(x.Schedule, x.Next))
            .ToList();

        dgUpcoming.ItemsSource = upcoming;
        tbNoUpcoming.Visibility = upcoming.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private ScheduleListItem ToListItem(CustomerSchedule schedule, DateTime? nextDate = null)
    {
        var customer = _appData.Customers.FirstOrDefault(c => c.Id == schedule.CustomerId);
        var start = schedule.StartDateTime;
        var end = schedule.EndDateTime;
        var timeRange = $"{start:hh:mm tt} - {end:hh:mm tt}";
        if (start.TimeOfDay == end.TimeOfDay)
            timeRange = start.ToString("hh:mm tt");

        return new ScheduleListItem
        {
            Schedule = schedule,
            Title = schedule.Title,
            CustomerName = customer?.Name ?? "Unknown customer",
            TimeRange = timeRange,
            NextDate = nextDate ?? schedule.StartDateTime.Date,
            RecurrenceLabel = GetRecurrenceLabel(schedule)
        };
    }

    private static string GetRecurrenceLabel(CustomerSchedule schedule)
    {
        return schedule.RecurrenceType switch
        {
            ScheduleRecurrenceType.Weekly => "Weekly",
            ScheduleRecurrenceType.BiWeekly => "Bi-weekly",
            ScheduleRecurrenceType.Monthly => "Monthly",
            ScheduleRecurrenceType.Yearly => "Yearly",
            ScheduleRecurrenceType.CustomInterval => $"Every {schedule.CustomIntervalValue} {schedule.CustomIntervalUnit}",
            ScheduleRecurrenceType.SpecificDates => $"{schedule.SpecificDates.Count} date(s)",
            _ => "Once"
        };
    }

    private void ScheduleItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not ScheduleListItem item)
            return;

        _selectedScheduleId = item.Schedule.Id;
        dgUpcoming.SelectedItem = item;
        UpdateActionStates();
    }

    private void DgUpcoming_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedScheduleId = (dgUpcoming.SelectedItem as ScheduleListItem)?.Schedule.Id;
        UpdateActionStates();
    }

    #endregion

    #region Actions

    private void AddSchedule_Click(object sender, RoutedEventArgs e)
    {
        var schedule = new CustomerSchedule();
        var window = new SchedulingEditWindow(schedule, _appData.Customers)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        _appData.Schedules.Add(schedule);
        SaveAndSyncIfRequested(schedule);
        RefreshData();
        SelectSchedule(schedule);
    }

    private void EditSchedule_Click(object sender, RoutedEventArgs e)
    {
        var schedule = SelectedSchedule;
        if (schedule == null)
            return;

        var window = new SchedulingEditWindow(schedule, _appData.Customers)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() != true)
            return;

        SaveAndSyncIfRequested(schedule);
        RefreshData();
    }

    private void DeleteSchedule_Click(object sender, RoutedEventArgs e)
    {
        var schedule = SelectedSchedule;
        if (schedule == null)
            return;

        var result = MessageBox.Show($"Delete schedule '{schedule.Title}'?\n\nThis will also remove it from Google Calendar if synced.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _ = _calendarService.DeleteScheduleAsync(schedule);
        _appData.Schedules.Remove(schedule);
        DataService.SaveAppData(_appData);
        _selectedScheduleId = null;
        RefreshData();
    }

    private void SyncSelected_Click(object sender, RoutedEventArgs e)
    {
        var schedule = SelectedSchedule;
        if (schedule == null)
            return;

        SyncSchedule(schedule);
    }

    private void SyncAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var schedule in _appData.Schedules.ToList())
        {
            SyncSchedule(schedule);
        }
        RefreshData();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshData();
    }

    private async void SyncSchedule(CustomerSchedule schedule)
    {
        var customer = _appData.Customers.FirstOrDefault(c => c.Id == schedule.CustomerId);
        if (customer == null)
        {
            MessageBox.Show("The customer for this schedule no longer exists.", "Sync Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _calendarService.SyncScheduleAsync(schedule, customer);
            DataService.SaveAppData(_appData);
            RefreshData();
            MessageBox.Show(result.Message, result.Success ? "Sync Complete" : "Sync Failed", MessageBoxButton.OK, result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sync failed: {ex.Message}", "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SaveAndSyncIfRequested(CustomerSchedule schedule)
    {
        schedule.UpdatedAt = DateTime.Now;
        DataService.SaveAppData(_appData);

        if (schedule.IsSynced)
        {
            SyncSchedule(schedule);
        }
    }

    private void SelectSchedule(CustomerSchedule schedule)
    {
        _selectedScheduleId = schedule.Id;
        _selectedDate = ScheduleOccurrenceCalculator.GetNextOccurrence(schedule) ?? schedule.StartDateTime.Date;
        _displayMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
        RefreshData();
    }

    private void SetBusy(bool busy)
    {
        IsEnabled = !busy;
    }

    #endregion

    private class DayCellTag
    {
        public TextBlock DayText { get; set; } = null!;
        public WrapPanel IndicatorPanel { get; set; } = null!;
    }
}

public sealed class ScheduleListItem
{
    public CustomerSchedule Schedule { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public DateTime NextDate { get; set; }
    public string RecurrenceLabel { get; set; } = string.Empty;
}
