using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            /* File name for received picture */
            const String FILE_NAME = "Received.jpg";

            /* Create a buffer for receiving */
            byte[] receiveBytes = new byte[1024];

            /* The IPEndPoint for the server. IP cannot be localhost */
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.8"), 3800);

            /* After this amount of time has passed, any connection will be terminated
             * Keep high for high latency networks and vice versa */
            const int TIMEOUT = 1000;

            /* Start listening for connections */
            TcpListener tcpListener = new TcpListener(remoteIpEndPoint);
            tcpListener.Start();

            /* The socket that will be used for listening */
            Socket sock = null;

            /* FileStream for writing */
            FileStream objWriter = null;

            /* Number and total number of bytes read till the end of loop */
            int bytesRead = 0;
            int totalBytesRead = 0;            

            /* Loop till something is read */
            while (totalBytesRead == 0) {

                /* Sleep for 100ms if no connection is being made */
                while (!tcpListener.Pending()) Thread.Sleep(100);

                sock = tcpListener.AcceptSocket();
                Console.WriteLine("Accepted Connection");
                sock.ReceiveTimeout = TIMEOUT;

                /* Sleep for another 100ms to give the client time to respond */
                Thread.Sleep(100);
                try
                {
                    while ((bytesRead = sock.Receive(receiveBytes)) > 0)
                    {
                        /* Delete existing file to be safe */
                        if (objWriter is null)
                        {
                            if (File.Exists(FILE_NAME)) File.Delete(FILE_NAME);
                            objWriter = File.OpenWrite(FILE_NAME);
                        }
                        totalBytesRead += bytesRead;
                        objWriter.Write(receiveBytes, 0, bytesRead);
                        Console.Write("Received till " + totalBytesRead.ToString()+"\r");
                    }
                }
                catch (Exception e)
                {
                    /* Write remaining bytes after timeout */
                    if (e is SocketException)
                    {
                        /* Initialize file if necessary, in case payload is less than buffer size */
                        if (objWriter is null)
                        {
                            if (File.Exists(FILE_NAME)) File.Delete(FILE_NAME);
                            objWriter = File.OpenWrite(FILE_NAME);
                        }
                        objWriter.Write(receiveBytes, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        Console.WriteLine("Received till " + totalBytesRead.ToString());
                        Console.WriteLine("COMPLETED");
                    }
                    else Console.WriteLine(e.GetType().ToString());
                }

                /* Close everything */
                sock.Close();
                if (!(objWriter is null))
                {
                    objWriter.Close();
                    objWriter = null;
                }
                Console.WriteLine("Closed Connection");
            }
            /* Clean up and open the received file */
            tcpListener.Stop();
            Process.Start(FILE_NAME);
            Console.ReadKey();
        }
    }
}
