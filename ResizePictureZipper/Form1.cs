using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResizePictureZipper
{
    public partial class Form1 : Form
    {
        private readonly long quality = 100;
        private EncoderParameters eps;
        private EncoderParameter ep;
        private ImageCodecInfo ici;

        public Form1()
        {
            InitializeComponent();
            JpegQualitySetting();
        }
        private void JpegQualitySetting()
        {
            //EncoderParameterオブジェクトを1つ格納できる
            //EncoderParametersクラスの新しいインスタンスを初期化
            //ここでは品質のみ指定するため1つだけ用意する
            eps = new EncoderParameters(1);
            //品質を指定
            ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
            //EncoderParametersにセットする
            eps.Param[0] = ep;
            //イメージエンコーダに関する情報を取得する
            ici = GetEncoderInfo("image/jpeg");
        }
        //MimeTypeで指定されたImageCodecInfoを探して返す
        private static ImageCodecInfo GetEncoderInfo(string mineType)
        {
            //GDI+ に組み込まれたイメージ エンコーダに関する情報をすべて取得
            return ImageCodecInfo.GetImageEncoders().ToList().Find(enc => enc.MimeType == mineType);
        }

        //ImageFormatで指定されたImageCodecInfoを探して返す
        private static ImageCodecInfo GetEncoderInfo(System.Drawing.Imaging.ImageFormat f)
        {
            return ImageCodecInfo.GetImageEncoders().ToList().Find(enc => enc.FormatID == f.Guid);
        }
        private void changeSizeImageInZipFile(string FileName)
        {
            using (var zto = new FileStream(FileName, FileMode.Open))
            {
                using (var zipArc = new ZipArchive(zto, ZipArchiveMode.Update))
                {
                    zipArc.Entries.ToList().ForEach(zipEntity =>
                    {
                        var imageFileName = zipEntity.FullName;
                        using (var stream = zipEntity.Open())
                        {
                            Image img = Image.FromStream(stream);

                            //描画先とするImageオブジェクトを作成する
                            Bitmap canvas = new Bitmap((1920 - (138 * 2)), 1200);
                            //ImageオブジェクトのGraphicsオブジェクトを作成する
                            Graphics g = Graphics.FromImage(canvas);
                            var srcRect = new Rectangle(138, 0, (1920 - (138 * 2)), 1200);
                            var destRect = new Rectangle(0, 0, (1920 - (138 * 2)), 1200);
                            g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
                            g.Dispose();

                            var memStream = new MemoryStream();
                            canvas.Save(memStream, ici, eps);
                            long len = memStream.Length;
                            int baseSize = int.MaxValue;
                            int offset = 0;
                            var buf = memStream.ToArray();
                            var entry = zipArc.CreateEntry(imageFileName);
                            using (var writer = entry.Open())
                            {
                                while (len > 0)
                                {
                                    int wlen = len > baseSize ? baseSize : (int)len;
                                    writer.Write(buf, offset, wlen);
                                    len -= wlen;
                                    offset += wlen;
                                }
                            }
                        }

                        zipEntity.Delete();
                    });
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openDialog.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            changeSizeImageInZipFile(textBox1.Text);
        }
    }
}
