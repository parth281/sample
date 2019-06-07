using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ProgressBarExample;

namespace TNT
{
    public partial class Home : Form
    {
        public static string role;
        public static string name;
        public static string companyname;
        string location;
        private SqlCommand cmd;

        public Home()
        {
            InitializeComponent();

        }

        public Home(string roles, string username,string company)
        {
            InitializeComponent();
            lb_Role.Text = roles;
            lb_Username.Text = username;
            lbl_company.Text = company;
            companyname = lbl_company.Text;
            role = lb_Role.Text;
            name = lb_Username.Text;

            groupBox1.Location = new Point(this.Width / 2 - groupBox1.Width / 2, this.Height / 2 - groupBox1.Height / 2);
            groupBox1.Anchor = AnchorStyles.None;
        }

        public Home(string roles, string username)
        {
            InitializeComponent();
            lb_Role.Text = roles;
            lb_Username.Text = username;
            companyname = lbl_company.Text;
            role = lb_Role.Text;
            name = lb_Username.Text;

            groupBox1.Location = new Point(this.Width / 2 - groupBox1.Width / 2, this.Height / 2 - groupBox1.Height / 2);
            groupBox1.Anchor = AnchorStyles.None;
            Home.productToolStripMenuItem.Enabled = false;
            Home.batchManagerToolStripMenuItem.Enabled = false;
            Home.reportsToolStripMenuItem.Enabled = false;
            Home.settingAndUtilityToolStripMenuItem.Enabled = false;
            Home.button1.Enabled = false;
            Home.button1.ForeColor = Color.Black;
            Home.button1.BackColor = Color.Orange;
            Home.button2.Enabled = false;
            Home.button2.ForeColor = Color.Black;
            Home.button2.BackColor = Color.Orange;
            Home.button3.Enabled = false;
            Home.button3.ForeColor = Color.Black;
            Home.button3.BackColor = Color.Orange;
            Home.button4.Enabled = false;
            Home.button4.ForeColor = Color.Black;
            Home.button4.BackColor = Color.Orange;
            Home.button7.Enabled = false;
            Home.button7.ForeColor = Color.Black;
            Home.button7.BackColor = Color.Orange;
            Home.button10.Enabled = false;
            Home.button10.ForeColor = Color.Black;
            Home.button10.BackColor = Color.Orange;
            Home.button8.Enabled = false;
            Home.button8.ForeColor = Color.Black;
            Home.button8.BackColor = Color.Orange;
            Home.button6.Enabled = false;
            Home.button6.ForeColor = Color.Black;
            Home.button6.BackColor = Color.Orange;

        }
        public void getprintdetails()
        {
            using (var fileStream = new FileStream("printconfig.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DataSet ds = new DataSet();
                ds.ReadXml(fileStream);
                int i = 0;
                for (i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    //cameraid = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[0]);
                    printerip = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[0]);
                    printersharename = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[1]);

                }
            }
        }

