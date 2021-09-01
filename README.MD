一个基于CefSharp的京东cookie获取工具

主要提供了验证码登录并自动获取cookie功能,规避夸地域登录判定导致的登陆失败.

运行后,登录,会自动将cookie复制到粘贴板,确定后会自动清空cookie,便于下一个手机号使用.

目前cookie有效期最长为24小时,且不稳定,暂时没啥别的办法.

## 介绍

本项目是我学习C#/WPF操作cefSharp/selenium的一个🌰，也是一种京东获取Cookie的方案.

## 声明

1. 本项目仅限于学习研究selenium库的操作，以及一些C#/WPF知识

## 须知

1. 需要.net core 3运行环境,根据提示自己下载安装 [运行环境下载传送门](https://dotnet.microsoft.com/download/dotnet/3.0/runtime)
2. 迫于wpf未跨平台,仅用于windows下使用.
3. 可以直接下载编译过的. [release](https://github.com/zhanggaolei001/JdLoginTool/releases/tag/v1)
4. 如果有安全警告,可添加信任或临时关闭安全软件.
## 最后
别忘了点亮那颗星哦
