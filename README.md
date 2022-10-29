节点端由node程序和server程序组成，你需要在同一台电脑上同时运行node程序和server程序。  
server程序是miner和node的代理，它将以往的轮询交互改成响应式交互，减少服务器流量。  
node教程 https://github.com/easy-3dp/3DP/blob/main/tutorial_cn.md  
  
运行命令，第一个参数`9933`是node的`rpc-port`，第二个参数`9999`是给miner连接的
```sh
dotnet Server.dll 9933 9999
```
  
  
  
The node side is composed of a node program and a server program, you need to run both node program and server program on same computer.   
The server program is a proxy for miner and node, changing polling interaction to reactive interaction to reduce server traffic.  
node tutorial https://github.com/easy-3dp/3DP/blob/main/tutorial_en.md  

How to run: The first parameter `9933` is node's `rpc-port`，The second parameter `9999` is for miner connection
```sh
dotnet Server.dll 9933 9999
```