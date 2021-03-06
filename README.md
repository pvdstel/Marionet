<div align="center">
    <img width="128" height="128" src="art/logo.png">
    <h1>Marionet</h1>
</div>

Marionet is a utility that allows you to remotely control other computers in your network. It enables you to move your mouse to other machines, and sends your keyboard input to the computer that you're controlling.

## Setting up

When launched for the first time, the application will generate its configuration file, and a pair of certificates that are used for authorization. The client and server certificates need to be the same for all desktops that you would like to include in the Marionet network, otherwise the instances will not be able to connect to one another.

The desktops in the network can be specified in the `desktops` field in the application settings. Add all the machines that should participate. The order of the names also determines how the virtual desktop will be constructed. The first desktop is on the far left, with the second to the right of it, etc. The last desktop will be on the far right.

It is possible to specify the location in the network using the `desktopAddresses` field. Simply specify a network location for any desktop in the `desktops` list.

To ensure that the application only runs in a trusted network, simply specify the run conditions in the `runConditions` field. Here you may specify the known SSIDs on which the application may run, or choose to simply block the application from running.

## Configuring Marionet

Marionet supports the following configuration options, which can be changed in the `config.json` file.

| Name | Type | Description |
|---|---|---|
| `blockTransferWhenButtonPressed` | `boolean` | Whether transferring control to another desktop is blocked when a button (keyboard or mouse) is currently being pressed. Defaults to true. |
| `clientCertificatePath` | `string` | The path to the client certificate file. |
| `desktopYOffsets` | `object` | A dictionary specifying the Y offset of each desktop, to align them vertically. Each field of the object must have a value of the `number` type, specifically integers. Defaults to 0 if not specified. |
| `desktopAddresses` | `object` | A map with desktop names as keys and a list of network addresses (strings) as values. Can be used to speed up connections. If a desktop is not specified, its name is used for the connection (and DNS resolution). |
| `desktops` | `string[]` | The list of known desktops. |
| `maxTransferDistance` | `number` | The maximum distance that the cursor may travel onto another desktop before it is allowed to transfer. Must be an integer number. No effect when set to 0. |
| `minTransferDistance` | `number` | The minimum distance that the cursor must travel onto another desktop before it is allowed to transfer. Must be an integer number. Defaults to 10. No effect when set to 0. |
| `runConditions` | `object` | A number of conditions that must be true for Marionet to run. The object has a number of fields: <ul><li>`blockAll` (`boolean`): blocks Marionet from running entirely.</li><li>`allowedSsids` (`string[]`): a whitelist of SSIDs that Marionet may run on.</li><li>`allowedNetworkInterfaces` (`string[]`): a whitelist of network interface names that Marionet may run on.</li></ul> |
| `self` | `string` | The name of the current node. Defaults to the machine name. |
| `serverCertificatePath` | `string` | The path to the server certificate file. |
| `showTrayIcon` | `boolean` | Determines whether Marionet UI displays a tray icon. |
| `stickyCornerSize` | `integer` | The size (in pixels) of sticky corners. Must be an integer. Defaults to 6. Disabled when set to 0. |

The options `maxTransferDistance` and `minTransferDistance` can be used to ensure that the cursor does not move to another desktop too easily, when moving slowly or when moving very fast. The run conditions can be used to ensure Marionet does not run on untrusted networks. `showTrayIcon` allows for opening the Marionet UI from the system tray if it has been closed. `desktopYOffsets` can be used to vertically align desktops.

## FAQ

### Why won't it work on some windows?

Windows does not allow sending input to privileged user interfaces, such as Task Manager, or the login UI. You could run this application as administrator, so it can send input to privileged userspace applications. Sending input to the login screen is not allowed for userspace applications.

### Why?

Just to see if I could.

### Where does the name come from?

The name is based on the Dutch word for "marionette".
