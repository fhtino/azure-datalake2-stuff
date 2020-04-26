# ADLSApplyPerms
A tool for massively updating ACL permissions on Azure Data Lake Gen2 files and directories.  

### Usage
The tool updates files and directories ACLs, adding and updating existing rules and removing specified users or groups. 
Existing ACL non-matching with supplied ACL are left as is.  
To run the tool, you need to supply a configuration file - see below - with all required parameters to connect to the storage, ACL to add/update and users/groups to be removed.   

```
dotnet ADLSApplyPerms.dll  myconfig.json
```

### Configuration file

Permissions must be expressed as POSIX-style ACL. 
AzureAD users and group must be speficied in different mode: users as "user@domain" and groups as GUID.
"Default" permissions are applied only to directories. Non-default permissions are applied both to files and directories.

More information about Data Lake Gen2 permissions here: 
https://docs.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-access-control

Configuration file example:
```json
{
  "AccountName": "storagename",
  "AccountKey": "1wb6X...",
  "FileSystem": "myfs",
  "StartingPath": "folderx/subfolder",
  "ExitAfter": -1,
  "LogVerbose": false,
  "Parallelism": 8,
  "ACL": [
    "user:ele@contoso.onmicrosoft.com:r--",
    "default:user:ele@contoso.onmicrosoft.com:r-x",
    "group:11111e4b-964b-46e4-af2e-aaaaacfa0ca07:rwx",
    "default:group:11111e4b-964b-46e4-af2e-aaaaacfa0ca07:rwx"
  ],
  "RemoveList": [
    "jane@contoso.onmicrosoft.com",
    "chiara@contoso.onmicrosoft.com"
  ]
}
```

<br/>

### Results example

Result on a file:

<table>
<tr><td>Before</td><td>After</td></tr>
<tr>
<td valign="top">
<pre><code>user::rw-
user:dario@contoso.onmicrosoft.com:rwx
user:chiara@contoso.onmicrosoft.com:r--
group::r-x
group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
mask::rw-
other::---</code></pre>
</td>
<td>
<pre><code>user::rw-
user:dario@contoso.onmicrosoft.com:rwx
user:ele@contoso.onmicrosoft.com:r--
group::r-x
group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
group:11111e4b-964b-46e4-af2e-aaaaacfa0ca07:rwx
mask::rw-
other::---</code></pre>
</td>
</table>
 


Result on a group:

<table>
<tr><td>Before</td><td>After</td></tr>
<tr>
<td valign="top">
<pre><code>
user::rwx
user:dario@contoso.onmicrosoft.com:r--
group::rwx
group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
mask::rwx
other::---
default:user::rwx
default:user:chiara@contoso.onmicrosoft.com:r-x
default:group::rwx
default:group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
default:mask::rwx
default:other::---
</code></pre>
</td>
<td>
<pre><code>
user::rwx
user:dario@contoso.onmicrosoft.com:r--
user:ele@contoso.onmicrosoft.com:r--
group::rwx
group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
group:11111e4b-964b-46e4-af2e-aaaaacfa0ca07:rwx
mask::rwx
other::---
default:user::rwx
default:user:ele@contoso.onmicrosoft.com:r-x
default:group::rwx
default:group:aaaaaaaa-e5f1-499e-9719-bbbbbbbbbbbb:rwx
default:group:11111e4b-964b-46e4-af2e-aaaaacfa0ca07:rwx
default:mask::rwx
default:other::---
</code></pre>
</td>
</table>
  
  
