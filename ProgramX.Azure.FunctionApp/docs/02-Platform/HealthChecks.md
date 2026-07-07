# Health Checks

## Services

Health Checks may be performed on services used by the application.

A list of services can be obtained using the `GET api/v1/healthcheck` endpoint. This returns a response that looks like:

```json
{
  "timeStamp": "2020-01-01T00:00:00Z",
  "services": [
    {
      "friendlyName": "My Service",
      "imageUrl": "https://example.com/my-service.png",
      "name": "unique-service-name",
      "url": "https://example.com/api/v1/healthcheck/unique-service-name"
    }
  ]
}
```

| Property | Description |
| --- | --- |
| `timeStamp` | The timestamp of the health check |
| `services` | An array of services |

Where each _Service_ in `services` has the following properties:

| Property | Description                                                                           |
| --- |---------------------------------------------------------------------------------------|
| `friendlyName` | A friendly name for the service                                                       |
| `imageUrl` | The URL of an image representing the service                                          |
| `name` | The name of the service. This may be used to get the health of the individual service |
| `url` | The URL of the health check endpoint for the service                                  |

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

Where:

| Property | Description                                                                 |
| --- |-----------------------------------------------------------------------------|
| `name` | The name of the service                                                     |
| `timeStamp` | The timestamp of the health check                                           |
| `isHealthy` | A boolean that indicates if the service is healthy                          |
| `message` | A friendly message describing the health of the service                     |
| `subItems` | An array of sub items that were checked (such as components of the service) |

Where each _Sub-item_ in `subItems` has the following properties:

| Property | Description                                                                 |
| --- |-----------------------------------------------------------------------------|
| `name` | The name of the sub item                                                    |
| `friendlyName` | A friendly name for the sub item                                            |
| `message` | A message describing the health of the sub item                             |
| `isHealthy` | A boolean that indicates if the sub item is healthy                         |

## Applications

Application health checks may be performed using the endpoint `GET api/v1/applications/{application-name}/health` and will return a response like:

```json
{
  "name": "My application",
  "timeStamp": "2020-01-01T00:00:00Z",
  "isHealthy": true,
  "message": "Application is healthy",
  "items": [
    {
      "isHealthy": true,
      "message": "Component is healthy",
      "healthCheckName": "Component health check",
      "friendlyName": "Component"
    }
  ]
}
```

Where:

| Property | Description                                                                     |
| --- |---------------------------------------------------------------------------------|
| `name` | The name of the application                                                     |
| `timeStamp` | The timestamp of the health check                                               |
| `isHealthy` | A boolean that indicates if the Application is healthy                          |
| `message` | A friendly message describing the health of the Application                     |
| `items` | An array of sub items that were checked (such as components of the Application) |

Individual components of the Application are returned in the `items` array and have the following properties:

| Property | Description |
| --- | --- |
| `isHealthy` | A boolean that indicates if the sub item is healthy |
| `message` | A message describing the health of the sub item |
| `healthCheckName` | The name of the health check that was performed |
| `friendlyName` | A friendly name for the sub item |
