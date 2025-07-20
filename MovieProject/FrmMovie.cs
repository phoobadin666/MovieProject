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
            string connectionString = @"Server=DESKTOP-9U4FO0V\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
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
                string connectionString = @"Server=DESKTOP-9U4FO0V\SQLEXPRESS;Database=movie_collection_db;Trusted_Connection=True";
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
    }
}
