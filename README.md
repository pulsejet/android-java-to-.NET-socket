# android-java-to-.NET-socket

Simple android client in Java that transfers a single file over a wireless network to an IP:Port over a socket to the server coded in C# or VisualBasic.NET.
The client currently clicks a photo using the camera intent.

## Notes:

 - The client transfers a single photograph right now after compressing and resizing. This can be changed as required.
 - The server IP and port are autodetected.
 - The file name for the received file is hard coded in the server. This can be modified if required by adding a file name to the header in the client's first buffer.