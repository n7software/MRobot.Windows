# MRobot.Windows
A Windows desktop client for Multiplayer Robot, the asynchronous multiplayer tool for Sid Meier's Civilization V and Beyond Earth.

![Multiplayer Robot main client window](https://raw.githubusercontent.com/n7software/MRobot.Windows/master/readme/app-main-bar.png)


### Looking to download the app?

You can download and install the latest version of any of the Multiplayer Robot desktop clients here: https://new.multiplayerrobot.com/#Apps

### Overview

The Multiplayer Robot Windows desktop client is designed to make playing game turns from [Multiplayer Robot](https://new.multiplayerrobot.com) convenient and easy. The main application window follows a minimalist design and mirrors the elements found on the website. It automates all the steps of downloading Civilization save files from the web service, placing it in the necessary local directory, launching the appropriate Civilization game, detecting when the new save for that game has been generated, and uploading that save back to the web service to complete the turn. The desktop client also provides a task tray icon for quick access and makes use toast notifications to notify the user of several events and allow them to quickly respond to new turns.

### Looking for the Mac and Linux desktop clients?

We've created some projects here on Github for the equivelant [Mac](/n7software/MRobot.Mac) and [Linux](/n7software/MRobot.Linux) desktop clients. Lacking the necessary experience, we haven't actually started these projects yet but are looking for some talented Multiplayer Robot community members to help us get them off the ground! If you'd like to get involved please visit our [Steam Discussion on these projects.](http://steamcommunity.com/groups/multiplayerrobot/discussions/2/618457398960463952/)

### Want to get involved?

Like the rest of the Multiplayer Robot service this desktop application is always a work in progress and we encourage and appreciate any and all contributions. We've released this code under the [MIT License](/n7software/MRobot.Windows/blob/master/LICENSE) so feel free to download it and modify it to your hearts content. If you feel up to making your own improvements and fixes please send us a pull request and we'll review adding it into this repository and publishing it in an update to the thousands that use this application every day.

### What technologies does this use?

This app is built using C# and WPF, targeting the .Net Framework 4.6. The best way to compile the code is by using the free [Visual Studio Community edition.](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx) It also makes use the [ASP.Net SignalR](http://www.asp.net/signalr) library for almost all communications with the Multiplayer Robot web service.