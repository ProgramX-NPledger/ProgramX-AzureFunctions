# Development principles

Development will be on/off and must survive periods of inactivity - and therefore must be well documented. This
avoids the peceived need to re-learn the same concepts over and over again and/or th refactor code.

In order to keep the code base clean and maintainable, the following principles should be followed.

## Architecture

* Access to APIs is via interfaces, which provide testability and abstraction.
* Test coverage must >= 80%
* All entities sent across API boundaries (eg. back-end -> front-end) are via DTOs

## Data access

* Repository/Service layer is responsible for data access and processing.
* Azure Cosmos DB implementation is hidden from the repository/service layer.
  * The `id` property required by Cosmos DB is not used by consumers. Instead, a unique key is used which may also be used as the partition key. The `id` property is never sent to consumers.
  * Each Cosmos DB entity has the following properties:
    * `id`
    * `createdAt`
    * `updatedAt`
    * `schemaVersionNumber`
    * `type`
  * The Cosmos DB entity required property names in camel-case. The convention in .NET is to use Pascal case. This is accommodated using a configurationhilst establilshing the `CosmosClient.`

## Azure Functions

* Exceptions are bubbled up to the enry point of execution, usually the HTTP Trigger.
  * Logging is not performed within the repository/service layer, instead preferred at the entry point.
* Requests and Responses are strongly typed and utilise DTOs as a means of transferring data.

## Angular

