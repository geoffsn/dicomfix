using System;
using System.Collections.Generic;
using System.Windows.Forms;




namespace dicomfix
{



    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        // ByteSearch is a function which will find a small byte array in a larger byte array and return its index location
        public static int ByteSearch(byte[] searchIn, byte[] searchBytes, int start = 0)
        {
            int found = -1;
            bool matched = false;
            //only look at this if we have a populated search array and search bytes with a sensible start
            if (searchIn.Length > 0 && searchBytes.Length > 0 && start <= (searchIn.Length - searchBytes.Length) && searchIn.Length >= searchBytes.Length)
            {
                //iterate through the array to be searched
                for (int i = start; i <= searchIn.Length - searchBytes.Length; i++)
                {
                    //if the start bytes match we will start comparing all other bytes
                    if (searchIn[i] == searchBytes[0])
                    {
                        if (searchIn.Length > 1)
                        {
                            //multiple bytes to be searched we have to compare byte by byte
                            matched = true;
                            for (int y = 1; y <= searchBytes.Length - 1; y++)
                            {
                                if (searchIn[i + y] != searchBytes[y])
                                {
                                    matched = false;
                                    break;
                                }
                            }
                            //everything matched up
                            if (matched)
                            {
                                found = i;
                                break;
                            }

                        }
                        else
                        {
                            //search byte is only one bit nothing else to do
                            found = i;
                            break; //stop the loop
                        }

                    }
                }

            }
            return found;
        }

        //InsertByte is a function which inserts a small byte array into a larger byte array
        public static byte[] InsertByte(byte[] orig, byte[] insertion, int startloc, int endloc)
        {
            byte[] newb = new byte[orig.Length + startloc - endloc + 1 + insertion.Length];
            Array.Copy(orig, 0, newb, 0, startloc + 1);
            Array.Copy(insertion, 0, newb, startloc + 1, insertion.Length);
            Array.Copy(orig, endloc, newb, newb.Length - orig.Length + endloc, orig.Length - endloc);
            
            return newb;

        }
        [STAThread]
        static void Main()
        {
            //run the windows program to fix dicom, additional code in form1.cs
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
       

    }
}
