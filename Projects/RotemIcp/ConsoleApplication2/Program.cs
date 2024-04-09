using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Ini;
using System.Data;
using System.Data.OracleClient;

namespace Rotem
{
    class Program
    {
        //*****************************************************************************
        //*  This program converts ICP files to $ro_instruments_rslt Format           *
        //*  The program gets (by parameter passing mechanism a ini file name         *
        //*  to determine which are it's working directories.                         *
        //*  It then scans the input directory for txt files, converts and passes all *
        //*  of them into the output directory.                                       *
        //*****************************************************************************
        public static string Instrument;
        public static string ConnectionString;
        public static string Inst;

        static void Main(string[] args)
        {
            string LogFileName;
            string FileName;

            string Line;
            int index;
            bool done;
            string[] Headers = new string[] { "" };
            string[] Results;

            StreamReader sr;
            StreamWriter swLog;
            StreamWriter swOut;

            //get ini filename from args
            string IniFile = args[0];


            if (IniFile == "") return;
            //Read ini file
            IniFile inif = new IniFile(IniFile);

            //Get Directories from ini file
            string InputDirectory = inif.IniReadValue("Directories", "Input");
            string OutputDirectory = inif.IniReadValue("Directories", "Output");
            string ErrorDirectory = inif.IniReadValue("Directories", "Error");
            string LogDirectory = inif.IniReadValue("Directories", "Log");
            string BackupDirectory = inif.IniReadValue("Directories", "Backup");
            ConnectionString = inif.IniReadValue("DB", "Connection String");
            Instrument = inif.IniReadValue("General", "Instrument");
            Inst = inif.IniReadValue("General", "Inst");


            //Create and open logfile

            DateTime dt0 = DateTime.Now;
            LogFileName = LogDirectory + "Log_" + dt0.ToString().Replace("/", "").Replace(":", "") + ".txt";        
            swLog = new StreamWriter(LogFileName);

            swLog.WriteLine(DateTime.Now + "--" + "Start run");

            //Check for input files in Directory for files

            DirectoryInfo CmyDir = new DirectoryInfo(InputDirectory);

            FileInfo[] AllFilesInCmyDir = CmyDir.GetFiles("*.ade");

            //Read files and create output files

            foreach (FileInfo curFileInfo in AllFilesInCmyDir)
            {
                try
                {
                    // open Input & Output files
                    done = false;
                    // sr is the input file
                    sr = new StreamReader(curFileInfo.FullName, System.Text.Encoding.Unicode);
                    // Create FileName as the name of the output file
                    FileName = InputDirectory + "RESULT_" + DateTime.Now.ToString("yyMMdd HHmm") + ".tmp";
                    // open outputfile
                    swOut = new StreamWriter(FileName);
           
                    index = 0;
                    while (((Line = sr.ReadLine()) != null) && (done == false))
                    {
                        index++;
                        if (index == 1)
                        {
                            //1st line is the header line
                            Headers = Line.Split(new String[] { "\t" }, StringSplitOptions.None);
                        }
                        else
                        {
                            //all other lines are result lines
                            Results = Line.Split(new String[] { "\t" }, StringSplitOptions.None);
                            if (Results[0] == "Average") //only "avarage line is the right result line
                            {
                                done = true;
                                bool ok = MakeFile(Headers, Results, swOut, swLog);
                                if (ok == false)
                                {
                                    curFileInfo.MoveTo(ErrorDirectory);
                                    return;
                                }
                            }
                        }

                    }

                    sr.Close();
                    swOut.Close();

                    FileInfo OutFile = new FileInfo(FileName);
                    OutFile.MoveTo(OutputDirectory + OutFile.Name.Replace(".tmp", ".txt"));
                    AppendFiles(curFileInfo.FullName, BackupDirectory + "ICP backup.txt");
                    curFileInfo.Delete();


                }
                catch (Exception e1)
                {

 
                    //write error to log

                    swLog.WriteLine(DateTime.Now.ToString());
                    swLog.WriteLine(e1.Message);
                    swLog.Close();
                    //Move input file into error directory

                    curFileInfo.MoveTo(ErrorDirectory + curFileInfo.Name);

                }
            }

            swLog.WriteLine(DateTime.Now + "--" + "Finished run");
            swLog.Close();

        }
        static bool MakeFile(string[] headers, string[] results, StreamWriter swout, StreamWriter swLog)
        {
            string Analysis;
            string Component;
            string FileComponent;
            string Result;
            string SQLstr;

            //Open database connection
            try
            {
                OracleConnection con = new OracleConnection();
                con.ConnectionString = ConnectionString;
                //con.ConnectionString = "User Id=vgsm;Password=vgsm;Data Source=VGSM;";
                con.Open();
                OracleDataReader rdr = null;
                OracleDataReader rdr1 = null;

                //Write header and results into output file

                //Headers

                string SampleId = results[5];
                try  // try to see if SampleId is numeric
                {
                    int SampleInt = Convert.ToInt32(SampleId);
                }
                catch (System.FormatException e)
                {
                    SampleId = "0"; // The sample id is not of a numeric value therefor i change it to 0
                }
                //Check if it is a valid and existing sample_id as the one inserted

                //OracleCommand cmd0 = new OracleCommand("select id_numeric from sample where id_numeric=" + SampleId.Trim(), con);
                SQLstr = "select id_numeric from sample where id_numeric='" + SampleId.Trim().PadLeft(10) + "'";
                OracleCommand cmd0 = new OracleCommand(SQLstr, con);
                rdr1 = cmd0.ExecuteReader();
                rdr1.Read();
                if (rdr1.HasRows == false)
                {
                    SampleId = "";
                }



                swout.WriteLine("Sample ID=" + SampleId);
                swout.WriteLine("Sample Point=");
                swout.WriteLine("Material=");
                swout.WriteLine("Sample Type=");
                swout.WriteLine("MLP=");
                swout.WriteLine("Test Schedule=");
                swout.WriteLine("Date/Time=");
                swout.WriteLine("Operator=" + Instrument);

                //Results

                swout.WriteLine("[Results]");
                int size = headers.Length;
                for (int i = 7; i < size - 1; i++)
                {
                    FileComponent = headers[i];
                    Result = results[i].Replace(">", "").Replace("<", "");
                    if (Result.Contains("---") == false)
                    {

                        OracleCommand cmd = new OracleCommand("select analysis,component_name from robot_result_detail where identity like '%"
                            + Inst + "%' and robot_result_name='" + FileComponent + "'", con);

                        rdr = cmd.ExecuteReader();
                     //   rdr.Read();
                        if (rdr.HasRows == false)
                        {
                            //swLog.WriteLine("No record for instrument: " + Instrument +
                            //    " and component:" + FileComponent + " in table robot_result_detail");
                            //con.Close();
                            //return false;
                            Component = FileComponent;
                            Analysis = "";

                            swout.WriteLine(Component + "=" + Result + ";" + Analysis);

                        }
                        else
                        {
                            //Write the original component as well
                            Component = FileComponent;
                            Analysis = "";
                            swout.WriteLine(Component + "=" + Result + ";" + Analysis);

                            while (rdr.Read())
                            {
                                Component = rdr[1].ToString().Trim();
                                if (Component != "")
                                {
                                    Analysis = rdr[0].ToString().Trim();
                                    swout.WriteLine(Component + "=" + Result + ";" + Analysis);
                                }
                            }
                        }
                    }
                }

                con.Close();
                return true;
            }
            catch (Exception e1)
            {

                //write error to log

                swLog.WriteLine(DateTime.Now.ToString());
                swLog.WriteLine(e1.Message);
                return false;
            }
        }
        public static void AppendFiles(string fn1, string fn2)
        {
            //Append the contents of file fn1 to the end of file fn2

            StreamReader sr1 = new StreamReader(fn1, System.Text.Encoding.Unicode);
            StreamWriter sw2 = new StreamWriter(fn2, true, System.Text.Encoding.Unicode);

            sw2.WriteLine(DateTime.Now + " -- " + fn1); //Create Header for the uppended text
            sw2.Write(sr1.ReadToEnd());                 //Write the contents of the input file to the apppend file
            sw2.Flush();
            sw2.Close();
            sr1.Close();



        }
    }
}
