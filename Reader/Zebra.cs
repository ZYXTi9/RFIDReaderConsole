using MySql.Data.MySqlClient;
using RfidReader.Database;
using Symbol.RFID3;
using System.Collections;
using System.ComponentModel;
using System.Data;
using static Symbol.RFID3.GPOs;

namespace RfidReader.Reader
{
    class Zebra
    {
        RFIDReader rfidReader;

        MySqlDatabase db = new MySqlDatabase();
        MySqlCommand? cmd;

        private Program p;
        public int ReaderTypeID { get; set; }
        public int ReaderID { get; set; }
        //public string? HostName { get; set; }
        //public int Port { get; set; }
        string HostName = "192.168.1.171";
        int Port = 5084;
        public int AntennaID { get; set; }
        public int AntennaInfoID { get; set; }
        public int GPIID { get; set; }
        public int GPOID { get; set; }

        private string zebraStatus = "Connected";

        readonly TagStorageSettings tagStorageSettings;
        private Symbol.RFID3.AntennaInfo antennaInfo;

        private ArrayList statusList = new ArrayList();

        private BackgroundWorker bgWorker;

        private delegate void updateRead(Events.ReadEventData eventData);
        private updateRead updateReadHandler;

        public static Hashtable uniqueTags = new();
        public static int totalTags = 0;
        public Zebra()
        {
            rfidReader = new RFIDReader();
            p = new();
            tagStorageSettings = new TagStorageSettings();
            antennaInfo = new AntennaInfo();
            //statusList = new ArrayList();
            bgWorker = new BackgroundWorker();
            updateReadHandler = new updateRead(MyUpdateRead);
            //uniqueTags = new Hashtable();
            //totalTags = 0;
        }

