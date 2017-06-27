
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Namespace TCPServerVB
    Class ProgramVB
        Public Shared Sub Main()
            ' File name for received picture 

            Const FILE_NAME As [String] = "Received.jpg"

            ' Create a buffer for receiving 

            Dim receiveBytes As Byte() = New Byte(1023) {}

            ' The IPEndPoint for the server. IP cannot be localhost 

            Dim remoteIpEndPoint As New IPEndPoint(IPAddress.Parse("192.168.1.8"), 3800)

            ' After this amount of time has passed, any connection will be terminated
            '             * Keep high for high latency networks and vice versa 

            Const TIMEOUT As Integer = 1000

            ' Start listening for connections 

            Dim tcpListener As New TcpListener(remoteIpEndPoint)
            tcpListener.Start()

            ' The socket that will be used for listening 

            Dim sock As Socket = Nothing

            ' FileStream for writing 

            Dim objWriter As FileStream = Nothing

            ' Number and total number of bytes read till the end of loop 

            Dim bytesRead As Integer = 0
            Dim totalBytesRead As Integer = 0

            ' Loop till something is read 

            While totalBytesRead = 0

                ' Sleep for 100ms if no connection is being made 

                While Not tcpListener.Pending()
                    Thread.Sleep(100)
                End While

                sock = tcpListener.AcceptSocket()
                Console.WriteLine("Accepted Connection")
                sock.ReceiveTimeout = TIMEOUT

                ' Sleep for another 100ms to give the client time to respond 

                Thread.Sleep(100)
                Dim filesize As Integer = 0
                Try
                    ' Receive the header, terminate if not found 

                    If (Assign(bytesRead, sock.Receive(receiveBytes))) > 0 Then
                        Dim headers As String() = System.Text.Encoding.ASCII.GetString(receiveBytes).Split(ControlChars.Lf)
                        If headers(0) = "HEADER" Then
                            Console.WriteLine("Receiving file of size " + headers(1) + " bytes")
                            Int32.TryParse(headers(1), filesize)
                        Else
                            Throw New Exception("No header received")
                        End If
                    Else
                        Throw New Exception("No header received")
                    End If

                    While (totalBytesRead <> filesize) AndAlso (Assign(bytesRead, sock.Receive(receiveBytes, receiveBytes.Length, SocketFlags.None))) > 0
                        ' Delete existing file to be safe 

                        If objWriter Is Nothing Then
                            If File.Exists(FILE_NAME) Then
                                File.Delete(FILE_NAME)
                            End If
                            objWriter = File.OpenWrite(FILE_NAME)
                        End If

                        objWriter.Write(receiveBytes, 0, bytesRead)

                        totalBytesRead += bytesRead

                        ' Reduce buffer size if bytes left are less than it 

                        If filesize - totalBytesRead < receiveBytes.Length Then
                            receiveBytes = New Byte(filesize - totalBytesRead - 1) {}
                        End If
                    End While
                Catch e As Exception
                    Console.WriteLine(e.Message)
                End Try

                ' Close everything 

                sock.Close()
                If Not (objWriter Is Nothing) Then
                    objWriter.Close()
                    objWriter = Nothing
                End If
                Console.WriteLine("Closed Connection")
            End While
            ' Clean up and open the received file 

            tcpListener.Stop()
            System.Diagnostics.Process.Start(FILE_NAME)
            Console.ReadKey()
        End Sub
        Private Shared Function Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Namespace