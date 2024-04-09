using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OracleClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Rotem.CreateFile
{
    public partial class Form1 : Form
    {
        #region Ctor
        public Form1()
        {
            InitializeComponent();

            string IniFile = Program.iniPath;
            inif = new IniFile(IniFile);



            string ConnectionString = inif.IniReadValue("DB", "Connection String");
            string ec = inif.IniReadValue("data", "employeeCode");
            outputPath = inif.IniReadValue("Directories", "Output");

            SetEmployeeCode(ec);

            //Set connection
            con = new OracleConnection();
            con.ConnectionString = ConnectionString;
            con.Open();


            panelEc.Visible = _employeesCode;
            dataGridView1.AutoGenerateColumns = false;
            var dataGridViewColumn = dataGridView1.Columns["EmployeeCode"];
            if (dataGridViewColumn != null)
                dataGridViewColumn.Visible = _employeesCode;

        }



        #endregion

        #region Fields
        List<SampleWrapper> list = new List<SampleWrapper>();
        private bool _employeesCode;
        private OracleConnection con;
        private string outputPath;
        private IniFile inif;
        #endregion

        #region Private Methods
        bool StatusIsAppropriate(string status)
        {
            return true;
            if (_employeesCode)
            {
                if (status == "W")
                {
                    return true;
                }
            }
            else
            {
                if (status == "U")
                    return true;
            }
            return false;
        }
        private void SetFocus()
        {
            if (_employeesCode)
            {
                txtEmployeeCode.Select();
            }
            else
            {
                txtSampleId.Select();
            }
        }
        private void SetEmployeeCode(string ec)
        {
            if (ec == "T")
            {
                _employeesCode = true;

            }
            else if (ec == "F")
            {
                _employeesCode = false;

            }
        }
        private void Export()
        {



                string dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName;
                if (_employeesCode)
                {
                    fileName = "Samples to start ";

                }
                else
                {
                    fileName = "Samples to lab ";
                }
                fileName += dt;

                using (var sw = new StreamWriter(outputPath + "\\" + fileName + ".txt", true))
                {
                    //var itemsToWrite = from item in list where item.WriteToFile select item;
                    foreach (SampleWrapper sampleWrapper in list)
                    {
                        string line = sampleWrapper.SampleId;
                        if (_employeesCode)
                        {
                            line += "," + sampleWrapper.EmployeeCode;
                        }
                        sw.WriteLine(line);
                    }
                }
                txtSampleId.Clear();
                txtEmployeeCode.Clear();

                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                lblmsg.Visible = true;
                timer1.Start();
                SetFocus();
                list.Clear();
       

        }
        #endregion

        #region Events


        private void txtSampleId_KeyDown(object sender, KeyEventArgs e)
        {

            try
            {


                if (e.KeyCode == Keys.Enter)
                {
                    lblmsg.Visible = false;
                    if (string.IsNullOrEmpty(txtSampleId.Text)) return;
                    if (txtSampleId.Text == "OK")
                    {
                        Export();
                        return;
                    }

                    string sqLstrold = "select Sample_id,Status from sample where Sample_Id=" + txtSampleId.Text;
                    string sqLstr = "select id_numeric,status  from sample where id_numeric=" + txtSampleId.Text;

                    var cmd0 = new OracleCommand(sqLstr, con);
                    var reader = cmd0.ExecuteReader();
                    var sw = new SampleWrapper { SampleId = txtSampleId.Text, EmployeeCode = txtEmployeeCode.Text };

                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            sw.Status = reader["Status"].ToString();

                            if (!StatusIsAppropriate(sw.Status))
                            {
                                sw.Remarks = "הדגינמה אינה בסטטוס המתאים";
                            }
                            else
                            {
                                sw.WriteToFile = true;
                            }
                        }

                    }
                    else
                    {
                        sw.Remarks = "הדגימה אינה קיימת במערכת";
                    }
                    list.Add(sw);
                    dataGridView1.DataSource = null;

                    dataGridView1.DataSource = list;
                    txtSampleId.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error " + ex.Message);
                txtSampleId.Text = string.Empty;
            }


        }



        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            panel3.Location = new Point(Width / 2 - panel3.Width / 2, panel3.Location.Y);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            SetFocus();
        }

        private void txtEmployeeCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtSampleId.Select();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            timer1.Stop();
            lblmsg.Visible = false;
        }
        #endregion

    }

    public class SampleWrapper
    {
        public SampleWrapper()
        {
            WriteToFile = false;
        }
        public string SampleId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string EmployeeCode { get; set; }
        public bool WriteToFile { get; set; }
    }
}

