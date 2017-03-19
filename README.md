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

Example of a bot command that goes through the list of users and assigns a role of "Active" to all users who have participated in the chat in the past week:

![Active Users](http://i.imgur.com/7BoeJer.png "Active Users")

Example of a bot command that provides a basic new user overview for the past N days:

![New Users Overview](http://i.imgur.com/LEUMvPt.png "New Users Overview")

Example of a group of bot commands that take input to update information about an event which can then be displayed to users:

![Movie Night](http://i.imgur.com/o5szYru.png "Movie Night")
