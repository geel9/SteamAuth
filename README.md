# SteamAuth
A C# library that provides vital Steam Mobile Authenticator functionality. **Looking for a desktop client to act as a Steam Mobile Authenticator? [Check out SteamDesktopAuthenticator](https://github.com/Jessecar96/SteamDesktopAuthenticator)**

# Functionality
Currently, this library can:

* Generate login codes for a given Shared Secret
* Login to a user account
* Link and activate a new mobile authenticator to a user account after logging in
* Remove itself from an account
* Fetch, accept, and deny mobile confirmations

# Requirements

* [Newtonsoft.Json](http://www.newtonsoft.com/json)

# Usage
To generate login codes if you already have a Shared Secret, simply instantiate a `SteamGuardAccount` and set its `SharedSecret`. Then call `SteamGuardAccount.GenerateSteamGuardCode()`.

To add a mobile authenticator to a user, instantiate a `UserLogin` instance which will allow you to login to the account. After logging in, instantiate an `AuthenticatorLinker` and use `AuthenticatorLinker.AddAuthenticator()` and `AuthenticatorLinker.FinalizeAddAuthenticator()` to link a new authenticator. **After calling AddAuthenticator(), and before calling FinalizeAddAuthenticator(), please save a JSON string of the `AuthenticatorLinker.LinkedAccount`. This will contain everything you need to generate subsequent codes. Failing to do this will lock you out of your account.**

To fetch mobile confirmations, call `SteamGuardAccount.FetchConfirmations()`. You can then call `SteamGuardAccount.AcceptConfirmation` and `SteamGuardAccount.DenyConfirmation`.

# Upcoming Features
In order to be feature complete, this library will:

* Be better documented (feature!!)


