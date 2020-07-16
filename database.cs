using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace kinodrom_bot
{
    public class database
    {
        string cs = "";// "Data Source=SQL01AO\\ROV06VCDB02;Initial Catalog=VISTA;Integrated Security=false;User ID=VistaApp; Password=pu3HNrAxZSjeKXYA";

        public database(string _cs)
        {
            cs = _cs;
        }
        public class SessionInfo
        {
            public DateTime showtime;
            public int sessionId;
            public string FilmTitle;
        }

        public List<SessionInfo> GetSessions()
        {
            List<SessionInfo> session_pairs = new List<SessionInfo>();

            var sql = @"SELECT SN.Session_dtmShowing, SN.Session_lngSessionId, FM.Film_strTitle   
                    FROM tblSession SN 
                    JOIN tblFilm FM ON SN.Film_strCode = FM.Film_strCode
                    WHERE Session_strAttributes like '%CAR%' 
                    AND @timeFrom < Session_dtmShowing
                    AND @timeTo > Session_dtmShowing
                    AND SN.Session_strStatus = 'O'";

            //'2020-07-15 06:00:00.000'

            //var configuration = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            //.AddJsonFile("config.json", false)
            //.Build();

            //var connectionString = configuration.GetSection("connectionString").Value;

            if (string.IsNullOrEmpty(cs))
                throw new ArgumentException("No connection string in config.json");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("timeFrom",  DateTime.Today.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("timeTo", DateTime.Today.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss"));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows) // если есть данные
                    {
                        while (reader.Read()) // построчно считываем данные
                        {
                            var showTime = reader.GetDateTime(0);
                            var SessionId = reader.GetInt32(1);
                            var filmTitle = reader.GetString(2);
                            session_pairs.Add(new SessionInfo { sessionId = SessionId, showtime = showTime, FilmTitle = filmTitle });
                        }
                    }
                }
                conn.Close();
            }
            return session_pairs;

        }

        public List<singleOrderInfo> GetKinodromOrders(int session_id)
        {
            List<singleOrderInfo> result = new List<singleOrderInfo>();

            var sql = @"SELECT BH.BookingH_strEmail, BH.BookingH_strPhone, BH.BookingH_intNextBookingNo, TI.TransI_lgnNumber, COUNT(TI.Item_strItemId), ITM.Item_strItemDescription 
                        FROM tblTrans_Inventory TI
                        JOIN tblBooking_Header BH ON TI.TransI_lgnNumber = BH.TransC_lgnNumber
                        JOIN tblItem  ITM ON ITM.Item_strItemId = TI.Item_strItemId
                        WHERE TransI_lgnNumber IN (SELECT TransC_lgnNumber FROM tblBooking_Header BH WHERE BH.BookingH_intNextBookingNo IN (
                            SELECT BD.BookingD_intNextBookingNo FROM tblBooking_Detail BD WHERE BD.Session_lngSessionId = @1 )) 
                        AND BH.BookingH_strStatus != 'C'
                        GROUP BY BookingH_strEmail, BookingH_strPhone, BH.BookingH_intNextBookingNo, TI.TransI_lgnNumber,ITM.Item_strItemDescription ";

            //var configuration = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            //.AddJsonFile("config.json", false)
            //.Build();

            //var connectionString = configuration.GetSection("connectionString").Value;

            if (string.IsNullOrEmpty(cs))
                throw new ArgumentException("No connection string in config.json");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("1", session_id);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows) // если есть данные
                    {
                        while (reader.Read()) // построчно считываем данные
                        {
                            singleOrderInfo ss = new singleOrderInfo();
                            ss.Email = reader.GetString(0);
                            ss.Phone = reader.GetString(1);
                            ss.BookingNumber = reader.GetInt32(2);
                            ss.TransactionNumber = reader.GetInt32(3);
                            var qty = reader.GetInt32(4);
                            var i_name = reader.GetString(5);

                            var lookup_tr = result.Find(x => x.TransactionNumber == ss.TransactionNumber);
                            if (lookup_tr == null)
                            {
                                ss.items.Add(new item_det { qty = qty, name = i_name });
                                result.Add(ss);
                            }
                            else
                            {
                                lookup_tr.items.Add(new item_det { qty = qty, name = i_name });
                            }
                        }
                    }
                }
                conn.Close();
            }
            return result;
        }

        public List<singleOrderInfo> GetKinodromOrders_seats(List<singleOrderInfo> data)
        {
            List<int> bnum = new List<int>();
            data.ForEach(d => bnum.Add(d.BookingNumber));
            if (data.Count == 0)
            {
                return data;
            }
            var sql = @"SELECT BD.BookingD_intNextBookingNo, ScreenD_strPhyRowId, ScreenD_strSeatId, TC.TransC_curValue, SN.Session_dtmRealShow, FM.Film_strTitle   
                    FROM tblBooking_Detail BD
                    JOIN tblBooking_Header BH on BD.BookingD_intNextBookingNo = BH.BookingH_intNextBookingNo
                    JOIN tblTrans_Cash TC on BH.TransC_lgnNumber = TC.TransC_lgnNumber
                    JOIN tblSession SN ON BD.Session_lngSessionId = SN.Session_lngSessionId
                    JOIN tblFilm FM ON SN.Film_strCode =  FM.Film_strCode 
                    WHERE BD.BookingD_intNextBookingNo IN (" + string.Join(", ", bnum.ToArray()) + ")";

            if (string.IsNullOrEmpty(cs))
                throw new ArgumentException("No connection string in config.json");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    //cmd.Parameters.AddWithValue("1", string.Join(", ", bnum.ToArray()));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows) // если есть данные
                    {
                        while (reader.Read()) // построчно считываем данные
                        {
                            var booking_num = reader.GetInt32(0);
                            var row = Convert.ToInt32(reader.GetString(1));
                            var seat = Convert.ToInt32(reader.GetString(2));
                            var price =  reader.GetDecimal(3);
                            var showtime = reader.GetDateTime(4);
                            var fTitle = reader.GetString(5);

                            var lookup_tr = data.Find(x => x.BookingNumber == booking_num);
                            lookup_tr.tickets.Add(new tic_det { RowId = row, SeatId = seat });
                            lookup_tr.total_price = price;
                            lookup_tr.ShowTime = showtime;
                            lookup_tr.FilmTitle = fTitle;
                        }
                    }
                }
                conn.Close();
            }
            return data;
        }

        public class singleOrderInfo
        {
            public string Email;
            public string Phone;
            public int BookingNumber;
            public int TransactionNumber;
            public List<item_det> items = new List<item_det>();
            public List<tic_det> tickets = new List<tic_det>();
            public decimal total_price;
            public DateTime ShowTime;
            public string FilmTitle;
        }
        public class item_det
        {
            public int qty;
            public string name;
        }
        public class tic_det
        {
            public int RowId;
            public int SeatId;
        }
    }
}
