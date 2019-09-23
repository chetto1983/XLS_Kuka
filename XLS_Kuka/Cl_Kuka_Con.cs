using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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



/*
Read Request Message Format
---------------------------
2 bytes Id (uint16)
2 bytes for content length (uint16)
1 byte for read/write mode (0=Read)
2 bytes for the variable name length (uint16)
N bytes for the variable name to be read (ASCII)

Write Request Message Format
---------------------------
2 bytes Id (uint16)
2 bytes for content length (uint16)
1 byte for read/write mode (1=Write)
2 bytes for the variable name length (uint16)
N bytes for the variable name to be written (ASCII)
2 bytes for the variable value length (uint16)
M bytes for the variable value to be written (ASCII)

Answer Message Format
---------------------------
2 bytes Id (uint16)
2 bytes for content length (uint16)
1 byte for read/write mode (0=Read, 1=Write, 2=ReadArray, 3=WriteArray)
2 bytes for the variable value length (uint16)
N bytes for the variable value (ASCII)
3 bytes for tail (000 on error, 011 on success)


*/

namespace XLS_Kuka
{
    internal class Cl_Kuka_Con
    {
        private int MSG_id = 0;
        public  TcpClient con;



        public bool disconnection()
        {
            try
            {

                con.Close();
                con.Dispose();
                // con.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }


        }

        public async
        Task<bool>
 connection(String ip, int connectTimeout)
        {

            con = new TcpClient();
            var result = con.BeginConnect(ip, 7000, null, null);

            var success = result.AsyncWaitHandle.WaitOne(connectTimeout);
            return (bool)success;
        }


        public string
  Read_var(string pVarName)
        {

            if (!con.Connected)
            {
                return "error";

            }

            try
            {

                byte[] PKT_var_name;
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                PKT_var_name = enc.GetBytes(pVarName);
                byte[] PKT_name_length = new byte[2];
                PKT_name_length[0] = Convert.ToByte((pVarName.Length >> 8) & 255);
                PKT_name_length[1] = Convert.ToByte(pVarName.Length & 255);
                Byte PKT_mode_is_read = 0;
                byte[] PKT_req_len = new byte[2];
                PKT_req_len[0] = Convert.ToByte(((pVarName.Length + 3) >> 8) & 255);
                PKT_req_len[1] = Convert.ToByte(pVarName.Length + 3 & 255);
                byte[] PKT_req_id = new byte[2];
                MSG_id += 1;
                PKT_req_id[0] = Convert.ToByte((MSG_id >> 8) & 255);
                PKT_req_id[1] = Convert.ToByte(MSG_id + 3 & 255);

                short total_pkt_length = (short)(pVarName.Length + 7);
                byte[] REQ_packet = new byte[total_pkt_length];


                REQ_packet[0] = PKT_req_id[0];
                REQ_packet[1] = PKT_req_id[1];
                REQ_packet[2] = PKT_req_len[0];
                REQ_packet[3] = PKT_req_len[1];
                REQ_packet[4] = PKT_mode_is_read;
                REQ_packet[5] = PKT_name_length[0];
                REQ_packet[6] = PKT_name_length[1];
                PKT_var_name.CopyTo(REQ_packet, 7);


                var stm = con.GetStream();
                stm.Write(REQ_packet, 0, REQ_packet.Length);
                stm.Flush();


                byte[] RSP_packet = new byte[con.ReceiveBufferSize];
                // stm.Read(RSP_packet, 0, (int)(con.ReceiveBufferSize));
                var result = stm.BeginRead(RSP_packet, 0, (int)(con.ReceiveBufferSize), null, null);
                bool data_recive = false;
                var success = result.AsyncWaitHandle.WaitOne(20);
                data_recive = (bool)success;

                /*   Read Variable response packet structure example:
               '  0  1     2  3      4         5  6          
               ' xx xx  | 00 0A   | 00      | 00 06       | 35 35 33 39 39 33 | 00 01 01
               '        |   10    |  0      |     6       | 5  5  3  9  9  3  |  0  1  1
               'SAME AS | RSP LEN | READ=00 | VALUE LEN   | VALUE CHARS       |  TRAILER
               'REQUEST | */

                if (success)
                {

                    short RSP_val_len = (short)(((RSP_packet[5] << 8) & 255) + RSP_packet[6]);
                    string RSP_val_payload;
                    RSP_val_payload = Encoding.ASCII.GetString(RSP_packet, 7, RSP_val_len);
                    int RSP_read_status = RSP_packet[7 + RSP_val_len + 2];




                    bool Ok = ((RSP_read_status > 0) && (RSP_val_len > 0)&& (RSP_val_len > 0) && (RSP_packet[0] == PKT_req_id[0]) && (RSP_packet[1] == PKT_req_id[1]));
                    
                    if (Ok)
                    {
                        return RSP_val_payload;
                    }
                    else
                    {

                        return "Error";
                    }



                }

                else
                {

                    return "error";

                }


            }

            catch (Exception Ex)
            {

                return "error";

            }

        }


        public bool Write_var(String pVarName, String pValue)
        {
            /*
        'Write Variable request packet structure example:
        '  0  1     2  3      4         5  6  
        ' xx xx  | 00 0F   | 01       | 00 07        | 24 4F 56 5F 50 52 4F | 00 03   |     31 32 33
        '        |   15    |  1       |     7        | $  O  V  _  P  R  O  |     3   |      1  2  3
        ' REQ ID | REQ LEN | WRITE=1  | VAR NAME LEN | VAR NAME CHARS       | VAL LEN | VAL AS STRING
        '(RANDOM)|
        */


            if (!con.Connected)
            {

                return false;
            }

            try
            {

                byte[] PKT_value;
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                PKT_value = enc.GetBytes(pValue);


                byte[] PKT_value_len = new byte[2];
                PKT_value_len[0] = Convert.ToByte((pValue.Length >> 8) & 255);
                PKT_value_len[1] = Convert.ToByte(pValue.Length & 255);

                byte[] PKT_var_name;
                PKT_var_name = enc.GetBytes(pVarName);

                byte[] PKT_name_length = new byte[2];
                PKT_name_length[0] = Convert.ToByte((pVarName.Length >> 8) & 255);
                PKT_name_length[1] = Convert.ToByte(pVarName.Length & 255);

                byte PKT_mode_is_write = 1;

                byte[] PKT_req_id = new byte[2];
                MSG_id++;
                PKT_req_id[0] = Convert.ToByte((MSG_id >> 8) & 255);
                PKT_req_id[1] = Convert.ToByte(MSG_id + 3 & 255);

                byte[] PKT_req_len = new byte[2];
                PKT_req_len[0] = Convert.ToByte(((2 + PKT_var_name.Length + 2 + PKT_value.Length + 3) >> 8) & 255);
                PKT_req_len[1] = Convert.ToByte((2 + PKT_var_name.Length + 2 + PKT_value.Length + 3) & 255);

                byte[] REQ_packet = new byte[5 + 2 + PKT_var_name.Length + 2 + PKT_value.Length - 1];

                REQ_packet[0] = PKT_req_id[0];
                REQ_packet[1] = PKT_req_id[1];
                REQ_packet[2] = PKT_req_len[0];
                REQ_packet[3] = PKT_req_len[1];
                REQ_packet[4] = PKT_mode_is_write;
                REQ_packet[5] = PKT_name_length[0];
                REQ_packet[6] = PKT_name_length[1];
                PKT_var_name.CopyTo(REQ_packet, 7);
                PKT_value_len.CopyTo(REQ_packet, 7 + PKT_var_name.Length);
                PKT_value.CopyTo(REQ_packet, 7 + PKT_var_name.Length + PKT_value_len.Length - 1);


                var ServerStream = con.GetStream();
                ServerStream.Write(REQ_packet, 0, REQ_packet.Length);
                ServerStream.Flush();
                /*
                'Write Variable response packet structure example:
                '  0  1     2  3         4      5  6  
                ' xx xx  | 00 0A   |    01   | 00 06       | 35 35 33 39 39 33   | 00  | 01 01
                '        |    10   |     1   |     6       |  5  5  3  9  9  3   |  0  |  1  1
                'SAME AS | RSP LEN | WRITE=1 | VALUE LEN   | WRITTEN VALUE CHARS | PAD | READ status 01 01 = OK
                'REQUEST |
                */


                byte[] RSP_packet = new byte[con.ReceiveBufferSize];
                var result = ServerStream.BeginRead(RSP_packet, 0, (int)(con.ReceiveBufferSize), null, null);
                var success = result.AsyncWaitHandle.WaitOne(20);



                if (success)
                {

                    short RSP_val_len = (short)(((RSP_packet[5] << 8) & 255) + RSP_packet[6]);
                    string RSP_val_payload;
                    RSP_val_payload = Encoding.ASCII.GetString(RSP_packet, 7, RSP_val_len);
                    int RSP_read_status = RSP_packet[7 + RSP_val_len + 2];

                    return ((RSP_read_status > 0) && (RSP_packet[0] == PKT_req_id[0]) && (RSP_packet[1] == PKT_req_id[1]));


                }
                else
                {
                    return false;
                }


            }
            catch (Exception ex)
            {
                return false;

            }
        }



    }





}

