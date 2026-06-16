# Health Checks

Health Checks may be performed on services used by the application.

A list of services can be obtained using the `GET api/v1/healthcheck` endpoint. This returns a JSON array that looks like:

```json
[
    {
        "friendlyName": "My Service",
        "imageUrl": "https://example.com/my-service.png",
        "name": "unique-service-name",
        "url": "https://example.com/api/v1/healthcheck/unique-service-name"
    }
]
```

The `name` can be used to identify the service to perform the health check. The `url` can also be used to perform the health check. This URL can be used to perform a health check on the service.

The `GET api/v1/healthcheck/unique-service-name` endpoint will perform the health check and return a response like:

```json
{
  "name": "unique-service-name",
  "timeStamp": "2020-01-01T00:00:00Z",
  "isHealthy": true,
  "message": "Service is healthy",
  "subItems": [
    {
      "isHealthy": true,
      "friendlyName": "Service component",
      "message": "",
      "name": ""
    }
  ]
}
```

The `message` contains a friendly message describing the health of the service. The `subItems` property contains an
array of sub items that were checked (such as components of the service). Each sub item has a `name`, `friendlyName`, `message`, and `isHealthy` property.
The `name` property is the name of the sub item. The `friendlyName` property is a friendly name for the sub item. The `message` property contains a message describing the health of the sub item. The `isHealthy` property is a boolean that indicates if the sub item is healthy.
