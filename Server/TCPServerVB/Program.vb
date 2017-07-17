Namespace TCPServerVB
    Class ProgramVB
        Public Shared Sub Main()
            ' File name for received picture 
            Const FILE_NAME As [String] = "Received.jpg"

            ' IP Address for incoming connections 
            Dim IP_ADDRESS As String = GetLocalIPAddress()

            ' Port for incoming connections
            Const PORT As Integer = 3800

            ' Create a buffer for receiving 
            Dim receiveBytes As Byte() = New Byte(1023) {}

            ' The IPEndPoint for the server. IP cannot be localhost 
            Dim remoteIpEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Parse(IP_ADDRESS), PORT)
            Console.WriteLine("Listening for connections on " + GetLocalIPAddress() + ":" + CStr(PORT))

            ' After this amount of time has passed, any connection will be terminated
            ' Keep high for high latency networks and vice versa 

            Const TIMEOUT As Integer = 1000

            ' Start listening for connections 

            Dim tcpListener As New System.Net.Sockets.TcpListener(remoteIpEndPoint)
            tcpListener.Start()

            ' The socket that will be used for listening 

            Dim sock As System.Net.Sockets.Socket = Nothing

            ' FileStream for writing 

            Dim objWriter As IO.FileStream = Nothing

            ' Number and total number of bytes read till the end of loop 

            Dim bytesRead As Integer = 0
            Dim totalBytesRead As Integer = 0

            ' Loop till something is read 

            While totalBytesRead = 0

                ' Sleep for 100ms if no connection is being made 

                While Not tcpListener.Pending()
                    System.Threading.Thread.Sleep(100)
                End While

                sock = tcpListener.AcceptSocket()
                Console.WriteLine("Accepted Connection")
                sock.ReceiveTimeout = TIMEOUT

                ' Sleep for another 100ms to give the client time to respond 

                System.Threading.Thread.Sleep(100)
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

                    While (totalBytesRead <> filesize) AndAlso (Assign(bytesRead, sock.Receive(receiveBytes, receiveBytes.Length, System.Net.Sockets.SocketFlags.None))) > 0
                        ' Delete existing file to be safe 

                        If objWriter Is Nothing Then
                            If System.IO.File.Exists(FILE_NAME) Then
                                System.IO.File.Delete(FILE_NAME)
                            End If
                            objWriter = System.IO.File.OpenWrite(FILE_NAME)
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

        Private Shared Function GetLocalIPAddress() As String
            Dim host As System.Net.IPHostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
            For Each ip As Net.IPAddress In host.AddressList
                If (ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork) Then
                    Return ip.ToString()
                End If
            Next
            Throw New Exception("Local IP Address Not Found!")
        End Function

        Private Shared Function Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Namespace