        public void ConnectToReader()
        {
            try
            {
                //Console.Write("Hostname or IP Name : ");
                //HostName = Console.ReadLine();

                //Console.Write("Port                : ");
                //Port = Convert.ToInt32(Console.ReadLine());

                rfidReader = new RFIDReader(HostName, (uint)Port, 0);

                Console.WriteLine("Attempting to connect to {0}", HostName);
                rfidReader.Connect();

                if (rfidReader.IsConnected == true)
                {
                    Console.WriteLine("Successfully connected.");

                    string selQuery = @"SpCheckZebraReader";
                    cmd = new MySqlCommand(selQuery, db.Con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@rtypeID", ReaderTypeID);
                    cmd.Parameters.AddWithValue("@ip", HostName);
                    cmd.Parameters.AddWithValue("@device", "Zebra" + rfidReader.ReaderCapabilities.ModelName);
                    cmd.Parameters.AddWithValue("@rport", Port);
                    cmd.Parameters.AddWithValue("@tout", 0);
                    cmd.Parameters.AddWithValue("@readerStatus", zebraStatus);
                    var getReaderID = cmd.ExecuteScalar();

                    if (getReaderID != null)
                    {
                        ReaderID = Convert.ToInt32(getReaderID);
                    }

                    db.Con.Close();

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
                        LoadDB();
                    }
                    else
                    {
                        Default();
                    }
                    Menu();
                }
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine(ioe.Message);
            }
            catch (InvalidUsageException)
            {
                Console.WriteLine("Please input the correct format of Hostname/IP Name or/and Port");
            }
            catch (OperationFailureException ofe)
            {
                Console.WriteLine(ofe.StatusDescription);
            }
            catch (Exception)
            {
                Console.WriteLine("Hostname/IP name and Port is incorrect, Please input the correct format");
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
                Console.WriteLine("Welcome to Zebra RFID Settings");
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
                            p.Main();
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-5");
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
        public void ReaderInfo()
        {
            Console.WriteLine("\nReader Capabilities");
            Console.WriteLine("---------------");
            Console.WriteLine("Firware Version                              : {0}", rfidReader.ReaderCapabilities.FirwareVersion);
            Console.WriteLine("Model Name                                   : {0}", rfidReader.ReaderCapabilities.ModelName);
            Console.WriteLine("Num Antenna Supported                        : {0}", rfidReader.ReaderCapabilities.NumAntennaSupported);
            Console.WriteLine("Num GPI Ports                                : {0}", rfidReader.ReaderCapabilities.NumGPIPorts);
            Console.WriteLine("Num GPO Ports                                : {0}", rfidReader.ReaderCapabilities.NumGPOPorts);
            Console.WriteLine("Is UTCC lock Supported                       : {0}", rfidReader.ReaderCapabilities.IsUTCClockSupported);
            Console.WriteLine("Is Block Erase Supported                     : {0}", rfidReader.ReaderCapabilities.IsBlockEraseSupported);
            Console.WriteLine("Is Block Write Supported                     : {0}", rfidReader.ReaderCapabilities.IsBlockWriteSupported);
            Console.WriteLine("IsTagInventoryStateAwareSingulationSupported : {0}", rfidReader.ReaderCapabilities.IsTagInventoryStateAwareSingulationSupported);
            Console.WriteLine("Max Num Operations In Access Sequence        : {0}", rfidReader.ReaderCapabilities.MaxNumOperationsInAccessSequence);
            Console.WriteLine("Max Num Pre Filters                          : {0}", rfidReader.ReaderCapabilities.MaxNumPreFilters);
            Console.WriteLine("Communication Standard                       : {0}", rfidReader.ReaderCapabilities.CommunicationStandard);
            Console.WriteLine("Country Code                                 : {0}", rfidReader.ReaderCapabilities.CountryCode);
            Console.WriteLine("Is Hopping Enabled                           : {0} \n", rfidReader.ReaderCapabilities.IsHoppingEnabled);

            Console.WriteLine("Reader Status");
            Console.WriteLine("---------------");
            Console.WriteLine("Is connected                                 : {0} \n", rfidReader.IsConnected);

            try
            {
                Console.WriteLine("Current Reader Settings");
                Console.WriteLine("---------------");

                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND a.Antenna = 1";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        string session = dataReader1.GetString("Session");
                        int tagPopulation = dataReader1.GetInt32("TagPopulation");

                        Console.WriteLine("Session                                      : {0} ", session);
                        Console.WriteLine("Tag Population                               : {0} \n", tagPopulation);
                    }
                    db1.Con.Close();
                }

                Console.WriteLine("Current Power Settings");
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
                        int txPower = dataReader2.GetInt32("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
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
                        string gpiStatus = dataReader3.GetString("GPIStatus");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiStatus.Equals("true")) Console.WriteLine("GPI Status                  : Enabled\n");
                        else Console.WriteLine("GPI Status                  : Disabled\n");
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
            int option;

            while (isWorking)
            {
                Console.WriteLine("\n----Reader Settings----");
                Console.WriteLine("1. Reader Mode");
                Console.WriteLine("2. Tag Settings");
                Console.WriteLine("3. Singulation Control");
                Console.WriteLine("4. Power Radio");
                Console.WriteLine("5. Go back");
                Console.Write("\n[1-5] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ConfigureRFModes();
                            break;
                        case 2:
                            TagSettings();
                            break;
                        case 3:
                            SingulationControl();
                            break;
                        case 4:
                            PowerRadio();
                            break;
                        case 5:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-5");
                            break;
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-5");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        public void ConfigureRFModes()
        {
            int antenna, option;
            bool isWorking = true;

            Antennas.RFMode rfMode;
            ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;

            int[] mode = { 0,1,2,3,4,5,6,7,8,9,10,
                                            11,12,13,14,15,16,17,18,19,20,
                                            21,22,23,24,25,26,27,28,29,30,
                                            31,32,33,34,35,36,37,38,39,40
                                          };

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Show RF Mode Table Value");
                Console.WriteLine("2. Set RF Mode");
                Console.WriteLine("3. Get RF Mode");
                Console.WriteLine("4. Go back");
                Console.Write("\n[1-4] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            for (int k = 0; k < mode.Length; k++)
                            {
                                RFModeTableEntry rfTableEntry = rfidReader.ReaderCapabilities.RFModes[0][mode[k]];
                                Console.WriteLine("\nMode Identifier : " + rfTableEntry.ModeIdentifier.ToString());
                                Console.WriteLine("DR              : " + rfTableEntry.DivideRatio.ToString());
                                Console.WriteLine("Bdr             : " + rfTableEntry.BdrValue.ToString());
                                Console.WriteLine("M               : " + rfTableEntry.Modulation.ToString());
                                Console.WriteLine("Forward Link    : " + rfTableEntry.ForwardLinkModulationType.ToString());
                                Console.WriteLine("PIE             : " + rfTableEntry.PieValue.ToString());
                                Console.WriteLine("Min. Tari       : " + rfTableEntry.MinTariValue.ToString());
                                Console.WriteLine("Max Tari        : " + rfTableEntry.MaxTariValue.ToString());
                                Console.WriteLine("Step Tari       : " + rfTableEntry.StepTariValue.ToString());
                                Console.WriteLine("Mask Indicator  : " + rfTableEntry.SpectralMaskIndicator.ToString());
                                Console.WriteLine("EPC Hag         : " + rfTableEntry.EPCHAGTCConformance.ToString());
                            }
                            break;
                        case 2:
                            //Console.WriteLine();
                            //Console.Write("Antenna       : ");

                            //try
                            //{
                            //    antenna = Convert.ToInt32(Console.ReadLine());
                            //    if (antenna <= 0 || antenna > rfidReader.ReaderCapabilities.NumAntennaSupported)
                            //    {
                            //        Console.WriteLine("Enter a valid Antenna in the range 1-" + rfidReader.ReaderCapabilities.NumAntennaSupported);
                            //        continue;
                            //    }
                            //}
                            //catch (IOException)
                            //{
                            //    Console.WriteLine("Enter a valid Antenna in the range 1-" + rfidReader.ReaderCapabilities.NumAntennaSupported);
                            //    continue;
                            //}

                            try
                            {
                                uint tableIndex = 0;
                                uint tariValue = 0;

                                Console.Write("\nMode Identifier : ");
                                tableIndex = (uint)Convert.ToInt32(Console.ReadLine());

                                if (mode.Contains((int)tableIndex))
                                {
                                    Console.Write("Tari value      : ");
                                    tariValue = (uint)Convert.ToInt32(Console.ReadLine());
                                }
                                else
                                {
                                    Console.WriteLine("Input is invalid");
                                    continue;
                                }


                                MySqlDatabase db1 = new();

                                for (antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                                {
                                    string query = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (antenna + 1) + "";

                                    cmd = new MySqlCommand(query, db1.Con);

                                    if (db1.Con.State != ConnectionState.Open)
                                    {
                                        db1.Con.Open();
                                    }
                                    var res = cmd.ExecuteScalar();
                                    if (res != null)
                                    {
                                        AntennaID = Convert.ToInt32(res);
                                    }
                                    db1.Con.Close();

                                    rfMode = rfidReader.Config.Antennas[antID[antenna]].GetRFMode();
                                    rfMode.TableIndex = tableIndex;
                                    rfMode.Tari = tariValue;

                                    rfidReader.Config.Antennas[antID[antenna]].SetRFMode(rfMode);

                                    MySqlDatabase db2 = new();
                                    string selQuery = @"SpCheckZebraRFMode";
                                    cmd = new MySqlCommand(selQuery, db2.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@aID", AntennaID);
                                    cmd.Parameters.AddWithValue("@rfIndex", rfMode.TableIndex);
                                    cmd.Parameters.AddWithValue("@tariIndex", rfMode.Tari);

                                    if (db2.Con.State != ConnectionState.Open)
                                    {
                                        db2.Con.Open();
                                    }
                                    cmd.ExecuteScalar();
                                    db2.Con.Close();

                                }
                                Console.WriteLine("Set RF Mode Successfully");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Zebra Reader Setting Error");
                            }
                            break;
                        case 3:
                            //for (int index = 0; index < rfidReader.ReaderCapabilities.NumAntennaSupported; index++)
                            //{
                            //    Console.WriteLine($"Antenna            : {index + 1} ");
                            //    rfMode = rfidReader.Config.Antennas[antID[index]].GetRFMode();
                            //    Console.WriteLine("RF ModeTable index : " + rfMode.TableIndex);
                            //    Console.WriteLine("Tari value         : " + rfMode.Tari);
                            //    Console.WriteLine();
                            //}
                            DisplayRFMode();
                            break;
                        case 4:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-4");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (OperationFailureException opEx)
                {
                    Console.WriteLine("Operation failed.Reason: " + opEx.StatusDescription + " Vendor message: " + opEx.VendorMessage);
                }
                catch (InvalidUsageException ex)
                {
                    Console.WriteLine(ex.Info);
                }
                catch (IOException ioe)
                {
                    Console.WriteLine("IO Exception" + ioe.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void DisplayRFMode()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM antenna_tbl a INNER JOIN rf_modes_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND a.Antenna = 1";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    int tari = dataReader.GetInt32("Tari");
                    int rfModeTable = dataReader.GetInt32("RFTable");

                    Console.WriteLine("Tari Value                  : {0} ", tari);
                    Console.WriteLine("RF Mode Table Index         : {0} \n", rfModeTable);
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void TagSettings()
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine();
                Console.WriteLine("----Tag Storage Settings---");
                Console.WriteLine("1. Set Tag Storage");
                Console.WriteLine("2. Get Tag Storage");
                Console.WriteLine("3. Go back");
                Console.WriteLine();
                Console.Write("[1-3] : ");
                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            try
                            {
                                Console.Write("\nMax Tag Count                : ");
                                int MaxTagCount = Convert.ToInt16(Console.ReadLine());

                                if (MaxTagCount <= 0)
                                {
                                    Console.WriteLine("Max Tag count should be > 0");
                                    continue;
                                }
                                tagStorageSettings.MaxTagCount = Convert.ToUInt32(MaxTagCount);

                                Console.Write("Max Tag ID Length            : ");
                                int tagLength = Convert.ToInt16(Console.ReadLine());

                                if (tagLength <= 0)
                                {
                                    Console.WriteLine("Max Size of the EPC bank should be > 0");
                                    continue;
                                }
                                tagStorageSettings.MaxTagIDLength = Convert.ToUInt32(tagLength);

                                Console.Write("Max Size of Memory Bank      : ");
                                int memoryBank = Convert.ToInt16(Console.ReadLine());

                                if (memoryBank <= 0)
                                {
                                    Console.WriteLine("Max Size of the memory bank should be > 0");
                                    continue;
                                }
                                tagStorageSettings.MaxSizeMemoryBank = Convert.ToUInt32(memoryBank);

                                Console.Write("Apply Phase Info [Y/N]       : ");
                                ConsoleKeyInfo choice = Console.ReadKey();
                                if (choice.Key == ConsoleKey.Y)
                                {
                                    tagStorageSettings.TagFields |= TAG_FIELD.PHASE_INFO;
                                }
                                else if (choice.Key == ConsoleKey.N)
                                {
                                    tagStorageSettings.TagFields &= ~TAG_FIELD.PHASE_INFO;
                                }
                                else
                                {
                                    Console.WriteLine("\n\nChoose between Y/N only");
                                }

                                rfidReader.Config.SetTagStorageSettings(tagStorageSettings);

                                MySqlDatabase db = new();
                                string selQuery = @"SpCheckZebraTagStorage";
                                cmd = new MySqlCommand(selQuery, db.Con);
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@rID", ReaderID);
                                cmd.Parameters.AddWithValue("@tagCount", tagStorageSettings.MaxTagCount);
                                cmd.Parameters.AddWithValue("@tagLength", tagStorageSettings.MaxTagIDLength);
                                cmd.Parameters.AddWithValue("@memorySize", tagStorageSettings.MaxSizeMemoryBank);
                                cmd.Parameters.AddWithValue("@tFields", tagStorageSettings.TagFields);

                                if (db.Con.State != ConnectionState.Open)
                                {
                                    db.Con.Open();
                                }
                                cmd.ExecuteScalar();
                                db.Con.Close();

                                Console.WriteLine("\nSet Tag Storage Settings Successfully");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Zebra Reader Setting Error");
                            }
                            break;
                        case 2:
                            //Console.WriteLine("Max Tag Count                : " + tagStorageSettings.MaxTagCount);
                            //Console.WriteLine("Max Tag ID Length            : " + tagStorageSettings.MaxTagIDLength);
                            //Console.WriteLine("Max Size of Memory Bank      : " + tagStorageSettings.MaxSizeMemoryBank);
                            //Console.WriteLine("Tag Fields                   : " + tagStorageSettings.TagFields);
                            //Console.WriteLine();
                            DisplayTagSettings();
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
                catch (InvalidUsageException iue)
                {
                    Console.WriteLine("\n\n" + iue.VendorMessage);
                }
                catch (OperationFailureException ofe)
                {
                    Console.WriteLine(ofe.StatusDescription);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void DisplayTagSettings()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM reader_tbl a INNER JOIN tag_storage_tbl b ON a.ReaderID = b.ReaderID WHERE a.ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int maxCount = dataReader.GetInt32("MaxCount");
                        int tagIDLength = dataReader.GetInt32("TagIDLength");
                        int bankSize = dataReader.GetInt32("BankSize");
                        string tagFields = dataReader.GetString("TagFields");

                        Console.WriteLine("Max Tag Count               : {0} ", maxCount);
                        Console.WriteLine("Max Tag ID Length           : {0} ", tagIDLength);
                        Console.WriteLine("Max Size of Memory Bank     : {0} ", bankSize);
                        Console.WriteLine("Tag Fields                  : {0} \n", tagFields);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                            ConfigurePower();
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
        public void ConfigurePower()
        {
            bool isWorking = true;
            int option, antenna;
            Antennas.Config antennaConfig;
            ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Antenna Power");
                Console.WriteLine("2. Get Antenna Power");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Console.Write("\nAntenna                    : ");
                            antenna = Convert.ToInt32(Console.ReadLine());

                            if (antenna <= 0 || antenna > rfidReader.ReaderCapabilities.NumAntennaSupported)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + rfidReader.ReaderCapabilities.NumAntennaSupported);
                                continue;
                            }

                            antenna -= 1;
                            antennaConfig = rfidReader.Config.Antennas[antID[antenna]].GetConfig();

                            //antennaConfig.ReceiveSensitivityIndex = 0;

                            int[] powerValues = new int[201];
                            for (int i = 0; i <= 200; i++)
                            {
                                powerValues[i] = i;
                            }
                            Console.Write("Transmit Power Index  Value : ");
                            antennaConfig.TransmitPowerIndex = (ushort)Convert.ToInt16(Console.ReadLine());

                            if (powerValues.Contains(antennaConfig.TransmitPowerIndex))
                            {
                                //antennaConfig.TransmitFrequencyIndex = 1;

                                rfidReader.Config.Antennas[antID[antenna]].SetConfig(antennaConfig);

                                string selQuery = @"SpCheckZebraAntenna";
                                cmd = new MySqlCommand(selQuery, db.Con);
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@rID", ReaderID);
                                cmd.Parameters.AddWithValue("@ant", antenna + 1);
                                cmd.Parameters.AddWithValue("@sensitivity", antennaConfig.ReceiveSensitivityIndex);
                                cmd.Parameters.AddWithValue("@power", antennaConfig.TransmitPowerIndex);
                                cmd.Parameters.AddWithValue("@freq", antennaConfig.TransmitFrequencyIndex);
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
                            break;
                        case 2:
                            //for (int index = 0; index < rfidReader.ReaderCapabilities.NumAntennaSupported; index++)
                            //{
                            //    Console.WriteLine($"Antenna                     : {index + 1} ");
                            //    antennaConfig = rfidReader.Config.Antennas[antID[index]].GetConfig();
                            //    Console.WriteLine("ReceiveSensitivityIndex     : " + antennaConfig.ReceiveSensitivityIndex);
                            //    Console.WriteLine("TransmitPowerIndex          : " + antennaConfig.TransmitPowerIndex);
                            //    Console.WriteLine("TransmitFrequencyIndex      : " + antennaConfig.TransmitFrequencyIndex);
                            //    Console.WriteLine();
                            //}
                            DisplayPower();
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
                catch (Exception)
                {
                    Console.WriteLine("Zebra Reader Setting Error");
                }
            }
        }
        public void DisplayPower()
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
                        int rxSensitivity = dataReader.GetInt32("ReceiveSensitivity");
                        int txPower = dataReader.GetInt32("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
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
            bool isWorking = true;
            int option;
            int antenna = 0;
            ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;

            if (statusList.Count == 0)
            {
                foreach (ushort x in antID)
                {
                    antenna += 1;
                    statusList.Add(antenna);
                }
            }

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
                            Console.WriteLine();
                            Console.Write("Antenna                          : ");
                            antenna = Convert.ToInt32(Console.ReadLine());
                            if (antenna <= 0 || antenna > rfidReader.ReaderCapabilities.NumAntennaSupported)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + rfidReader.ReaderCapabilities.NumAntennaSupported);
                                continue;
                            }

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
                                    if (statusList.Contains(antenna))
                                    {
                                        statusList.Remove(Convert.ToInt32(antenna));
                                        if (statusList.Count > 0)
                                        {
                                            ushort[] antList = new ushort[statusList.Count];
                                            for (int index = 0; index < statusList.Count; index++)
                                            {
                                                antList[index] = Convert.ToUInt16(statusList[index].ToString());
                                            }

                                            if (null == antennaInfo)
                                            {
                                                antennaInfo = new Symbol.RFID3.AntennaInfo(antList);
                                            }
                                            else
                                            {
                                                antennaInfo.AntennaID = antList;
                                            }
                                        }

                                        string selQuery = @"SpCheckAntennaInfo";
                                        cmd = new MySqlCommand(selQuery, db.Con);
                                        cmd.CommandType = CommandType.StoredProcedure;

                                        cmd.Parameters.AddWithValue("@aID", AntennaID);

                                        db.Con.Open();

                                        cmd.ExecuteScalar();

                                        db.Con.Close();

                                        Console.WriteLine("Antenna Port :  {0} ", antenna);
                                        Console.WriteLine("Status       : OFF");
                                        Console.WriteLine("Set Antenna Successfully\n");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Antenna Port {antenna} is already OFF");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                            else if (status == 1)
                            {
                                if (statusList.Contains(antenna))
                                {
                                    Console.WriteLine($"Antenna Port {antenna} is already ON");
                                }
                                else
                                {
                                    statusList.Add(antenna);
                                    statusList.Sort();

                                    Console.WriteLine();
                                    Console.WriteLine($"Antenna Port : {antenna}");
                                    Console.WriteLine($"Status      : ON");
                                    Console.WriteLine("Set Antenna Successfully");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Enter a valid integer in the range 0-1");
                                break;
                            }
                            break;
                        case 2:
                            //for (int index = 0; index < statusList.Count; index++)
                            //{
                            //    Console.WriteLine($"Antenna   : {statusList[index]}");
                            //    Console.WriteLine($"Status    : ON ");
                            //    Console.WriteLine();
                            //}
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
                    Console.WriteLine("Enter a valid Integer in the range 1-4");
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
                    if (status.Equals("Disabled")) Console.WriteLine("Antenna Status:             : OFF\n");
                    else Console.WriteLine("Antenna Status:             : ON\n");
                }
                db.Con.Close();
            }
        }
        public void SetEnableAllAntennaInfo()
        {
            try
            {
                MySqlDatabase db1 = new();

                for (int i = 0; i < rfidReader.ReaderCapabilities.NumAntennaSupported; i++)
                {
                    if (statusList.Contains(i + 1))
                    {
                    }
                    else
                    {
                        statusList.Add(i + 1);
                        statusList.Sort();
                    }

                    string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (i + 1) + "";
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
                        string selQuery2 = "SELECT * FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND b.AntennaID = " + AntennaID + " AND b.AntennaStatus= 'Disabled'";
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
                            string updQuery = "UPDATE antenna_info_tbl SET AntennaStatus = 'Enabled' WHERE AntennaInfoID = " + AntennaInfoID + "";
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
        public void ConfigureGPI()
        {
            bool isWorking = true;
            int option, gpiStatus, gpiPort;
            string gpiMode = "";

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

                                if (gpiPort == 0 || gpiPort > rfidReader.ReaderCapabilities.NumGPIPorts)
                                {
                                    Console.WriteLine("Enter a valid Port in the range 1-" + rfidReader.ReaderCapabilities.NumGPIPorts);
                                    continue;
                                }
                            }
                            catch (IOException)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-" + rfidReader.ReaderCapabilities.NumGPIPorts);
                                continue;
                            }


                            Console.WriteLine("\nGPI Status");
                            Console.WriteLine("1. Disable");
                            Console.WriteLine("2. Enable");
                            Console.Write("Option   : ");

                            gpiStatus = Convert.ToInt32(Console.ReadLine());

                            if (gpiStatus == 1 || gpiStatus == 2)
                            {
                                if (gpiStatus == 1)
                                {
                                    gpiMode = "false";
                                    rfidReader.Config.GPI[gpiPort].Enable = false;

                                    Console.WriteLine("\nGPI Port : {0} \nStatus   : Disabled", gpiPort);
                                }
                                else if (gpiStatus == 2)
                                {
                                    gpiMode = "true";
                                    rfidReader.Config.GPI[gpiPort].Enable = true;

                                    Console.WriteLine("\nGPI Port : {0} \nStatus   : Enabled", gpiPort);
                                }

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
                                        string updQuery = "UPDATE gpi_tbl SET GPIStatus = @gpiMode WHERE GPIID = " + GPIID + " AND ReaderID = " + ReaderID + " AND GPIPort = " + gpiPort + "";
                                        cmd = new MySqlCommand(updQuery, db2.Con);
                                        cmd.Parameters.AddWithValue("@gpiMode", gpiMode);
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
                            //bool enabled = true;

                            //for (int index = 0; index < rfidReader.ReaderCapabilities.NumGPIPorts; index++)
                            //{
                            //    if (rfidReader.Config.GPI[index + 1].IsEnabled == enabled)
                            //    {
                            //        Console.WriteLine($"Port Number : {index + 1}");
                            //        Console.WriteLine($"Status      : Enabled");
                            //        Console.WriteLine();
                            //    }
                            //    else
                            //    {
                            //        Console.WriteLine($"Port Number : {index + 1}");
                            //        Console.WriteLine($"Status      : Disabled");
                            //        Console.WriteLine();
                            //    }
                            //}
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
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (InvalidUsageException iue)
                {
                    Console.WriteLine("Operation failed.Reason: " + iue.Info);
                }
                catch (OperationFailureException opex)
                {
                    Console.WriteLine("Failed to get the port state. Reason: " + opex.VendorMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
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
                        string gpiStatus = dataReader.GetString("GPIStatus");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiStatus.Equals("true")) Console.WriteLine("GPI Status                  : Enabled\n");
                        else Console.WriteLine("GPI Status                  : Disabled\n");
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
            int option, gpoPort, gpoStatus;
            string gpoMode = "";

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

                                if (gpoPort <= 0 || gpoPort > rfidReader.ReaderCapabilities.NumGPOPorts)
                                {
                                    Console.WriteLine("Enter a valid Port in the range 1-" + rfidReader.ReaderCapabilities.NumGPOPorts);
                                    continue;
                                }
                            }
                            catch (IOException)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-" + rfidReader.ReaderCapabilities.NumGPOPorts);
                                continue;
                            }

                            Console.WriteLine("\nGPO Status");
                            Console.WriteLine("1. Low");
                            Console.WriteLine("2. High");
                            Console.Write("Option   : ");

                            gpoStatus = Convert.ToInt32(Console.ReadLine());

                            if (gpoStatus == 1 || gpoStatus == 2)
                            {
                                if (gpoStatus == 1)
                                {
                                    gpoMode = "FALSE";
                                    rfidReader.Config.GPO[gpoPort].PortState = GPOs.GPO_PORT_STATE.FALSE;

                                    Console.WriteLine("\nGPI Port : {0} \nMode     : Low", gpoPort);
                                }
                                else if (gpoStatus == 2)
                                {
                                    gpoMode = "TRUE";
                                    rfidReader.Config.GPO[gpoPort].PortState = GPOs.GPO_PORT_STATE.TRUE;

                                    Console.WriteLine("\nGPI Port : {0} \nMode     : High", gpoPort);
                                }

                                Console.WriteLine("\nSet GPO Successfully");

                                try
                                {
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
                                        cmd.Parameters.AddWithValue("@gpoMode", gpoMode);
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
                            //for (int index = 0; index < rfidReader.ReaderCapabilities.NumGPOPorts; index++)
                            //{
                            //    if (rfidReader.Config.GPO[index + 1].PortState == GPOs.GPO_PORT_STATE.TRUE)
                            //    {
                            //        Console.WriteLine($"Port Number : {index + 1}");
                            //        Console.WriteLine($"Status      : Enabled");
                            //        Console.WriteLine();
                            //    }
                            //    else
                            //    {
                            //        Console.WriteLine($"Port Number : {index + 1}");
                            //        Console.WriteLine($"Status      : Disabled");
                            //        Console.WriteLine();
                            //    }
                            //}
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
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (InvalidUsageException iue)
                {
                    Console.WriteLine("Operation failed.Reason: " + iue.Info);
                }
                catch (OperationFailureException opex)
                {
                    Console.WriteLine("Failed to get the port state. Reason: " + opex.VendorMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
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
                        if (gpoMode.Equals("TRUE")) Console.WriteLine("GPO Mode                    : High\n");
                        else Console.WriteLine("GPO Mode                    : Low\n");
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        public void SingulationControl()
        {
            bool isWorking = true;
            int option, antenna, invState = 0, slFlag = 0;

            ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;
            Antennas.SingulationControl singularControl;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Singulation Control");
                Console.WriteLine("2. Get Singulation Control");
                Console.WriteLine("3. Go back");
                Console.Write("\n[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            try
                            {
                                //Console.Write("\nAntenna                    : ");
                                //antenna = Convert.ToInt32(Console.ReadLine());

                                //if (antenna <= 0 || antenna > rfidReader.ReaderCapabilities.NumAntennaSupported)
                                //{
                                //    Console.WriteLine("Enter a valid Antenna in the range 1-" + rfidReader.ReaderCapabilities.NumAntennaSupported);
                                //    continue;
                                //}

                                //antenna -= 1;
                                //singularControl = rfidReader.Config.Antennas[antID[antenna]].GetSingulationControl();

                                Console.WriteLine("\nSession Mode Menu");
                                Console.WriteLine("1. Session 0");
                                Console.WriteLine("2. Session 1");
                                Console.WriteLine("3. Session 2");
                                Console.WriteLine("4. Session 3\n");

                                Console.Write("Session           : ");
                                int session = Convert.ToInt32(Console.ReadLine());

                                //if (session == 1) singularControl.Session = SESSION.SESSION_S0;
                                //else if (session == 2) singularControl.Session = SESSION.SESSION_S1;
                                //else if (session == 3) singularControl.Session = SESSION.SESSION_S2;
                                //else if (session == 4) singularControl.Session = SESSION.SESSION_S3;
                                //else
                                //{
                                //    Console.WriteLine("Enter a valid integer in the range 1-4");
                                //    break;
                                //}
                                if (session <= 0 || session > 4)
                                {
                                    Console.WriteLine("Input Valid Session");
                                    continue;
                                }

                                Console.Write("\nTag Population    : ");
                                int TagPopulation = Convert.ToInt32(Console.ReadLine());
                                if (TagPopulation < 0)
                                {
                                    Console.WriteLine("Value was too small");
                                    continue;
                                }

                                Console.Write("Tag Transmit Time : ");
                                int TagTransitTime = Convert.ToInt32(Console.ReadLine());

                                if (TagTransitTime < 0)
                                {
                                    Console.WriteLine("Value was too small");
                                    continue;
                                }

                                Console.WriteLine("\nState Aware Singulation");
                                Console.Write("Press 'Y' to enable, 'Any key' to save the Singulation Config : ");
                                ConsoleKeyInfo choice = Console.ReadKey();
                                if (choice.Key == ConsoleKey.Y)
                                {
                                    //singularControl.Action.PerformStateAwareSingulationAction = true;
                                    Console.WriteLine("\n\nState Aware Menu");
                                    Console.WriteLine("1. STATE A");
                                    Console.WriteLine("2. STATE B");
                                    Console.WriteLine("3. AB FLIP");

                                    Console.Write("\nInventory State    : ");
                                    invState = Convert.ToInt32(Console.ReadLine());
                                    //if (invState == 1) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_A;
                                    //else if (invState == 2) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_B;
                                    //else if (invState == 3) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_AB_FLIP;
                                    //else
                                    //{
                                    //    Console.WriteLine("Enter a valid value of Inventory State 1-3");
                                    //    break;
                                    //}
                                    Console.WriteLine("\nSLFlag Menu");
                                    Console.WriteLine("1. ASSERTED");
                                    Console.WriteLine("2. DEASSERTED");
                                    Console.WriteLine("3. SL ALL");
                                    Console.Write("\nSL Flag            : ");
                                    slFlag = Convert.ToInt32(Console.ReadLine());
                                    //if (slFlag == 1) singularControl.Action.SLFlag = SL_FLAG.SL_FLAG_ASSERTED;
                                    //else if (slFlag == 2) singularControl.Action.SLFlag = SL_FLAG.SL_FLAG_DEASSERTED;
                                    //else if (slFlag == 3) singularControl.Action.SLFlag = SL_FLAG.SL_ALL;
                                    //else
                                    //{
                                    //    Console.WriteLine("Enter a valid value of SL Flag");
                                    //    break;
                                    //}
                                }

                                MySqlDatabase db1 = new();

                                for (antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                                {
                                    string query = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (antenna + 1) + "";

                                    cmd = new MySqlCommand(query, db1.Con);

                                    if (db1.Con.State != ConnectionState.Open)
                                    {
                                        db1.Con.Open();
                                    }
                                    var res = cmd.ExecuteScalar();
                                    if (res != null)
                                    {
                                        AntennaID = Convert.ToInt32(res);
                                    }
                                    db1.Con.Close();

                                    singularControl = rfidReader.Config.Antennas[antID[antenna]].GetSingulationControl();

                                    if (session == 1) singularControl.Session = SESSION.SESSION_S0;
                                    else if (session == 2) singularControl.Session = SESSION.SESSION_S1;
                                    else if (session == 3) singularControl.Session = SESSION.SESSION_S2;
                                    else if (session == 4) singularControl.Session = SESSION.SESSION_S3;

                                    singularControl.TagPopulation = Convert.ToUInt16(TagPopulation);
                                    singularControl.TagTransitTime = Convert.ToUInt16(TagTransitTime);

                                    if (choice.Key == ConsoleKey.Y)
                                    {
                                        singularControl.Action.PerformStateAwareSingulationAction = true;
                                    }
                                    else
                                    {
                                        singularControl.Action.PerformStateAwareSingulationAction = false;
                                    }

                                    if (invState == 1) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_A;
                                    else if (invState == 2) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_B;
                                    else if (invState == 3) singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_AB_FLIP;

                                    if (slFlag == 1) singularControl.Action.SLFlag = SL_FLAG.SL_FLAG_ASSERTED;
                                    else if (slFlag == 2) singularControl.Action.SLFlag = SL_FLAG.SL_FLAG_DEASSERTED;
                                    else if (slFlag == 3) singularControl.Action.SLFlag = SL_FLAG.SL_ALL;

                                    rfidReader.Config.Antennas[antID[antenna]].SetSingulationControl(singularControl);

                                    MySqlDatabase db2 = new();
                                    string selQuery = @"SpCheckZebraSingulation";
                                    cmd = new MySqlCommand(selQuery, db2.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@aID", AntennaID);
                                    cmd.Parameters.AddWithValue("@sess", singularControl.Session);
                                    cmd.Parameters.AddWithValue("@tp", singularControl.TagPopulation);
                                    cmd.Parameters.AddWithValue("@tt", singularControl.TagTransitTime);
                                    cmd.Parameters.AddWithValue("@sa", singularControl.Action.PerformStateAwareSingulationAction.ToString());
                                    cmd.Parameters.AddWithValue("@invState", singularControl.Action.InventoryState.ToString());
                                    cmd.Parameters.AddWithValue("@sl", singularControl.Action.SLFlag.ToString());

                                    if (db2.Con.State != ConnectionState.Open)
                                    {
                                        db2.Con.Open();
                                    }
                                    cmd.ExecuteScalar();
                                    db2.Con.Close();
                                }
                                Console.WriteLine("\nSet Singulation Control Successfully");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Zebra Reader Setting Error");
                            }
                            break;
                        case 2:
                            //for (int index = 0; index < rfidReader.ReaderCapabilities.NumAntennaSupported; index++)
                            //{
                            //    Console.WriteLine();
                            //    Console.WriteLine($"Antenna                          : {index + 1}");
                            //    singularControl = rfidReader.Config.Antennas[antID[index]].GetSingulationControl();

                            //    Console.WriteLine("Session                            : " + singularControl.Session);
                            //    Console.WriteLine("TagPopulation                      : " + singularControl.TagPopulation);
                            //    Console.WriteLine("TagTransitTime                     : " + singularControl.TagTransitTime);
                            //    Console.WriteLine("PerformStateAwareSingulationAction : " + singularControl.Action.PerformStateAwareSingulationAction);
                            //    Console.WriteLine("InventoryState                     : " + singularControl.Action.InventoryState);
                            //    Console.WriteLine("SLFlag                             : " + singularControl.Action.SLFlag);
                            //}
                            DisplaySingulationControl();
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
        public void DisplaySingulationControl()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND a.Antenna = 1";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    string session = dataReader.GetString("Session");
                    int tagPopulation = dataReader.GetInt32("TagPopulation");
                    int tagTransmit = dataReader.GetInt32("TagTransmit");
                    string stateAware = dataReader.GetString("StateAware");
                    string slFlag = dataReader.GetString("SLFlag");
                    string invState = dataReader.GetString("InventoryState");
                    //string? slFlag = dataReader.IsDBNull(dataReader.GetOrdinal("SLFlag")) ? null : dataReader.GetString("SLFlag");
                    //string? invState = dataReader.IsDBNull(dataReader.GetOrdinal("InventoryState")) ? null : dataReader.GetString("InventoryState");

                    Console.WriteLine("Session                     : {0} ", session);
                    Console.WriteLine("Tag Population              : {0} ", tagPopulation);
                    Console.WriteLine("Tag Transmit                : {0} ", tagTransmit);
                    Console.WriteLine("State Aware                 : {0} ", stateAware);
                    Console.WriteLine("SL Flag                     : {0} ", slFlag);
                    Console.WriteLine("Inventory State             : {0} \n", invState);
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void PowerRadio()
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine();
                Console.WriteLine("----Power Radio Menu----");
                Console.WriteLine("1. Set Power Radio");
                Console.WriteLine("2. Get Power Radio Status");
                Console.WriteLine("3. Go back");
                Console.WriteLine();
                Console.Write("[1-3]  : ");
                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Console.WriteLine();
                            Console.WriteLine("[0] Power Radio OFF");
                            Console.WriteLine("[1] Power Radio ON");
                            Console.Write("[0-1] : ");

                            int powerRadio = Convert.ToInt32(Console.ReadLine());
                            if (powerRadio == 0)
                            {
                                rfidReader.Config.RadioPowerState = RADIO_POWER_STATE.OFF;
                                Console.WriteLine("Set Power Radio OFF Successfully");
                            }
                            else if (powerRadio == 1)
                            {
                                rfidReader.Config.RadioPowerState = RADIO_POWER_STATE.ON;
                                Console.WriteLine("Set Power Radio ON Successfully");
                            }
                            else
                            {
                                Console.WriteLine("Enter a valid integer in the range 0-1");
                            }
                            rfidReader.Config.RadioPowerState = RADIO_POWER_STATE.OFF;
                            break;
                        case 2:
                            //Console.WriteLine("Power Radio Status : " + rfidReader.Config.RadioPowerState);
                            try
                            {
                                MySqlDatabase db = new();

                                string selQuery = "SELECT * FROM power_radio_tbl WHERE ReaderID = " + ReaderID + "";
                                cmd = new MySqlCommand(selQuery, db.Con);
                                MySqlDataReader dataReader = cmd.ExecuteReader();

                                if (dataReader.HasRows)
                                {
                                    while (dataReader.Read())
                                    {
                                        string radioStatus = dataReader.GetString("RadioStatus");

                                        Console.WriteLine("Radio Status                : {0} ", radioStatus);
                                    }
                                    db.Con.Close();
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
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
        public void Read()
        {
            rfidReader.Actions.Inventory.Perform(null, null, GetInfo());
            uniqueTags.Clear();
            totalTags = 0;
            Console.WriteLine("Inventory Started");
            updateReadHandler = new updateRead(MyUpdateRead);
            bgWorker = new BackgroundWorker();
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.WorkerReportsProgress = true;
            bgWorker.RunWorkerAsync();
            Console.WriteLine("Press Enter to stop inventory");
            try
            {
                Console.ReadKey();
                Console.WriteLine("Total Tags: " + uniqueTags.Count + "(" + totalTags + ")");
            }
            catch (IOException)
            {
                Console.WriteLine("IO Exception");
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine(ioe.Message);
            }
            catch (InvalidUsageException iue)
            {
                Console.WriteLine(iue.Info);
            }
            catch (OperationFailureException ofe)
            {
                Console.WriteLine(ofe.Result + ":" + ofe.StatusDescription);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                rfidReader.Actions.Inventory.Stop();
            }
        }
        private void MyUpdateRead(Events.ReadEventData eventData)
        {
            DataTable dt = new();
            dt.Columns.Add("EPC");
            dt.Columns.Add("Antenna");
            Symbol.RFID3.TagData[] tagData = rfidReader.Actions.GetReadTags(1000);
            if (tagData != null)
            {
                for (int nIndex = 0; nIndex < tagData.Length; nIndex++)
                {
                    Symbol.RFID3.TagData tag = tagData[nIndex];
                    string tagID = tag.TagID;
                    bool isFound = false;

                    lock (uniqueTags.SyncRoot)
                    {
                        isFound = uniqueTags.ContainsKey(tagID);
                        if (!isFound)
                        {
                            isFound = uniqueTags.ContainsKey(tagID);
                        }
                    }

                    dt.Rows.Add(tagID, tag.AntennaID);

                    if (isFound)
                    {
                        totalTags += tag.TagSeenCount;
                    }
                    else
                    {
                        totalTags += tag.TagSeenCount;
                        Console.WriteLine($"{tagID} {tag.AntennaID}");
                    }
                    lock (uniqueTags.SyncRoot)
                    {
                        uniqueTags.Add(tagID, dt.Rows);
                    }
                }
            }
        }
        private void BgWorker_RunWorkerCompleted(object? sender,
            RunWorkerCompletedEventArgs connectEventArgs)
        {
            rfidReader.Events.ReadNotify += new Events.ReadNotifyHandler(EventsReadNotify);
            rfidReader.Events.AttachTagDataWithReadEvent = false;
            rfidReader.Events.NotifyGPIEvent = true;
            rfidReader.Events.NotifyBufferFullEvent = true;
            rfidReader.Events.NotifyBufferFullWarningEvent = true;
            rfidReader.Events.NotifyReaderDisconnectEvent = true;
            rfidReader.Events.NotifyReaderExceptionEvent = true;
            rfidReader.Events.NotifyAccessStartEvent = true;
            rfidReader.Events.NotifyAccessStopEvent = true;
            rfidReader.Events.NotifyInventoryStartEvent = true;
            rfidReader.Events.NotifyInventoryStopEvent = true;
        }
        private void EventsReadNotify(object sender, Events.ReadEventArgs readEventArgs)
        {
            try
            {
                updateReadHandler.Invoke(readEventArgs.ReadEventData);
            }
            catch (Exception)
            {
            }
        }
        public Symbol.RFID3.AntennaInfo GetInfo()
        {
            return antennaInfo;
        }
        public void Default()
        {
            try
            {
                Antennas.RFMode rfMode;
                Antennas.SingulationControl singularControl;
                Antennas.Config antennaConfig;

                ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;

                //rf mode
                //for (int antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                //{
                //    rfMode = rfidReader.Config.Antennas[antID[antenna]].GetRFMode();

                //    rfMode.Tari = 0;
                //    rfMode.TableIndex = 0;

                //    rfidReader.Config.Antennas[antID[antenna]].SetRFMode(rfMode);
                //}

                //tag settings
                //tagStorageSettings.MaxTagCount = 4096;
                //tagStorageSettings.MaxTagIDLength = 64;
                //tagStorageSettings.MaxSizeMemoryBank = 64;
                //tagStorageSettings.TagFields &= ~TAG_FIELD.PHASE_INFO;

                //rfidReader.Config.SetTagStorageSettings(tagStorageSettings);

                //singulation control
                //for (int antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                //{
                //    singularControl = rfidReader.Config.Antennas[antID[antenna]].GetSingulationControl();

                //    singularControl.Session = SESSION.SESSION_S0;
                //    singularControl.TagPopulation = 100;
                //    singularControl.TagTransitTime = 0;
                //    singularControl.Action.PerformStateAwareSingulationAction = false;

                //    rfidReader.Config.Antennas[antID[antenna]].SetSingulationControl(singularControl);
                //}

                //power
                //for (int antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                //{
                //    antennaConfig = rfidReader.Config.Antennas[antID[antenna]].GetConfig();

                //    antennaConfig.ReceiveSensitivityIndex = 0;
                //    antennaConfig.TransmitPowerIndex = 200;
                //    antennaConfig.TransmitFrequencyIndex = 1;

                //    rfidReader.Config.Antennas[antID[antenna]].SetConfig(antennaConfig);
                //}

                //enable
                //for (int antenna = 0; antenna < rfidReader.ReaderCapabilities.NumAntennaSupported; antenna++)
                //{
                //    if (statusList.Contains(antenna + 1))
                //    {
                //    }
                //    else
                //    {
                //        statusList.Add(antenna + 1);
                //        statusList.Sort();
                //    }
                //}

                ////gpi
                //for (int gpiPort = 0; gpiPort < rfidReader.ReaderCapabilities.NumGPIPorts; gpiPort++)
                //{
                //    rfidReader.Config.GPI[gpiPort].Enable = true;
                //}

                ////gpo
                //for (int gpoPort = 0; gpoPort < rfidReader.ReaderCapabilities.NumGPOPorts; gpoPort++)
                //{
                //    rfidReader.Config.GPO[gpoPort].PortState = GPOs.GPO_PORT_STATE.FALSE;
                //}

                //radio
                //rfidReader.Config.RadioPowerState = RADIO_POWER_STATE.OFF;

                //Antenna Power
                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + "";
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
                    string insQuery1 = "INSERT INTO antenna_tbl (ReaderID, Antenna, ReceiveSensitivity, TransmitPower, TransmitFreq) VALUES (@rID, @ant, @sensitivity, @power, @freq)";
                    cmd = new MySqlCommand(insQuery1, db1.Con);

                    for (int i = 0; i < rfidReader.ReaderCapabilities.NumAntennaSupported; i++)
                    {
                        antennaConfig = rfidReader.Config.Antennas[antID[i]].GetConfig();

                        antennaConfig.ReceiveSensitivityIndex = 0;
                        antennaConfig.TransmitPowerIndex = 200;
                        antennaConfig.TransmitFrequencyIndex = 1;

                        rfidReader.Config.Antennas[antID[i]].SetConfig(antennaConfig);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@ant", i + 1);
                        cmd.Parameters.AddWithValue("@sensitivity", antennaConfig.ReceiveSensitivityIndex);
                        cmd.Parameters.AddWithValue("@power", antennaConfig.TransmitPowerIndex);
                        cmd.Parameters.AddWithValue("@freq", antennaConfig.TransmitFrequencyIndex);

                        if (db1.Con.State != ConnectionState.Open)
                        {
                            db1.Con.Open();
                        }
                        cmd.ExecuteNonQuery();
                        db1.Con.Close();
                    }
                }

                //RF Modes
                MySqlDatabase db2 = new();

                for (int i = 0; i < rfidReader.ReaderCapabilities.NumAntennaSupported; i++)
                {
                    string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (i + 1) + "";
                    cmd = new MySqlCommand(selQuery2, db2.Con);

                    if (db2.Con.State != ConnectionState.Open)
                    {
                        db2.Con.Open();
                    }

                    MySqlDataReader dataReader2 = cmd.ExecuteReader();

                    if (dataReader2.HasRows)
                    {
                        dataReader2.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        rfMode = rfidReader.Config.Antennas[antID[i]].GetRFMode();

                        rfMode.Tari = 0;
                        rfMode.TableIndex = 0;

                        rfidReader.Config.Antennas[antID[i]].SetRFMode(rfMode);

                        string insQuery2 = "INSERT INTO rf_modes_tbl (AntennaID, Tari, RFTable) VALUES (@aID, @tari, @rfTableIndex)";
                        cmd = new MySqlCommand(insQuery2, db2.Con);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                        cmd.Parameters.AddWithValue("@tari", rfMode.Tari);
                        cmd.Parameters.AddWithValue("@rfTableIndex", rfMode.TableIndex);

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        dataReader2.Close();
                    }

                    db2.Con.Close();
                }

                //Tag Storage
                MySqlDatabase db3 = new();

                string selQuery3 = "SELECT * FROM tag_storage_tbl WHERE ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery3, db3.Con);
                MySqlDataReader dataReader3 = cmd.ExecuteReader();

                if (dataReader3.HasRows)
                {
                    dataReader3.Close();
                    if (db3.Con.State != ConnectionState.Open)
                    {
                        db3.Con.Open();
                    }
                    db3.Con.Close();
                }
                else
                {
                    tagStorageSettings.MaxTagCount = 4096;
                    tagStorageSettings.MaxTagIDLength = 64;
                    tagStorageSettings.MaxSizeMemoryBank = 64;
                    tagStorageSettings.TagFields &= ~TAG_FIELD.PHASE_INFO;

                    rfidReader.Config.SetTagStorageSettings(tagStorageSettings);

                    dataReader3.Close();
                    string insQuery3 = @"SpZebraDefaultTagStorage";
                    cmd = new MySqlCommand(insQuery3, db3.Con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@rID", ReaderID);
                    cmd.Parameters.AddWithValue("@tagCount", tagStorageSettings.MaxTagCount);
                    cmd.Parameters.AddWithValue("@tagLength", tagStorageSettings.MaxTagIDLength);
                    cmd.Parameters.AddWithValue("@memorySize", tagStorageSettings.MaxSizeMemoryBank);
                    cmd.Parameters.AddWithValue("@tFields", tagStorageSettings.TagFields);

                    cmd.ExecuteScalar();
                    db3.Con.Close();
                }

                //Singulation Control
                MySqlDatabase db4 = new();

                for (int i = 0; i < rfidReader.ReaderCapabilities.NumAntennaSupported; i++)
                {
                    string selQuery4 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (i + 1) + "";
                    cmd = new MySqlCommand(selQuery4, db4.Con);

                    if (db4.Con.State != ConnectionState.Open)
                    {
                        db4.Con.Open();
                    }

                    MySqlDataReader dataReader4 = cmd.ExecuteReader();

                    if (dataReader4.HasRows)
                    {
                        dataReader4.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        singularControl = rfidReader.Config.Antennas[antID[i]].GetSingulationControl();

                        singularControl.Session = SESSION.SESSION_S0;
                        singularControl.TagPopulation = 100;
                        singularControl.TagTransitTime = 0;
                        singularControl.Action.PerformStateAwareSingulationAction = false;
                        singularControl.Action.InventoryState = INVENTORY_STATE.INVENTORY_STATE_A;
                        singularControl.Action.SLFlag = SL_FLAG.SL_FLAG_DEASSERTED;

                        rfidReader.Config.Antennas[antID[i]].SetSingulationControl(singularControl);

                        string insQuery4 = "INSERT INTO singulation_tbl (AntennaID, Session, TagPopulation, TagTransmit, StateAware, InventoryState, SLFlag) VALUES (@aID, @session, @tagPopulation, @tagTransmit, @stateAware, @invState, @slFlag)";
                        cmd = new MySqlCommand(insQuery4, db4.Con);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                        cmd.Parameters.AddWithValue("@session", singularControl.Session.ToString());
                        cmd.Parameters.AddWithValue("@tagPopulation", singularControl.TagPopulation);
                        cmd.Parameters.AddWithValue("@tagTransmit", singularControl.TagTransitTime);
                        cmd.Parameters.AddWithValue("@stateAware", singularControl.Action.PerformStateAwareSingulationAction.ToString());
                        cmd.Parameters.AddWithValue("@invState", singularControl.Action.InventoryState.ToString());
                        cmd.Parameters.AddWithValue("@slFlag", singularControl.Action.SLFlag.ToString());

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        dataReader4.Close();
                    }
                    db4.Con.Close();
                }

                //Power Radio
                MySqlDatabase db5 = new();

                string selQuery5 = "SELECT * FROM power_radio_tbl WHERE ReaderID = " + ReaderID + "";
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
                    rfidReader.Config.RadioPowerState = RADIO_POWER_STATE.OFF;

                    dataReader5.Close();
                    string insQuery5 = @"SpZebraDefaultPowerRadio";
                    cmd = new MySqlCommand(insQuery5, db5.Con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@rID", ReaderID);
                    cmd.Parameters.AddWithValue("@rpStatus", RADIO_POWER_STATE.OFF.ToString());

                    cmd.ExecuteScalar();
                    db5.Con.Close();
                }

                //Enabling Antenna
                MySqlDatabase db6 = new();

                for (int i = 0; i < rfidReader.ReaderCapabilities.NumAntennaSupported; i++)
                {
                    string selQuery6 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " AND Antenna = " + (i + 1) + "";
                    cmd = new MySqlCommand(selQuery6, db6.Con);

                    if (db6.Con.State != ConnectionState.Open)
                    {
                        db6.Con.Open();
                    }

                    MySqlDataReader dataReader6 = cmd.ExecuteReader();

                    if (dataReader6.HasRows)
                    {
                        dataReader6.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }
                        if (statusList.Contains(i + 1))
                        {
                        }
                        else
                        {
                            statusList.Add(i + 1);
                            statusList.Sort();
                        }

                        string insQuery6 = "INSERT INTO antenna_info_tbl (AntennaID, AntennaStatus) VALUES (@aID, @antStatus)";
                        cmd = new MySqlCommand(insQuery6, db6.Con);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                        cmd.Parameters.AddWithValue("@antStatus", "Enabled");

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        dataReader6.Close();
                    }

                    db6.Con.Close();
                }

                //GPI
                MySqlDatabase db7 = new();

                string selQuery7 = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery7, db7.Con);
                MySqlDataReader dataReader7 = cmd.ExecuteReader();

                if (dataReader7.HasRows)
                {
                    dataReader7.Close();
                    if (db7.Con.State != ConnectionState.Open)
                    {
                        db7.Con.Open();
                    }
                    db7.Con.Close();
                }
                else
                {
                    dataReader7.Close();
                    string insQuery7 = "INSERT INTO gpi_tbl (ReaderID, GPIPort, GPIStatus) VALUES (@rID, @gpiPortNo, @gpiStats)";
                    cmd = new MySqlCommand(insQuery7, db7.Con);

                    for (int i = 0; i < rfidReader.ReaderCapabilities.NumGPIPorts; i++)
                    {
                        rfidReader.Config.GPI[i + 1].Enable = true;

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@gpiPortNo", (i + 1));
                        cmd.Parameters.AddWithValue("@gpiStats", "true");

                        if (db7.Con.State != ConnectionState.Open)
                        {
                            db7.Con.Open();
                        }
                        cmd.ExecuteNonQuery();
                        db7.Con.Close();
                    }
                }

                //GPO
                MySqlDatabase db8 = new();

                string selQuery8 = "SELECT * FROM gpo_tbl WHERE ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery8, db8.Con);
                MySqlDataReader dataReader8 = cmd.ExecuteReader();

                if (dataReader8.HasRows)
                {
                    dataReader8.Close();
                    if (db8.Con.State != ConnectionState.Open)
                    {
                        db8.Con.Open();
                    }
                    db8.Con.Close();
                }
                else
                {
                    dataReader8.Close();
                    string insQuery8 = "INSERT INTO gpo_tbl (ReaderID, GPOPort, GPOMode) VALUES (@rID, @gpoPortNo, @gpoStats)";
                    cmd = new MySqlCommand(insQuery8, db8.Con);

                    for (int i = 0; i < rfidReader.ReaderCapabilities.NumGPOPorts; i++)
                    {
                        rfidReader.Config.GPO[i + 1].PortState = GPOs.GPO_PORT_STATE.FALSE;

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@gpoPortNo", (i + 1));
                        cmd.Parameters.AddWithValue("@gpoStats", GPOs.GPO_PORT_STATE.FALSE.ToString());

                        if (db5.Con.State != ConnectionState.Open)
                        {
                            db5.Con.Open();
                        }
                        cmd.ExecuteNonQuery();
                        db5.Con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void LoadDB()
        {
            try
            {
                Antennas.RFMode rfMode;
                Antennas.SingulationControl singularControl;
                Antennas.Config antennaConfig;

                ushort[] antID = rfidReader.Config.Antennas.AvailableAntennas;

                //Antenna Power
                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = " + ReaderID + " ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        int antennaIndex = dataReader1.GetInt32("Antenna") - 1;
                        if (antennaIndex < antID.Length)
                        {
                            antennaConfig = rfidReader.Config.Antennas[antID[antennaIndex]].GetConfig();

                            antennaConfig.ReceiveSensitivityIndex = Convert.ToUInt16(dataReader1.GetInt32("ReceiveSensitivity"));
                            antennaConfig.TransmitPowerIndex = Convert.ToUInt16(dataReader1.GetInt32("TransmitPower"));
                            antennaConfig.TransmitFrequencyIndex = Convert.ToUInt16(dataReader1.GetInt32("TransmitFreq"));

                            rfidReader.Config.Antennas[antID[antennaIndex]].SetConfig(antennaConfig);
                        }
                        else
                        {
                            Console.WriteLine("Antenna index {0} is out of range", antennaIndex);
                        }
                    }
                    db1.Con.Close();
                }

                //RF Modes
                MySqlDatabase db2 = new();

                string selQuery2 = "SELECT * FROM antenna_tbl a INNER JOIN rf_modes_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " ORDER BY a.Antenna ASC";
                cmd = new MySqlCommand(selQuery2, db2.Con);
                MySqlDataReader dataReader2 = cmd.ExecuteReader();

                if (dataReader2.HasRows)
                {
                    while (dataReader2.Read())
                    {
                        int antennaIndex = dataReader2.GetInt32("Antenna") - 1;
                        if (antennaIndex < antID.Length)
                        {
                            rfMode = rfidReader.Config.Antennas[antID[antennaIndex]].GetRFMode();

                            rfMode.Tari = Convert.ToUInt16(dataReader2.GetInt32("Tari"));
                            rfMode.TableIndex = Convert.ToUInt16(dataReader2.GetInt32("RFTable"));

                            rfidReader.Config.Antennas[antID[antennaIndex]].SetRFMode(rfMode);
                        }
                        else
                        {
                            Console.WriteLine("Antenna index {0} is out of range", antennaIndex);
                        }
                    }
                    db2.Con.Close();
                }

                //Tag Storage
                MySqlDatabase db3 = new();

                string selQuery3 = "SELECT * FROM reader_tbl a INNER JOIN tag_storage_tbl b ON a.ReaderID = b.ReaderID WHERE a.ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery3, db3.Con);
                MySqlDataReader dataReader3 = cmd.ExecuteReader();

                if (dataReader3.HasRows)
                {
                    while (dataReader3.Read())
                    {
                        tagStorageSettings.MaxTagCount = Convert.ToUInt16(dataReader3.GetInt32("MaxCount"));
                        tagStorageSettings.MaxTagIDLength = Convert.ToUInt16(dataReader3.GetInt32("TagIDLength"));
                        tagStorageSettings.MaxSizeMemoryBank = Convert.ToUInt16(dataReader3.GetInt32("BankSize"));
                        tagStorageSettings.TagFields = (TAG_FIELD)System.Enum.Parse(typeof(TAG_FIELD), dataReader3.GetString("TagFields"));

                        rfidReader.Config.SetTagStorageSettings(tagStorageSettings);
                    }
                    db3.Con.Close();
                }

                //Singulation Control
                MySqlDatabase db4 = new();

                string selQuery4 = "SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " AND a.Antenna = 1";
                cmd = new MySqlCommand(selQuery4, db4.Con);
                MySqlDataReader dataReader4 = cmd.ExecuteReader();

                if (dataReader4.HasRows)
                {
                    while (dataReader4.Read())
                    {
                        int antennaIndex = dataReader4.GetInt32("Antenna") - 1;
                        if (antennaIndex < antID.Length)
                        {
                            singularControl = rfidReader.Config.Antennas[antID[antennaIndex]].GetSingulationControl();

                            singularControl.Session = (SESSION)System.Enum.Parse(typeof(SESSION), dataReader4.GetString("Session"));
                            singularControl.TagPopulation = Convert.ToUInt16(dataReader4.GetInt32("TagPopulation"));
                            singularControl.TagTransitTime = Convert.ToUInt16(dataReader4.GetInt32("TagTransmit"));
                            singularControl.Action.PerformStateAwareSingulationAction = Convert.ToBoolean(dataReader4.GetString("StateAware"));

                            if (!dataReader4.IsDBNull(dataReader4.GetOrdinal("SLFlag")) && !dataReader4.IsDBNull(dataReader4.GetOrdinal("InventoryState")))
                            {
                                singularControl.Action.SLFlag = (SL_FLAG)System.Enum.Parse(typeof(SL_FLAG), dataReader4.GetString("SLFlag"));
                                singularControl.Action.InventoryState = (INVENTORY_STATE)System.Enum.Parse(typeof(INVENTORY_STATE), dataReader4.GetString("InventoryState"));
                            }

                            rfidReader.Config.Antennas[antID[antennaIndex]].SetSingulationControl(singularControl);
                        }
                        else
                        {
                            Console.WriteLine("Antenna index {0} is out of range", antennaIndex);
                        }
                    }
                    db4.Con.Close();
                }

                //Power Radio
                MySqlDatabase db5 = new();

                string selQuery5 = "SELECT * FROM power_radio_tbl WHERE ReaderID = " + ReaderID + "";
                cmd = new MySqlCommand(selQuery5, db5.Con);
                MySqlDataReader dataReader5 = cmd.ExecuteReader();

                if (dataReader5.HasRows)
                {
                    while (dataReader5.Read())
                    {
                        rfidReader.Config.RadioPowerState = (RADIO_POWER_STATE)System.Enum.Parse(typeof(RADIO_POWER_STATE), dataReader5.GetString("RadioStatus"));
                    }
                    db5.Con.Close();
                }

                //Enabling Antenna
                MySqlDatabase db6 = new();

                string selQuery6 = "SELECT a.Antenna, b.AntennaStatus FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = " + ReaderID + " ORDER BY a.Antenna ASC";
                cmd = new MySqlCommand(selQuery6, db6.Con);
                MySqlDataReader dataReader6 = cmd.ExecuteReader();

                if (dataReader6.HasRows)
                {
                    while (dataReader6.Read())
                    {
                        int antennaIndex = dataReader6.GetInt32("Antenna") - 1;

                        if (antennaIndex < antID.Length)
                        {
                            if (statusList.Contains(antennaIndex + 1))
                            {
                            }
                            else
                            {
                                statusList.Add(antennaIndex + 1);
                                statusList.Sort();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Antenna index {0} is out of range", antennaIndex);
                        }
                    }
                    db6.Con.Close();
                }

                //GPI
                MySqlDatabase db7 = new();

                string selQuery7 = "SELECT * FROM gpi_tbl WHERE ReaderID = " + ReaderID + " ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery7, db7.Con);
                MySqlDataReader dataReader7 = cmd.ExecuteReader();

                if (dataReader7.HasRows)
                {
                    while (dataReader7.Read())
                    {
                        int gpiIndex = dataReader7.GetInt32("GPIPort");
                        if (gpiIndex <= rfidReader.Config.GPI.Length)
                        {
                            rfidReader.Config.GPI[gpiIndex].Enable = Convert.ToBoolean(dataReader7.GetString("GPIStatus"));
                        }
                        else
                        {
                            Console.WriteLine("GPI index {0} is out of range", gpiIndex);
                        }
                    }
                    db7.Con.Close();
                }

                //GPO
                MySqlDatabase db8 = new();

                string selQuery8 = "SELECT * FROM gpo_tbl WHERE ReaderID = " + ReaderID + " ORDER BY GPOPort ASC";
                cmd = new MySqlCommand(selQuery8, db8.Con);
                MySqlDataReader dataReader8 = cmd.ExecuteReader();

                if (dataReader8.HasRows)
                {
                    while (dataReader8.Read())
                    {
                        int gpoIndex = dataReader8.GetInt32("GPOPort");
                        if (gpoIndex <= rfidReader.Config.GPO.Length)
                        {
                            rfidReader.Config.GPO[gpoIndex].PortState = (GPO_PORT_STATE)System.Enum.Parse(typeof(GPO_PORT_STATE), dataReader8.GetString("GPOMode"));
                        }
                        else
                        {
                            Console.WriteLine("GPO index {0} is out of range", gpoIndex);
                        }
                    }
                    db8.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
