# Swift Client CLI 

SwiftClient.Cli is a cross-platform console application compatible with DNX 4.5.1, it's main purpose is to transfer large objects into and out of OpenStack Swift. 
Large files are split into chunks of configurable size and uploaded to a temporary container, when all chunks are uploaded a merge operation is triggered on the server using Swift object manifest. 
The CLI also supports bulk upload of an entire directory tree, files and uploaded in parallel, the degree of parallelism is also configurable.

# Commands

SwiftClient.Cli will prompt for authentication if these environment variables are not present:

```
$ export SWIFT_URL=http://localhost:8080
$ export SWIFT_USER=test:tester
$ export SWIFT_KEY=testing
```

### Login

Usage:
```bash
login -h http://localhost:8080 -u test:tester -p testing
```

Result:
```
Connecting to http://localhost:8080 as test:tester
Authentication token received from http://localhost:8080/v1/AUTH_test
```

### Stats

List account statistics:
```bash
stats
```

Result:
```
 | ContainersCount | ObjectsCount | Size      |
 |--------------------------------------------|
 | 6               | 143          | 367.80 MB |
```

### List

List containers:
```bash
ls
```

Result:
```
 | Container | Objects | Size      |
 |---------------------------------|
 | docs      | 13      | 24.87 MB  |
 | images    | 117     | 26.25 MB  |
 | videos    | 8       | 205.29 MB |

```

List objects in a container:
```bash
ls -c docs
```

Result:
```
 | Object                                                | Size     | LastModified          |
 |------------------------------------------------------------------------------------------|
 | 2015/06/09/file-3c10cce7ca03453b87c65e75d4db850e.pdf  | 88.48 KB | 10/27/2015 2:48:43 PM |
 | 2015/07/10/file-2283bb7c4318481cbbacbe87ac850bbc.docx | 18.32 KB | 10/27/2015 2:48:42 PM |
 | 2015/07/23/file-2737d45f2d674f36a47cb622eaff2be3.pptx | 5.30 MB  | 10/27/2015 2:48:44 PM |
```

### Put

Upload a single file to a container:
```bash
put -c videos -o test.mp4 -f "C:\vid12GB.mp4"
```

Upload a single file to a container with a 20MB chunk size:
```bash
put -c videos -o test.mp4 -b 20 -f "C:\vid12GB.mp4"
```

Result:
```
Uploaded 12 GB
Upload done in 55 seconds
```

Upload a directory tree to a container: 
```bash
put -c videos  -f "C:\videos"
```

Upload a directory tree to a container with 10 parallel calls: 
```bash
put -c videos -p 10 -f "C:\videos"
```

Sub-directories will be added to the object name `sub-dir1/sub-dir2/file`.

Result:
```
Uploaded 20/20
Upload done in 2 minutes
```

### Get

Download file:
```bash
get -c videos -o test.mp4 -f "C:\my videos\my.mp4"
```

Result:
```
videos/test.mp4 downloaded to C:\my videos\my.mp4
```

### Remove

Delete file from container:
```bash
rm -c videos -o test.mp4
```

Result:
```
videos/test.mp4 deleted
```

Delete container and objects:
```bash
rm -c videos
```

Result:
```
videos deleted
```

