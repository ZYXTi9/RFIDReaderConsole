using MySql.Data.MySqlClient;
using RfidReader.Database;

namespace RfidReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Lunox Access Control";
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Program p = new();
            p.Main();
        }
        public void Main()
        {
            Reader.Impinj impinj = new();
            Reader.Zebra zebra = new();
            MySqlDatabase db = new();

            try
            {
                string selQuery = "SELECT ReaderType FROM reader_type_tbl";
                var cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                Console.WriteLine("Choose Setup");
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        Console.WriteLine(dataReader.GetValue(i));
                    }
                }
                Console.WriteLine("Inventory");
                Console.WriteLine("Exit");
                db.Con.Close();
                dataReader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            int connectedTo = 0;

            while (connectedTo != 1 && connectedTo != 2 && connectedTo != 3 && connectedTo != 4)
            {
                try
                {

                    Console.Write("\nOption[1-4]: ");
                    connectedTo = Convert.ToInt32(Console.ReadLine());

                    if (connectedTo == 1)
                    {
                        impinj.ReaderTypeID = connectedTo;
                        impinj.ConnectToReader();
                    }
                    else if (connectedTo == 2)
                    {
                        zebra.ReaderTypeID = connectedTo;
                        zebra.ConnectToReader();
                    }
                    else if (connectedTo == 3)
                    {
                        if (impinj.impinjStatus == "Connected" || zebra.zebraStatus == "Connected")
                        {
                            impinj.Read();
                            zebra.Read();
                        }
                        else
                        {
                            Console.WriteLine("No Reader Connected");
                        }
                    }
                    else if (connectedTo == 4)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Invalid Input!");
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
            }
        }
    }
}