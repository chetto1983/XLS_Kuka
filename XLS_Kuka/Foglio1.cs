using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace XLS_Kuka


/*
 * 
 This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * 
 */
{
    public partial class Foglio1
    {
        private static readonly Cl_Kuka_Con robot = new Cl_Kuka_Con();
        public static Boolean connected = false;
        private bool start = false;
        private static string[] tag;
        private static string[] value;
        
        System.Timers.Timer t;
        private object _lock = new object();

        public static object TASK { get; private set; }

        private void Foglio1_Startup(object sender, System.EventArgs e)
        {
            Bn_Start_stop.Visible = false;
            Bt_Con.BackColor = Color.Aquamarine;
            Bn_Start_stop.BackColor = Color.Aquamarine;


        }

        private void Foglio1_Shutdown(object sender, System.EventArgs e)
        {

        }

        #region Codice generato dalla finestra di progettazione di VSTO

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InternalStartup()
        {
            this.Bt_Con.Click += new System.EventHandler(this.Bn_Connection);
            this.Bn_Start_stop.Click += new System.EventHandler(this.Bn_Start_stop_click);
            this.Startup += new System.EventHandler(this.Foglio1_Startup);
            this.Shutdown += new System.EventHandler(this.Foglio1_Shutdown);

        }

        #endregion

        private void Bn_Connection(object sender, EventArgs e)
        {

            _ =  ConnectAsync();

            if (connected)
            {

                Bt_Con.BackColor = Color.Green;
                Bt_Con.Text = "Connected";
                Bn_Start_stop.Visible = true;


            }

            else
            {
              
                Bt_Con.BackColor = Color.LightGray;
                Bt_Con.Text = "Connect";
                Bt_Con.BackColor = Color.Aquamarine;
                Bn_Start_stop.Visible = false;

            }
        }


        private async System.Threading.Tasks.Task ConnectAsync()
        {

            string ip = Globals.Foglio1.Cells[4, 7].Value;


            if (!connected)
            {
                connected = await robot.connection(ip, 1000);
            }
            else 

            {

                bool close = robot.disconnection();
                connected = false;
            }

            







        }

        private  void Bn_Start_stop_click(object sender, EventArgs e)
        {

            Start();


        }


        private void Start()
        {
            start = ! start;

            if (start)
            {

                
                int interval = (int)Globals.Foglio1.Cells[5, 7].Value;
                t = new System.Timers.Timer(interval);
                t.AutoReset = true;
                t.Elapsed += new ElapsedEventHandler(Readvarasync);
                t.Start();
                Bn_Start_stop.Text = "Started";
                Bn_Start_stop.BackColor = Color.GreenYellow;
                Bt_Con.Enabled = false;

            }

            else
            {
                t.Stop();
                t.Dispose();
                Bn_Start_stop.Text = "Start";
                Bt_Con.Enabled = true;
                Bn_Start_stop.BackColor = Color.Aquamarine;



            }

        }

        private async void Readvarasync(Object source, ElapsedEventArgs e)
        {
               
                t.Stop();
                int counter_row = Globals.Foglio1.Tabella1.Range.Rows.Count;
                int Start_counter = 5;
                int pointer = 0;



                tag = new string[counter_row];

                for (int i = Globals.Foglio1.Rows.Row; i <= counter_row; i++)
                {
                try
                {

                    tag[pointer] = Globals.Foglio1.Cells[Start_counter, 2].value;
                    if (tag[pointer] == null)
                    {
                        break;
                    }
                    Start_counter++;
                    pointer++;
                }
                catch (Exception ex)
                {
                    break;
                }
                }



                value = new string[pointer];
                for (int i = 0; i < pointer; i++)
                {

                value[i] = robot.Read_var(tag[i]);
               

                }



                Start_counter = 5;
                for (int i = 0; i < pointer; i++)
                {
                try
                {
                    Globals.Foglio1.Cells[Start_counter, 3].value = value[i];
                    Start_counter++;
                }
                catch (Exception)
                {

                    break;
                }

                }

            if (start)
            {
                t.Start();
            }
        }

      
        
    }
}
