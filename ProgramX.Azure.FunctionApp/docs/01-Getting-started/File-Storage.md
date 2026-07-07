# File Storage

File Storage is achieved using Azure Storage, which provides cheap and fast storage of files that can be easily referenced.

## Storing files

Files are stored according to their purpose, with the following purposes being used:

|Purpose|Name|Description|
|-------|----|-----------|
|Profile images|`BlobNames.AvatarImages`|Profile images|

References to files are stored as their _original filename_, including the purpose of the file.

Therefore a profile photo will be stored as `AvatarImages/user.jpg`. This refers to the original, un-resized filename.

Files are stored using the endpoint: `POST /api/v1/file/{imageType}/{filename.ext}?[mustHaveAnyOfRoles=(roles)]`.

Where:

|Parameter|Description|Example|
|---------|-----------|-------|
|`filename.ext`|The original filename, including the path and extension|`AvatarImages/user.jpg`|
|`mustHaveAnyOfRoles`|A comma-separated list of roles that the user must have to access the file|`Admin,User`|
|`imageType`|The type of image being uploaded|`Avatar`|

The endpoint will return a response indicating success or otherwise.

| Response | Description                                                                                                                                                                    |
|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 201      | Created. The `Location` HTTP Header will contain the URL required to retrieve the first created item. More details of the created object will be returned in the body payload. |
| 400      | Bad request. A reason will be provided.                                                                                                                                        |
| 401      | Unauthorized.                                                                                                                                                                  |

On success, a location of the file will be returned.

```json
{
  "fileNames": [
    "user.jpg"
  ]
}
```

### Internal management of files

Internally, files are stored within a folder for their purpose, as defined by the `imageType` parameter.
Within this, the original file is store

A typical file path would be:
`(purpose)/(filename)/original.ext`

Where:

|Parameter| Description                                    |Example|
|---------|------------------------------------------------|-------|
|`purpose`| The purpose of the file                        |`AvatarImages`|
|`filename`| The original filename, excluding the extension |`user`|
|`ext`| The original file extension                    |`jpg`|

Resized images are stored in the same folder, with the following naming convention:
`(purpose)/(filename)/[wnn][hnn].(ext)`

Where:

|Parameter| Description                                                  |Example|
|---------|--------------------------------------------------------------|-------|
|`purpose`| The purpose of the file                                      |`AvatarImages`|
|`filename`| The original filename, excluding the extension               |`user`|
|`ext`| The original file extension                                  |`jpg`|
|`wnn`| The width of the image. `nn` refers to the number of pixels. |`100`|
|`hnn`| The height of the image. `nn` refers to the number of pixels.                                     |`100`|

An index file is also stored in the same folder, named `blobIndexEntry.json`. This contains:

* Original filename
* The roles required to access the file

## Retrieving files

Files are served by a common endpoint, which accepts the filename and required dimensions.

Files may be retrieved using the endpoint `GET /api/files/{imageType}/{filename.ext}?[w=(width)&][h=(height)]`.

Where:

|Parameter| Description                                                                                                                                                |Example|
|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-------|
|`imageType`| The type of image to be returned. This is used to determine the roles required to access the image.                                                       |`AvatarImages`|
|`filename.ext`| The original filename.  |`user.jpg`|
|`w`| The width of the image to be returned. If not specified, and a `h` is specified, a resized image to the correct aspect-ratio is returned.                  |`100`|
|`h`| The height of the image to be returned. If not specified, and a `w` is specified, a resized image to the correct aspect-ratio is returned.                 |`100`|

If no `w` or `h` is specified, the original file is returned.

A caching layer is provided, to ensure rapid retrieval of files. If the requested dimensions are not available for the image, the image is resized, stored/cached and served.

The endpoint will return a response indicating success or otherwise.

| Response | Description                             |
|----------|-----------------------------------------|
| 200      | OK. The image will be served.           |           
| 400      | Bad request. A reason will be provided. |
| 401      | Unauthorized.                           |
| 404      | Not Found. The image does not exist.    |
