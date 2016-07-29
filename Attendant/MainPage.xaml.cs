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
using System.Text.RegularExpressions;

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
        // Define the pivot pages.  Add one here if you need to create a new one.
        private const int MainSignInOutPivot = 0;
        private const int SignOutPivot = 1;
        private const int ConfigurationPivot = 2;
        private const int SignOutCustomPivot = 3;
        private const int SignOutCustomWithInitialsPivot = 4;

        // The Windows RT mysql release does not support SSL.  This needs fixed...
        // TODO: Convert to use app-settings, add hidden configuration screen.
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        // Database connection
        private MySqlConnection dbConn;

        // Common destinations.
        private CommonDestination[] CommonDestinations = new CommonDestination[8];

        // Global application state variables including student information, tab state information, etc.
        private Int32 iStudentNumber = 0;
        private Int32 iStudentID = 0;
        private bool bStudentCheckedIn = false;
        private TextBlock currentTextBlockMessage;
        private int KeyModeCounter = 0;
        private int ConfigKeyCounter = 0;

        public MainPage()
        {
            this.InitializeComponent();

            // Initalize the application and it's state variables.
            SelectUIPage(0);

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

            // Attempt to open the long term connection.
            OpenDatabase();

            // Attempt to fill in the common destinations array.
            PopulateCommonDestinations();

            // Get the daily news blurb.
            string url = "http://" + localSettings.Values["DBServer"] + "/motd.php";
            Uri navUri = new Uri(url);
            //webviewDailyNews.NavigateToString(url);
            webviewDailyNews.Navigate(navUri);            
        }

        private void OpenDatabase()
        {
            // Attempt to open the long term connection.
            try
            {
                string csMySQL = "Server=" + localSettings.Values["DBServer"] +
                    ";Database=" + localSettings.Values["DBDatabase"] +
                    ";Uid=" + localSettings.Values["DBUser"] +
                    ";Pwd=" + localSettings.Values["DBPassword"] + ";SslMode=None;";

                dbConn = new MySqlConnection(csMySQL);
                dbConn.Open();
                string debugOutput = String.Format("MySQL datbase connection open.  Mysql version : {0}", dbConn.ServerVersion);
                Debug.WriteLine(debugOutput);
            }
            catch (Exception ex)
            {
                // Show the error message, and the restart button.
                textBlockMessage.Visibility = Visibility.Visible;
                btnRestart.Visibility = Visibility.Visible;
                currentTextBlockMessage.Text = "An error occured when connecting to the MySQL Database server.  Please validate the hostname, database name, username and password, then restart the system." + "\nException: " + ex.Message + "\n";
            }
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
                            btnSignOut_Dest1.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest1.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest1.Content = CommonDestinations[i].LocationName;
                        break;
                    case 1:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest2.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest2.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest2.Content = CommonDestinations[i].LocationName;
                        break;
                    case 2:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest3.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest3.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest3.Content = CommonDestinations[i].LocationName;
                        break;
                    case 3:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest4.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest4.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest4.Content = CommonDestinations[i].LocationName;
                        break;
                    case 4:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest5.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest5.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest5.Content = CommonDestinations[i].LocationName;
                        break;
                    case 5:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest6.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest6.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest6.Content = CommonDestinations[i].LocationName;
                        break;
                    case 6:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest7.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest7.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest7.Content = CommonDestinations[i].LocationName;
                        break;
                    case 7:
                        if (CommonDestinations[i].Active)
                            btnSignOut_Dest8.Visibility = Visibility.Visible;
                        else
                            btnSignOut_Dest8.Visibility = Visibility.Collapsed;
                        btnSignOut_Dest8.Content = CommonDestinations[i].LocationName;
                        break;
                }
            }

        }

        /// <summary>
        /// Restart button handler.  Click me to Exit() the CoreApplication and reload it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Windows.ApplicationModel.Core.CoreApplication.Exit();
        }


        private bool ProcessStudentNumber()
        {
            // Validate the database connection is still open.  This can close for a number of reasons, so validate that it's still alive.
            if (!dbConn.Ping())
                OpenDatabase();

            // Clear any old messages.
            textBlockMessage.Text = "";

            if (tbStudentID.Text == "")
            {
                String text = String.Format("An empty student ID is like an empty imagination...  Please enter your student ID and try again.");
                currentTextBlockMessage.Text = text;
                return false;
            }

            // Check for a valid Student ID number, and if found, go to check in / out screen.
            try
            {
                bool bStudentFound = false;

                // Parse the student number.
                iStudentNumber = Int32.Parse(tbStudentID.Text);

                string sqlStatement = "SELECT * FROM Students WHERE StudentNumber = " + iStudentNumber + " AND CurrentStudent = TRUE";
                MySqlDataReader studentReader = null;
                MySqlCommand sqlCmd = new MySqlCommand(sqlStatement, dbConn);
                studentReader = sqlCmd.ExecuteReader();

                // Ensure there are rows, and pull the data.
                if (studentReader.HasRows)
                {
                    bStudentFound = true;
                    studentReader.Read();
                    iStudentID = studentReader.GetInt32("ID");
                    bStudentCheckedIn = studentReader.GetBoolean("CurrentlyCheckedIn");
                }

                // Close the reader.  We have what we need.
                studentReader.Close();

                if (!bStudentFound)
                {
                    String text = String.Format("Your student number was not found!  Please verify that you exist.");
                    currentTextBlockMessage.Text = text;
                    tbStudentID.Text = "";
                    return false;
                }
                // Fall through to success.
            }
            catch (FormatException)
            {
                String text = String.Format("Please stop trying to create a SQL injection and play nice.");
                currentTextBlockMessage.Text = text;
                tbStudentID.Text = "";
                return false;
            }
            catch (Exception ex)
            {
                String text = String.Format("An unusual error occured while attempting to execute a database operation." + "\nException: " + ex.Message + "\n");
                currentTextBlockMessage.Text = text;
                tbStudentID.Text = "";
                return false;
            }

            return true;
        }


        private void SelectUIPage(int destinationTab)
        {
            PivotItem piMainSignInOutPivot = (PivotItem)this.pivMainPivot.Items[MainSignInOutPivot];
            PivotItem piSignOutPivot = (PivotItem)this.pivMainPivot.Items[SignOutPivot];
            PivotItem piConfiguration = (PivotItem)this.pivMainPivot.Items[ConfigurationPivot];
            PivotItem piSignOutCustom = (PivotItem)this.pivMainPivot.Items[SignOutCustomPivot];
            PivotItem piSignOutCustomWithInitials = (PivotItem)this.pivMainPivot.Items[SignOutCustomWithInitialsPivot];

            switch (destinationTab)
            {
                case MainSignInOutPivot: // Main tab.
                    
                    this.pivMainPivot.SelectedIndex = 0;
                    piMainSignInOutPivot.IsEnabled = true;
                    piSignOutPivot.IsEnabled = false;
                    piConfiguration.IsEnabled = false;
                    piSignOutCustom.IsEnabled = false;
                    piSignOutCustomWithInitials.IsEnabled = false;

                    // Set focus.
                    piMainSignInOutPivot.Focus(FocusState.Programmatic);
                    tbStudentID.Focus(FocusState.Programmatic);

                    // Clear state.
                    tbStudentID.Text = "";
                    iStudentNumber = 0;
                    iStudentID = 0;
                    bStudentCheckedIn = false;
                    textBlockSignInMessage.Text = "";

                    currentTextBlockMessage = textBlockSignInMessage;
                    break;
                case SignOutPivot: // Main Sign Out Pivot.
                    this.pivMainPivot.SelectedIndex = 1;
                    piMainSignInOutPivot.IsEnabled = false;
                    piSignOutPivot.IsEnabled = true;
                    piConfiguration.IsEnabled = false;
                    piSignOutCustom.IsEnabled = false;
                    piSignOutCustomWithInitials.IsEnabled = false;

                    piSignOutPivot.Focus(FocusState.Programmatic);
                    currentTextBlockMessage = textBlockSignOut;
                    break;
                case ConfigurationPivot: // Configuration Pivot
                    this.pivMainPivot.SelectedIndex = 2;
                    piMainSignInOutPivot.IsEnabled = false;
                    piSignOutPivot.IsEnabled = false;
                    piConfiguration.IsEnabled = true;
                    piSignOutCustom.IsEnabled = false;
                    piSignOutCustomWithInitials.IsEnabled = false;

                    piConfiguration.Focus(FocusState.Programmatic);
                    currentTextBlockMessage = textBlockConfigurationMessage;

                    // Set the values from the application local settings data.
                    tbDatabaseIP.Text = (string)localSettings.Values["DBServer"];
                    tbDatabaseInstance.Text = (string)localSettings.Values["DBDatabase"];
                    tbDatabaseUserName.Text = (string)localSettings.Values["DBUser"];
                    pbPassword.Password = (string)localSettings.Values["DBPassword"];

                    break;
                case SignOutCustomPivot: // Sign out with custom destination.
                    this.pivMainPivot.SelectedIndex = 3;
                    piMainSignInOutPivot.IsEnabled = false;
                    piSignOutPivot.IsEnabled = false;
                    piConfiguration.IsEnabled = false;
                    piSignOutCustom.IsEnabled = true;
                    piSignOutCustomWithInitials.IsEnabled = false;

                    piSignOutCustom.Focus(FocusState.Programmatic);
                    currentTextBlockMessage = textBlockCheckOutCustomMessage;

                    tbCustomDest.Text = "";
                    break;
                case SignOutCustomWithInitialsPivot: // Sign out with custom destination and initials.
                    this.pivMainPivot.SelectedIndex = 4;
                    piMainSignInOutPivot.IsEnabled = false;
                    piSignOutPivot.IsEnabled = false;
                    piConfiguration.IsEnabled = false;
                    piSignOutCustom.IsEnabled = false;
                    piSignOutCustomWithInitials.IsEnabled = true;

                    piSignOutCustomWithInitials.Focus(FocusState.Programmatic);
                    currentTextBlockMessage = textBlockCheckOutCustomWithInitialsMessage;

                    tbCustomDestWithStaffInitials.Text = "";
                    tbStaffInitials.Text = "";
                    break;
            }

            // Clear any existing message when we change state.
            currentTextBlockMessage.Text = "";
        }

        //
        // Primary database manipulation methods.
        //

        private async void ProcessSignIn()
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
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text2; });
                    await System.Threading.Tasks.Task.Delay(5000);
                }

                String text1 = String.Format("Checkin Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text1; });
                await System.Threading.Tasks.Task.Delay(5000);
            }


            // Show status message for 3 seconds.
            String text = String.Format("You are signed in!");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
            await System.Threading.Tasks.Task.Delay(2000);
            SelectUIPage(0);
        }

        private async void ProcessCheckOutCommonDestination(UInt32 destination)
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
                cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID) VALUES ( 2, NOW(), " + iStudentID + ", NULL, " + destination + " )";
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
                    String text2 = String.Format("Sign-Out Processing Rollback Error!  Please inform your administrator: \nException: " + ex2.Message);
                    currentTextBlockMessage.Text = text2;
                }

                String text1 = String.Format("Sign-Out Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                currentTextBlockMessage.Text = text1;
            }

            // Show status message for 3 seconds.
            String text;
            if (destination > 0 && destination < 9)
                text = String.Format("Processing Sign Out to {0}", CommonDestinations[destination-1].LocationName);
            else
                text = String.Format("Processing Check Out...");

            //textBlockCheckInOut.Text = text;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
            await System.Threading.Tasks.Task.Delay(3000);

            // Flip back to the main page.
            SelectUIPage(MainSignInOutPivot);
        }

        private async void ProcessCheckOutCustomDestination(string destinationName)
        {
            // Execute the checkin event.
            MySqlTransaction trans = null;
            try
            {
                // Clean up the text for SQL Injection and such.
                string cleanDestination = MySqlEscape(destinationName);


                trans = dbConn.BeginTransaction();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = dbConn;
                cmd.Transaction = trans;

                cmd.CommandText = "UPDATE Students SET CurrentlyCheckedIn=FALSE WHERE ID=" + iStudentID;
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID) VALUES ( 2, NOW(), " + iStudentID + ", '" + cleanDestination + "', NULL)";
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
                    String text2 = String.Format("Sign-Out Processing Rollback Error!  Please inform your administrator: \nException: " + ex2.Message);
                    currentTextBlockMessage.Text = text2;
                }

                String text1 = String.Format("Sign-Out Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                currentTextBlockMessage.Text = text1;
            }

            // Show status message for 3 seconds.
            String text;
            text = String.Format("Processing Check Out...");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
            await System.Threading.Tasks.Task.Delay(3000);

            // Flip back to the main page.
            SelectUIPage(MainSignInOutPivot);
        }

        private async void ProcessCheckOutCustomDestinationWithInitials(string destinationName, string initials)
        {
            // Execute the checkin event.
            MySqlTransaction trans = null;
            try
            {
                // Clean up the text for SQL Injection and such.
                string cleanDestination = MySqlEscape(destinationName);
                string cleanInitials = MySqlEscape(initials);


                trans = dbConn.BeginTransaction();

                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = dbConn;
                cmd.Transaction = trans;

                cmd.CommandText = "UPDATE Students SET CurrentlyCheckedIn=FALSE WHERE ID=" + iStudentID;
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO Events (EventType, InstanceDate, StudentID, Destination, CommonDestinationID, Initials) VALUES ( 2, NOW(), " + iStudentID + ", '" + cleanDestination + "', NULL, '" + cleanInitials + "')";
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
                    String text2 = String.Format("Sign-Out Processing Rollback Error!  Please inform your administrator: \nException: " + ex2.Message);
                    currentTextBlockMessage.Text = text2;
                }

                String text1 = String.Format("Sign-Out Processing Error!  Please inform your administrator: \nException: " + ex1.Message);
                currentTextBlockMessage.Text = text1;
            }

            // Show status message for 3 seconds.
            String text;
            text = String.Format("Processing Check Out...");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
            await System.Threading.Tasks.Task.Delay(3000);

            // Flip back to the main page.
            SelectUIPage(MainSignInOutPivot);
        }


        //
        // Handlers for the Main Pivot
        //

        /// <summary>
        /// The Main Pivot's Sign In button handler.
        /// </summary>
        private async void btnMainSignIn_Click(object sender, RoutedEventArgs e)
        {
            // Check the student's state and see if they're already checked in.
            if (ProcessStudentNumber())
            {
                if (bStudentCheckedIn)
                {
                    // Show an error message letting them know they're already signed in.
                    String text = String.Format("You are already signed in!  Have a {0} and carry on...", GetRandomFoodItem());
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
                    await System.Threading.Tasks.Task.Delay(2000);
                    SelectUIPage(MainSignInOutPivot);
                    return;
                }

                // We now have global state on the student.  If are already signed in, flash the you're already signed in message and return to the main screen.
                ProcessSignIn();
            }
            else
            {
                // Wait for the error message to show.
                await System.Threading.Tasks.Task.Delay(2000);
            }
        }

        private async void btnMainSignOut_Click(object sender, RoutedEventArgs e)
        {
            // Check the student's state and see if they're already checked in.
            if (ProcessStudentNumber())
            {

                if (!bStudentCheckedIn)
                {
                    // Show an error message letting them know they're already signed out.
                    String text = String.Format("You are already signed out!  Have a {0} and carry on...", GetRandomFoodItem());
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { currentTextBlockMessage.Text = text; });
                    await System.Threading.Tasks.Task.Delay(2000);
                    SelectUIPage(MainSignInOutPivot);
                    return;
                }

                // We now have global state on the student.  If are already signed in, flash the you're already signed in message and return to the main screen.
                SelectUIPage(SignOutPivot);
            }
            else
            {
                // Wait for the error message to show.
                await System.Threading.Tasks.Task.Delay(2000);
            }

        }

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
                //ActivateConfigurationScreen();
                SelectUIPage(ConfigurationPivot);
            }

        }

        private void tbStudentID_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            //if (e.Key == Windows.System.VirtualKey.Enter)
            //    ProcessStudentID();

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



        //
        // Handlers for the Primary Sign Out Pivot
        //

        private async void btnSignOutCustomDest_Click(object sender, RoutedEventArgs e)
        {
            SelectUIPage(SignOutCustomPivot);
        }

        private async void btnSignOutWithInitials_Click(object sender, RoutedEventArgs e)
        {
            // Take them to the checkout screen with the destination field.
            SelectUIPage(SignOutCustomWithInitialsPivot);
        }

        private async void btnSignOut_Dest1_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(1);
        }
        private async void btnSignOut_Dest2_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(2);
        }

        private async void btnSignOut_Dest3_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(3);
        }

        private async void btnSignOut_Dest4_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(4);
        }

        private async void btnSignOut_Dest5_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(5);
        }

        private async void btnSignOut_Dest6_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(6);
        }

        private async void btnSignOut_Dest7_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(7);
        }

        private async void btnSignOut_Dest8_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCommonDestination(8);
        }

        //
        // Handlers for Custom Destination Pivot.
        //

        private async void btnCheckOutCustomDest_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCustomDestination(tbCustomDest.Text);
        }

        //
        // Handlers for Custom Destination with Initials Pivot.
        //

        private async void btnCheckOutCustomDestWithStaffInitisls_Click(object sender, RoutedEventArgs e)
        {
            ProcessCheckOutCustomDestinationWithInitials(tbCustomDestWithStaffInitials.Text, tbStaffInitials.Text);
        }

        //
        // Handlers for the Configuration pivot.
        //

        private void btnAbortChanges_Click(object sender, RoutedEventArgs e)
        {
            // Switch to the main input page
            SelectUIPage(MainSignInOutPivot);
        }

        private void btnSaveConfigChanges_Click(object sender, RoutedEventArgs e)
        {
            // Save application state.
            localSettings.Values["DBServer"] = tbDatabaseIP.Text;
            localSettings.Values["DBDatabase"] = tbDatabaseInstance.Text;
            localSettings.Values["DBUser"] = tbDatabaseUserName.Text;
            localSettings.Values["DBPassword"] = pbPassword.Password;

            // Switch to the main input page
            SelectUIPage(MainSignInOutPivot);
        }

        //
        // Utility Methods
        //

        private string MySqlEscape(string usString)
        {
            if (usString == null)
            {
                return null;
            }
            // SQL Encoding for MySQL Recommended here:
            // http://au.php.net/manual/en/function.mysql-real-escape-string.php
            // it escapes \r, \n, \x00, \x1a, baskslash, single quotes, and double quotes
            return Regex.Replace(usString, @"[\r\n\x00\x1a\\'""]", @"\$0");
        }

        private string GetRandomFoodItem()
        {
            Random rnd = new Random();

            string[] foodArray =
            {
                "scone","donut","struddel","cheese danish","cherry danish","slice of hamloaf","slice of vegan hamloaf","peice of cherry pie",
                "peice of apple pie","bowl of icecream","couscous","dumpling","french fry","pea","spaghetti squash","marshmallow","baguette",
                "truffle","soybean","chestnut","olive","almond","shallot","Mandarin orange","gelatin","white chocolate","lentil","cookie",
                "cauliflower","eggplant","blueberry","turkey","pine nut","green bean","fig","octopus","pumpkin","cucumber","apricot","coconut",
                "plantain","strawberry","tortilla","nectarine","watermelon","cashew nut","cranberry","custard","tomatoe","chickpea","wild rice",
                "arugula","steak","grapefruit","jelly bean","lobster","potatoe","melon","kumquat","chive","sweet pepper","kiwi","huckleberry",
                "aioli","okra","granola","crouton","brazil nut","ice cream","marmalade sandwich","avocado","raisin","banana","snow pea",
                "chipotle pepper","bruschetta","parsnip","succotash","summer squash","pickle","apple","asparagus","pineapple","guava",
                "chaurice sausage","ancho chile pepper","lemon","hazelnut","pear","hash brown","English muffin","plum","papaya","anchovy",
                "grape","honeydew melon","raspberry","broccoli","coffee","peach","sweet potatoe","bowl of macaroni","Goji berry","hamburger",
                "swiss cheese wedge","sausage","sushi roll","pecan","peanut butter sammich","fried egg","habanero","red cabbage","passion fruit",
                "gorgonzola crumble","lettuce","onion","mozzarella stick","breadfruit","turnip","broccoli raab","black olive","sunflower seed",
                "chocolate","cabbage","focaccia","cider","ham","celery","squash","cottage cheese","bagle"

            };

            int r = rnd.Next(foodArray.Length);
            return foodArray[r];
        }


    }
}
