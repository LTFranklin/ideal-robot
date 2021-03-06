﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace location

{
    class Program
    {
        static void Main(string[] args)
        {            
            
            TcpClient client = new TcpClient();
            string hostName = "localhost";
            int portNum = 43;
            string type = "";
            string[] data = new string[5];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/h")
                {
                    args[i] = null;
                    i++;
                    hostName = args[i];
                    args[i] = null;
                }
                else if (args[i] == "/p")
                {
                    args[i] = null;
                    i++;
                    bool valid = int.TryParse(args[i], out portNum);
                    args[i] = null;
                    if (valid == false)
                    {
                        Console.WriteLine("Invalid Port Number");
                    }
                }
                else if (args[i] == "/h9")
                {
                    args[i] = null;
                    type = "h9";
                }
                else if (args[i] == "/h0")
                {
                    args[i] = null;
                    type = "h0";
                }
                else if (args[i] == "/h1")
                {
                    args[i] = null;
                    type = "h1";
                }
            }
            int k = 0;
            for(int i = 0; i < args.Length; i++)
            {
                if(args[i] != null)
                {
                    data[k] = args[i];
                    k++;
                }
            }

            client.Connect(hostName, portNum);
            StreamWriter streamWrite = new StreamWriter(client.GetStream());
            StreamReader streamRead = new StreamReader(client.GetStream());
            NetworkStream netStream = client.GetStream();

            try
            {
                if (type == "h9")
                {
                    h09Pro(data, streamWrite);
                }
                else if (type == "h0")
                {
                    h10Pro(data, streamWrite);
                }
                else if (type == "h1")
                {
                    h11Pro(data, streamWrite, hostName);
                }
                else
                {
                    whoisPro(data, streamWrite);
                }

                streamWrite.Flush();
                client.ReceiveTimeout = 2000;
                try
                {
                    string x = streamRead.ReadToEnd().ToString();
                    //Console.WriteLine(x);
                    if (portNum != 80)
                    {
                        match(x, data);
                    }
                    else
                    {
                        Console.WriteLine(data[0] + "is" + x);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Timed out");
                }
                streamWrite.Close();
                streamRead.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
                       
        }

        static public void whoisPro(string[] args, StreamWriter streamWrite)
        {
            if (args[1] != null)
            {
                streamWrite.Write(args[0] + " " + args[1] + "\r\n");
            }
            else
            {
                streamWrite.Write(args[0] + "\r\n");
            }
        }

        static public void h09Pro(string[] args, StreamWriter streamWrite)
        {
            if (args[1] != null)
            {
                streamWrite.Write("PUT /" + args[0] + "\r\n\r\n" + args[1] +"\r\n");
            }
            else
            {
                streamWrite.Write("GET /" + args[0] + "\r\n");
            }
        }

        static public void h10Pro(string[] args, StreamWriter streamWrite)
        {
            if (args[1] != null)
            {
                streamWrite.Write("POST /" + args[0] + " HTTP/1.0\r\nContent-Length: " + args[1].Length + "\r\n" + args[1]);
            }
            else
            {
                streamWrite.Write("GET /?" + args[0] + " HTTP/1.0\r\n");
            }
        }

        static public void h11Pro(string[] args, StreamWriter streamWrite, string hostName)
        {
            if (args[1] != null)
            {
                streamWrite.Write("POST / HTTP/1.1\r\nHost: " + hostName + "\r\nContent-Length: " + (args[0].Length+args[1].Length+15) + "\r\nname=" + args[0] + "&location=" + args[1]);
            }
            else
            {
                streamWrite.Write("GET /?name=" + args[0] + " HTTP/1.1\r\nHost: " + hostName + "\r\n");
            }
        }

        static  public void match(string rawInput, string [] args)
        {            
            string whoisFindPass = @"^([ -~]*)\r\n$";
            string whoisFindFail = @"^ERROR: no entries found\r\n$";
            string whoisEdit = @"^\bOK\r\n$";
            string http09FindPass = @"^\bHTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n([ -~]*)\r\n$";
            string http09FindFail = @"^\bHTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n$";
            string http09Edit = @"^\bHTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n$";
            string http10FindPass = @"^\bHTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n([ -~]*)\r\n$";
            string http10FindFail = @"^\bHTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n\r\n$";
            string http10Edit = @"^\bHTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n$";
            string http11FindPass = @"^\bHTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n([ -~]*)\r\n$";
            string http11FindFail = @"^\bHTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\n$";
            string http11Edit = @"^\bHTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n$";

    

            if (Regex.IsMatch(rawInput, whoisFindFail) || Regex.IsMatch(rawInput, http09FindFail) || Regex.IsMatch(rawInput, http10FindFail) || Regex.IsMatch(rawInput, http11FindFail))
            {
                Console.WriteLine("ERROR: no entries found");
            }
            else if ((Regex.IsMatch(rawInput, whoisEdit) && args[1] != null) || Regex.IsMatch(rawInput, http09Edit) || Regex.IsMatch(rawInput, http10Edit) || Regex.IsMatch(rawInput, http11Edit))
            {
                Console.WriteLine(args[0] + " location changed to be " + args[1]);
            }
            else if (Regex.IsMatch(rawInput, http09FindPass))
            {
                foreach (Match info in Regex.Matches(rawInput, http09FindPass))
                {
                    Console.WriteLine(args[0] + " is " + info.Groups[1]);
                }
            }
            else if (Regex.IsMatch(rawInput, http10FindPass))
            {
                foreach (Match info in Regex.Matches(rawInput, http10FindPass))
                {
                    Console.WriteLine(args[0] + " is " + info.Groups[1]);
                }
            }
            else if (Regex.IsMatch(rawInput, http11FindPass))
            {
                foreach (Match info in Regex.Matches(rawInput, http11FindPass))
                {
                    Console.WriteLine(args[0] + " is " + info.Groups[1]);
                }
            }
            else if (Regex.IsMatch(rawInput, whoisFindPass))
            {
                foreach (Match info in Regex.Matches(rawInput, whoisFindPass))
                {
                    Console.WriteLine(args[0] + " is " + info.Groups[1]);
                }
            }

            
        }
    }
    
}
