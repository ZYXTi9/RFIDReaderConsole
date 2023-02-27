using Impinj.OctaneSdk;
using MySql.Data.MySqlClient;
using RfidReader.Database;
using System.Collections;
using System.Data;
using System.Net.NetworkInformation;
using System.Text;

namespace RfidReader.Reader
{
    class Impinj
    {
        static ImpinjReader impinjReader = new ImpinjReader();

        public List<ImpinjReader> listImpinjReaders = new List<ImpinjReader>();

        MySqlDatabase db = new MySqlDatabase();
        MySqlCommand? cmd;
        public int ReaderTypeID { get; set; }
        public int ReaderID { get; set; }
        //public string? HostName { get; set; }
        private string HostName = "192.168.1.15";
        public int AntennaID { get; set; }
        public int AntennaInfoID { get; set; }
        public int GPIID { get; set; }
        public int GPOID { get; set; }

        public string impinjStatus = "Connected";

        public static Hashtable uniqueTags = new();
        public static int totalTags = 0;
        public void ConnectToReader()
        {
            try
            {
                //Console.Write("Hostname or IP Name   : ");
                //HostName = Console.ReadLine();

                Console.WriteLine("Attempting to connect to {0} ({1}).",
                    impinjReader.Name, HostName);

                impinjReader.Connect(HostName);

                if (impinjReader.IsConnected == true)
                    //{
                    Console.WriteLine("Successfully connected.");

                //impinjReader.ApplyDefaultSettings();
                //Settings settings = impinjReader.QuerySettings();
                //if (settings != null)
                //{
                //    settings.Report.IncludeAntennaPortNumber = true;
                //    settings.Report.IncludeSeenCount = true;
                //    settings.TagPopulationEstimate = 32;
                //    settings.HoldReportsOnDisconnect = true;

                //    settings.Keepalives.Enabled = true;
                //    settings.Keepalives.PeriodInMs = 3000;
                //    settings.Keepalives.EnableLinkMonitorMode = true;
                //    settings.Keepalives.LinkDownThreshold = 5;

                //    impinjReader.KeepaliveReceived += OnKeepaliveReceived;
                //    impinjReader.ConnectionLost += OnConnectionLost;

                //    impinjReader.ApplySettings(settings);
                //    impinjReader.SaveSettings();
                //    settings.Save("settings.xml");
                //}
                {
                    string selQuery = @"SpCheckReader";
                    cmd = new MySqlCommand(selQuery, db.Con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    FeatureSet featureSet = impinjReader.QueryFeatureSet();

                    cmd.Parameters.AddWithValue("@rtypeID", ReaderTypeID);
                    cmd.Parameters.AddWithValue("@ip", HostName);
                    cmd.Parameters.AddWithValue("@device", "Impinj-" + featureSet.ModelName);
                    cmd.Parameters.AddWithValue("@readerStatus", impinjStatus);
                    var getReaderID = cmd.ExecuteScalar();

                    if (getReaderID != null)
                    {
                        ReaderID = Convert.ToInt32(getReaderID);
                    }

                    db.Con.Close();

                    MySqlDatabase db2 = new();

                    string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + "";
                    cmd = new MySqlCommand(selQuery2, db2.Con);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        dataReader.Close();
                        if (db2.Con.State != ConnectionState.Open)
                        {
                            db2.Con.Open();
                        }
                        db2.Con.Close();
                    }
                    else
                    {
                        Default();
                    }
                    Settings settings = Settings.Load("settings.xml");

                    impinjReader.KeepaliveReceived += OnKeepaliveReceived;
                    impinjReader.ConnectionLost += OnConnectionLost;

                    impinjReader.ApplySettings(settings);
                    impinjReader.SaveSettings();
                    settings.Save("settings.xml");
                    Menu();
                }
            }
            catch (OctaneSdkException)
            {
                Console.WriteLine("Failed to connect.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine();
            ConnectToReader();
        }
        public void Menu()
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("..................................................");
                Console.WriteLine("Welcome to Impinj RFID Settings");
                Console.WriteLine("..................................................\n");

                Console.WriteLine("----Command Menu----");
                Console.WriteLine("1. Reader Info");
                Console.WriteLine("2. Reader Settings");
                Console.WriteLine("3. Antenna Settings");
                Console.WriteLine("4. GPIO Configuration");
                Console.WriteLine("5. Back\n");
                Console.Write("[1-5] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ReaderInfo();
                            break;
                        case 2:
                            ReaderSettings();
                            break;
                        case 3:
                            AntennaSettings();
                            break;
                        case 4:
                            GPIOConfig();
                            break;
                        case 5:
                            Read();
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-5");
                            break;
                    }
                }
                catch (OctaneSdkException e)
                {
                    Console.WriteLine("Octane SDK exception: {0}", e.Message);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void ReaderInfo()
        {
            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            Console.WriteLine("\nReader Capabilities");
            Console.WriteLine("---------------");
            Console.WriteLine("Firware Version                              : {0}", featureSet.FirmwareVersion);
            Console.WriteLine("Model Name                                   : {0}", featureSet.ModelName);
            Console.WriteLine("Model Number                                 : {0}", featureSet.ModelNumber);
            Console.WriteLine("Reader model                                 : {0}", featureSet.ReaderModel.ToString());
            Console.WriteLine("Num Antenna Supported                        : {0}", featureSet.AntennaCount);
            Console.WriteLine("Num GPI Ports                                : {0}", featureSet.GpiCount);
            Console.WriteLine("Num GPO Ports                                : {0} \n", featureSet.GpoCount);

            Console.WriteLine("Reader Status");
            Console.WriteLine("---------------");
            Status status = impinjReader.QueryStatus();
            Console.WriteLine("Is connected                                 : {0}", status.IsConnected);
            Console.WriteLine("Is singulating                               : {0}", status.IsSingulating);
            Console.WriteLine("Temperature                                  : {0}° C\n", status.TemperatureInCelsius);

            try
            {
                Console.WriteLine("Current Reader Settings");
                Console.WriteLine("---------------");

                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM reader_settings_tbl WHERE ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        string readerMode = dataReader1.GetString("ReaderMode");
                        string searchMode = dataReader1.GetString("SearchMode");
                        int session = dataReader1.GetInt32("Session");
                        int tagPopulation = dataReader1.GetInt32("TagPopulation");

                        Console.WriteLine("Reader mode                                  : {0}", readerMode);
                        Console.WriteLine("Search mode                                  : {0}", searchMode);
                        Console.WriteLine("Session                                      : {0} ", session);
                        Console.WriteLine("Tag Population                               : {0} \n", tagPopulation);
                    }
                    db1.Con.Close();
                }

                Console.WriteLine("Current Power and Sensitivity Settings");
                Console.WriteLine("---------------");

                MySqlDatabase db2 = new();

                string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery2, db2.Con);
                MySqlDataReader dataReader2 = cmd.ExecuteReader();

                if (dataReader2.HasRows)
                {
                    while (dataReader2.Read())
                    {
                        int antenna = dataReader2.GetInt32("Antenna");
                        double rxSensitivity = dataReader2.GetDouble("ReceiveSensitivity");
                        double txPower = dataReader2.GetDouble("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("ReceiveSensitivityIndex     : {0} ", rxSensitivity);
                        Console.WriteLine("TransmitPowerIndex          : {0} \n", txPower);
                    }
                    db2.Con.Close();
                }

                Console.WriteLine("Current GPI Configuration");
                Console.WriteLine("---------------");

                MySqlDatabase db3 = new();

                string selQuery3 = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + " ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery3, db3.Con);
                MySqlDataReader dataReader3 = cmd.ExecuteReader();

                if (dataReader3.HasRows)
                {
                    while (dataReader3.Read())
                    {
                        int gpiPort = dataReader3.GetInt32("GPIPort");
                        string gpiLevel = dataReader3.GetString("GPIStatus");
                        int debounce = dataReader3.GetInt32("Debounce");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiLevel.Equals("True")) Console.WriteLine("GPI Level                   : High");
                        else Console.WriteLine("GPI Level                   : Low");
                        Console.WriteLine("Debounce                    : {0} \n", debounce);
                    }
                    db3.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void ReaderSettings()
        {
            bool isWorking = true;
            int option, readerMode = 0, searchMode = 0, session = 0, tagPopulation = 0;

            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            while (isWorking)
            {
                Console.WriteLine("\n----Reader Settings----");
                Console.WriteLine("1. Reader Mode, Search Mode & Session");
                Console.WriteLine("2. Go back");
                Console.Write("\n[1-2] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Settings settings = Settings.Load("settings.xml");
                            while (readerMode != 1 && readerMode != 2 && readerMode != 3)
                            {
                                try
                                {
                                    Console.WriteLine("\nReader Mode Menu");
                                    Console.WriteLine("1. DenseReaderM4");
                                    Console.WriteLine("2. DenseReaderM8");
                                    Console.WriteLine("3. AutoSetDenseReader\n");

                                    Console.Write("Reader Mode  : ");
                                    readerMode = Convert.ToInt32(Console.ReadLine());

                                    if (readerMode == 1) settings.ReaderMode = ReaderMode.DenseReaderM4;
                                    else if (readerMode == 2) settings.ReaderMode = ReaderMode.DenseReaderM8;
                                    else if (readerMode == 3) settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                                    else Console.WriteLine("Enter a valid Integer in the range 1-3\n");
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("Invalid Input Format");
                                }
                            }
                            while (searchMode != 1 && searchMode != 2 && searchMode != 3 && searchMode != 4)
                            {
                                try
                                {
                                    Console.WriteLine("\nSearch Mode Menu");
                                    Console.WriteLine("1. ReaderSelected");
                                    Console.WriteLine("2. SingleTarget");
                                    Console.WriteLine("3. DualTarget");
                                    Console.WriteLine("4. TagFocus\n");

                                    Console.Write("Search Mode : ");
                                    searchMode = Convert.ToInt32(Console.ReadLine());

                                    if (searchMode == 1) settings.SearchMode = SearchMode.ReaderSelected;
                                    else if (searchMode == 2) settings.SearchMode = SearchMode.SingleTarget;
                                    else if (searchMode == 3) settings.SearchMode = SearchMode.DualTarget;
                                    else if (searchMode == 4) settings.SearchMode = SearchMode.TagFocus;
                                    else Console.WriteLine("Enter a valid Integer in the range 1-4\n");
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("Invalid Input Format");
                                }
                            }
                            while (session != 1 && session != 2)
                            {
                                try
                                {
                                    Console.WriteLine("\nSession Mode Menu");
                                    Console.WriteLine("1. Session 1");
                                    Console.WriteLine("2. Session 2\n");

                                    Console.Write("Session     : ");
                                    session = Convert.ToInt32(Console.ReadLine());

                                    if (session == 1) settings.Session = 1;
                                    else if (session == 2) settings.Session = 2;
                                    else Console.WriteLine("Enter a valid Integer in the range 1-2\n");
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("Invalid Input Format\n");
                                }
                            }
                            while (tagPopulation <= 0 || tagPopulation > 1000)
                            {
                                try
                                {
                                    Console.Write("Tag Population : ");
                                    tagPopulation = Convert.ToInt32(Console.ReadLine());

                                    if (tagPopulation <= 0 || tagPopulation > 10000)
                                    {
                                        Console.WriteLine("Value " + tagPopulation + " could not be converted.\n");
                                    }
                                    else
                                    {
                                        settings.TagPopulationEstimate = Convert.ToUInt16(tagPopulation);

                                        impinjReader.ApplySettings(settings);
                                        impinjReader.SaveSettings();
                                        settings.Save("settings.xml");

                                        string selQuery = @"SpReaderSettings";
                                        cmd = new MySqlCommand(selQuery, db.Con);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                                        cmd.Parameters.AddWithValue("@readerM", settings.ReaderMode.ToString());
                                        cmd.Parameters.AddWithValue("@searchM", settings.SearchMode.ToString());
                                        cmd.Parameters.AddWithValue("@sess", settings.Session.ToString());
                                        cmd.Parameters.AddWithValue("@tp", settings.TagPopulationEstimate.ToString());

                                        db.Con.Open();
                                        cmd.ExecuteScalar();
                                        db.Con.Close();

                                        Console.WriteLine("\nSet Reader Settings Successfully");
                                    }
                                }
                                catch (OctaneSdkException)
                                {
                                    Console.WriteLine("Impinj Reader Setting Error");
                                }
                            }
                            isWorking = false;
                            break;
                        case 2:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (OctaneSdkException ose)
                {
                    Console.WriteLine(ose.Message);
                    isWorking = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void AntennaSettings()
        {
            int option;
            bool isWorking = true;

            while (isWorking)
            {
                Console.WriteLine("\n----Antenna Settings---");
                Console.WriteLine("1. Power & Sensitivity");
                Console.WriteLine("2. Enable/Disable Antenna");
                Console.WriteLine("3. Back to Main Menu\n");
                Console.Write("[1-3] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ConfigurePowerAndSensitivity();
                            break;
                        case 2:
                            EnableDisableAntenna();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void ConfigurePowerAndSensitivity()
        {
            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            bool isWorking = true;
            int option;
            int antenna;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Antenna Power & Sensitivity");
                Console.WriteLine("2. Get Antenna Power & Sensitivity");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Console.Write("\nAntenna                          : ");

                            antenna = Convert.ToInt32(Console.ReadLine());
                            if (antenna <= 0 || antenna > featureSet.AntennaCount)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + featureSet.AntennaCount);
                                continue;
                            }

                            Settings settings = Settings.Load("settings.xml");

                            ushort antID = (ushort)antenna;
                            double[] sensitivityValue = { -80, -79, -78, -77, -76, -75, -74, -73, -72, -71, -70, -69, -68, -67, -66, -65, -64, -63, -62, -61, -60, -59, -58, -57, -56, -55, -54, -53, -52, -51, -50, -49, -48, -47, -46, -45, -44, -43, -42, -41, -40, -39, -38, -37, -36, -35, -34, -33, -32, -31, -30 };
                            Console.WriteLine("\nChoose between: -80 to -10");
                            Console.Write("Receive Sensitivity              : ");
                            settings.Antennas.GetAntenna(antID).RxSensitivityInDbm = Convert.ToDouble(Console.ReadLine());

                            if (sensitivityValue.Contains(settings.Antennas.GetAntenna(antID).RxSensitivityInDbm))
                            {
                                double[] powerValues = { 32.5, 32.25, 32, 31.75, 31.5, 31.25, 31, 30.75, 30.5, 30.25, 30, 29.75, 29.5, 29.25, 29, 28.75, 28.5, 28.25, 28, 27.75, 27.5, 27.25, 27, 26.75, 26.5, 26.25, 26, 25.75, 25.5, 25.25, 25, 24.75, 24.5, 24.25, 24, 23.75, 23.5, 23.25, 23, 22.75, 22.25, 22, 21.75, 21.5, 21.25, 21, 20.75, 20.5, 20.25, 20, 19.75, 19.5, 19.25, 19, 18.75, 18.5, 18.25, 18, 17.75, 17.5, 17.25, 17, 16.75, 16.5, 16.25, 16, 15.75, 15.5, 15.25, 15, 14.75, 14.5, 14.25, 14, 13.75, 13.5, 13.25, 13, 12.75, 12.5, 12.25, 12, 11.75, 11.5, 11.25, 11, 10.75, 10.5, 10.25, 10 };
                                Console.WriteLine("\nChoose between: 10 to 32.5");
                                Console.WriteLine("Ex. 32.5, 25.75, 15.25, 10");
                                Console.Write("Transmit Power                   : ");
                                settings.Antennas.GetAntenna(antID).TxPowerInDbm = Convert.ToDouble(Console.ReadLine());

                                if (powerValues.Contains(settings.Antennas.GetAntenna(antID).TxPowerInDbm))
                                {
                                    impinjReader.ApplySettings(settings);
                                    impinjReader.SaveSettings();
                                    settings.Save("settings.xml");

                                    string selQuery = @"SpCheckAntenna";
                                    cmd = new MySqlCommand(selQuery, db.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@rID", ReaderID);
                                    cmd.Parameters.AddWithValue("@ant", antenna);
                                    cmd.Parameters.AddWithValue("@sensitivity", settings.Antennas.GetAntenna(antID).RxSensitivityInDbm);
                                    cmd.Parameters.AddWithValue("@power", settings.Antennas.GetAntenna(antID).TxPowerInDbm);
                                    db.Con.Open();
                                    cmd.ExecuteScalar();
                                    db.Con.Close();
                                    Console.WriteLine("\nSet Antenna Configuration Successfully");
                                }
                                else
                                {
                                    Console.WriteLine("Input is invalid");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Input is invalid");
                                continue;
                            }
                            break;
                        case 2:
                            DisplayPowerAndSensitivity();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void DisplayPowerAndSensitivity()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int antenna = dataReader.GetInt32("Antenna");
                        double rxSensitivity = dataReader.GetDouble("ReceiveSensitivity");
                        double txPower = dataReader.GetDouble("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("ReceiveSensitivityIndex     : {0} ", rxSensitivity);
                        Console.WriteLine("TransmitPowerIndex          : {0} \n", txPower);
                    }
                    db.Con.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void EnableDisableAntenna()
        {
            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            bool isWorking = true;
            int option;
            int antenna;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Antenna Port");
                Console.WriteLine("2. Get Antenna Port Info");
                Console.WriteLine("3. Set Enable All Antenna Port");
                Console.WriteLine("4. Go back\n");
                Console.Write("[1-4] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Console.Write("\nAntenna      : ");

                            antenna = Convert.ToInt32(Console.ReadLine());
                            if (antenna <= 0 || antenna > featureSet.AntennaCount)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + featureSet.AntennaCount);
                                continue;
                            }

                            Settings settings = Settings.Load("settings.xml");

                            ushort antID = (ushort)antenna;

                            string query = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + antenna + "";

                            cmd = new MySqlCommand(query, db.Con);

                            db.Con.Open();
                            var res = cmd.ExecuteScalar();
                            if (res != null)
                            {
                                AntennaID = Convert.ToInt32(res);
                            }
                            db.Con.Close();

                            Console.WriteLine("\n[0] OFF");
                            Console.WriteLine("[1] ON");
                            Console.Write("Option       : ");
                            int status = Convert.ToInt32(Console.ReadLine());

                            if (status == 0)
                            {
                                try
                                {
                                    if (settings.Antennas.GetAntenna(antID).IsEnabled == true)
                                    {
                                        settings.Antennas.GetAntenna(antID).IsEnabled = false;

                                        impinjReader.ApplySettings(settings);
                                        impinjReader.SaveSettings();
                                        settings.Save("settings.xml");

                                        string selQuery = @"SpCheckAntennaInfo";
                                        cmd = new MySqlCommand(selQuery, db.Con);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                                        cmd.Parameters.AddWithValue("@antStatus", settings.Antennas.GetAntenna(antID).IsEnabled.ToString());

                                        db.Con.Open();

                                        cmd.ExecuteScalar();

                                        db.Con.Close();

                                        Console.WriteLine("Antenna Port :  {0} ", antenna);
                                        Console.WriteLine("Status       : OFF");
                                        Console.WriteLine("Set Antenna Successfully\n");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Antenna {antenna} is already OFF");
                                    }
                                }
                                catch (OctaneSdkException)
                                {
                                    Console.WriteLine("Impinj Reader Setting Error");
                                }
                            }
                            else if (status == 1)
                            {
                                if (settings.Antennas.GetAntenna(antID).IsEnabled == false)
                                {
                                    settings.Antennas.GetAntenna(antID).IsEnabled = true;

                                    impinjReader.ApplySettings(settings);
                                    impinjReader.SaveSettings();
                                    settings.Save("settings.xml");

                                    string selQuery = @"SpCheckAntennaInfo";
                                    cmd = new MySqlCommand(selQuery, db.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@aID", AntennaID);
                                    cmd.Parameters.AddWithValue("@antStatus", settings.Antennas.GetAntenna(antID).IsEnabled.ToString());

                                    db.Con.Open();

                                    cmd.ExecuteScalar();

                                    db.Con.Close();

                                    Console.WriteLine("Antenna Port :  {0} ", antenna);
                                    Console.WriteLine("Status      : ON");
                                    Console.WriteLine("Set Antenna Successfully\n");
                                }
                                else
                                {
                                    MySqlDatabase db = new();

                                    string selQuery = "SELECT * FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND b.AntennaID = " + AntennaID + "";
                                    cmd = new MySqlCommand(selQuery, db.Con);
                                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                                    {
                                        if (dataReader.HasRows)
                                        {
                                            dataReader.Close();
                                            if (db.Con.State != ConnectionState.Open)
                                            {
                                                db.Con.Open();
                                            }
                                            db.Con.Close();
                                            Console.WriteLine($"Antenna {antenna} is already ON");
                                        }
                                        else
                                        {
                                            string insertQuery = "INSERT INTO antenna_info_tbl (AntennaID, AntennaStatus) VALUES (" + AntennaID + ", 'True')";
                                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, db.Con))
                                            {
                                                dataReader.Close();
                                                if (db.Con.State != ConnectionState.Open)
                                                {
                                                    db.Con.Open();
                                                }
                                                insertCmd.ExecuteNonQuery();
                                                db.Con.Close();
                                            }
                                            Console.WriteLine($"Antenna {antenna} is already ON");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Enter a valid integer in the range 0-1");
                                break;
                            }
                            break;
                        case 2:
                            DisplayAntennaStatus();
                            break;
                        case 3:
                            SetEnableAllAntennaInfo();
                            break;
                        case 4:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid integer in the range 1-4");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void DisplayAntennaStatus()
        {
            MySqlDatabase db = new();

            string selQuery = "SELECT a.Antenna, b.AntennaStatus FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " ORDER BY a.Antenna ASC";
            cmd = new MySqlCommand(selQuery, db.Con);
            MySqlDataReader dataReader = cmd.ExecuteReader();

            if (dataReader.HasRows)
            {
                while (dataReader.Read())
                {
                    int antenna = dataReader.GetInt32("Antenna");
                    string status = dataReader.GetString("AntennaStatus");

                    Console.WriteLine($"Antenna                     : {antenna}");
                    if (status.Equals("False")) Console.WriteLine("Antenna Status:             : OFF\n");
                    else Console.WriteLine("Antenna Status:             : ON\n");
                }
                db.Con.Close();
            }
        }
        public void SetEnableAllAntennaInfo()
        {
            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            Settings settings = Settings.Load("settings.xml");

            settings.Antennas.EnableAll();

            impinjReader.ApplySettings(settings);
            impinjReader.SaveSettings();
            settings.Save("settings.xml");

            try
            {
                MySqlDatabase db1 = new();

                for (int c = 0; c < featureSet.AntennaCount; c++)
                {
                    string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (c + 1) + "";
                    cmd = new MySqlCommand(selQuery1, db1.Con);

                    if (db1.Con.State != ConnectionState.Open)
                    {
                        db1.Con.Open();
                    }

                    MySqlDataReader dataReader1 = cmd.ExecuteReader();

                    if (dataReader1.HasRows)
                    {
                        dataReader1.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        MySqlDatabase db2 = new();
                        string selQuery2 = "SELECT * FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND b.AntennaID = " + AntennaID + " AND b.AntennaStatus= 'False'";
                        cmd = new MySqlCommand(selQuery2, db2.Con);

                        if (db2.Con.State != ConnectionState.Open)
                        {
                            db2.Con.Open();
                        }

                        MySqlDataReader dataReader2 = cmd.ExecuteReader();

                        if (dataReader2.HasRows)
                        {
                            dataReader2.Close();
                            var res2 = cmd.ExecuteScalar();
                            if (res2 != null)
                            {
                                AntennaInfoID = Convert.ToInt32(res2);
                            }

                            MySqlDatabase db3 = new();
                            string updQuery = "UPDATE antenna_info_tbl SET AntennaStatus = 'True' WHERE AntennaInfoID = " + AntennaInfoID + "";
                            cmd = new MySqlCommand(updQuery, db3.Con);

                            cmd.Parameters.Clear();

                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            dataReader2.Close();
                        }
                        db1.Con.Close();
                    }
                }
                Console.WriteLine("\nSuccessfully updated and enabled all antennas.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void GPIOConfig()
        {
            bool isWorking = true;
            int option;

            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            while (isWorking)
            {
                Console.WriteLine("\n----GPIO Menu----");
                Console.WriteLine("1. GPI Config");
                Console.WriteLine("2. GPO Config");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            ConfigureGPI();
                            break;
                        case 2:
                            ConfigureGPO();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        public void ConfigureGPI()
        {
            bool isWorking = true;
            int option, gpiPort, gpiLevel, debounce;

            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            while (isWorking)
            {
                Console.WriteLine("\n----GPI Menu----");
                Console.WriteLine("1. Set GPI State");
                Console.WriteLine("2. Get GPI State");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            Console.Write("\nGPI Port : ");

                            try
                            {
                                gpiPort = Convert.ToInt32(Console.ReadLine());
                                if (gpiPort <= 0 || gpiPort > featureSet.GpiCount)
                                {
                                    Console.WriteLine("Enter a valid Port in the range 1-" + featureSet.GpiCount);
                                    continue;
                                }
                            }
                            catch (IOException)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-" + featureSet.GpiCount);
                                continue;
                            }

                            Settings settings = Settings.Load("settings.xml");

                            Console.WriteLine("\nGPI Level");
                            Console.WriteLine("1. High");
                            Console.WriteLine("2. Low\n");
                            Console.Write("Option   : ");

                            gpiLevel = Convert.ToInt32(Console.ReadLine());

                            if (gpiLevel == 1 || gpiLevel == 2)
                            {
                                if (gpiLevel == 1)
                                {
                                    settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).IsEnabled = true;
                                }
                                else if (gpiLevel == 2)
                                {
                                    settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).IsEnabled = false;
                                }

                                Console.Write("Debounce : ");
                                debounce = Convert.ToInt32(Console.ReadLine());

                                settings.Gpis.GetGpi(Convert.ToUInt16(gpiPort)).DebounceInMs = Convert.ToUInt16(debounce);

                                if (settings.Gpis.GetGpi(Convert.ToUInt16(gpiPort)).IsEnabled == true)
                                {
                                    Console.WriteLine("\nGPI Port : {0} \nLevel    : High", gpiPort);
                                    Console.Write("Debounce : {0}", settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).DebounceInMs);
                                }
                                else
                                {
                                    Console.WriteLine("\nGPI Port : {0} \nLevel    : Low", gpiPort);
                                    Console.Write("Debounce : {0}", settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).DebounceInMs);
                                }

                                impinjReader.ApplySettings(settings);
                                impinjReader.SaveSettings();
                                settings.Save("settings.xml");

                                Console.WriteLine("\nSet GPI Successfully");

                                try
                                {
                                    MySqlDatabase db1 = new();
                                    string selQuery = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + " AND GPIPort =" + gpiPort + "";
                                    cmd = new MySqlCommand(selQuery, db1.Con);

                                    if (db1.Con.State != ConnectionState.Open)
                                    {
                                        db1.Con.Open();
                                    }

                                    MySqlDataReader dataReader2 = cmd.ExecuteReader();

                                    if (dataReader2.HasRows)
                                    {
                                        dataReader2.Close();
                                        var res2 = cmd.ExecuteScalar();
                                        if (res2 != null)
                                        {
                                            GPIID = Convert.ToInt32(res2);
                                        }

                                        MySqlDatabase db2 = new();
                                        string updQuery = "UPDATE gpi_tbl SET GPIStatus = @gpiMode, Debounce = @debounce WHERE GPIID = " + GPIID + " AND ReaderID = " + ReaderID + " AND GPIPort = " + gpiPort + "";
                                        cmd = new MySqlCommand(updQuery, db2.Con);
                                        cmd.Parameters.AddWithValue("@gpiMode", settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).IsEnabled.ToString());
                                        cmd.Parameters.AddWithValue("@debounce", settings.Gpis.GetGpi(Convert.ToUInt16(gpiLevel)).DebounceInMs);
                                        cmd.ExecuteNonQuery();
                                    }
                                    else
                                    {
                                        dataReader2.Close();
                                    }
                                    db1.Con.Close();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid Input Format");
                            }
                            break;
                        case 2:
                            DisplayGPI();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (OctaneSdkException)
                {
                    Console.WriteLine("\nImpinj Reader Setting Error");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        public void DisplayGPI()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + " ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int gpiPort = dataReader.GetInt32("GPIPort");
                        string gpiLevel = dataReader.GetString("GPIStatus");
                        int gpiDebounce = dataReader.GetInt32("Debounce");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiLevel.Equals("True")) Console.WriteLine("GPI Level                   : High");
                        else Console.WriteLine("GPI Level                   : Low");
                        Console.WriteLine("Debounce                    : {0} \n", gpiDebounce);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        public void ConfigureGPO()
        {
            bool isWorking = true;
            int option, gpoPort;

            FeatureSet featureSet = impinjReader.QueryFeatureSet();

            while (isWorking)
            {
                Console.WriteLine("\n----GPO Menu----");
                Console.WriteLine("1. Set GPO State");
                Console.WriteLine("2. Get GPO State");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            Console.Write("\nGPO Port : ");

                            try
                            {
                                gpoPort = Convert.ToInt32(Console.ReadLine());
                                if (gpoPort <= 0 || gpoPort > featureSet.GpoCount)
                                {
                                    Console.WriteLine("Enter a valid Port in the range 1-" + featureSet.GpoCount);
                                    continue;
                                }
                            }
                            catch (IOException)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-" + featureSet.GpoCount);
                                continue;
                            }

                            try
                            {
                                Console.WriteLine("\nGPO Mode");
                                Console.WriteLine("1. Normal");
                                Console.WriteLine("2. Pulsed");
                                Console.WriteLine("3. ReaderOperationalStatus");
                                Console.WriteLine("4. LLRPConnectionStatus");
                                Console.WriteLine("5. ReaderInventoryStatus");
                                Console.WriteLine("6. NetworkConnectionStatus");
                                Console.WriteLine("7. ReaderInventoryTagsStatus\n");
                                Console.Write("Option   : ");
                                int status = Convert.ToInt32(Console.ReadLine());

                                Settings settings = Settings.Load("settings.xml");
                                if (status == 1)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.Normal;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.Normal);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 2)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.Pulsed;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.Pulsed);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 3)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.ReaderOperationalStatus;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.ReaderOperationalStatus);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 4)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.LLRPConnectionStatus;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.LLRPConnectionStatus);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 5)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.ReaderInventoryStatus;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.ReaderInventoryStatus);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 6)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.NetworkConnectionStatus;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.NetworkConnectionStatus);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else if (status == 7)
                                {
                                    settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode = GpoMode.ReaderInventoryTagsStatus;
                                    Console.WriteLine("\nGPO Port : {0} \nMode     : {1}\n", gpoPort, GpoMode.ReaderInventoryTagsStatus);
                                    Console.WriteLine("Set GPO Successfully");
                                }
                                else
                                {
                                    Console.WriteLine("Enter a valid integer in the range 1-7");
                                    break;
                                }

                                impinjReader.ApplySettings(settings);
                                impinjReader.SaveSettings();
                                settings.Save("settings.xml");

                                MySqlDatabase db1 = new();
                                string selQuery = "SELECT * FROM gpo_tbl WHERE ReaderID = " + ReaderID + " AND GPOPort =" + gpoPort + "";
                                cmd = new MySqlCommand(selQuery, db1.Con);

                                if (db1.Con.State != ConnectionState.Open)
                                {
                                    db1.Con.Open();
                                }

                                MySqlDataReader dataReader2 = cmd.ExecuteReader();

                                if (dataReader2.HasRows)
                                {
                                    dataReader2.Close();
                                    var res2 = cmd.ExecuteScalar();
                                    if (res2 != null)
                                    {
                                        GPOID = Convert.ToInt32(res2);
                                    }

                                    MySqlDatabase db2 = new();
                                    string updQuery = "UPDATE gpo_tbl SET GPOMode = @gpoMode WHERE GPOID = " + GPOID + " AND ReaderID = " + ReaderID + " AND GPOPort = " + gpoPort + "";
                                    cmd = new MySqlCommand(updQuery, db2.Con);
                                    cmd.Parameters.AddWithValue("@gpoMode", settings.Gpos.GetGpo(Convert.ToUInt16(gpoPort)).Mode.ToString());
                                    cmd.ExecuteNonQuery();
                                }
                                else
                                {
                                    dataReader2.Close();
                                }
                                db1.Con.Close();
                            }
                            catch (OctaneSdkException ose)
                            {
                                Console.WriteLine(ose.Message);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Invalid Input Format");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case 2:
                            DisplayGPO();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (OctaneSdkException e)
                {
                    Console.WriteLine("Octane SDK exception: {0}", e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        public void DisplayGPO()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM gpo_tbl WHERE ReaderID = " + ReaderID + " ORDER BY GPOPort ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int gpoPort = dataReader.GetInt32("GPOPort");
                        string gpoMode = dataReader.GetString("GPOMode");

                        Console.WriteLine("GPO Port                    : {0} ", gpoPort);
                        Console.WriteLine("GPO Mode                    : {0} \n", gpoMode);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        public void Read()
        {
            try
            {
                uniqueTags.Clear();
                totalTags = 0;
                Console.WriteLine("Inventory Started");

                impinjReader.TagsReported += OnTagsReported;
                impinjReader.Start();

                Console.WriteLine("Press Enter to stop Inventory");
                Console.ReadLine();

                impinjReader.Stop();
                Console.WriteLine("Total Tags: " + uniqueTags.Count + "(" + totalTags + ")");
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                string epc = tag.Epc.ToString();

                totalTags += tag.TagSeenCount;

                if (!uniqueTags.ContainsKey(epc))
                {
                    Console.WriteLine("Antenna : {0}, EPC : {1}",
                                        tag.AntennaPortNumber, tag.Epc);
                    uniqueTags.Add(epc, tag);
                }
            }
        }
        static bool ReaderIsAvailable(string address)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.Default.GetBytes("12345");
            PingReply reply = pingSender.Send(address, 500, buffer, options);
            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }
        static void OnKeepaliveReceived(ImpinjReader reader)
        {
        }
        static void OnConnectionLost(ImpinjReader reader)
        {
            Console.WriteLine("Connection lost : {0} ({1})", reader.Name, reader.Address);

            while (!ReaderIsAvailable(reader.Address))
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }

            if (ReaderIsAvailable(reader.Address) == true)
            {
                impinjReader.Connect(reader.Address);
                impinjReader.ResumeEventsAndReports();
            }
        }
        public void Default()
        {
            impinjReader.ApplyDefaultSettings();
            Settings settings = impinjReader.QuerySettings();
            if (settings != null)
            {
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludeSeenCount = true;
                settings.TagPopulationEstimate = 32;
                settings.HoldReportsOnDisconnect = true;

                settings.Keepalives.Enabled = true;
                settings.Keepalives.PeriodInMs = 3000;
                settings.Keepalives.EnableLinkMonitorMode = true;
                settings.Keepalives.LinkDownThreshold = 5;

                impinjReader.ApplySettings(settings);
                impinjReader.SaveSettings();
                settings.Save("settings.xml");

                try
                {
                    FeatureSet featureSet = impinjReader.QueryFeatureSet();

                    MySqlDatabase db1 = new();

                    string selQuery1 = "SELECT * FROM reader_settings_tbl WHERE ReaderID = " + ReaderID + "";
                    cmd = new MySqlCommand(selQuery1, db1.Con);
                    MySqlDataReader dataReader1 = cmd.ExecuteReader();

                    if (dataReader1.HasRows)
                    {
                        dataReader1.Close();
                        if (db1.Con.State != ConnectionState.Open)
                        {
                            db1.Con.Open();
                        }
                        db1.Con.Close();
                    }
                    else
                    {
                        dataReader1.Close();
                        string insQuery1 = @"SpDefaultReaderSettings";
                        cmd = new MySqlCommand(insQuery1, db1.Con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@readerM", settings.ReaderMode.ToString());
                        cmd.Parameters.AddWithValue("@searchM", settings.SearchMode.ToString());
                        cmd.Parameters.AddWithValue("@sess", settings.Session.ToString());
                        cmd.Parameters.AddWithValue("@tp", settings.TagPopulationEstimate);

                        cmd.ExecuteScalar();
                        db1.Con.Close();
                    }

                    MySqlDatabase db2 = new();

                    string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + "";
                    cmd = new MySqlCommand(selQuery2, db2.Con);
                    MySqlDataReader dataReader2 = cmd.ExecuteReader();

                    if (dataReader2.HasRows)
                    {
                        dataReader2.Close();
                        if (db2.Con.State != ConnectionState.Open)
                        {
                            db2.Con.Open();
                        }
                        db2.Con.Close();
                    }
                    else
                    {
                        dataReader2.Close();
                        string insQuery2 = "INSERT INTO antenna_tbl (ReaderID, Antenna, ReceiveSensitivity, TransmitPower) VALUES (@rID, @ant, @sensitivity, @power)";
                        cmd = new MySqlCommand(insQuery2, db2.Con);

                        for (int b = 0; b < featureSet.AntennaCount; b++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@rID", ReaderID);
                            cmd.Parameters.AddWithValue("@ant", (b + 1));
                            cmd.Parameters.AddWithValue("@sensitivity", settings.Antennas.GetAntenna((ushort)(b + 1)).RxSensitivityInDbm);
                            cmd.Parameters.AddWithValue("@power", settings.Antennas.GetAntenna((ushort)(b + 1)).TxPowerInDbm);

                            if (db2.Con.State != ConnectionState.Open)
                            {
                                db2.Con.Open();
                            }
                            cmd.ExecuteNonQuery();
                            db2.Con.Close();
                        }
                    }

                    MySqlDatabase db3 = new();

                    for (int c = 0; c < featureSet.AntennaCount; c++)
                    {
                        string selQuery3 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (c + 1) + "";
                        cmd = new MySqlCommand(selQuery3, db3.Con);

                        if (db3.Con.State != ConnectionState.Open)
                        {
                            db3.Con.Open();
                        }

                        MySqlDataReader dataReader3 = cmd.ExecuteReader();

                        if (dataReader3.HasRows)
                        {
                            dataReader3.Close();
                            var res = cmd.ExecuteScalar();
                            if (res != null)
                            {
                                AntennaID = Convert.ToInt32(res);
                            }

                            string insQuery3 = "INSERT INTO antenna_info_tbl (AntennaID, AntennaStatus) VALUES (@aID, @antStatus)";
                            cmd = new MySqlCommand(insQuery3, db3.Con);

                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@aID", AntennaID);
                            cmd.Parameters.AddWithValue("@antStatus", settings.Antennas.GetAntenna(Convert.ToUInt16(c + 1)).IsEnabled.ToString());

                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            dataReader3.Close();
                        }

                        db3.Con.Close();
                    }

                    MySqlDatabase db4 = new();

                    string selQuery4 = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + "";
                    cmd = new MySqlCommand(selQuery4, db4.Con);
                    MySqlDataReader dataReader4 = cmd.ExecuteReader();

                    if (dataReader4.HasRows)
                    {
                        dataReader4.Close();
                        if (db4.Con.State != ConnectionState.Open)
                        {
                            db4.Con.Open();
                        }
                        db4.Con.Close();
                    }
                    else
                    {
                        dataReader4.Close();
                        string insQuery3 = "INSERT INTO gpi_tbl (ReaderID, GPIPort, GPIStatus, Debounce) VALUES (@rID, @gpiPortNo, @gpiStats, @ms)";
                        cmd = new MySqlCommand(insQuery3, db4.Con);

                        for (int d = 0; d < featureSet.GpiCount; d++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@rID", ReaderID);
                            cmd.Parameters.AddWithValue("@gpiPortNo", (d + 1));
                            cmd.Parameters.AddWithValue("@gpiStats", settings.Gpis.GetGpi(Convert.ToUInt16(d + 1)).IsEnabled.ToString());
                            cmd.Parameters.AddWithValue("@ms", settings.Gpis.GetGpi(Convert.ToUInt16(d + 1)).DebounceInMs);

                            if (db4.Con.State != ConnectionState.Open)
                            {
                                db4.Con.Open();
                            }
                            cmd.ExecuteNonQuery();
                            db4.Con.Close();
                        }
                    }

                    MySqlDatabase db5 = new();

                    string selQuery5 = "SELECT * FROM gpo_tbl WHERE ReaderID = " + ReaderID + "";
                    cmd = new MySqlCommand(selQuery5, db5.Con);
                    MySqlDataReader dataReader5 = cmd.ExecuteReader();

                    if (dataReader5.HasRows)
                    {
                        dataReader5.Close();
                        if (db5.Con.State != ConnectionState.Open)
                        {
                            db5.Con.Open();
                        }
                        db5.Con.Close();
                    }
                    else
                    {
                        dataReader5.Close();
                        string insQuery5 = "INSERT INTO gpo_tbl (ReaderID, GPOPort, GPOMode) VALUES (@rID, @gpoPortNo, @gpoStats)";
                        cmd = new MySqlCommand(insQuery5, db5.Con);

                        for (int f = 0; f < featureSet.GpoCount; f++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@rID", ReaderID);
                            cmd.Parameters.AddWithValue("@gpoPortNo", (f + 1));
                            cmd.Parameters.AddWithValue("@gpoStats", settings.Gpos.GetGpo(Convert.ToUInt16(f + 1)).Mode.ToString());

                            if (db5.Con.State != ConnectionState.Open)
                            {
                                db5.Con.Open();
                            }
                            cmd.ExecuteNonQuery();
                            db5.Con.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
