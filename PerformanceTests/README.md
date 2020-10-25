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

| Name | Params | Run1 | Run2 | Run3 | API calls|
|--|--|
| ListSimple         | | 36 | 31 | 33 | 23 |
| ListAsync          | | 38 | 30 | 34 | 23 |
| PartiallyRecursive | 0 | 30 | 31 | 29 | 31 |
| PartiallyRecursive | 1 | 8.5 | 9.2 | 9.0 | 111 |
| PartiallyRecursive | 2 | 7.8 | 9.0 | 8.9 | 1110 |
| PartiallyRecursive | >2 | 10.6 | 8.7 | 10.3 | 1110 |


