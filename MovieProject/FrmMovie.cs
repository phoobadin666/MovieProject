using System.Data.SqlClient;
using System.Data;
using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace MovieProject
{
    public partial class FrmMovie : Form
    {
        byte[] movieImage;
        byte[] movieDirectorImage;
        public FrmMovie()
        {
            InitializeComponent();
        }

        private Image convertByteArrayToImage(byte[] byteArrayIn)
        {
            if (byteArrayIn == null || byteArrayIn.Length == 0)
            {
                return null;
            }
            try
            {
                using (MemoryStream ms = new MemoryStream(byteArrayIn))
                {
                    return Image.FromStream(ms);
                }
            }
            catch (ArgumentException ex)
            {
                // อาจเกิดขึ้นถ้า byte array ไม่ใช่ข้อมูลรูปภาพที่ถูกต้อง
                Console.WriteLine("Error converting byte array to image: " + ex.Message);
                return null;
            }
        }
        private byte[] convertImageToByteArray(Image image, ImageFormat imageFormat)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, imageFormat);
                return ms.ToArray();
            }
        }
        private void getAllMovie()
        {
            string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                try
                {
                    sqlConnection.Open();

                    string strSQL = "SELECT movieId,movieImage, movieName, movieDetail, movieDate, movieType FROM movie_tb";

                    using (SqlDataAdapter dataAdapter = new SqlDataAdapter(strSQL, sqlConnection))
                    {
                        //เอาข้อมูลที่ได้จาก strSQL เป็นก้อนใน dataAdapter มาทำให้เป็นตารางโดยใส่ไว้ใน DataTable
                        DataTable dataTable = new DataTable();
                        dataAdapter.Fill(dataTable);

                        //ตั้งค่า ListView
                        lvShowAllMovie.Items.Clear();
                        lvShowAllMovie.Columns.Clear();
                        lvShowAllMovie.FullRowSelect = true;
                        lvShowAllMovie.View = View.Details;

                        if (lvShowAllMovie.SmallImageList == null)
                        {
                            lvShowAllMovie.SmallImageList = new ImageList();
                            lvShowAllMovie.SmallImageList.ImageSize = new Size(50, 50);
                            lvShowAllMovie.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
                        }
                        lvShowAllMovie.SmallImageList.Images.Clear();

                        lvShowAllMovie.Columns.Add("รูปภาพยนต์", 100, HorizontalAlignment.Left);
                        lvShowAllMovie.Columns.Add("ชื่อภาพยนต์", 100, HorizontalAlignment.Left);
                        lvShowAllMovie.Columns.Add("รายละเอียดหนัง", 150, HorizontalAlignment.Left);
                        lvShowAllMovie.Columns.Add("วันที่ฉาย", 200, HorizontalAlignment.Left);
                        lvShowAllMovie.Columns.Add("ประเภทภาพยนต์", 100, HorizontalAlignment.Left);

                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            ListViewItem item = new ListViewItem(); //สร้าง ITem เพื่อเก็บข้อมูลในแต่ละรายการ
                            //เอารูปใส่ใน Item
                            Image movieImage = null;
                            if (dataRow["movieImage"] != DBNull.Value)
                            {
                                byte[] imgByte = (byte[])dataRow["movieImage"];
                                //แปลงข้อมูลรูปจากฐานข้อมูล Binary ให้เป็นรูป
                                movieImage = convertByteArrayToImage(imgByte);
                            }
                            string imageKey = null;
                            if (movieImage != null)
                            {
                                imageKey = $"movie_{dataRow["movieId"]}";
                                lvShowAllMovie.SmallImageList.Images.Add(imageKey, movieImage);
                                item.ImageKey = imageKey;
                            }
                            else
                            {
                                item.ImageIndex = -1;
                            }
                            // เอาแต่ละรายการใส่ใน Item
                            item.SubItems.Add(dataRow["movieName"].ToString());
                            item.SubItems.Add(dataRow["movieDetail"].ToString());
                            item.SubItems.Add(dataRow["movieDate"].ToString());
                            item.SubItems.Add(dataRow["movieType"].ToString());

                            //เอาข้อมูลใน Item 
                            lvShowAllMovie.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);
                }
            }
        }

        private void FrmMovie_Load(object sender, System.EventArgs e)
        {
            getAllMovie();
            btUpdateMovie.Enabled = false;
            btDeleteMovie.Enabled = false;
            cbbMovieType.SelectedIndex = 0;
        }

        private void btSaveMovie_Click(object sender, EventArgs e)
        {
            //Validate 
            if (tbMovieName.Text.Trim() == "")
            {
                MessageBox.Show("กรุณากรอกชื่อภาพยนต์");
            }
            else if (tbMovieDetail.Text.Trim() == "")
            {
                MessageBox.Show("กรุณากรอกรายละเอียดภาพยนต์");
            }
            else if (nudMovieHour.Value == 0)
            {
                MessageBox.Show("กรุณาระบุชั่วโมงหนัง");
            }
            else if (pcbMovieDirectorImage == null)
            {
                MessageBox.Show("กรุณาระบุรูปภาพผู้กำกับภาพยนต์");
            }
            else if (pcbMovieImage == null)
            {
                MessageBox.Show("กรุณาระบุรูปภาพภาพยนต์");
            }
            else
            {
                string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS=movie_collection_db;Trusted_Connection=True";
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        sqlConnection.Open();

                        SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();


                        string strSQL = "INSERT INTO movie_tb (movieName, movieDetail, movieDate, movieHour, movieMinute, movieType, movieImage, movieDirectorImage) " +
                                         "VALUES (@movieName, @movieDetail, @movieDate, @movieHour, @movieMinute, @movieType, @movieImage, @movieDirectorImage)";

                        using (SqlCommand sqlCommand = new SqlCommand(strSQL, sqlConnection, sqlTransaction))
                        {
                            sqlCommand.Parameters.Add("@movieName", SqlDbType.NVarChar, 150).Value = tbMovieName.Text;
                            sqlCommand.Parameters.Add("@movieDetail", SqlDbType.NVarChar, 500).Value = tbMovieDetail.Text;
                            sqlCommand.Parameters.Add("@movieDate", SqlDbType.Date).Value = dtpMovieDate.Value.ToString();
                            sqlCommand.Parameters.Add("@movieHour", SqlDbType.Int).Value = nudMovieHour.Value.ToString();
                            sqlCommand.Parameters.Add("@movieMinute", SqlDbType.Int).Value = nudMovieMinute.Value.ToString();
                            sqlCommand.Parameters.Add("@movieType", SqlDbType.NVarChar, 150).Value = cbbMovieType.Text;
                            sqlCommand.Parameters.Add("@movieImage", SqlDbType.Image).Value = movieImage;
                            sqlCommand.Parameters.Add("@movieDirectorImage", SqlDbType.Image).Value = movieDirectorImage;

                            sqlCommand.ExecuteNonQuery();
                            sqlTransaction.Commit();


                            MessageBox.Show("บันทึกเรียบร้อย", "ผลการทำงาน", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            getAllMovie();
                            pcbMovieImage.Image = null;
                            pcbMovieDirectorImage.Image = null;
                            tbMovieName.Clear();
                            tbMovieDetail.Clear();
                            dtpMovieDate.Value = DateTime.Now;
                            nudMovieHour.Value = 0;
                            nudMovieMinute.Value = 0;
                            cbbMovieType.SelectedIndex = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);

                    }
                }
            }

        }

        private void btMovieImage_Click(object sender, EventArgs e)
        {
            //เปิด File Dialog  ให้เลือกรูปโดยฟิวเตอร์เฉพาะไฟล์ jpg/png
            //แล้วนำรูปทื่เลือกไปแสดงที่ pbMenuImage
            //แล้วแปลงเป็นร Binary/Byte เก็บในตัวแปรเพื่อเอาไว้บันทึก DB
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"C:\";
            openFileDialog.Filter = "Image File (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //เอารูปที่เลือกไปแสดงที่ pcbProImage
                pcbMovieImage.Image = Image.FromFile(openFileDialog.FileName);
                //ตรวจสอบ Format ของรูป แล้วส่งรูปไปแปลงเป็น Binary/Byte เก็บในตัวแปร
                if (pcbMovieImage.Image.RawFormat == ImageFormat.Jpeg)
                {
                    movieImage = convertImageToByteArray(pcbMovieImage.Image, ImageFormat.Jpeg);

                }
                else
                {
                    movieImage = convertImageToByteArray(pcbMovieImage.Image, ImageFormat.Png);
                }
            }
        }

        private void btMovieDirectorImage_Click(object sender, EventArgs e)
        {
            //เปิด File Dialog  ให้เลือกรูปโดยฟิวเตอร์เฉพาะไฟล์ jpg/png
            //แล้วนำรูปทื่เลือกไปแสดงที่ pbMenuImage
            //แล้วแปลงเป็นร Binary/Byte เก็บในตัวแปรเพื่อเอาไว้บันทึก DB
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"C:\";
            openFileDialog.Filter = "Image File (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //เอารูปที่เลือกไปแสดงที่ pcbProImage
                pcbMovieDirectorImage.Image = Image.FromFile(openFileDialog.FileName);
                //ตรวจสอบ Format ของรูป แล้วส่งรูปไปแปลงเป็น Binary/Byte เก็บในตัวแปร
                if (pcbMovieDirectorImage.Image.RawFormat == ImageFormat.Jpeg)
                {
                    movieDirectorImage = convertImageToByteArray(pcbMovieDirectorImage.Image, ImageFormat.Jpeg);

                }
                else
                {
                    movieDirectorImage = convertImageToByteArray(pcbMovieDirectorImage.Image, ImageFormat.Png);
                }
            }
        }

        private void btSearchMovie_Click(object sender, EventArgs e)
        {
            //Validate 
            if (tbSearchMovie.Text.Trim() == "")
            {
                MessageBox.Show("กรุณากรอกชื่อภาพยนต์ที่ต้องการค้นหา");
            }
            else
            {
                string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        string keyword = tbSearchMovie.Text.Trim();
                        string strSQL = "SELECT movieId, movieName FROM movie_tb WHERE movieName LIKE @keyword";

                        using (SqlCommand cmd = new SqlCommand(strSQL, sqlConnection))
                        {
                            cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");

                            using (SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd))
                            {
                                DataTable dataTable = new DataTable();
                                dataAdapter.Fill(dataTable);

                                // ตั้งค่า ListView
                                lvShowSearchMovie.Items.Clear();
                                lvShowSearchMovie.Columns.Clear();
                                lvShowSearchMovie.FullRowSelect = true;
                                lvShowSearchMovie.View = View.Details;

                                lvShowSearchMovie.Columns.Add("รหัสภาพยนต์", 80, HorizontalAlignment.Left);
                                lvShowSearchMovie.Columns.Add("ชื่อภาพยนต์", 100, HorizontalAlignment.Left);

                                foreach (DataRow row in dataTable.Rows)
                                {
                                    ListViewItem item = new ListViewItem(row["movieId"].ToString());
                                    item.SubItems.Add(row["movieName"].ToString());
                                    lvShowSearchMovie.Items.Add(item);
                                }

                                if (lvShowSearchMovie.Items.Count == 0)
                                {
                                    MessageBox.Show("ไม่พบภาพยนต์ที่ค้นหา");
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);

                    }

                }
            }
        }

        private void lvShowSearchMovie_ItemActivate(object sender, EventArgs e)
        {
            btSaveMovie.Enabled = false;
            btUpdateMovie.Enabled = true;
            btDeleteMovie.Enabled = true;

            if (lvShowSearchMovie.SelectedItems.Count > 0)
            {
                string movieID = lvShowSearchMovie.SelectedItems[0].SubItems[0].Text;

                string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        sqlConnection.Open();
                        string strSQL = "SELECT movieId, movieName, movieDetail, movieDate, movieHour, movieMinute, movieType, movieImage, movieDirectorImage FROM movie_tb WHERE movieId = @movieId";
                        using (SqlCommand sqlCommand = new SqlCommand(strSQL, sqlConnection))
                        {
                            sqlCommand.Parameters.AddWithValue("@movieId", movieID);

                            using (SqlDataReader reader = sqlCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {

                                    lbMovieId.Text = reader["movieId"].ToString();

                                    // TextBox
                                    tbMovieName.Text = reader["movieName"].ToString();
                                    tbMovieDetail.Text = reader["movieDetail"].ToString();

                                    // DateTimePicker
                                    dtpMovieDate.Value = Convert.ToDateTime(reader["movieDate"]);

                                    // NumericUpDown
                                    nudMovieHour.Value = Convert.ToInt32(reader["movieHour"]);
                                    nudMovieMinute.Value = Convert.ToInt32(reader["movieMinute"]);

                                    // ComboBox
                                    cbbMovieType.SelectedItem = reader["movieType"].ToString();

                                    // ภาพยนตร์
                                    byte[] movieImageBytes = reader["movieImage"] as byte[];
                                    if (movieImageBytes != null && movieImageBytes.Length > 0)
                                    {
                                        using (MemoryStream ms = new MemoryStream(movieImageBytes))
                                        {
                                            pcbMovieImage.Image = Image.FromStream(ms);
                                        }
                                    }
                                    else
                                    {
                                        pcbMovieImage.Image = null; // ใช้ภาพว่างจาก Resources
                                    }

                                    // ผู้กำกับ
                                    byte[] directorImageBytes = reader["movieDirectorImage"] as byte[];
                                    if (directorImageBytes != null && directorImageBytes.Length > 0)
                                    {
                                        using (MemoryStream ms = new MemoryStream(directorImageBytes))
                                        {
                                            pcbMovieDirectorImage.Image = Image.FromStream(ms);
                                        }
                                    }
                                    else
                                    {
                                        pcbMovieDirectorImage.Image = null;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);
                    }
                }
            }
        }

        private void reset()
        {
            lbMovieId.Text = "";
            tbMovieName.Clear();
            tbMovieDetail.Clear();
            dtpMovieDate.Value = DateTime.Now;
            nudMovieHour.Value = 0;
            nudMovieMinute.Value = 0;
            cbbMovieType.SelectedIndex = 0;
            pcbMovieImage.Image = null;
            pcbMovieDirectorImage.Image = null;
            btSaveMovie.Enabled = true;
            btUpdateMovie.Enabled = false;
            btDeleteMovie.Enabled = false;
            lvShowSearchMovie.Items.Clear();
            tbSearchMovie.Clear();
        }
        private void btDeleteMovie_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("ต้องการลบภาพยนต์หรือไม่", "ยีนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        sqlConnection.Open();

                        SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();

                        string strSQL = "DELETE FROM movie_tb WHERE movieId = @movieId";
                        using (SqlCommand sqlCommand = new SqlCommand(strSQL, sqlConnection, sqlTransaction))
                        {
                            sqlCommand.Parameters.Add("@movieId", SqlDbType.Int).Value = int.Parse(lbMovieId.Text);
                            sqlCommand.ExecuteNonQuery();
                            sqlTransaction.Commit();
                        }
                        MessageBox.Show("ลบภาพยนต์เรียบร้อย", "ผลการทำงาน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        getAllMovie();
                        reset();


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);
                    }
                }

            }
        }

        private void btUpdateMovie_Click(object sender, EventArgs e)
        {
            //Validate 
            if (tbMovieName.Text.Trim() == "")
            {
                MessageBox.Show("กรุณากรอกชื่อภาพยนต์");
            }
            else if (tbMovieDetail.Text.Trim() == "")
            {
                MessageBox.Show("กรุณากรอกรายละเอียดภาพยนต์");
            }
            else if (nudMovieHour.Value == 0)
            {
                MessageBox.Show("กรุณาระบุชั่วโมงหนัง");
            }
            else
            {
                string connectionString = @"Server=DESKTOP-G2RQ5QR\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        sqlConnection.Open();

                        SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();


                        string strSQL = "";

                        if (movieImage == null)
                        {
                            strSQL = "UPDATE movie_tb SET movieName = @movieName, movieDetail = @movieDetail, " +
                                     "movieDate = @movieDate, movieHour = @movieHour, movieMinute = @movieMinute, " +
                                     "movieType = @movieType, movieDirectorImage = @movieDirectorImage WHERE movieId = @movieId";
                        }
                        else if (movieDirectorImage == null)
                        {
                            strSQL = "UPDATE movie_tb SET movieName = @movieName, movieDetail = @movieDetail, " +
                                        "movieDate = @movieDate, movieHour = @movieHour, movieMinute = @movieMinute, " +
                                        "movieType = @movieType, movieImage = @movieImage WHERE movieId = @movieId";
                        }
                        else
                        {
                            strSQL = "UPDATE movie_tb SET movieName = @movieName, movieDetail = @movieDetail, " +
                                        "movieDate = @movieDate, movieHour = @movieHour, movieMinute = @movieMinute, " +
                                        "movieType = @movieType, movieImage = @movieImage, movieDirectorImage = @movieDirectorImage WHERE movieId = @movieId";

                        }

                        using (SqlCommand sqlCommand = new SqlCommand(strSQL, sqlConnection, sqlTransaction))
                        {
                            sqlCommand.Parameters.Add("@movieId", SqlDbType.Int).Value = int.Parse(lbMovieId.Text);
                            sqlCommand.Parameters.Add("@movieName", SqlDbType.NVarChar, 150).Value = tbMovieName.Text;
                            sqlCommand.Parameters.Add("@movieDetail", SqlDbType.NVarChar, 500).Value = tbMovieDetail.Text;
                            sqlCommand.Parameters.Add("@movieDate", SqlDbType.Date).Value = dtpMovieDate.Value.Date;
                            sqlCommand.Parameters.Add("@movieHour", SqlDbType.Int).Value = nudMovieHour.Value.ToString();
                            sqlCommand.Parameters.Add("@movieMinute", SqlDbType.Int).Value = nudMovieMinute.Value.ToString();
                            sqlCommand.Parameters.Add("@movieType", SqlDbType.NVarChar, 150).Value = cbbMovieType.Text;
                            if (movieImage != null)
                            {
                                sqlCommand.Parameters.Add("@movieImage", SqlDbType.Image).Value = movieImage;
                            }
                            if (movieDirectorImage != null)
                            {
                                sqlCommand.Parameters.Add("@movieDirectorImage", SqlDbType.Image).Value = movieDirectorImage;
                            }




                            sqlCommand.ExecuteNonQuery();
                            sqlTransaction.Commit();


                            MessageBox.Show("บันทึกเรียบร้อย", "ผลการทำงาน", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            getAllMovie();
                            reset();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("พบข้อผิดพลาด กรุณาลองใหม่หรือติดต่อ IT : " + ex.Message);

                    }
                }
            }
        }

        private void btResetMovie_Click(object sender, EventArgs e)
        {
            getAllMovie();
            reset();
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
