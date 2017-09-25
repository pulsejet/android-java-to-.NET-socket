# android-java-to-.NET-socket

Simple android client in Java that transfers a single file over a wireless network to an IP:Port over a socket to the server coded in C# or VisualBasic.NET.
The client currently clicks a photo using the camera intent.

## Protocol

Communication is only from client to server. Before sending the image, the clients sends the word "HEADER" (without quotes) followed by a newline, then the exact size of the file to be sent in bytes followed by another newline and then the contents of the file. Following this, the connection is closed.

To test connectivity, the client opens and closes a connection without sending any data.

## Notes:

 - The client transfers a single photograph right now after compressing and resizing. This can be changed as required.
 - The server IP and port are autodetected.
 - The file name for the received file is hard coded in the server. This can be modified if required by adding a file name to the header in the client's first buffer.

## Binaries:

Binaries for the server and the client can be downloaded from [releases](https://github.com/radialapps/android-java-to-.NET-socket/releases). The client is also available on [Google Play](https://play.google.com/store/apps/details?id=com.radial.client).