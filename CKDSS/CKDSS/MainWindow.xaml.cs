using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using DocumentFormat.OpenXml;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Security.Cryptography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace CKDSS
{
    public partial class MainWindow : Window
    {
        //khởi tạo biến khóa private và public
        private RSAParameters _privateKey;
        private RSAParameters _publicKey;

        private string _HBsave;
        private string _CKsave;
        private bool check;
        public MainWindow()
        {
            InitializeComponent();
            //gọi phương thức sinh khóa để tạo khóa khi bắt đầu chương trình
            GenerateKeys();
        }

        //--------------Phần các hàm liên quan đến ký chữ ký(DSS)--------------------
        //Phướng thức sinh khóa private và public
        private void GenerateKeys()
        {
            //sử dụng thư viện thuật toán RSA
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                //khởi tạo key public và key private
                _privateKey = rsa.ExportParameters(true);
                _publicKey = rsa.ExportParameters(false);
            }
        }

        //Phương thức ký chữ ký
        static byte[] SignData(byte[] data, RSAParameters privateKey)
        {
            //dùng thư viện thuật toán RSA để ký
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                //tải khóa private đã tạo từ trước
                rsa.ImportParameters(privateKey);
                return rsa.SignData(data, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
            }
        }

        //Phương thức kiếm tra chữ ký có hợp lệ hay ko
        static bool VerifySignature(byte[] data, byte[] signature, RSAParameters publicKey)
        {
            //dùng thư viện thuật toán RSA để kiểm tra chữ ký
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                //tải khóa public đã tạo từ trước
                rsa.ImportParameters(publicKey);
                return rsa.VerifyData(data, signature, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
            }
        }

        //Phương thức này để chuyển chuỗi string sang mảng byte để tiện cho việc kiểm tra chữ ký
        static byte[] HexStringToByteArray(string hex)
        {
            //thay đổi 1 số ký tự đặc biệt trong chuỗi string để có thể chuyển đổi chuỗi string thành mảng byte
            hex = hex.Replace("-", "").Replace(" ", "").Replace("\n", "").Replace("\r", "");

            //nếu thay đổi chuỗi hệ thập lục nhị phân sẽ báo lỗi
            if (hex.Length % 2 != 0)
            {
                MessageBox.Show("Chuỗi hệ thập lục phân không đúng định dạng", "Lỗi chữ ký");
            }

            //chuyển đổi từ chuỗi string sang mảng byte
            byte[] byteArray = new byte[hex.Length / 2];
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return byteArray;
        }

        private void checkhex(string sig,string _CK)
        {
            string normalizedSigned = sig.Replace("-", "").Replace("\n", "").Replace("\r", "").ToUpper();
            string normalizedCKsave = _CK.Replace("-", "").Replace("\n", "").Replace("\r", "").ToUpper();

            if (normalizedSigned == normalizedCKsave)
            {
                check = true;
            }
            else
            {
                Console.WriteLine("The hex strings are different.");
                for (int i = 0; i < Math.Min(normalizedSigned.Length, normalizedCKsave.Length); i += 2)
                {
                    string segmentSigned = normalizedSigned.Substring(i, 2);
                    string segmentCKsave = normalizedCKsave.Substring(i, 2);
                }

                if (normalizedSigned.Length != normalizedCKsave.Length)
                {
                    check = false;
                }
            }
        }

        //Phương thức này gọi lại hàm ký và kiểm tra chữ ký, dùng thư viện md5 để băm văn bản ký và ký hoặc kiểm tra chữ ký tùy theo ấn nút nào
        private void DSS(string data, bool signOrCheck, string signed)
        {
            //dùng thư viện thuật MD5 để có thể băm văn bản chữ ký
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashedData = md5.ComputeHash(Encoding.UTF8.GetBytes(data));

                //hiển thị hàm đã băm ra ô text box tùy theo quy trình đang làm
                if (signOrCheck)
                    HBPhatsinh.Text = BitConverter.ToString(hashedData);
                else
                    HBKiemtra.Text = BitConverter.ToString(hashedData);

                //ký văn bản cần ký
                if (signOrCheck)
                {
                    byte[] signature = SignData(hashedData, _privateKey);
                    CKPhatsinh.Text = BitConverter.ToString(signature);
                }
                //kiểm tra chữ ký
                else
                {
                    byte[] checkSignature = HexStringToByteArray(signed);
                    bool signatureValid = VerifySignature(hashedData, checkSignature, _publicKey);

                    checkhex(signed,_CKsave);

                    //hiển thị thông báo chữ ký sai hay đúng
                    if (signatureValid)                     
                        Thongbao.Text = "Chữ ký đúng! \r\r";
                    else
                    {
                        if (check)
                            Thongbao.Text = "Chữ ký đúng! \r\r";
                        else
                            Thongbao.Text = "Chữ ký sai! \r\r";
                    }                     
                }
            }
        }

        //--------------Phần các hàm sử lý nút ấn--------------------
        //Phương thức chuyển văn bản từ văn bản ký và chữ ký bên phát sinh chữ ký sang bên kiểm tra chữ ký
        private void Chuyen(object sender, RoutedEventArgs e)
        {

            //chuyển văn bản của ô văn bản ký bên phát sinh chữ ký sang bên kiểm tra chữ ký
            var sourceBlocks = VBkyPhatsinh.Document.Blocks.ToList();
            VBkyKiemtra.Document.Blocks.Clear();
            foreach (var block in sourceBlocks)
            {
                VBkyKiemtra.Document.Blocks.Add(block);
            }

            //chuyển văn bản của ô chữ ký bên phát sinh chữ ký sang bên kiểm tra chữ ký
            CKKiemtra.Document.Blocks.Clear();
            CKKiemtra.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(CKPhatsinh.Text)));
            CKPhatsinh.Text = "";

            //lưu lại văn bản hàm băm để cho việc so sánh 2 hàm băm của 2 bên
            HBPhatsinh.Text = "";
        }

        //Phương thức ấn nút ký để tạo chữ ký
        private void Ky(object sender, RoutedEventArgs e)
        {
            //gọi đến phương thức DSS để tạo chữ ký
            TextRange textRange = new TextRange(VBkyPhatsinh.Document.ContentStart, VBkyPhatsinh.Document.ContentEnd);
            DSS(textRange.Text, true, "");
            _HBsave = HBPhatsinh.Text;
            _CKsave = CKPhatsinh.Text;
        }

        //Phương thức ấn nút kiểm tra chữ ký
        private void Kiemtrachuky(object sender, RoutedEventArgs e)
        {
            //gọi đến phương thức DSS để kiểm tra chữ ký
            TextRange VBkytext = new TextRange(VBkyKiemtra.Document.ContentStart, VBkyKiemtra.Document.ContentEnd);
            TextRange CKtext = new TextRange(CKKiemtra.Document.ContentStart, CKKiemtra.Document.ContentEnd);
            DSS(VBkytext.Text, false, CKtext.Text);

            //kiểm tra xem chữ ký đã bị thay đổi hay không
            if (HBKiemtra.Text != _HBsave)
                Thongbao.Text += "Văn bản ký đã được sửa đổi!";
            else
                Thongbao.Text += "Văn bản ký không thay đổi!";
        }

        //--------------Phần các hàm liên quan đến mở file để up vào textbox--------------------
        //Phương thức mở folder file .docx hoặc file .txt
        private void MoFile(object sender, RoutedEventArgs e)
        {
            //chỉ đến nút và ô textbox cần upload file
            Button button = sender as Button;
            RichTextBox targetRichTextBox = button?.Tag as RichTextBox;

            //lỗi nếu textbox ko được tìm thấy(thường do lỗi code)
            if (targetRichTextBox == null)
            {
                MessageBox.Show("No target TextBox found.");
                return;
            }

            // mở cửa sổ folder để chọn file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                //filter các file cần thiết gồm .txt và .docx
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Word documents (*.docx)|*.docx",
                FilterIndex = 1,
                Multiselect = false
            };

            // chọn file và kiểm tra nếu đúng định dạng file hay không
            if (openFileDialog.ShowDialog() == true)
            {
                //lấy ra vị trí file và định dạng file
                string filePath = openFileDialog.FileName;
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

                // gắn nội dung đọc được trong file vào textbox
                if (fileExtension == ".txt")
                {
                    targetRichTextBox.Document.Blocks.Clear();
                    targetRichTextBox.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(File.ReadAllText(filePath))));
                }
                else if (fileExtension == ".docx")
                {
                    //gọi lại phương thức đọc file .docx
                    ReadWordDocument(filePath, targetRichTextBox);
                }
                else
                {
                    //nếu không đúng định dạng file sẽ trả về thông báo lỗi
                    MessageBox.Show("File không được hỗ trợ.");
                }
            }
        }

        //Phương thức đọc file .docx
        private void ReadWordDocument(string filePath, RichTextBox richTextBox)
        {
            //dùng thư viện đọc file word để có thể đọc file word
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {          
                //khởi tạo 1 biến để chứ nội dung đọc được và gắn vào ô textbox
                Body body = wordDoc.MainDocumentPart.Document.Body;
                richTextBox.Document.Blocks.Clear();

                foreach (var para in body.Elements<Paragraph>())
                {
                    //thư viên Documents của Windows
                    System.Windows.Documents.Paragraph wpfPara = new System.Windows.Documents.Paragraph();
                    foreach (var run in para.Elements<Run>())
                    {
                        //thư viên Documents của Windows
                        System.Windows.Documents.Run frun = new System.Windows.Documents.Run(run.InnerText);

                        if (run.RunProperties != null)
                        {
                            // Lấy kích thước font từ RunProperties
                            string fontSize = run.RunProperties.FontSize?.Val;
                            if (fontSize != null)
                            {
                                // Chuyển đổi kích thước font từ chuỗi sang double
                                double size;
                                if (double.TryParse(fontSize, out size))
                                {
                                    // Thiết lập kích thước font cho TextBox
                                    frun.FontSize = size/1.5;
                                }
                            }

                            //đọc các định dạng khác của file .docx
                            if (run.RunProperties.Bold != null)
                            {
                                frun.FontWeight = FontWeights.Bold;
                            }

                            if (run.RunProperties.Italic != null)
                            {
                                frun.FontStyle = FontStyles.Italic;
                            }
                            
                            if (run.RunProperties.Underline != null)
                            {
                                frun.TextDecorations = TextDecorations.Underline;
                            }

                            //đọc định dang màu của file word
                            if (run.RunProperties.Color != null)
                            {
                                var Color = run.RunProperties.Color.Val;
                                System.Windows.Media.Color color = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#" + Color);
                                frun.Foreground = new SolidColorBrush(color);
                            }

                            //đọc định dạng phông chữ của file
                            if (run.RunProperties.RunFonts != null)
                            {
                                // Lấy tên font family cho ASCII
                                string fontFamilyName = run.RunProperties.RunFonts.Ascii;

                                // Tạo một đối tượng FontFamily từ tên font family
                                System.Windows.Media.FontFamily fontFamily = new System.Windows.Media.FontFamily(fontFamilyName);

                                // Thiết lập FontFamily cho TextBox
                                frun.FontFamily = fontFamily;
                            }
                            //đọc định dạng background dòng chữ của file
                            if (run.RunProperties.Shading != null)
                            {
                                //kiểm tra màu background
                                var backgroundColor = run.RunProperties.Shading.Fill;
                                if (!string.IsNullOrEmpty(backgroundColor) && backgroundColor != "auto")
                                {
                                    //thiết lập màu background
                                    var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#" + backgroundColor);
                                    frun.Background = new SolidColorBrush(color);
                                }
                            }
                            //đọc định dạng mũ trên hoặc dưới của file 
                            if (run.RunProperties.VerticalTextAlignment != null)
                            {
                                //thiết lập mũ trên
                                if (run.RunProperties.VerticalTextAlignment.Val == VerticalPositionValues.Superscript)
                                {
                                    frun.Typography.Variants = FontVariants.Superscript;
                                }
                                //thiết lập mũ dưới
                                else if (run.RunProperties.VerticalTextAlignment.Val == VerticalPositionValues.Subscript)
                                {
                                    frun.Typography.Variants = FontVariants.Subscript;
                                }
                            }
                        }
                        wpfPara.Inlines.Add(frun);
                    }
                    richTextBox.Document.Blocks.Add(wpfPara);
                }
            }
        }

        //--------------Phần các hàm liên quan đến lưu trữ nội dung--------------------
        //Phương thức lưu file cho nút lưu
        private void LuuFile(object sender, RoutedEventArgs e)
        {
            //chỉ đến nút và ô textbox cần lưu file
            Button button = sender as Button;
            System.Windows.Controls.TextBox targetTextBox = button?.Tag as System.Windows.Controls.TextBox;

            //lỗi textbox do code
            if (targetTextBox == null)
            {
                MessageBox.Show("No target TextBox found.");
                return;
            }

            //mở của sổ folder đẻ lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FilterIndex = 1
            };

            // chọn định dạng file và lưu file
            if (saveFileDialog.ShowDialog() == true)
            {
                //lấy ra vị trí lưu file và ddingj dạng file cần lưu
                string filePath = saveFileDialog.FileName;
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

                //tiến hành lưu file
                if (fileExtension == ".txt")
                {
                    File.WriteAllText(filePath, targetTextBox.Text);
                }
                else
                {
                    //lỗi nếu định dạng file ko đúng
                    MessageBox.Show("Định dạng file ko đúng.");
                }
            }
        }
    }
}