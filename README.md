# 中文
节点端由node程序和server程序组成，你需要在同一台电脑上同时运行node程序和server程序。  
server程序是miner和node的代理，它将以往的轮询交互改成响应式交互，减少服务器流量。    
node教程 https://github.com/easy-3dp/3DP/blob/main/tutorial_cn.md  
  
<font color=red>注意</font>运行这个程序需要安装.net6，安装教程：  
```sh
apt update && apt install -y dotnet-sdk-6.0
```
  
所有参数
```sh
--node-url  节点地址
--node-port 节点端口，默认server程序和node程序在同一台电脑
--port      对锄头的端口
--pool-id   官方版矿池id
--interval  轮询节点的时间间隔，单位毫秒
```
如果你想solo，你的server程序和node程序在同一台电脑里：
```sh
dotnet Server.dll --node-port 9933 --port 9999
```
如果你想solo，你的server程序和node程序不在同一台电脑里：
```sh
dotnet Server.dll --node-url http://ip:9933 --port 9999
```
如果你想pool，你的server程序和node程序在同一台电脑里：
```sh
dotnet Server.dll --node-port 9933 --port 9999 --pool-id d1...
```
如果你想pool，你的server程序和node程序不在同一台电脑里：
```sh
dotnet Server.dll --node-url http://ip:9933 --port 9999 --pool-id d1...
```
        
        
# EN
The node side is composed of a node program and a server program, you need to run both node program and server program on same computer.   
The server program is a proxy for miner and node, changing polling interaction to reactive interaction to reduce server traffic.  
node tutorial https://github.com/easy-3dp/3DP/blob/main/tutorial_en.md  

<font color=red>Note</font> that to run this program you need to install .net6, installation tutorial:  
```sh
apt update && apt install -y dotnet-sdk-6.0
```
  
All parameters
```sh
--node-url  node url
--node-port node port (server program and node program are considered to be on the same computer)
--port      for miner
--pool-id   mining pool id
--interval  for polling the node, in milliseconds
```
If you want to solo mine and your server program and node program are on the same computer:
```sh
dotnet Server.dll --node-port 9933 --port 9999
```
If you want to solo mine and your server program and node program are on different computers:
```sh
dotnet Server.dll --node-url http://ip:9933 --port 9999
```
If you want to mine in a pool and your server program and node program are on the same computer:
```sh
dotnet Server.dll --node-port 9933 --port 9999 --pool-id d1...
```
If you want to mine in a pool and your server program and node program are on different computers:
```sh
dotnet Server.dll --node-url http://ip:9933 --port 9999 --pool-id d1...
```
