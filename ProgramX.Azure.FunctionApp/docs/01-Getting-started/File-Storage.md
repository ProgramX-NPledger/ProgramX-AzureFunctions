# File Storage

File Storage is achieved using Azure Storage, which provides cheap and fast storage of files that can be easily referenced.

## Storing files

Files are stored according to their purpose, with the following purposes being used:

|Purpose|Name|Description|
|-------|----|-----------|
|Profile images|`BlobNames.AvatarImages`|Profile images|

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

The location of the file must be stored to allow for its re-use.

### Internal management of files

Internally, files are stored within a folder for their purpose, as defined by the `imageType` parameter.
Within this, the original file is stored in the following pattern:

A typical file path would be:
`(purpose)/(guid.ext)/original.ext`

Where:

| Parameter | Description                                                   | Example                                   |
|-----------|---------------------------------------------------------------|-------------------------------------------|
| `purpose` | The purpose of the file                                       | `AvatarImages`                            |
| `guid`    | A unique filename to avoid possibility of filename collisions | `01234567-1234-1234-1234-123467890ab.ext` |
| `ext`     | The original file extension                                   | `jpg`                                     |

Resized images are stored in the same folder, with the following naming convention:
`(purpose)/(guid.ext)/(filename)_[wnn]_[hnn]_[mwnn]_[mhnn].(ext)`

Where:

| Parameter  | Description                                                          |Example|
|------------|----------------------------------------------------------------------|-------|
| `purpose`  | The purpose of the file                                              |`AvatarImages`|
| `guid`     | A unie filename to avoid possibility of filename collision           | `01234567-1234-1234-1234-1234567890ab` |
| `filename` | The original filename, excluding the extension                       |`user`|
| `ext`      | The original file extension                                          |`jpg`|
| `wnn`      | The width of the image. `nn` refers to the number of pixels.         |`100`|
| `hnn`      | The height of the image. `nn` refers to the number of pixels.        |`100`|
| `mwnn`     | The maximum width of the image. `nn` refers to the number of pixels  | `100` |
| `mhnn`     | The maximum height of the image. `nn` refers to the number of pixels | `100` |

An index file is also stored in the same folder, named `blobIndexEntry.json`. This contains:

* Original filename
* The roles required to access the file
* The content type of the file

## Retrieving files

Files are served by a common endpoint, which accepts the filename and required dimensions.

Files may be retrieved using the endpoint `GET /api/files/{imageType}/{filename.ext}?[width=(width)&][height=(height)][&maximumWidth=(maximum-width][&maximumHeight=(maximum-height)]`.

Where:

| Parameter       | Description                                                                                                                                |Example|
|-----------------|--------------------------------------------------------------------------------------------------------------------------------------------|-------|
| `imageType`     | The type of image to be returned. This is used to determine the roles required to access the image.                                        |`AvatarImages`|
| `filename.ext`  | The original filename.                                                                                                                     |`user.jpg`|
| `width`         | The width of the image to be returned. If not specified, and a `h` is specified, a resized image to the correct aspect-ratio is returned.  |`100`|
| `height`        | The height of the image to be returned. If not specified, and a `w` is specified, a resized image to the correct aspect-ratio is returned. |`100`|
| `maximumWidth`  | The maximum width of the image to be returned, after aspect ratio has been respected.                                                      | `100` |
| `maximumHeight` | The maximum height of the image to be returned, after aspect ratio has been respected.                                                     | `100` |

If no `width`, `height`, `maximumWidth` or `maximumHeight` is specified, the original file is returned.

A caching layer is provided, to ensure rapid retrieval of files. If the requested dimensions are not available for the image, the image is resized, stored/cached and served.

The endpoint will return a response indicating success or otherwise.

| Response | Description                             |
|----------|-----------------------------------------|
| 200      | OK. The image will be served.           |           
| 400      | Bad request. A reason will be provided. |
| 401      | Unauthorized.                           |
| 404      | Not Found. The image does not exist.    |
