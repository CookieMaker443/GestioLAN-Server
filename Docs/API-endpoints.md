Items endpoint:
- *GET* */api/Items*
    - [FromQuery] int[] ids_category
    - [FromQuery] string? name
    - [FromQuery] int? quantity
    - [FromQuery] string? type_quantity
- *GET* */api/Items/{id}*
    - [FromURL] int id
- *POST* */api/Items*
    - [FromQuery] string name
    - [FromQuery] string? description
    - [FromQuery] string? image
    - [FromQuery] int id_category
    - [FromQuery] int quantity
    - [FromQuery] string type_quantity
- *DELETE* */api/Items/{id}*
    - [FromURL] int id
- *PUT* */api/Items/{id}*
    - [FromQuery] int id
    - [FromQuery] string name
    - [FromQuery] string description
    - [FromQuery] string image
    - [FromQuery] int id_category
    - [FromQuery] int quantity
    - [FromQuery] string type_quantity
    - [FromBody] Item updatedItem

Da sistemare questi endpoint


## User endpoints:

- *GET* */api/Users/AllUsers*
- *POST* */api/Users/Login*
    - [FromBody] User loginUserData
- *POST* */api/Users/Register*
    - [FromBody] User user
- *DELETE* */api/Users/DeleteUser*
    - [FromQuery] string username
- *PUT* */api/Users/{targetUsername}*
    - [FromURL] string targetUsername
    - [FromBody] user newUser
- *GET* */image/{username}*
    - [FromURL] string username


## Category endpoints:

- *GET* */api/Category/AllCategories*
- *POST* */api/Category/AddCategory* [Authorize(Policy = "AdminOnly")]
    - [FromBody] string nomeù
- *PUT* */api/Category/UpdateCategory/{id* [Authorize(Policy = "AdminOnly")]
    - [FromURL] int id
    - [FromBody] Category category
- *DELETE* */api/Category/DeleteCategory/{id}* [Authorize(Policy = "AdminOnly")]
    - [FromURL] int id
