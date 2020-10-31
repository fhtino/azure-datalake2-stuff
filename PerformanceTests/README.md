\* \* \* **DRAFT** \* \* \*

### On my machine with my internet connection

 - client location: Italy
 - azure datacenter: West Europe (Nederland)
 - date: 2020-10-25
 - library: Azure.Storage.Files.DataLake 12.4.0
 - content: 112210 items
   - 1110 folders on 3 levels
     - folder1\folder2\folder3
   - 111100 files (100 files/folder)


#### Results

| Name | Params | Run1 | Run2 | Run3 | API calls |
| - | - | - | - | - | - |
| ListSimple         | | 36 | 31 | 33 | 23 |
| ListAsync          | | 38 | 30 | 34 | 23 |
| PartiallyRecursive | 0 | 30 | 31 | 29 | 31 |
| PartiallyRecursive | 1 | 8.5 | 9.2 | 9.0 | 111 |
| PartiallyRecursive | 2 | 7.8 | 9.0 | 8.9 | 1110 |
| PartiallyRecursive | >2 | 10.6 | 8.7 | 10.3 | 1110 |


**BE CAREFUL!!!**

Calling DataLake Storage API is quite expensive, in particular write operations.  
https://azure.microsoft.com/en-us/pricing/details/storage/data-lake/

The above tests costed me around € 2.38. As you can see in the table below, the main cost is related to creating files. For each file I have to create the file, append data and flush it.

| API |	Calls |	Type	| Price	| Total € |
| - | - | - | - | - |
| CreatePathFile	| 113850	| WriteOperation	| 0,00000592	| 0,6740 |
| AppendFile	| 113850	| WriteOperation	| 0,00000592	| 0,6740 |
| FlushFile	| 113850	| WriteOperation	| 0,00000592	| 0,6740 |
| ListFilesystemDir	| 57650	| IterativeRead	| 0,00000592	| 0,3413 |
| CreatePathDir	| 3720	| WriteOperation	| 0,00000592	| 0,0220 |
| | | | | **2,3853** |



