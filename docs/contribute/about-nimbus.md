# About Nimbus — Not Applicable

The Apex original's contributor docs relied on [Nimbus](https://testnimbus.dev/),
a third-party local Apex runtime, for a fast no-org inner test loop, and this
page explained what it was and its limits.

**None of that applies to this port.** There is no Apex, no Salesforce
platform, and no org to simulate — `dotnet test` already runs the entire
suite locally in seconds with no external dependency at all. See
[local-development](local-development.md).
