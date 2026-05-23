## Software Architecture

The database (in SQL) communicates with the API (in C#, using AspNetCore and EntityFramework). The idea is that they reside on a server within the LAN (a Raspberry Pi, a dedicated server, or an old laptop).

Clients will connect to the home network and will be able to communicate with the database and receive the information contained within it!

#### How does it work?

- The client sends requests to the backend server.
- The backend (written in C# with AspNetCore) executes the logic to query the database using Entity Framework.
- Entity Framework abstracts the DB, making it queryable via code by mapping it into Models:
    - The context, which describes the database.
    - The other classes, each representing a table in the DB.
- Pomelo is responsible for translating the queries from C# code to SQL.