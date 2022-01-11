using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Configuration;
using System.IO;
using FAXCOMEXLib;
using System.Diagnostics;

namespace faxserver
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class FaxService : System.Web.Services.WebService
    {

        /// <summary>
        /// This structure use for sendig fax information
        /// </summary>
        public struct file_holder
        {
            public string request;
            public string msg;
            public string id;
            public string tsid;
            public string CallerId;
            public int Pages;
            public int Size;
            public DateTime TransmissionStart;
            public DateTime TransmissionEnd;
            public byte[] file_byte;
        }
        /// <summary>
        /// store an image
        /// </summary>
        public struct imageFile
        {
            public byte[] image_byte;
        }
        /// <summary>
        /// This structure use for sendig fax information
        /// </summary>
        public struct allFile_holder
        {
            public string id;
            public string tsid;
            public string CallerId;
            public int Pages;
            public int Size;
            public DateTime TransmissionStart;
            public DateTime TransmissionEnd;
            public imageFile[] file_byte;
        }
        /// <summary>
        /// This structure use for sendig fax information
        /// </summary>
        public struct resivedFaxList
        {
            public string request;
            public string msg;
            public allFile_holder[] allFile_byte;
        }
        /// <summary>
        /// This structure use for sending modems and queues state
        /// </summary>
        public struct modemInfo
        {
            public string modemId;
            public string modemName;
            public string modemSendState;
            public string modemReciveState;
        }
        /// <summary>
        /// This structure use for sending modems state
        /// </summary>
        public struct state_holder
        {
            public bool is_true;
            public string msg;
            public int in_n;
            public int in_q;
            public int out_n;
            public int out_q;
            public int d_count;
            public modemInfo[] modemInformation;
        }

        /// <summary>
        /// contain modem info
        /// </summary>
        public struct modemsHolder
        {
            public string request;
            public string msg;
            public modemInfo[] modemInformation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            string password = s.ToString();
            return password;
        }

        /// <summary>
        /// get image mimetype info
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        private ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            try
            {
                int j;
                ImageCodecInfo[] encoders;
                encoders = ImageCodecInfo.GetImageEncoders();
                for (j = 0; j < encoders.Length; ++j)
                {
                    if (encoders[j].MimeType == mimeType)
                        return encoders[j];
                }
                return null;
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                return null;
            }
        }

        /// <summary>
        /// convert tiff file to other image format
        /// </summary>
        private bool imageConvertor(string tiffPath, string tiffID)
        {
            try
            {
                string tiffFile = tiffPath + "UnAssigned$" + tiffID + ".tif";
                if (System.IO.File.Exists(tiffFile))
                {
                    System.Drawing.Image img;
                    Bitmap img1;//, imgBoth;
                    String imgBothPath = tiffPath + "\\tempImage\\faxImage";
                    Int16 imgPages; // page count for image1
                    img = Image.FromFile(tiffFile); // grab first tiff
                    System.Drawing.Imaging.FrameDimension fd = new System.Drawing.Imaging.FrameDimension(img.FrameDimensionsList[0]);
                    imgPages = (Int16)img.GetFrameCount(fd); // set page count
                    //this.WriteInLogFile(imgPages.ToString() + " dddddddddddd");
                    /*
                    imgBoth = new Bitmap(img);
                    EncoderParameters encParams = new EncoderParameters(1);

                    // Get an ImageCodecInfo object that represents the TIFF codec.
                    ImageCodecInfo codecInfo = GetEncoderInfo("image/gif");
                    //set the type of tiff
                    encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)(EncoderValue.MultiFrame));
                    //set tiff frame
                    encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)(EncoderValue.FrameDimensionPage));
                    */
                    Int16 pagecounter;
                    for (pagecounter = 0; pagecounter < imgPages; pagecounter++)
                    { // save all previous pages in image 1
                        img.SelectActiveFrame(fd, pagecounter);
                        //img1 = new Bitmap(img, img.Height, img.Width); //img1 = (Bitmap)img; //img1 = Converter.ConvertToRGB(img1);
                        int imgH = Convert.ToInt16(ConfigurationSettings.AppSettings["imgh"]);
                        int imgW = Convert.ToInt16(ConfigurationSettings.AppSettings["imgw"]);
                        img1 = new Bitmap(img, imgW, imgH);
                        img1.Save(imgBothPath + pagecounter + ".gif");
                        //imgBoth = new Bitmap(img);
                        //imgBoth.Save(imgBothPath + pagecounter + ".gif", codecInfo, encParams);
                    }

                    img.Dispose();
                    //imgBoth.Dispose();
                    return true;
                }
                else
                {
                    this.WriteInLogFile(tiffFile + " no file");
                    return false;
                }
            }
            catch (Exception e)
            {
                WriteInLogFile("erroooorr  " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Security function for login
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool logincheck(string user, string pass, string level)
        {
            if (level == "user")
            {
                if (GetMD5Hash(ConfigurationSettings.AppSettings["username"].ToString()) == user &&
                    GetMD5Hash(ConfigurationSettings.AppSettings["password"].ToString()) == pass)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (level == "admin")
                {
                    if (GetMD5Hash(ConfigurationSettings.AppSettings["username_a"].ToString()) == user &&
                        GetMD5Hash(ConfigurationSettings.AppSettings["password_a"].ToString()) == pass)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else { return false; }
            }
        }

        /// <summary>
        /// Error recorder function
        /// </summary>
        /// <param name="errormsg"></param>
        public string WriteInLogFile(string errormsg)
        {
            //try
            {
                //string path = ConfigurationSettings.AppSettings["mypath"].ToString() + "error.log";
                string path = AppDomain.CurrentDomain.BaseDirectory + @"\" + "error.log";
                
                if (!File.Exists(path))
                {
                    // Create the file.
                    using (FileStream fs = File.Create(path)) { }
                }

                // Open the stream and write to it.
                using (StreamWriter fs = File.AppendText(path))
                {
                    //Byte[] info = new UTF8Encoding(true).GetBytes(errormsg + "\n");
                    fs.WriteLine("");
                    fs.WriteLine("-------------------------------------" + System.DateTime.Now + "---------------------------------------------");
                    fs.WriteLine(errormsg);
                }
                return "";
            }
            //catch (Exception e) {
            //    //System.Windows.Forms.MessageBox.Show(e.Message);
            //    //Console.WriteLine(e.Message);
            //    return e.Message;
            //}

        }

        [WebMethod]
        public string getMachineName()
        {
            return Environment.MachineName.ToString();
        }

        [WebMethod]
        public string[] sendFax(string username, string password, string my_nums, string file_type, byte[] file_data, string subject)
        {
            string[] strArray;
            if (!this.logincheck(username, password, "user"))
            {
                return new string[] { "false", "Login failed" };
            }
            if (((my_nums == "") || (file_type == "")) || (file_data == null))
            {
                strArray = new string[] { "false" };
                strArray[1] = "no number";
                return strArray;
            }
            string[] strArray2 = my_nums.Split(new char[] { ',' });
            try
            {
                FaxServerClass faxServer = new FaxServerClass();
                faxServer.Connect(ConfigurationSettings.AppSettings["server"].ToString());
                FaxFolders folders = faxServer.Folders;
                FaxIncomingArchive incomingArchive = folders.IncomingArchive;
                string path = incomingArchive.ArchiveFolder.Substring(0, incomingArchive.ArchiveFolder.LastIndexOf('\\') + 1) + "fax." + file_type;
                try
                {
                    if (file_data.Length != 0)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        FileStream stream = File.Create(path);
                        stream.Write(file_data, 0, file_data.Length);
                        stream.Close();
                    }
                }
                catch (Exception exception)
                {
                    this.WriteInLogFile(exception.Message);
                    return new string[] { "false", "Request failed2" };
                }
                if (File.Exists(path))
                {
                    FaxDocument document = new FaxDocumentClass
                    {
                        DocumentName = "fax." + file_type,
                        Body = path,
                        Subject = subject
                    };
                    FaxRecipients recipients = document.Recipients;
                    foreach (string str2 in strArray2)
                    {
                        recipients.Add(str2, "");
                    }
                    string[] strArray3 = (string[])document.Submit(ConfigurationSettings.AppSettings["server"].ToString());//ConnectedSubmit(faxServer);
                    strArray = new string[strArray3.Length + 2];
                    strArray[0] = "true";
                    strArray[1] = strArray3.Length.ToString();
                    strArray3.CopyTo(strArray, 2);
                    return strArray;
                }
                faxServer.Disconnect();
                return new string[] { "false", "can not find some file" };
            }
            catch (Exception exception2)
            {
                this.WriteInLogFile(exception2.Message);
                return new string[] { "false", ("Request failed\n" + exception2.Message) };
            }
        }

        /// <summary>
        /// This function use for sending fax
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="my_nums"></param>
        /// <param name="file_type"></param>
        /// <param name="file_data"></param>
        /// <param name="subject"></param>
        /// <param name="csid"></param>
        /// <param name="email"></param>
        /// <param name="recname"></param>
        /// <returns></returns>
        [WebMethod]
        public string[] sendFaxNew(string username, string password, string my_nums, string file_type, byte[] file_data, string subject)
        {
            string[] returner;
            if (!logincheck(username, password, "user"))
            {
                returner = new string[2];
                returner[0] = "false";
                returner[1] = "Login failed";
                return returner;
            }


            string[] words;

            if (my_nums == "" || (file_type == "" || file_data == null))
            {
                returner = new string[1];
                returner[0] = "false";
                returner[1] = "no number";
                return returner;
            }



            if (file_type == "pdf")
            {
                string pathfile = ConfigurationSettings.AppSettings["procceskillerpath"].ToString() + "processKiller.exe";
                //WriteInLogFile("after..... tooooooo1");
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = pathfile;
                p.Start();
                p.WaitForExit();
            }


            words = my_nums.Split(';');
            //returner = new string[(words.Length + 2)];
            //string path = ConfigurationSettings.AppSettings["mypath"].ToString() + "fax." + file_type;

            try
            {
                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_fobj = fsc.Folders;
                FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;

                object fax_aobj = fax_f.IncomingArchive;
                FAXCOMEXLib.FaxIncomingArchive fax_a = (FAXCOMEXLib.FaxIncomingArchive)fax_aobj;

                string path = fax_a.ArchiveFolder.Substring(0, fax_a.ArchiveFolder.LastIndexOf('\\') + 1) + "fax." + file_type;
                try
                {
                    if (file_data.Length != 0)
                    {
                        // Delete the file if it exists.
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        //Create the file.
                        FileStream fs = File.Create(path);
                        fs.Write(file_data, 0, file_data.Length);
                        fs.Close();
                    }
                }
                catch (Exception e)
                {
                    WriteInLogFile(e.Message);
                    returner = new string[2];
                    returner[0] = "false";
                    returner[1] = "Request failed2";
                    return returner;
                }
                if (System.IO.File.Exists(path))
                {
                    FAXCOMEXLib.FaxDocument fd = new FAXCOMEXLib.FaxDocument();
                    fd.DocumentName = "fax." + file_type;
                    fd.Body = path;

                    fd.Subject = subject;
                    object Recipients_obj = fd.Recipients;
                    FAXCOMEXLib.FaxRecipients Recipients = (FAXCOMEXLib.FaxRecipients)Recipients_obj;

                    foreach (string s in words)
                    {//add all Recipients
                        Recipients.Add(s, "");
                    }

                    //send operation
                    string[] id_holder = (string[])fd.Submit(ConfigurationSettings.AppSettings["server"].ToString());

                    returner = new string[(id_holder.Length + 2)];
                    returner[0] = "true";
                    returner[1] = id_holder.Length.ToString();
                    id_holder.CopyTo(returner, 2);

                    return returner;
                }
                else
                {
                    fsc.Disconnect();
                    returner = new string[2];
                    returner[0] = "false";
                    returner[1] = "can not find some file";
                    return returner;
                }
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                returner = new string[2];
                returner[0] = "false";
                returner[1] = "Request failed\n" + e.Message;
                return returner;
                //return "Error passing data to faxserver. " + e;
            }
        }

        /// <summary>
        /// checking outgoing fax status
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="fax_id"></param>
        /// <returns></returns>
        [WebMethod]
        public string checkForSendingFax(string username, string password, string fax_id)
        {
            if (!logincheck(username, password, "user") && !logincheck(username, password, "admin"))
            {
                return "false";
            }
            try
            {
                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_fobj = fsc.Folders;
                FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;

                object fax_oqobj = fax_f.OutgoingQueue;
                FAXCOMEXLib.FaxOutgoingQueue fax_oq = (FAXCOMEXLib.FaxOutgoingQueue)fax_oqobj;

                object fax_jsobj = fax_oq.GetJobs();
                FAXCOMEXLib.FaxOutgoingJobs fax_js = (FAXCOMEXLib.FaxOutgoingJobs)fax_jsobj;

                System.Collections.IEnumerator enum_var = fax_js.GetEnumerator();
                for (int i = 1; enum_var.MoveNext(); i++)
                {
                    FAXCOMEXLib.FaxOutgoingJob fax_j = (FAXCOMEXLib.FaxOutgoingJob)enum_var.Current;
                    string faxinserver_id = Convert.ToInt64(fax_j.Id, 16).ToString();
                    if (faxinserver_id == fax_id)
                    {
                        return "wait";
                    }
                }

                fsc.Disconnect();
                return "sent";
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                return "error";
            }
        }


        [WebMethod]
        public imageFile[] getFaxImage(string username, string password, string MsgID)
        {
            imageFile[] ret = new imageFile[0];
            if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
            {
                return ret;
            }
            try
            {
                FaxServer server = new FaxServer();
                server.Connect("");
                FaxAccountFoldersClass folders = (FaxAccountFoldersClass)server.CurrentAccount.Folders;
                FaxAccountIncomingArchive incomingArchive = folders.IncomingArchive;
                FaxIncomingMessageIteratorClass fax_ms = (FaxIncomingMessageIteratorClass)incomingArchive.GetMessages();
                if (fax_ms.AtEOF)
                {//no recived fax
                    return ret;
                }
                else
                {
                    
                    //String id = Convert.ToInt64(MsgID).ToString("X");
                    IFaxIncomingMessage msg = incomingArchive.GetMessage(MsgID);
                    string fax_path = server.Configuration.ArchiveLocation + "\\Inbox\\";
                    ret = new imageFile[msg.Pages];
                    if (imageConvertor(fax_path, msg.Id.ToString()))
                    {
                        for (int i = 0; i < msg.Pages; i++)
                        {
                            using (FileStream fs = File.OpenRead(fax_path + "\\tempImage\\faxImage" + i + ".gif"))
                            {
                                ret[i].image_byte = new byte[fs.Length];
                                int numBytesToRead = (int)fs.Length;
                                fs.Read(ret[i].image_byte, 0, numBytesToRead);
                            }
                        }
                    }
                    return ret;
                }
                /*
                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());
                object fax_fobj = fsc.Folders;
                FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;
                object fax_aobj = fax_f.IncomingArchive;
                FAXCOMEXLib.FaxIncomingArchive fax_a = (FAXCOMEXLib.FaxIncomingArchive)fax_aobj;
                if (fax_a.SizeHigh == 0 && false)
                {
                    return ret;
                }
                FaxIncomingMessage msg = null;// fax_a.GetMessage(MsgID);
                string fax_path = fax_a.ArchiveFolder.ToString() + "\\";
                ret = new imageFile[msg.Pages];
                if (imageConvertor(fax_path, msg.Id.ToString()))
                {
                    for (int i = 0; i < msg.Pages; i++)
                    {
                        using (FileStream fs = File.OpenRead(fax_path + "\\tempImage\\faxImage" + i + ".gif"))
                        {
                            ret[i].image_byte = new byte[fs.Length];
                            int numBytesToRead = (int)fs.Length;
                            fs.Read(ret[i].image_byte, 0, numBytesToRead);
                        }
                    }
                }
                return ret;*/
            }
            catch (Exception e)
            {
                WriteInLogFile("getFaxImage " + MsgID + " " + e.Message);
                return ret;
            }
        }

        /// <summary>
        /// get a list of incoming fax without image
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        public resivedFaxList getRecivedFaxListOnly(string username, string password, string DName)
        {
            resivedFaxList ret = new resivedFaxList();
            if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
            {
                ret.request = "false";
                ret.msg = "Login failed";
                return ret;
            }
            try
            {
                FaxServer server = new FaxServer();
                server.Connect("");
                FaxAccountFoldersClass folders = (FaxAccountFoldersClass)server.CurrentAccount.Folders;
                FaxAccountIncomingArchive incomingArchive = folders.IncomingArchive;
                FaxIncomingMessageIteratorClass fax_ms = (FaxIncomingMessageIteratorClass)incomingArchive.GetMessages();
                if (fax_ms.AtEOF)
                {//no recived fax
                    ret.request = "null";
                    ret.msg = "no recived fax!!";
                    return ret;
                }
                else
                {
                    fax_ms.MoveFirst();

                    int counter = 0;
                    while (!fax_ms.AtEOF)
                    {
                        counter++;
                        fax_ms.MoveNext();
                    }

                    ret.allFile_byte = new allFile_holder[counter];
                    fax_ms.MoveFirst();
                    string fax_path = server.Configuration.ArchiveLocation + "\\Inbox\\";
                    int numBytesToRead = 0;
                    for (int i = 0; !fax_ms.AtEOF; i++)
                    {
                        if (true || (DName == "all") || (DName == fax_ms.Message.DeviceName))
                        {
                            try
                            {
                                //ret.allFile_byte[i].id = fax_ms.Message.Id;// Convert.ToInt64(fax_ms.Message.Id, 16).ToString();
                                ret.allFile_byte[i].id = Convert.ToInt64(fax_ms.Message.Id, 16).ToString();
                                int l = 0;
                                string tmp = "";
                                if (fax_ms.Message.TSID != null)
                                {
                                    l = fax_ms.Message.TSID.Length;
                                    for (int j = 0; j < l; j++)
                                    {
                                        if (char.IsLetterOrDigit(fax_ms.Message.TSID[j]) == true)
                                        {
                                            tmp += fax_ms.Message.TSID[j];
                                        }
                                    }
                                }

                                ret.allFile_byte[i].tsid = tmp;
                                ret.allFile_byte[i].CallerId = fax_ms.Message.CallerId;
                                ret.allFile_byte[i].Pages = fax_ms.Message.Pages;
                                ret.allFile_byte[i].TransmissionStart = fax_ms.Message.TransmissionStart;
                                ret.allFile_byte[i].TransmissionEnd = fax_ms.Message.TransmissionEnd;
                                ret.allFile_byte[i].Size = fax_ms.Message.Size;
                                ret.allFile_byte[i].file_byte = new imageFile[0];
                            }
                            catch (Exception ie)
                            {
                                ret.allFile_byte[i].Pages = 1;
                                ret.allFile_byte[i].file_byte = new imageFile[0];
                            }
                        }
                        else
                        {
                            counter--;
                        }
                        fax_ms.MoveNext();
                    }
                    if (counter == 0)
                    {//device specifid has no fax
                        ret.request = "null";
                        ret.msg = "no recived fax for you";
                        ret.allFile_byte = null;
                        return ret;
                    }
                    ret.request = "true";
                    ret.msg = counter.ToString();
                    return ret;
                }
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                ret.request = "false";
                ret.msg = "Request Failed " + e.Message;
                return ret;
            }
        }//end of getRecivedFaxList

        /// <summary>
        /// get a list of incoming fax
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        public resivedFaxList getRecivedFaxList(string username, string password, string DName)
        {
            resivedFaxList ret = new resivedFaxList();
            if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
            {
                ret.request = "false";
                ret.msg = "Login failed";
                return ret;
            }
            try
            {
                FaxServer server = new FaxServer();
                server.Connect("");
                FaxAccountFoldersClass folders = (FaxAccountFoldersClass)server.CurrentAccount.Folders;
                FaxAccountIncomingArchive incomingArchive = folders.IncomingArchive;
                FaxIncomingMessageIteratorClass fax_ms = (FaxIncomingMessageIteratorClass)incomingArchive.GetMessages();
                if (fax_ms.AtEOF){//no recived fax
                    ret.request = "null";
                    ret.msg = "no recived fax!!";
                    return ret;
                }else{
                    fax_ms.MoveFirst();

                    int counter = 0;
                    while (!fax_ms.AtEOF)
                    {
                        counter++;
                        fax_ms.MoveNext();
                    }

                    ret.allFile_byte = new allFile_holder[counter];
                    fax_ms.MoveFirst();
                    string fax_path = server.Configuration.ArchiveLocation + "\\Inbox\\";
                    int numBytesToRead = 0;
                    for (int i = 0; !fax_ms.AtEOF; i++)
                    {
                        if (true || (DName == "all") || (DName == fax_ms.Message.DeviceName))
                        {
                            try
                            {
                                ret.allFile_byte[i].id = fax_ms.Message.Id;// Convert.ToInt64(fax_ms.Message.Id, 16).ToString();
                                int l = 0;
                                string tmp = "";
                                if (fax_ms.Message.TSID != null)
                                {
                                    l = fax_ms.Message.TSID.Length;
                                    for (int j = 0; j < l; j++)
                                    {
                                        if (char.IsLetterOrDigit(fax_ms.Message.TSID[j]) == true)
                                        {
                                            tmp += fax_ms.Message.TSID[j];
                                        }
                                    }
                                }

                                ret.allFile_byte[i].tsid = tmp;
                                ret.allFile_byte[i].CallerId = fax_ms.Message.CallerId;
                                ret.allFile_byte[i].Pages = fax_ms.Message.Pages;
                                ret.allFile_byte[i].TransmissionStart = fax_ms.Message.TransmissionStart;
                                ret.allFile_byte[i].TransmissionEnd = fax_ms.Message.TransmissionEnd;
                                ret.allFile_byte[i].Size = fax_ms.Message.Size;
                                ret.allFile_byte[i].file_byte = new imageFile[fax_ms.Message.Pages];
                                if (imageConvertor(fax_path, fax_ms.Message.Id.ToString()))
                                {
                                    for (int tempCounter = 0; tempCounter < fax_ms.Message.Pages; tempCounter++)
                                    {
                                        using (FileStream fs = File.OpenRead(fax_path + "\\tempImage\\faxImage" + tempCounter + ".gif"))
                                        {
                                            ret.allFile_byte[i].file_byte[tempCounter].image_byte = new byte[fs.Length];
                                            numBytesToRead = (int)fs.Length;
                                            fs.Read(ret.allFile_byte[i].file_byte[tempCounter].image_byte, 0, numBytesToRead);
                                        }
                                    }
                                }
                            }
                            catch (Exception ie)
                            {
                                ret.allFile_byte[i].Pages = 1;
                                ret.allFile_byte[i].file_byte = new imageFile[1];
                                using (FileStream efs = File.OpenRead(fax_path + "\\tempImage\\badfax.gif"))
                                {
                                    ret.allFile_byte[i].file_byte[0].image_byte = new byte[efs.Length];
                                    numBytesToRead = (int)efs.Length;
                                    efs.Read(ret.allFile_byte[i].file_byte[0].image_byte, 0, numBytesToRead);
                                }
                            }
                        }
                        else
                        {
                            counter--;
                        }
                        fax_ms.MoveNext();
                    }
                    if (counter == 0)
                    {//device specifid has no fax
                        ret.request = "null";
                        ret.msg = "no recived fax for you";
                        ret.allFile_byte = null;
                        return ret;
                    }
                    ret.request = "true";
                    ret.msg = counter.ToString();
                    return ret;
                }
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                ret.request = "false";
                ret.msg = "Request Failed " + e.Message;
                return ret;
            }
        }//end of getRecivedFaxList

        /// <summary>
        /// get a report from modems state
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        public state_holder getFaxStatus(string username, string password)
        {
            state_holder ret = new state_holder();
            try
            {
                if (!logincheck(username, password, "admin"))
                {
                    ret.is_true = false;
                    ret.msg = "Login failed";
                    return ret;
                }
                ret.is_true = true;

                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_aobj = fsc.Activity;
                FAXCOMEXLib.FaxActivity fax_a = (FAXCOMEXLib.FaxActivity)fax_aobj;

                ret.in_q = fax_a.IncomingMessages;
                ret.out_n = fax_a.OutgoingMessages;
                ret.out_q = fax_a.QueuedMessages;
                ret.in_n = fax_a.RoutingMessages;

                object fax_dsobj = fsc.GetDevices();
                FAXCOMEXLib.FaxDevices fax_ds = (FAXCOMEXLib.FaxDevices)fax_dsobj;
                ret.d_count = fax_ds.Count;

                System.Collections.IEnumerator enum_var = fax_ds.GetEnumerator();

                FAXCOMEXLib.FaxDevice fax_d;
                int conter = 0;
                ret.modemInformation = new modemInfo[fax_ds.Count];

                while (enum_var.MoveNext())
                {
                    fax_d = (FAXCOMEXLib.FaxDevice)enum_var.Current;
                    ret.modemInformation[conter].modemId = fax_d.Id.ToString();
                    ret.modemInformation[conter].modemName = fax_d.DeviceName;
                    ret.modemInformation[conter].modemSendState = fax_d.SendEnabled.ToString();
                    ret.modemInformation[conter].modemReciveState = fax_d.ReceiveMode.ToString();
                    conter++;
                    /*
                    ret.state[(conter * 4)] = fax_d.Id.ToString();
                    ret.state[(conter * 4) + 1] = fax_d.DeviceName;
                    ret.state[(conter * 4) + 2] = fax_d.SendEnabled.ToString();
                    ret.state[(conter * 4) + 3] = fax_d.ReceiveMode.ToString();
                     */
                }

                fsc.Disconnect();
                ret.msg = "done successfully";
                return ret;
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                ret.is_true = false;
                ret.msg = "Can not countinue right now plase try later";
                return ret;
            }
        }

        /// <summary>
        ///  Working with modems status
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="sendmode"></param>
        /// <param name="recmode"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [WebMethod]
        public bool setModemState(string username, string password, string sendmode, string recmode, string id)
        {
            try
            {
                if (!logincheck(username, password, "admin"))
                {
                    return false;
                }

                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_dsobj = fsc.GetDevices();
                FAXCOMEXLib.FaxDevices fax_ds = (FAXCOMEXLib.FaxDevices)fax_dsobj;

                System.Collections.IEnumerator enum_var = fax_ds.GetEnumerator();
                FAXCOMEXLib.FaxDevice fax_d;

                while (enum_var.MoveNext())
                {
                    fax_d = (FAXCOMEXLib.FaxDevice)enum_var.Current;
                    if (fax_d.Id.ToString() == id)
                    {
                        switch (recmode)
                        {
                            case "fdrmAUTO_ANSWER":
                                fax_d.ReceiveMode = FAXCOMEXLib.FAX_DEVICE_RECEIVE_MODE_ENUM.fdrmAUTO_ANSWER;
                                break;
                            case "fdrmMANUAL_ANSWER":
                                fax_d.ReceiveMode = FAXCOMEXLib.FAX_DEVICE_RECEIVE_MODE_ENUM.fdrmMANUAL_ANSWER;
                                break;
                            case "fdrmNO_ANSWER":
                                fax_d.ReceiveMode = FAXCOMEXLib.FAX_DEVICE_RECEIVE_MODE_ENUM.fdrmNO_ANSWER;
                                break;
                            default:
                                fsc.Disconnect();
                                return false;
                        }
                        if (sendmode == "true") { fax_d.SendEnabled = true; } else { fax_d.SendEnabled = false; }
                        fax_d.Save();
                        fsc.Disconnect();
                        return true;
                    }
                }
                fsc.Disconnect();
                return false;
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Delete any recived fax
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        [WebMethod]
        public bool removeRecivedFax(string username, string password, string Id)
        {
            try
            {
                if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
                {
                    return false;
                }

                FaxServer server = new FaxServer();
                server.Connect("");
                FaxAccountFoldersClass folders = (FaxAccountFoldersClass)server.CurrentAccount.Folders;
                FaxAccountIncomingArchive incomingArchive = folders.IncomingArchive;
                FaxIncomingMessageIteratorClass fax_ms = (FaxIncomingMessageIteratorClass)incomingArchive.GetMessages();
                while (!fax_ms.AtEOF){
                    //if (Convert.ToInt64(fax_ms.Message.Id, 16).ToString() == Id)
                    if (fax_ms.Message.Id == Id)
                    {
                        fax_ms.Message.Delete();
                        return true;
                    }
                    else
                    {
                        fax_ms.MoveNext();
                    }
                }
                return false;
                

                /*
                if (fax_ms.AtEOF)
                {//no recived fax
                    ret.request = "null";
                    ret.msg = "no recived fax!!";
                    return ret;
                }
                else
                {
                    fax_ms.MoveFirst();

                    int counter = 0;
                    while (!fax_ms.AtEOF)
                    {
                        counter++;
                        fax_ms.MoveNext();
                    }

                    ret.allFile_byte = new allFile_holder[counter];
                    fax_ms.MoveFirst();
                    string fax_path = server.Configuration.ArchiveLocation + "\\Inbox\\";
                    int numBytesToRead = 0;
                    for (int i = 0; !fax_ms.AtEOF; i++)
                    {

                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_fobj = fsc.Folders;
                FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;

                object fax_aobj = fax_f.IncomingArchive;
                FAXCOMEXLib.FaxIncomingArchive fax_a = (FAXCOMEXLib.FaxIncomingArchive)fax_aobj;

                object fax_msobj = fax_a.GetMessages(fax_a.SizeHigh);
                FAXCOMEXLib.FaxIncomingMessageIterator fax_ms = (FAXCOMEXLib.FaxIncomingMessageIterator)fax_msobj;
                fax_ms.MoveFirst();

                string fax_path = fax_a.ArchiveFolder.ToString() + "\\";
                while (!fax_ms.AtEOF)
                {
                    if (Convert.ToInt64(fax_ms.Message.Id, 16).ToString() == Id)
                    {
                        fax_ms.Message.Delete();
                        //File.Delete(fax_path + fax_ms.Message.Id.ToString() + ".tif");
                        return true;
                    }
                    else
                    {
                        fax_ms.MoveNext();
                    }
                }
                return false;*/
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                return false;
            }
        }

        /// <summary>
        /// show an incoming fax
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        [WebMethod]
        public file_holder showRecivedFax(string username, string password, string Id)
        {
            file_holder ret = new file_holder();
            try
            {
                if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
                {
                    ret.tsid = "false";
                    return ret;
                }

                FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                object fax_fobj = fsc.Folders;
                FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;

                object fax_aobj = fax_f.IncomingArchive;
                FAXCOMEXLib.FaxIncomingArchive fax_a = (FAXCOMEXLib.FaxIncomingArchive)fax_aobj;

                object fax_msobj = fax_a.GetMessages(fax_a.SizeHigh);
                FAXCOMEXLib.FaxIncomingMessageIterator fax_ms = (FAXCOMEXLib.FaxIncomingMessageIterator)fax_msobj;
                fax_ms.MoveFirst();

                string fax_path = fax_a.ArchiveFolder.ToString() + "\\";
                while (!fax_ms.AtEOF)
                {
                    if (Convert.ToInt64(fax_ms.Message.Id, 16).ToString() == Id)
                    {
                        ret.id = fax_ms.Message.Id;
                        ret.tsid = fax_ms.Message.TSID;
                        ret.CallerId = fax_ms.Message.CallerId;
                        ret.Pages = fax_ms.Message.Pages;
                        ret.TransmissionStart = fax_ms.Message.TransmissionStart;
                        ret.TransmissionEnd = fax_ms.Message.TransmissionEnd;
                        ret.Size = fax_ms.Message.Size;

                        using (FileStream fs = File.OpenRead(fax_path + fax_ms.Message.Id.ToString() + ".tif"))
                        {
                            ret.file_byte = new byte[fs.Length];
                            int numBytesToRead = (int)fs.Length;
                            fs.Read(ret.file_byte, 0, numBytesToRead);
                        }
                        ret.request = "true";
                        return ret;
                    }
                    else
                    {
                        fax_ms.MoveNext();
                    }
                }
                ret.request = "false";
                ret.msg = "The tiff file can not found";
                return ret;
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                ret.request = "false";
                ret.msg = "Request Failed " + e.Message;
                return ret;
            }
        }

        /// <summary>
        /// remove a fax from sinding list
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="Ids"></param>
        /// <returns></returns>
        [WebMethod]
        public string[] removeFaxFromSendingList(string username, string password, string Ids)
        {
            string[] returner;
            try
            {
                if (!logincheck(username, password, "admin") && !logincheck(username, password, "user"))
                {
                    returner = new string[2];
                    returner[0] = "false";
                    returner[1] = "Login failed";
                    return returner;
                }
                else
                {
                    FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                    fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());

                    object fax_fobj = fsc.Folders;
                    FAXCOMEXLib.FaxFolders fax_f = (FAXCOMEXLib.FaxFolders)fax_fobj;

                    object fax_oqobj = fax_f.OutgoingQueue;
                    FAXCOMEXLib.FaxOutgoingQueue fax_oq = (FAXCOMEXLib.FaxOutgoingQueue)fax_oqobj;

                    object fax_jsobj = fax_oq.GetJobs();
                    FAXCOMEXLib.FaxOutgoingJobs fax_js = (FAXCOMEXLib.FaxOutgoingJobs)fax_jsobj;
                    if (fax_js.Count == 0)
                    {
                        returner = new string[2];
                        returner[0] = "false";
                        returner[1] = "no fax";
                        return returner;
                    }
                    string[] words = Ids.Split(',');
                    returner = new string[(words.Length + 2)];
                    returner[0] = "true";

                    System.Collections.IEnumerator enum_var = fax_js.GetEnumerator();
                    for (int j = 2; j < (words.Length + 2); j++)
                    {
                        enum_var.Reset();
                        for (int i = 1; enum_var.MoveNext(); i++)
                        {
                            FAXCOMEXLib.FaxOutgoingJob fax_j = (FAXCOMEXLib.FaxOutgoingJob)enum_var.Current;
                            if (Convert.ToInt64(fax_j.Id, 16).ToString() == words[(j - 2)])
                            {
                                fax_j.Cancel();
                                returner[j] = "done";
                                returner[1] = (j - 1).ToString();
                                break;
                            }
                            if (i == fax_js.Count)
                            {
                                returner[j] = "null";
                                returner[1] = (j - 1).ToString();
                            }
                        }
                    }
                    return returner;
                }
            }
            catch (Exception e)
            {
                WriteInLogFile(e.Message);
                returner = new string[2];
                returner[0] = "error";
                returner[1] = "Request failed";
                return returner;
            }
        }

        /// <summary>
        /// remove a fax from sinding list
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="Ids"></param>
        /// <returns></returns>
        [WebMethod]
        public modemsHolder getModemList(string username, string password)
        {
            modemsHolder returner = new modemsHolder();
            try
            {
                if (!logincheck(username, password, "admin"))
                {
                    returner.request = "false";
                    returner.msg = "Login failed";
                    return returner;
                }
                else
                {
                    FAXCOMEXLib.FaxServerClass fsc = new FAXCOMEXLib.FaxServerClass();
                    //fsc.Connect(System.Net.Dns.GetHostName());
                    fsc.Connect(ConfigurationSettings.AppSettings["server"].ToString());
                    FaxDevices fax_ds = (FaxDevices)fsc.GetDevices();
                    System.Collections.IEnumerator enum_var = fax_ds.GetEnumerator();

                    returner.request = "true";
                    returner.msg = fax_ds.Count.ToString();
                    returner.modemInformation = new modemInfo[fax_ds.Count];

                    enum_var.Reset();
                    FaxDevice fax_d;
                    for (int i = 0; enum_var.MoveNext(); i++)
                    {
                        fax_d = (FaxDevice)enum_var.Current;
                        returner.modemInformation[i].modemName = fax_d.DeviceName;
                        returner.modemInformation[i].modemId = fax_d.Id.ToString();
                        returner.modemInformation[i].modemReciveState = fax_d.ReceiveMode.ToString();
                        returner.modemInformation[i].modemSendState = fax_d.SendEnabled.ToString();
                    }
                    return returner;
                }
            }
            catch (Exception e)
            {
                string message = WriteInLogFile(e.Message);
                returner.request = "error";
                returner.msg = "Request failed " + e.Message;
                return returner;
            }
        }
    }
}