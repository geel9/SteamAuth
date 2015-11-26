# SteamAuth
A C# library that provides vital Steam Mobile Authenticator functionality. **Looking for a desktop client to act as a Steam Mobile Authenticator? [Check out SteamDesktopAuthenticator](https://github.com/Jessecar96/SteamDesktopAuthenticator)**

# Functionality
Currently, this library can:

* Generate login codes for a given Shared Secret
* Login to a user account
* Link and activate a new mobile authenticator to a user account after logging in

In essence, it can be used to add a mobile authenticator to an account, and then be used to generate login codes after doing so.

# Usage
To generate login codes if you already have a Shared Secret, simply instantiate a `SteamGuardAccount` and set its `SharedSecret`. Then call `SteamGuardAccount.GenerateSteamGuardCode()`.

To add a mobile authenticator to a user, instantiate a `UserLogin` instance which will allow you to login to the account. After logging in, instantiate an `AuthenticatorLinker` and use `AuthenticatorLinker.AddAuthenticator()` and `AuthenticatorLinker.FinalizeAddAuthenticator()` to link a new authenticator. **After calling AddAuthenticator(), and before calling FinalizeAddAuthenticator(), please save a JSON string of the `AuthenticatorLinker.LinkedAccount`. This will contain everything you need to generate subsequent codes. Failing to do this will lock you out of your account.**

#Upcoming Features
In order to be feature complete, this library will:

* Be able to remove an authenticator from an account
* Be able to fetch a list of pending confirmations, and confirm them individually
* Be better documented (feature!!)


