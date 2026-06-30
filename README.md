# EewcnJsCT
添加[CysTerra](https://www.cryville.world/Projects/A017)对EEWCN自定义脚本的支持。

目前仅有Windows版本。

# 使用方法
先将custom.js文件放在以下任一位置，同时将settings.json放在跟custom.js相同的位置（若有），扩展会按以下次序查找脚本文件：
1. CysTerra程序文件所在目录
2. `（假设系统盘是C盘）C:\Users\（你的用户名）\AppData\Roaming\lxfly2000\eewcn\custom.js`

打开CysTerra的扩展页面，将该扩展的zip文件拖入，安装好后分别在事件源、事件详情、事件摘要页面中添加“EEWCN自定义脚本数据源”，配置完成后重启CysTerra生效。

日志文件位置：`(CysTerra程序文件所在目录)\eewcnjs.log`