        public static string bulk;
        public static string seconly;
        public static string pcronly;
        public static string onlineonly;
        private void Home_Load(object sender, EventArgs e)
        {
            label6.Text = "Ver." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            getprintdetails();
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Left = Top = 0;
            Width = Screen.PrimaryScreen.WorkingArea.Width;
            Height = Screen.PrimaryScreen.WorkingArea.Height;
          //  openclock();
            menuStrip1.BackColor = Color.White;//FromKnownColor(KnownColor.Control);
            lb_date.Text = DateTime.Now.ToString("dd-MM-yyyy");
            lb_time.Text = DateTime.Now.ToString("hh:mm:ss tt");
            lb_day.Text = DateTime.Today.DayOfWeek.ToString();
            //lb_date.Text = DateTime.Now.ToString("dd-MM-yyyy");
            //lb_time.Text = DateTime.Now.ToString("hh:mm:ss tt");
            //lb_day.Text = DateTime.Today.DayOfWeek.ToString();
            if (role == "Super Admin")
            {
                superAdminToolStripMenuItem.Visible = true;
                changeAdminPasswordToolStripMenuItem.Visible = true;
                superAdminToolStripMenuItem1.Visible = true;
            }
            else if (role == "Admin")
            {
                changeAdminPasswordToolStripMenuItem.Visible = true;
            }
            else
            {
                superAdminToolStripMenuItem.Visible = false;
                changeAdminPasswordToolStripMenuItem.Visible = false;
            }
            SqlCommand cmd = new SqlCommand("select isbulk2d from tbl_settings", Login.conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                bulk = dr[0].ToString();
            }
            dr.Close();
            if (bulk == "True")
            {
                performAggregationToolStripMenuItem.Visible = false;
            }
            else
            {
                performAggregationToolStripMenuItem.Visible = true;
            }
            SqlCommand cmd1 = new SqlCommand("select seconly from tbl_settings", Login.conn);
            SqlDataReader dr1 = cmd1.ExecuteReader();
            while (dr1.Read())
            {
                seconly = dr1[0].ToString();
            }
            dr1.Close();
            SqlCommand cmd12 = new SqlCommand("select pcronly from tbl_settings", Login.conn);
            SqlDataReader dr12 = cmd12.ExecuteReader();
            while (dr12.Read())
            {
                pcronly = dr12[0].ToString();
            }
            dr12.Close();
            SqlCommand cmd13 = new SqlCommand("select onlineonly from tbl_settings", Login.conn);
            SqlDataReader dr13 = cmd13.ExecuteReader();
            while (dr13.Read())
            {
                onlineonly = dr13[0].ToString();
            }
            dr13.Close();
            cmd = new SqlCommand("select hw_name from tbl_hwinfo where hw_type='Tertiary Printer'", Login.conn);
            dr = cmd.ExecuteReader();
            while(dr.Read())
            {
                terprintername = dr[0].ToString();
            }
            dr.Close();
        }
        public static string terprintername;
        private static void openclock()
        {
            // Home hm = new Home();
            var ca = Process.Start(".\\clock.exe");
            //SetParent(ca.MainWindowHandle, hm.Handle);
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private void productToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void productsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Products b = new Products();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();

        }

