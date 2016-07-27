using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Attendant
{
    struct CommonDestination {
        public int ID;
        public string LocationName;
        public string LocationPhoneNumber;
        public string AdminComments;
        public bool Active;
    };

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool bFlipedPage = false;
        // The Windows RT mysql release does not support SSL.  This needs fixed...
        // TODO: Convert to use app-settings, add hidden configuration screen.
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private MySqlConnection dbConn;

        // This is the main student ID variable we deal with.
        private Int32 iStudentNumber = 0;
        private Int32 iStudentID = 0;
        private bool bStudentCheckedIn = false;
        private CommonDestination[] CommonDestinations = new CommonDestination[8];

        public MainPage()
        {
            this.InitializeComponent();

            // Initalize the Application Data if it's not already present.
            if ((string)localSettings.Values["DBServer"] == null ) localSettings.Values["DBServer"] = "Server IP";
            if ((string)localSettings.Values["DBDatabase"] == null) localSettings.Values["DBDatabase"] = "Database Name";
            if ((string)localSettings.Values["DBUser"] == null) localSettings.Values["DBUser"] = "User Name";
            if ((string)localSettings.Values["DBPassword"] == null) localSettings.Values["DBPassword"] = "User Password";

            // Register an encoder so that the .NET CRL will be able to convert strings (unicode) to ASCII for the
            // mysql client libraries for Windows 8.1 RT
            System.Text.EncodingProvider ppp;
            ppp = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(ppp);

            try
            {
                string csMySQL = "Server=" + localSettings.Values["DBServer"] +
                    ";Database=" + localSettings.Values["DBDatabase"] + 
                    ";Uid=" + localSettings.Values["DBUser"] +
                    ";Pwd=" + localSettings.Values["DBPassword"] + ";SslMode=None;";
                
                
                dbConn = new MySqlConnection(csMySQL);
                dbConn.Open();
                string debugOutput = String.Format("MySQL version : {0}", dbConn.ServerVersion);
                Debug.WriteLine(debugOutput);

            }
            catch (Exception ex)
            {
                // Show the error message, and the restart button.
                textBlockMessage.Visibility = Visibility.Visible;
                btnRestart.Visibility = Visibility.Visible;
                textBlockMessage.Text = "An error occured when connecting to the MySQL Database server.  Please validate the hostname, database name, username and password, then restart the system." + "\nException: " + ex.Message + "\n";
            }

            // Attempt to fill in the common destinations array.
            PopulateCommonDestinations();

            // Get the daily news blurb.
            string url = "http://" + localSettings.Values["DBServer"] + "/motd.php";
            Uri navUri = new Uri(url);
            //webviewDailyNews.NavigateToString(url);
            webviewDailyNews.Navigate(navUri);
        }

        void PopulateCommonDestinations()
        {
            try
            {
                // Likely a better way to do this mess.
                string sqlStatement = "SELECT * FROM CommonDestinations WHERE ID in (1,2,3,4,5,6,7,8)";
                MySqlDataReader destinationsReader = null;
                MySqlCommand sqlCmd = new MySqlCommand(sqlStatement, dbConn);
                destinationsReader = sqlCmd.ExecuteReader();

                if (destinationsReader.HasRows)
                {
                    // If we have rows, read them.
                    int index = 0;
                    while (destinationsReader.Read())
                    {
                        CommonDestinations[index].ID = destinationsReader.GetInt32("ID");
                        CommonDestinations[index].LocationName = destinationsReader.GetString("LocationName");
                        CommonDestinations[index].LocationPhoneNumber = destinationsReader.GetString("LocationPhoneNumber");
                        CommonDestinations[index].AdminComments = destinationsReader.GetString("AdminComments");
                        CommonDestinations[index].Active = destinationsReader.GetBoolean("Active");
                        index++;
                    }
                    // Clear the rest out.
                    for (; index < 8; index++)
                    {
                        CommonDestinations[index].ID = 0;
                        CommonDestinations[index].LocationName = "";
                        CommonDestinations[index].LocationPhoneNumber = "";
                        CommonDestinations[index].AdminComments = "";
                        CommonDestinations[index].Active = false;
                    }
                }

                destinationsReader.Close();
            }
            catch (Exception)
            {
                // Something went wrong.  We can't do anything about it right here, so just blank out the common destinations.
                for (int index = 0; index < 8; index++)
                {
                    CommonDestinations[index].ID = 0;
                    CommonDestinations[index].LocationName = "";
                    CommonDestinations[index].LocationPhoneNumber = "";
                    CommonDestinations[index].AdminComments = "";
                    CommonDestinations[index].Active = false;
                }
            }

            // Now populate the destinations with the data on the check out controls.
            for (int i = 0; i < 8; i++)
            {
                // I know there's a way to do this cleanly by creating a string control name and
                // manipulating the control through some kind of dynamic execution.  Sadly, I don't
                // have time to look for that elegance right now.

                switch(i)
                {
                    case 0:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest1.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest1.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest1.Content = CommonDestinations[i].LocationName;
                        break;
                    case 1:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest2.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest2.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest2.Content = CommonDestinations[i].LocationName;
                        break;
                    case 2:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest3.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest3.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest3.Content = CommonDestinations[i].LocationName;
                        break;
                    case 3:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest4.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest4.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest4.Content = CommonDestinations[i].LocationName;
                        break;
                    case 4:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest5.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest5.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest5.Content = CommonDestinations[i].LocationName;
                        break;
                    case 5:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest6.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest6.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest6.Content = CommonDestinations[i].LocationName;
                        break;
                    case 6:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest7.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest7.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest7.Content = CommonDestinations[i].LocationName;
                        break;
                    case 7:
                        if (CommonDestinations[i].Active)
                            btnCheckOut_Dest8.Visibility = Visibility.Visible;
                        else
                            btnCheckOut_Dest8.Visibility = Visibility.Collapsed;
                        btnCheckOut_Dest8.Content = CommonDestinations[i].LocationName;
                        break;
                }
            }

        }

        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void tbStudentID_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Windows.ApplicationModel.Core.CoreApplication.Exit();
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            ProcessStudentID();
        }

        private void ProcessStudentID()
        {
            // Clear any old messages.
            textBlockMessage.Text = "";

            if (tbStudentID.Text == "")
            {
                textBlockMessage.Text = "An empty student ID is like an empty imagination...  Please enter your student ID and try again.";
                return;
            }

            // Check for a valid Student ID number, and if found, go to check in / out screen.
            try
            {
                bool bStudentFound = false;

                // Parse the student number.
                iStudentNumber = Int32.Parse(tbStudentID.Text);

                string sqlStatement = "SELECT * FROM Students WHERE StudentNumber = " + iStudentNumber;
                MySqlDataReader studentReader = null;
                MySqlCommand sqlCmd = new MySqlCommand(sqlStatement, dbConn);
                studentReader = sqlCmd.ExecuteReader();

                if (studentReader.HasRows)
                {
                    bStudentFound = true;
                    studentReader.Read();
                    iStudentID = studentReader.GetInt32("ID");
                    bStudentCheckedIn = studentReader.GetBoolean("CurrentlyCheckedIn");
                }

                studentReader.Close();

                if (!bStudentFound)
                {
                    textBlockMessage.Text = "Your student number was not found!  Please verify that you exist.";
                    tbStudentID.Text = "";
                    return;
                }
                else
                {
                    ToggleSelectUIPage();
                }
            }
            catch (FormatException)
            {
                textBlockMessage.Text = "Please stop trying to create a SQL injection and play nice.";
                tbStudentID.Text = "";
            }
            catch (Exception ex)
            {
                textBlockMessage.Text = "An unusual error occured while attempting to execute a database operation." + "\nException: " + ex.Message + "\n";
                tbStudentID.Text = "";
            }
        }

        private void ToggleSelectUIPage()
        {
            if (bFlipedPage)
            {
                // Flip the UI back to the main page.
                this.pivMainPivot.SelectedIndex = 0;
                PivotItem piStudentIDPivot = (PivotItem)this.pivMainPivot.Items[0];
                PivotItem piCheckInOutPivot = (PivotItem)this.pivMainPivot.Items[1];
                piCheckInOutPivot.IsEnabled = false;
                piStudentIDPivot.IsEnabled = true;
                piStudentIDPivot.Focus(FocusState.Programmatic);
                tbStudentID.Focus(FocusState.Programmatic);
                bFlipedPage = !bFlipedPage;
                tbStudentID.Text = "";
                iStudentNumber = 0;
                iStudentID = 0;
                bStudentCheckedIn = false;
            }
            else
            {
                // Flip the UI back to the check in/out.
                this.pivMainPivot.SelectedIndex = 1;
                PivotItem piStudentIDPivot = (PivotItem)this.pivMainPivot.Items[0];
                PivotItem piCheckInOutPivot = (PivotItem)this.pivMainPivot.Items[1];
                piCheckInOutPivot.IsEnabled = true;
                piStudentIDPivot.IsEnabled = false;
                piCheckInOutPivot.Focus(FocusState.Programmatic);
                bFlipedPage = !bFlipedPage;
                textBlockCheckInOut.Text = "";

                if (bStudentCheckedIn)
                {
                    // Set the Check in options as invalid.
                    btnCheckIn.IsEnabled = false;
                    btnCheckOut.IsEnabled = true;
                    btnCheckOut_Dest1.IsEnabled = true;
                    btnCheckOut_Dest2.IsEnabled = true;
                    btnCheckOut_Dest3.IsEnabled = true;
                    btnCheckOut_Dest4.IsEnabled = true;
                    btnCheckOut_Dest5.IsEnabled = true;
                    btnCheckOut_Dest6.IsEnabled = true;
                    btnCheckOut_Dest7.IsEnabled = true;
                    btnCheckOut_Dest8.IsEnabled = true;
                }
                else
                {
                    // Set the check out options as invalid.  
                    btnCheckOut.IsEnabled = false;
                    btnCheckOut_Dest1.IsEnabled = false;
                    btnCheckOut_Dest2.IsEnabled = false;
                    btnCheckOut_Dest3.IsEnabled = false;
                    btnCheckOut_Dest4.IsEnabled = false;
                    btnCheckOut_Dest5.IsEnabled = false;
                    btnCheckOut_Dest6.IsEnabled = false;
                    btnCheckOut_Dest7.IsEnabled = false;
                    btnCheckOut_Dest8.IsEnabled = false;
                    btnCheckIn.IsEnabled = true;
                }
            }
        }

        private async void ProcessCheckIn()
        {
            // Execute the checkin event.
            MySqlTransaction trans = null;
            try
            {
                trans = dbConn.BeginTransaction();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = dbConn;
                cmd.Transaction = trans;

                cmd.CommandText = "UPDATE Students SET CurrentlyCheckedIn=TRUE WHERE ID=" + iStudentID;
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID) VALUES ( 1, NOW(), " + iStudentID + ", NULL, NULL )";
                cmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch (MySqlException ex1)
            {
                try
                {
                    trans.Rollback();
                }
                catch (MySqlException ex2)
                {
                    String text2 = String.Format("Checkin Processing Rollback Error!  Please inform your administrator: \nException: " + ex2.Message);
                    textBlockCheckInOut.Text = text2;
                }

                String text1 = String.Format("Checkin Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                textBlockCheckInOut.Text = text1;
            }


            // Show status message for 3 seconds.
            String text = String.Format("Processing Check In...");
            //textBlockCheckInOut.Text = text;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { textBlockCheckInOut.Text = text; });
            await System.Threading.Tasks.Task.Delay(3000);
            ToggleSelectUIPage();
        }

        private async void ProcessCheckOut(UInt32 destination)
        {
            // Execute the checkin event.
            MySqlTransaction trans = null;
            try
            {
                trans = dbConn.BeginTransaction();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = dbConn;
                cmd.Transaction = trans;

                cmd.CommandText = "UPDATE Students SET CurrentlyCheckedIn=FALSE WHERE ID=" + iStudentID;
                cmd.ExecuteNonQuery();
                if (destination == 0) // no actual destination given.
                {
                    cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID) VALUES ( 2, NOW(), " + iStudentID + ", NULL, NULL)";
                }
                else
                {
                    cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID) VALUES ( 2, NOW(), " + iStudentID + ", NULL, " + destination + " )";
                }
                
                cmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch (MySqlException ex1)
            {
                try
                {
                    trans.Rollback();

                }
                catch (MySqlException ex2)
                {
                    String text2 = String.Format("CheckOut Processing Rollback Error!  Please inform your administrator: \nException: " + ex2.Message);
                    textBlockCheckInOut.Text = text2;
                }

                String text1 = String.Format("CheckOut Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                textBlockCheckInOut.Text = text1;
            }

            // Show status message for 3 seconds.
            String text;
            if (destination > 1 && destination < 9)
                text = String.Format("Processing Check Out to {0}", CommonDestinations[destination].LocationName);
            else
                text = String.Format("Processing Check Out...");

            //textBlockCheckInOut.Text = text;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { textBlockCheckInOut.Text = text; });
            await System.Threading.Tasks.Task.Delay(3000);

            // Flip back to the main page.
            ToggleSelectUIPage();
        }

        private async void btnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckIn();
        }

        private async void btnCheckOut_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(0);
        }

        private async void btnCheckOut_Dest1_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(1);
        }
        private async void btnCheckOut_Dest2_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(2);
        }

        private async void btnCheckOut_Dest3_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(3);
        }

        private async void btnCheckOut_Dest4_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(4);
        }

        private async void btnCheckOut_Dest5_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(5);
        }

        private async void btnCheckOut_Dest6_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(6);
        }

        private async void btnCheckOut_Dest7_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(7);
        }

        private async void btnCheckOut_Dest8_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOut(8);
        }

        private int KeyModeCounter = 0;
        private int ConfigKeyCounter = 0;

        private void tbStudentID_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
                KeyModeCounter++;

            if (e.Key == Windows.System.VirtualKey.Control)
                KeyModeCounter++;

            if (KeyModeCounter >= 2 && e.Key == Windows.System.VirtualKey.Escape)
                ConfigKeyCounter++;

            if (ConfigKeyCounter == 5)
            {
                ActivateConfigurationScreen();
            }

        }

        private void ActivateConfigurationScreen()
        {
            // Set the values from the application local settings data.
            tbDatabaseIP.Text = (string)localSettings.Values["DBServer"];
            tbDatabaseInstance.Text = (string)localSettings.Values["DBDatabase"];
            tbDatabaseUserName.Text = (string)localSettings.Values["DBUser"];
            pbPassword.Password = (string)localSettings.Values["DBPassword"];

            // Switch to the hidden pivot.
            this.pivMainPivot.SelectedIndex = 2;
            PivotItem piStudentIDPivot = (PivotItem)this.pivMainPivot.Items[0];
            PivotItem piCheckInOutPivot = (PivotItem)this.pivMainPivot.Items[1];
            PivotItem piConfigPivot = (PivotItem)this.pivMainPivot.Items[2];
            piCheckInOutPivot.IsEnabled = false;
            piStudentIDPivot.IsEnabled = false;
            piConfigPivot.IsEnabled = true;
            piConfigPivot.Focus(FocusState.Programmatic);
            ConfigKeyCounter = 0;
        }

        private void DeActivateConfigurationScreen()
        {
            this.pivMainPivot.SelectedIndex = 0;
            PivotItem piStudentIDPivot = (PivotItem)this.pivMainPivot.Items[0];
            PivotItem piCheckInOutPivot = (PivotItem)this.pivMainPivot.Items[1];
            PivotItem piConfigPivot = (PivotItem)this.pivMainPivot.Items[2];
            piStudentIDPivot.IsEnabled = true;
            piCheckInOutPivot.IsEnabled = false;
            piConfigPivot.IsEnabled = false;
            piStudentIDPivot.Focus(FocusState.Programmatic);
        }

        private void tbStudentID_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ProcessStudentID();
            }

            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                KeyModeCounter = 0;
                ConfigKeyCounter = 0;
            }

            if (e.Key == Windows.System.VirtualKey.Control)
            {
                KeyModeCounter = 0;
                ConfigKeyCounter = 0;
            }

            
        }


        private void btnAbortChanges_Click(object sender, RoutedEventArgs e)
        {
            // Switch to the main input page
            DeActivateConfigurationScreen();
        }

        private void btnSaveConfigChanges_Click(object sender, RoutedEventArgs e)
        {
            // Save application state.
            localSettings.Values["DBServer"] = tbDatabaseIP.Text;
            localSettings.Values["DBDatabase"] = tbDatabaseInstance.Text;
            localSettings.Values["DBUser"] = tbDatabaseUserName.Text;
            localSettings.Values["DBPassword"] = pbPassword.Password;

            // Switch to the main input page
            DeActivateConfigurationScreen();
        }
    }
}
