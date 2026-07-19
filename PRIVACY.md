# Privacy Policy

Last updated: 2026-07-19

TableCloth MCP (`tablecloth-mcp` on npm, `TableCloth.Mcp` on NuGet) is a local MCP server that
runs on the user's own machine. This policy describes how it handles data.

## Data collection

The server does not collect, store, or transmit any personal data, conversation content, or usage
telemetry. It has no analytics, no accounts, and no tracking.

## Network usage

The server makes outbound HTTPS requests only to public, read-only sources, and sends no user data
with them:

- The public service catalog at `https://yourtablecloth.app/TableClothCatalog/Catalog.xml`, and
  icon images under the same host.
- GitHub Release assets under `https://github.com/yourtablecloth/TableClothMcp/releases/`, fetched
  only when a sandbox is launched (the sandbox downloads the launcher and portable app).

No credentials, personal information, or conversation data are sent to these or any other endpoint.

## Local actions

The `launch_sandbox` tool starts Windows Sandbox (or macSandbox) on the user's own machine and
writes a temporary `.wsb` configuration file to the system temporary directory. Nothing is uploaded.
Login and authentication on the opened websites are performed by the user directly, inside the
sandbox; the server never sees or handles those credentials.

## Storage and retention

The server is stateless. It keeps only an in-memory cache of the public catalog for the lifetime of
its process and retains no data after it exits. Temporary `.wsb` files remain in the system temp
directory until the operating system or the user removes them.

## Third-party sharing

None. No data is shared with any third party.

## Children

The service is not directed to children and collects no data from anyone.

## Changes

Updates to this policy are published in this file in the project repository.

## Contact

Jung Hyun, Nam. Email: rkttu at rkttu dot com.
