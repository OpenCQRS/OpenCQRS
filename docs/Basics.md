# Basics

The **IDispatcher** interface contains all the methods needed to send commands, publish events and get results from queries.

There are three kinds of requests that can be sent through the dispatcher:
- [Commands](Commands) (single handler)
- [Queries](Queries) (single handler)
- [Events](Events) (multiple handlers)

OpenCQRS uses the result pattern to return the result of commands and queries. The result contains information about the success or failure of the operation, any errors that occurred, and the data returned by the operation.
