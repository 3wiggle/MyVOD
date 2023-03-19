My VOD (HomeBrew Video On Demand - Kinda)
=====
Understanding components... and how they SHOULD play together
-------

First there was Eric's Movie Database... Chosen many years ago when I was collecting VHS movies and wanted a catalog tool.  Acceptable for a number of reasons... not least of which is it's interface with on line movie data sources resulting in a great catalog searching tool.

Over time of course advents like DVD, Blue Ray and digital movie files... spawned a desire for browsing that would not need to build a second data repository. As with most of the currently popular streaming browsers.  Those that were reviewed not only limited the movie meta data but often failed to select the proper movie when dealing with remakes.  For example all the versions of *Cinderella*.

Being a coder it was not long before I began to peek into the raw movie data contain within the EMDB data file. 

NOTE: For my purposes it is not necessary to parse 100% of that data... 

The next step was to review the EMDB EXPORT to HTML feature... While useful, it soon became cumbersome when one reaches lots movies cataloged.  The EMDB Export creates static HTML pages for each movie... causing almost as much a storage space issue as the movies themselves.

So I wrote my own browser as a web application.  But unlike MOST this web application is not dependant upon a Web Server... it is it's own server... (think of this as the old school CGI programming pattern.)  

Using a component adopted in 2012 from Code Project we have the TCP/IP Socket platform for performing all necessary Web Server communications. (https://www.codeproject.com/Articles/12286/Simple-Client-server-Interactions-using-C)

This package also provides BASIC web client session, request and response processing... complete with headers, security, cookies and the like. 

This solution depends on HTML TEMPLATE files... these are derived form HTML files as produced by the EMDB HTML export content.  Each HTML file was parsed down into a template containing a number of PLACEHOLDERS... the placeholders are replaced with movie data as being sent out as client response.  

See index.htm, movie.htm and genres.htm for the base web site content templates. 

>>SlideShow is an add on and more of a fun feature to the solution.

SO we have a Movie Cataloging service the data of which is now reused in a web presentation layer ... but we still need STREAMING... 

Enter Mist Server (or is it Mist Controller)... both seem to be correct.

This application is an open source solution designed as a hosted media streaming service... and ALMOST perfect for our needs.  The shortcomings were easily circumvented inside the Web Application... 

Mist Controller depends on a textual STERAM NAME as a key into the proper media source desired.  There are some very specific rules about how to form this stream name.  

Streams MUST be pre-registered with Mist before making a stream request from a client web browser... 

>>SO.. this version of the Web Application, when launched will first build an internal array of EMDB movie data.
 
>>From that list it will then 'register' each and every movie with MIST...
Constructing a unique Stream Name from the movie title. 
>>(this needs to change into prefix tag and movie ID to prevent movie remake conflicts)

ONCE the Mist Server is running and loaded/updated with current movie stream list.. we open the Web Server listening port and await client session.. 

Once selected for streaming the client browser will request the file VOD.HTML with the proper movie ID in the query string... 

That page will allow the client browser to FIND the MistServer and attach to the desired stream by name...

THE SPECIAL SAUCE can be found in the Web Application CONFIG file....

>Where there is: 

>>The ConnectionString which points to the EMDB data file... 
>>>The directory of which is also used as path for imagery files (eliminating need for image file copies)
>>>**HOWEVER** you must ensure that the user account running the Web Application has read priveleges to that folder... 

>>The full path to the MistController executable... so that the web application can ensure Mist is running before it tries to register streams.

>>The movie port (poorly named setting for the web application listening port)

>>The Mist Streaming Port - used to compose the VOD.HTM content for client.

>>The MIST API endpoint - used to register streams 

>These settings are all used so that you may install the components as desired yet TIE them together for correct web application operation.


**INSTALLATION**

Matters not what comes first... so long as all three systems are in place and opperating correctly before launching and using the web application.

Mist Server: can acquired at - https://mistserver.org/download

Eric's Movie Database: https://www.emdb.eu/downloads.html

And of course this is the repository for the web application... 
 

For only the executable, simply unzip the WebSite ZIP file to location desired... And then launch the VODWebSite executable. 


We had considered making this CONSOLE application a Windows Service... have yet to implement logging ... making a service would lose the Console output and error messages.

To ensure application is always UP ... we use an older PC ... with only a single user and then set that user to Not Require Login... adding this application to the user startup completes the installation. 


  


 
 

    
 