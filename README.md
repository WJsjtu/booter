# windows-startups
* * *
##功能
####Dectect the startups options on Windows
####检测开机启动项
* * *
##实现细节
###1.    Logon
主要就是：

HKLM\System\CurrentControlSet\Control\Terminal Server\Wds\rdpwd
下的StartupPrograms键所对应的值。
HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon下Userinit键所对应的值。
HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon下Shell键所对应的值（默认路径system32下）。
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run下的所有键值对，如果系统是64位的话，那么还有HKLM\SOFTWARE\Wow6432Node\Microsoft \Windows\CurrentVersion\Run下的所有键值对。
HKCU\Software\Microsoft\Windows\CurrentVersion\Run下所有的键值对。

###2.	Explore
在HKLM中：

SOFTWARE\Classes\Protocols\Filter下的CLSID的键值（COM）对，显示父节点的名字。
SOFTWARE\Classes\Protocols\Handler下的CLSID的键值（COM）对，显示父节点的名字。
SOFTWARE\Microsoft\Active Setup\Installed Components下的StubPath，显示兄弟节点（键为“”）的值。
64位还有SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components下的StubPath，显示兄弟节点（键为“”）的值。
SOFTWARE\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad下的所有键值对。
64位有SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad的所有键值对。
Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved的键名所对应的文件，显示的是对应的值，与之前的都反过来的。
同样的，64位也是Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved的键名所对应的文件，显示的是对应的值，与之前的都反过来的。

Software\Classes\Folder\Shellex\ColumnHandlers下的所有键值对。
64位下有Software\Wow6432Node\Classes\Folder\Shellex\ColumnHandlers下的所有键值对。

###3.	Internet Explorer
HKLM\Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects的文件夹COM文件，名字为其默认值。
64位下有HKLM\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects的文件夹COM文件，名字为其默认值。
HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\UrlSearchHooks的键所对应的文件

###4.	Service
在HKLM\System\CurrentControlSet\Services下寻找ImagePath键所对应的值的文件的后缀名是exe 或者是dll的值，显示父节点的名字，默认路径是system目录下的drivers文件夹。
###5.	Drivers
在HKLM\System\CurrentControlSet\Services下寻找ImagePath键所对应的值的文件的后缀名是sys的值，显示父节点的名字，默认路径是system目录下的drivers文件夹。
###6.	KnownDlls
HKLM\System\CurrentControlSet\Control\Session Manager\KnownDlls下所有dll文件，默认路径system文件夹，如果是64位系统那么还要包括（systemx86文件夹，即SysWOW64文件夹）。

