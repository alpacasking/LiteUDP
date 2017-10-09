# LiteUDP
LiteUDP is a Lite UDP Library for C#,which is included by a asynchronous client and a asynchronous server.
You can just download the source code and complie it,then input any string you like on the client console and they will be showed on the server console.
### Feature
1.Use SocketAsyncEventArgs for asynchronous I/O;<br>
2.Use KCP as ARQ Protocol;<br>
3.Simple Session Manage;<br>
4.Two times handshake;<br>
### Usage
1.Client Command
connect:connect to server
send "anything you like":send message to server
exit:shutdown client
2.Server Command
start:start the server
exit:shutdown server

