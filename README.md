# LiteUDP
LiteUDP is a Lite UDP Library for C#,which is included by a asynchronous client and a asynchronous server.
You can just download the source code and complie it,then input any string you like on the client console and they will be showed on the server console.
### Feature
1.Use SocketAsyncEventArgs for asynchronous I/O;<br>
2.Use KCP as ARQ Protocol;<br>
3.Simple Session Manage;<br>
4.Two times handshake;<br>
### Usage
1.Client Command<br>
connect:connect to server<br>
send "anything you like":send message to server<br>
loop "anything you like":send message to server,1 time per 10ms<br>
exit:shutdown client<br>
2.Server Command<br>
start:start the server<br>
exit:shutdown server<br>

