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

## Why?

Just to see if I could.

## Name origins

The name is based on the Dutch word for "marionette".
