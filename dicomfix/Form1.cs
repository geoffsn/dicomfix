using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace dicomfix
{


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
        }



        private void Form1_Load(object sender, EventArgs e)
        {


            //Prompt user to select the folder containing the DCM files which need their iso adjusted
            folderBrowserDialog1.ShowDialog();

            ActiveForm.Show();
            textBox1.AppendText("Processing files..." + Environment.NewLine);
            

            //Loop through all files in folder
            string direct = folderBrowserDialog1.SelectedPath;
            //Get list of all dcm files in the folder
            string[] files = Directory.GetFiles(direct, "*.dcm");

            //Several byte arrays which will later be used to find specific DICOM tags
            byte[] isoheader = { 0x20, 0x00, 0x32, 0x00 };
            byte[] tableh = { 0x18, 0x00, 0x30, 0x11 };
            byte[] datacentertag = { 0x18, 0x00, 0x13, 0x93 };
            byte[] ptorient = { 0x20, 0x00, 0x37, 0x00 };
            byte[] stationname = { 0x08, 0x00, 0x10, 0x10 };
            byte[] ptname = { 0x10, 0x00, 0x10, 0x00 };
            byte[] ptnum = { 0x10, 0x00, 0x20, 0x00 };
            byte[] imgtype = { 0x08, 0x00, 0x3E, 0x10 };
            byte[] fix = { 0x20, 0x2D, 0x20, 0x49, 0x73, 0x6F, 0x66, 0x69, 0x78, 0x65, 0x64 };
            byte[] axial = { 0x41, 0x58, 0x49, 0x41, 0x4C };
            byte[] backslsh = { 0x5C };
            byte[] endtype = { 0x08, 0x00 };
            byte[] end = { 0x20 };
            byte[] vrtype = { 0x4C, 0x4F };
            string tempname = "";
            string tempnum = "";
            int filescng = 0;

            for (int i = 0; i < files.Length; i++)
            {
                //read in the file
                byte[] filein = File.ReadAllBytes(files[i]);

                //find DICOM tag for machine id
                int stationloc = Program.ByteSearch(filein, stationname);
                string comp = System.Text.Encoding.Default.GetString(filein, stationloc + 8, 8);

                //only continue if the machine id is our Siemens Confidence scanner
                if (comp == "CT100018")
                {
                    ////Console.WriteLine("Right machine");
                    int imgtypeloc = Program.ByteSearch(filein, imgtype);
                    
                    //Check series description to see if "-Isofixed" is in there, if not, proceed
                    int fixd = Program.ByteSearch(filein, fix, imgtypeloc);
                    if (fixd == -1)
                    {
                        ////Console.WriteLine("Not yet corrected");
                        //Check to see if dcm file is an axial image file, if yes, proceed
                        int isaxial = Program.ByteSearch(filein, axial);
                        if (isaxial > 0)
                        {
                            ////Console.WriteLine("Is Axial");
                            //get length of the series description tag
                            byte[] typelength = new byte[2];
                            Array.Copy(filein, imgtypeloc + 6, typelength, 0, 2);
                            short imgtypeint = BitConverter.ToInt16(typelength, 0);

                            //If the variable length is listed as zero, it is because Siemens incorrectly created the series description by omitting the Variable type. This corrects that mistake.
                            if (imgtypeint == 0)
                            {
                                Array.Copy(filein, imgtypeloc + 4, typelength, 0, 2);
                                imgtypeint = BitConverter.ToInt16(typelength, 0);
                                //filein = Program.InsertByte(filein, vrtype, imgtypeloc + 3, imgtypeloc + 6);
                                short newimgtypeint = (short)(imgtypeint + fix.Length);
                                typelength = BitConverter.GetBytes(newimgtypeint);

                                //write in the new length of the series descrition tag with room for "-Isofixed" 
                                filein = Program.InsertByte(filein, typelength, imgtypeloc + 3, imgtypeloc + 6);
                            }
                            else
                            {
                                short newimgtypeint = (short)(imgtypeint + fix.Length);
                                typelength = BitConverter.GetBytes(newimgtypeint);

                                //write in the new length of the series descrition tag with room for "-Isofixed" 
                                filein = Program.InsertByte(filein, typelength, imgtypeloc + 5, imgtypeloc + 8);
                            }
                            //Find location of Image Position (0020,0032)
                            int isoloc = Program.ByteSearch(filein, isoheader);
                            //Console.WriteLine(isoloc);

                            //Find location of Table Height (0018,1130)
                            int tabloc = Program.ByteSearch(filein, tableh);

                            //Find location of the Tag following the Table Height
                            int tablocend = Program.ByteSearch(filein, end, tabloc + 8);
                            byte[] datacenterbits1 = new byte[8];
                            byte[] datacenterbits2 = new byte[8];
                            byte[] datacenterbits3 = new byte[8];

                            //extract Table Height value from file
                            string tablestr = System.Text.Encoding.Default.GetString(filein, tabloc + 8, tablocend - tabloc - 8);
                            double tablenum = double.Parse(tablestr);

                            // Some dcm files, such as scouts, lack a tag for the Image Position, so only continue if the tag exists
                            if (isoloc > 0)
                            {
                                //Console.WriteLine("has the tag for image position");
                                //add "-Isofixed" to the series description and create byte array for the updated dcm file
                                byte[] filemid1 = Program.InsertByte(filein, fix, imgtypeloc + imgtypeint + 7, imgtypeloc + imgtypeint + 8);

                                //reacquire the location of Image Position (0020,0032) now that the length of the byte array is greater
                                isoloc = Program.ByteSearch(filemid1, isoheader);

                                //Find location of Data Collection Center Tag
                                int datacenterloc = Program.ByteSearch(filemid1, datacentertag);

                                //extract each of the three float numbers of the Data Collection Center from the byte array and convert to necessary variable type
                                Array.Copy(filemid1, datacenterloc + 8, datacenterbits1, 0, 8);
                                Array.Copy(filemid1, datacenterloc + 16, datacenterbits2, 0, 8);
                                Array.Copy(filemid1, datacenterloc + 24, datacenterbits3, 0, 8);
                                double datacenter1 = BitConverter.ToDouble(datacenterbits1, 0);
                                double datacenter2 = BitConverter.ToDouble(datacenterbits2, 0);
                                double datacenter3 = BitConverter.ToDouble(datacenterbits3, 0);
                                string datacenter1str = datacenter1.ToString();
                                string datacenter2str = datacenter2.ToString();
                                string datacenter3str = datacenter3.ToString();

                                //Find location of each of the three string numbers of the Image Position from the byte array
                                int frstnum = Program.ByteSearch(filemid1, backslsh, isoloc);
                                int lastnum = Program.ByteSearch(filemid1, backslsh, (frstnum + 1));
                                int endiso = Program.ByteSearch(filemid1, end, (lastnum + 1));

                                //extract the values of the Image Position and convert to necessary variable type
                                string isostr1 = System.Text.Encoding.Default.GetString(filemid1, isoloc + 8, frstnum - isoloc - 8);
                                string isostr2 = System.Text.Encoding.Default.GetString(filemid1, frstnum + 1, lastnum - frstnum - 1);
                                string isostr3 = System.Text.Encoding.Default.GetString(filemid1, lastnum + 1, endiso - lastnum - 1);
                                double isodub1 = double.Parse(isostr1);
                                double isodub2 = double.Parse(isostr2);
                                double isodub3 = double.Parse(isostr3);

                                //Subtract the Data Collection Center from the Image Position and convert to necessary variable type
                                double newiso1 = isodub1 - datacenter1;
                                double newiso2 = isodub2 - datacenter2;
                                double newiso3 = datacenter3;
                                string newisostr1 = newiso1.ToString();
                                string newisostr2 = newiso2.ToString();
                                string newisostr3 = newiso3.ToString();

                                //Find location of the Patient name & MRN DICOM tags
                                int ptnameloc = Program.ByteSearch(filein, ptname);
                                int ptnumloc = Program.ByteSearch(filein, ptnum);

                                //extract the patient name
                                string name = Encoding.Default.GetString(filein, ptnameloc + 8, ptnumloc - ptnameloc - 8);

                                //If the patient name or table height are different from the previous file in the for-loop then continue
                                if ((tablestr != tempnum || name != tempname))
                                {
                                    if (filescng == 0)
                                    {
                                        //Write the Patient Name and Table Height to the display window
                                        textBox1.AppendText("Patient: ");
                                        textBox1.AppendText(name);
                                        textBox1.AppendText(Environment.NewLine + "Table Height = " + tablestr + "mm" + Environment.NewLine + Environment.NewLine);

                                        //reset the variables for checking if the patient name or table height change between files in the for-loop
                                        tempnum = tablestr;
                                        tempname = name;

                                    }
                                    else
                                    {
                                        textBox1.AppendText("Number of files fixed: " + filescng + Environment.NewLine + Environment.NewLine);
                                        filescng = 0;

                                        //Write the Patient Name and Table Height to the display window
                                        textBox1.AppendText("Patient: ");
                                        textBox1.AppendText(name);
                                        textBox1.AppendText(Environment.NewLine + "Table Height = " + tablestr + "mm" + Environment.NewLine + Environment.NewLine);

                                        //reset the variables for checking if the patient name or table height change between files in the for-loop
                                        tempnum = tablestr;
                                        tempname = name;
                                    }

                                }

                                //convert new Image Position (isocenter) to a byte array
                                char[] newiso1c = newisostr1.ToCharArray();
                                char[] newiso2c = newisostr2.ToCharArray();
                                char[] newiso3c = newisostr3.ToCharArray();
                                byte[] niso1 = System.Text.Encoding.Default.GetBytes(newiso1c);
                                byte[] niso2 = System.Text.Encoding.Default.GetBytes(newiso2c);
                                byte[] niso3 = System.Text.Encoding.Default.GetBytes(newiso3c);

                                //write in the adjusted values for the Image Position
                                byte[] fileout = Program.InsertByte(filemid1, niso2, frstnum, lastnum);
                                fileout = Program.InsertByte(fileout, niso1, isoloc + 7, frstnum);

                                //write the changes into the dcm file
                                File.WriteAllBytes(files[i], fileout);


                                //Console.WriteLine(datacenter1str + "  " + datacenter2str + "  " + datacenter3str);
                                //Console.WriteLine(isostr1 + "  " + isostr2 + "  " + isostr3);
                                //Console.WriteLine(newisostr1 + "  " + newisostr2 + "  " + newisostr3);

                                filescng = filescng + 1;

                            }
                        }
                    }
                }
                //continue through loop to the next file
            }
            

            if (filescng > 0)
            {
                textBox1.AppendText("Number of files fixed: " + filescng + Environment.NewLine + Environment.NewLine);
            }
            //If any dcm files in the folder were adjusted, then announce that the program is completed
            if (tempnum != "")
            {
                textBox1.AppendText("Isofix Completed");
                //Console.Read();
            }

            //If no dcm files needed to be adjusted, then announce it
            if (tempnum == "")
            {
                textBox1.AppendText("No dicom files need IsoFix");
            }
            
        }
    }

}
