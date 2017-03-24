# Discord.Net bot and Accompanying Web Application

*Work in Progress.*

This project was made to organize data, and provide Quality of Life features to help me better run my online community.

This repository contains the entire solution for my personal project.  The two main parts are:
1) A bot that gathers data and providers server members with helpful commands and tools.
2) A web app that provides moderators with an Admin Dashboard.  

TODO: In the future, I would like to expand the application so users can log in, view their profile, achievements, current games being played by server members, weekly events, etc.
___
**The first part of this project is a console application which serves as a bot, written using the Discord.Net 0.9 framework (which utilizes Discord's public API).**
- If you wish to use this bot, you will need to provide your own API key.
- Once invited to a server, the bot will track data (server growth, user retention, activity, etc).
- Because Discord does not natively track user activity (only how often users are online), this allows me to have a better understanding of who is using the server and how often.
- The bot utilizes Entity Framework to save all data to a SQL database.
- The bot also has various Quality of Life features such as *New User Guides*, a tool to help moderators plan movie nights, a command to display user statistics, and more.

Bot Commands:


| Command       | Value         | Description  |
| ------------- |:-------------:| -----|
| !update users         |                     |   Syncs up the local DB userlist with the remote Discord userlist (in case of discrepancies due to bot down-time) |
| !update active users  |                     |    Assigns an "active" role to users who have participated in the past week.  Removes "active" role from those who have not participated.  Displays total number of active users. |
| !users overview       | ["number of days"]  |    Displays new user info (retention, activity, etc) and existing user decay for the past N days |
| !movie info           |                     | Displays the movie event date/time and list of movie selections |
| !movie add            | ["movie name"]      |   Add a movie to the voting list |
| !movie remove         | ["movie name"]      |    Remove a movie from the voting list |
| !movie clear          |                     |    Clear all movies from the list |
| !movie time           | ["date/time"]       |  Update the time of the event |
| !help new user        |                     |    Displays a *New User Guide* |
| !help games           |                     |    Displays a guide on how to self-assign game-roles |
| !help gambling        |                     |    Displays a tutorial on using the server currency |
| !help events          |                     |    Displays info about weekly events |
___
*Note: For this personal project, I am just using the local SQL Server Developer's Edition that comes with Visual Studio 2015 (Community Edition).  I am also running the bot locally.
In the future, I hope to deploy the application to the cloud using Microsoft Azure.*
___
**The second part of this project is a web application built using the ASP.NET MVC 5 Framework.**
- This Web Application was built off of the stock MVC 5 blank template (with routing and the Identity module already configured).
- The database was created using the Entity Framework Code First approach (with model classes and a DbContext layer created first).
- There are currently two main areas in the Admin Dashboard: a page for searching and filtering users (with the help of LINQ queries), and a page for viewing server statistics.
- Some of the server statistics are displayed using third party JavaScript libraries (ChartJS and Highcharts).
___

**Sample Images:**

Example of what the Server Statistics page looks like:

![Server Statistics](http://i.imgur.com/2hvOcf9.png "Server Statistics")


Example of what the searchable User List looks like:

![User List](http://i.imgur.com/eGBpFGy.png "User List")

