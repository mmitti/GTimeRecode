using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Google;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Globalization;

namespace gtest
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CalendarList mCalendarList;
        private CalendarService mService;
        public MainWindow()
        {
            InitializeComponent();
            StartDate.SelectedDate = EndDate.SelectedDate = DateTime.Now;
        }

        private async Task Run()
        {
            UserCredential credential;
            FileStream stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read);
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { CalendarService.Scope.Calendar },
                    "user", CancellationToken.None);
            mService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar API Sample",
            });
            mCalendarList = mService.CalendarList.List().Execute();
            foreach (CalendarListEntry entry in mCalendarList.Items)
            {
                CalenderCombo.Items.Add(entry);
            }
        
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Run();
            CalenderCombo.SelectedIndex = 0;
        }

        private async void CalenderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            content.Visibility = Visibility.Collapsed;
            loading.Visibility = Visibility.Visible;
            StringBuilder sb = new StringBuilder();
            CalendarListEntry entry = (CalendarListEntry)CalenderCombo.SelectedItem;
            await Task.Run(() =>{
                EventsResource.ListRequest req = mService.Events.List(entry.Id);
                //req.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                req.TimeMax = DateTime.Now;
                req.TimeMin = DateTime.Now.AddDays(-5);
                req.SingleEvents = true;
                //req.MaxResults = 10;
               
                DateTime dt = DateTime.FromFileTimeUtc(0);
                foreach (Event ev in req.Execute().Items)
                {
                    if (!dt.Date.Equals(((DateTime)ev.Start.DateTime).Date))
                    {
                        dt = (DateTime)ev.Start.DateTime;
                        sb.AppendLine(dt.Date.ToShortDateString());
                    }
                    sb.AppendLine(ev.Summary + ":" + ev.Description);
                }
            });
            
            CalanderPreview.Text = sb.ToString();
            content.Visibility = Visibility.Visible;
            loading.Visibility = Visibility.Collapsed;
        }

        private async void Regist_Click(object sender, RoutedEventArgs e)
        {
            Event ev = new Event();

            String[] formats = { "HH:mm", "H:mm", "HH:m", "H:m", "HHmm" };
            DateTime start_date = (DateTime)StartDate.SelectedDate;
            DateTime start_time = DateTime.ParseExact(StartTime.Text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            DateTime end_date = (DateTime)EndDate.SelectedDate;
            DateTime end_time = DateTime.ParseExact(EndTime.Text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            DateTime start = new DateTime(start_date.Year, start_date.Month, start_date.Day, start_time.Hour, start_time.Minute, 0);
            DateTime end = new DateTime(end_date.Year, end_date.Month, end_date.Day, end_time.Hour, end_time.Minute, 0);

            ev.Summary = EventTitle.Text;
            ev.Description = Description.Text;
            ev.Start = new EventDateTime();
            ev.Start.TimeZone = "Asia/Tokyo";
            ev.End = new EventDateTime();
            ev.Start.DateTime = start;


            ev.End.DateTime = end;
            ev.End.TimeZone = "Asia/Tokyo";
            CalendarListEntry entry = (CalendarListEntry)CalenderCombo.SelectedItem;
            Regist.Content = "登録中";
            content.IsEnabled = false;
            await Task.Run(() =>
            {
                mService.Events.Insert(ev, entry.Id).Execute();
            });
            Description.Text = "";
            StartTime.Text = EndTime.Text;
            EndTime.Text = "";
            Regist.Content = "登録";
            content.IsEnabled = true;
        }
    }

}