###7.	Winsock Providers
位置在HKLM\System\CurrentControlSet\Services\WinSock2\Parameters\ Protocol_Catalog9下，但是内容在PackedCatalogItem键所对应的之当中。对于其功能的了解还是最初在
http://msdn.microsoft.com/en-us/library/windows/desktop/aa374737(v=vs.85).aspx
上得到的，后来，我为了知道如何读取其值的具体内容，通过谷歌的查找找到了
http://read.pudn.com/downloads3/sourcecode/windows/network/11373/winsock2/dll/winsock2/dcatitem.cpp__.htm
这篇文章，其实就是winsock2的源码……，里面读到了这么一个数据结构（C++）
```c
	// The following typedef is used in packing and unpacking catalog item data for   
	// reading and writing in the registry.   
	   
	typedef struct {   
	    char            LibraryPath[MAX_PATH];   
	        // The unexpanded path where the provider DLL is found.   
	   
	    WSAPROTOCOL_INFOW   ProtoInfo;   
	        // The  protocol information.  Note that if the WSAPROTOCOL_INFOW structure   
	        // is  ever changed to a non-flat structure (i.e., containing pointers)   
	        // then  this  type  definition  will  have  to  be changed, since this   
	        // structure must be strictly flat.   
	} PACKED_CAT_ITEM;  
```
新建一个C++的工程，测试一下MAX_PATH和WSAPROTOCOL_INFOW，发现MAX_PATH是系统的最大文件名长度（以前写大作业的时候查过，知道是260，但是如何编写C#代码获取还是不知道）。于是查到了
http://stackoverflow.com/questions/3406494/what-is-the-maximum-amount-of-characters-or-length-for-a-directory
，
```c
// reflection FieldInfo maxPathField = typeof(Path).GetField("MaxPath", 
    BindingFlags.Static | 
    BindingFlags.GetField | 
    BindingFlags.NonPublic ); 
// invoke the field gettor, which returns 260 int MaxPathLength = (int) maxPathField.GetValue(null);
WSAPROTOCOL_INFOW可以再MSDN http://technet.microsoft.com/zh-cn/library/ff565963上查到其结构：
typedef struct _WSAPROTOCOL_INFOW {
  DWORD            dwServiceFlags1;
  DWORD            dwServiceFlags2;
  DWORD            dwServiceFlags3;
  DWORD            dwServiceFlags4;
  DWORD            dwProviderFlags;
  GUID             ProviderId;
  DWORD            dwCatalogEntryId;
  WSAPROTOCOLCHAIN ProtocolChain;
  int              iVersion;
  int              iAddressFamily;
  int              iMaxSockAddr;
  int              iMinSockAddr;
  int              iSocketType;
  int              iProtocol;
  int              iProtocolMaxOffset;
  int              iNetworkByteOrder;
  int              iSecurityScheme;
  DWORD            dwMessageSize;
  DWORD            dwProviderReserved;
  WCHAR            szProtocol[WSAPROTOCOL_LEN+1];
} WSAPROTOCOL_INFOW, *LPWSAPROTOCOL_INFOW;
```
测试的结果是，C#程序中断点监视的值转为byte[]刚好为888长度（字节，C#中byte为8位），sizeof(WSAPROTOCOL_INFOW) 刚好是628，两者在本机上相差260，刚好符合PACKED_CAT_ITEM的描述

http://www.herongyang.com/Windows/Winsock-netsh-winsock-show-catalog-LSP.html 这篇文章中提到了命令行获取列表的方式.

###8.    Print Monitors
位置在HKLM\SYSTEM\CurrentControlSet\Control\Print\Monitors下的Driver所对应的值中，默认路径是System文件夹，显示的是父节点的名称。

###9.	LSA Providers
在HKLM\SYSTEM\CurrentControlSet\Control\SecurityProviders下SecurityProviders键所对应的值，默认路径为System文件夹，特别的是还要在64位系统中寻找SysWOW64文件夹下的对应文件。
在HKLM\SYSTEM\CurrentControlSet\Control\Lsa下找到Authentication Packages、Notification Packages和Security Packages键所对应的值，值得注意的是他们的值类型都是REG_MULTI_SZ，程序获取后应转为数组类型。对于其中每个值需要加上system文件夹的路径和dll问件的后缀名。
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Provider Filters
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\PLAP Providers
都是获取文件夹的名字所对应的COM组件文件就可以了。

###10.	 Network Providers
在HKLM\SYSTEM\CurrentControlSet\Control\NetworkProvider\Order中找到ProviderOrder所对应的值，他是一个用逗号分隔的一些列Provider的名字。
每一项的具体信息还要到HKLM\SYSTEM\CurrentControlSet\Services\ + 名字 + \NetworkProvider中ProviderPath和Name键中分别获得路径值和描述。
当然这些方法，我都是从http://msdn.microsoft.com/en-us/library/windows/desktop/aa374737(v=vs.85).aspx
中知道的。

###11.	Scheduled Tasks
这些都是C:\Windows\Tasks下的job文件的信息，所以只要解析了这些文件就行了。当然我说的轻松，做的也轻松……。在CodeProject上找到了
http://www.codeproject.com/Articles/2407/A-New-Task-Scheduler-Class-Library-for-NET
，这里它是导出到dll库的，我懒得拖一个文件在外面就将源码复制到了我的程序中去（就是那个Task文件夹）。
在TaskSchedulerInterop.cs文件中，通过它对于非托管代码的描述，可以详细的知道里面的数据结构和数据位置。
