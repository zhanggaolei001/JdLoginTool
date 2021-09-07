后端面板接口使用示例项目:https://github.com/zhanggaolei001/raypanel

一个基于CefSharp的京东cookie获取工具
运行后,登录京东,会自动将cookie复制到粘贴板.

新增:配置文件中增加了上传到服务器的选项,可以把获取到的cookie,提交给指定服务器接口,如果你的面板支持,可以简化操作,减少手动复制粘贴.
服务端接口示例:
 ```JavaScript
app.post('/addck', function (request, response) {
        try {
             const cookie = request.query.ck;
             AutoAddCK(cookie, "");//这里逻辑请根据实际实现新ck的后台添加等逻辑.
	     response.send({ err: 0, msg: '添加成功' });
        }
        catch (err) {
            response.send({ err: 1, msg: err });
        }
})
 ```


主要提供了验证码登录并自动获取cookie功能,规避夸地域登录判定导致的登陆失败. 
目前cookie有效期应该恢复30天了。
## 介绍

本项目是我学习C#/WPF操作cefSharp/selenium的一个🌰，也是一种京东获取Cookie的方案.

## 声明

1. 本项目仅限于学习研究selenium库的操作，以及一些C#/WPF知识

## 须知

1. 需要.net core 3运行环境,根据提示自己下载安装(两步Step 1: .NET Core Runtime and Step 2: .NET Core Desktop Runtime) [运行环境下载传送门](https://dotnet.microsoft.com/download/dotnet/3.0/runtime)
2. 迫于wpf未跨平台,仅用于windows下使用.
3. 可以直接下载编译过的. [release](https://github.com/zhanggaolei001/JdLoginTool/releases)
4. 如果有安全警告,可添加信任或临时关闭安全软件.
## 最后
别忘了点亮那颗星哦