        private void batchReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            BatchReport brt = new BatchReport();
            brt.MdiParent = this;
            brt.Show();
            groupBox1.Hide();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void lineMasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Companydetails b = new Companydetails();

            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void plantMasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Plantmaster b = new Plantmaster();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void lineMasterToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Linemaster b = new Linemaster();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void packsMasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Packmaster b = new Packmaster();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void roleMasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Rolemaster b = new Rolemaster();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            DialogResult res = MsgBox.Show("Do you want to exit?", "Warning", MsgBox.Buttons.YesNo, MsgBox.Icon.Warning, MsgBox.AnimateStyle.FadeIn);
            if (res == DialogResult.Yes)
            {
                if (Home.role == "Super Admin")
                {
                    cmd = new SqlCommand("insert into tbl_SuperAdmin(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " logged out from the system','Logout Successfully','"+Login.cmpyid+"')", Login.conn);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " logged out from the system','Logout Successfully','"+Login.cmpyid+"')", Login.conn);
                    cmd.ExecuteNonQuery();
                }
                Login.conn.Close();

                Login obj = new TNT.Login();
                obj.Show();
                this.Hide();
                closeclock();
            }
            else
            {
                Home.groupBox1.Visible = true;
            }
        }

        private void closeclock()
        {
            try
            {
                if (System.Diagnostics.Process.GetProcessesByName("clock").Count() > 0)
                {
                    System.Diagnostics.Process asd = System.Diagnostics.Process.GetProcessesByName("clock").First();
                    asd.Kill();
                }
            }
            catch
            {

            }
        }

        private void brandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Brandmaster b = new Brandmaster();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //   lb_time.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MsgBox.Show("Contact Administrator for Restore the DATA","Information",MsgBox.Buttons.OK,MsgBox.Icon.Info,MsgBox.AnimateStyle.FadeIn);
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MsgBox.Show("Are you Sure want to Database Backup?", "Warning", MsgBox.Buttons.YesNo,MsgBox.Icon.Warning,MsgBox.AnimateStyle.FadeIn);
            if (dialogResult == DialogResult.Yes)
            {
                ProgressDialog progressDialog1 = new ProgressDialog();
                Thread backgroundThread = new Thread(
                new ThreadStart(() =>
                {
                    progressDialog1.SetIndeterminate(true);
                    generatebackup();
                    this.BeginInvoke((Action)(() =>
                    {

                        progressDialog1.Close();
                        if (backupdone)
                        {
                            MsgBox.Show("Backup Successfully Done.", "Information", MsgBox.Buttons.OK, MsgBox.Icon.Info, MsgBox.AnimateStyle.FadeIn);
                            backupdone = false;
                        }
                        else if(!backupdone)
                        {
                            MsgBox.Show("Backup Drive not Found or Drive Access Denied", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                        }

                    }));
                }));


                backgroundThread.Start();
                progressDialog1.ShowDialog();            
            }
        }
        bool backupdone = false;
        string databasename;
        private void generatebackup()
        {
            using (FileStream fs = new FileStream("dbconfig.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DataSet ds = new DataSet();
                ds.ReadXml(fs);
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    location = ds.Tables[0].Rows[i].ItemArray[5].ToString();
                    databasename = ds.Tables[0].Rows[i].ItemArray[2].ToString();
                }
            }

            try
            {
                SqlCommand cmd = new SqlCommand("GetBackup", Login.conn);
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@location", SqlDbType.NVarChar).Value = location;
                cmd.Parameters.Add("@databasename", SqlDbType.NVarChar).Value = databasename;
                string dt = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                string dz = dt.Replace("-", "_");
                string dz1 = dz.Replace(":", "_");
                string dz2 = dz1.Replace(" ", "_");
                cmd.Parameters.Add("@time", SqlDbType.NVarChar).Value = dz2;
                cmd.ExecuteNonQuery();

                backupdone = true;

                cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " tooked backup of database','Backup Successfully')", Login.conn);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                backupdone = false;
                cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + "backup drive not found','Backup is not Successfull')", Login.conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void companyDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Companydetails b = new Companydetails();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void usersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Users b = new Users();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        private void createBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(1, 0, 0, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void verifyBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 1, 0, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void configureBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 1, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void startBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 1, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void pauseBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 1, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void closeBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 0, 1, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void viewBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 0, 0, 1, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void batchRelationshipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 0, 0, 0, 1);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void performAggregationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            PerformAggregration pa = new PerformAggregration();
            pa.MdiParent = this;
            pa.Show();
            groupBox1.Hide();


        }

        private void offlinePrintingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            OfflinePrinting op = new OfflinePrinting();
            op.MdiParent = this;
            op.Show();
            groupBox1.Hide();
        }

        private void productReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            ProductReport pr = new ProductReport();
            pr.MdiParent = this;
            pr.Show();
            groupBox1.Hide();
        }

        private void changeAdminPasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            AdminPass ap = new AdminPass();
            ap.MdiParent = this;
            ap.Show();
            groupBox1.Hide();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Settings st = new Settings();
            st.MdiParent = this;
            st.Show();
            groupBox1.Hide();
        }

        private void Home_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Products b = new Products();
            b.MdiParent = this;
            b.Show();
            groupBox1.Hide();
        }

        

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(1, 0, 0, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

      

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 1, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

       

        private void button5_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            DialogResult res = MsgBox.Show("Do you want to exit", "Warning", MsgBox.Buttons.YesNo, MsgBox.Icon.Warning, MsgBox.AnimateStyle.FadeIn);
            if (res == DialogResult.Yes)
            {
                cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " logged out from the system','Logout Successfully')", Login.conn);
                cmd.ExecuteNonQuery();
                Login.conn.Close();

                Login obj = new TNT.Login();
                obj.Show();
                this.Hide();
            }
            else
            {

            }
        }

       

       

        private void auditTrailToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void dAVAUploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Form2 fm2 = new Form2();
            fm2.MdiParent = this;
            fm2.Show();
            groupBox1.Hide();
        }

       

        private void usersReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Auditscreen asc = new Auditscreen();
            asc.MdiParent = this;
            asc.Show();
            groupBox1.Hide();
        }

        private void batchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Batchscreen bsc = new Batchscreen();
            bsc.MdiParent = this;
            bsc.Show();
            groupBox1.Hide();
        }

        private void productToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Productscreen psc = new Productscreen();
            psc.MdiParent = this;
            psc.Show();
            groupBox1.Hide();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 1, 0, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 1, 0, 0, 0, 0, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 0, 0, 0, 1);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            Form2 fm2 = new Form2();
            fm2.MdiParent = this;
            fm2.Show();
            groupBox1.Hide();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Calc");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Notepad.exe");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process[] p = Process.GetProcessesByName("Calc");
            if (p.Length >= 1)
            {
                foreach (Process ps in p)
                {
                    ps.Kill();
                }
                Process.Start("Calc.exe");
            }
            else
            {
                System.Diagnostics.Process.Start("Calc");
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Notepad.exe");
        }

        private void button2_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button2.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);

        }

        private void button3_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button3.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button4_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button4.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button7_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button7.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button1_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button1.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button8_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button8.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button6_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button6.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            DialogResult res = MsgBox.Show("Do you want to exit ?", "Warning", MsgBox.Buttons.YesNo, MsgBox.Icon.Warning, MsgBox.AnimateStyle.FadeIn);
            if (res == DialogResult.Yes)
            {
                if (Home.role == "Super Admin")
                {
                    cmd = new SqlCommand("insert into tbl_SuperAdmin(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " logged out from the system','Logout Successfully','"+Login.cmpyid+"')", Login.conn);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + name + " logged out from the system','Logout Successfully','"+Login.cmpyid+"')", Login.conn);
                    cmd.ExecuteNonQuery();
                }
                Login.conn.Close();

                Login obj = new TNT.Login();
                obj.Show();
                this.Hide();
                closeclock();
                
            }
            else
            {

            }
        }

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        private const int SW_MAXIMIZE = 1;
        private string role1;
        private string user;
        public static string printerip;
        public static string printersharename;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int CmdShow);


        private void calculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (System.Diagnostics.Process.GetProcessesByName("Calc").Count() > 0)
            {
                var p = System.Diagnostics.Process.GetProcessesByName("Calc").FirstOrDefault();
                if (p != null)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    ShowWindow(p.MainWindowHandle,SW_MAXIMIZE);
                }
            }
            else
            {
                Process.Start("Calc.exe");
            }
        }

        private void notepadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessesByName("Notepad").Count() > 0)
            {
                var p = System.Diagnostics.Process.GetProcessesByName("Notepad").FirstOrDefault();
                if (p != null)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    ShowWindow(p.MainWindowHandle, SW_MAXIMIZE);
                }
            }
        
            else
            {
                Process.Start("Notepad.exe");
            }
        }

        private void calendarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            monthCalendar1.Visible = true;
            button11.Visible = true;
        }

        private void button9_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button9.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button10_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button10.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            Batchmanager bm = new Batchmanager(0, 0, 0, 0, 0, 0, 1, 0);
            bm.MdiParent = this;
            bm.Show();
            groupBox1.Hide();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            monthCalendar1.Visible = false;
            button11.Visible = false;
        }

        private void hardwareSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }

            hwinfo hw = new hwinfo();
            hw.MdiParent = this;
            hw.Show();
            groupBox1.Hide();
        }

        private void lineHardwareRelationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void lineHardwareRelationToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            linehwrelationship lhr = new linehwrelationship();
            lhr.MdiParent = this;
            lhr.Show();
            groupBox1.Hide();
        }

        private void superAdminToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            SAscreen sc = new SAscreen();
            sc.MdiParent = this;
            sc.Show();
            groupBox1.Hide();
        }

        private void labelLayoutPrintingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            lld ld = new lld();
            ld.MdiParent = this;
            ld.Show();
            groupBox1.Hide();
        }

        private void dAVAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form c in this.MdiChildren)
            {
                c.Close();
            }
            DAVAscreen dsc = new DAVAscreen();
            dsc.MdiParent = this;
            dsc.Show();
            groupBox1.Hide();
        }

        private void superAdminToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            lb_time.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }
    }
}
