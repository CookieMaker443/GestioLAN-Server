## Items endpoint:
- *GET* */api/Items* [Authorize]
	- [FromQuery] bool? has_category
    - [FromQuery] int? id_category
    - [FromQuery] string? name
    - [FromQuery] bool? has_image
    - [FromQuery] int? quantity
    - [FromQuery] string? type_quantity
- *GET* */api/Items/{id}* [Authorize]
    - [FromURL] int id
- *POST* */api/Items/CreateItem* [Authorize]
    - [FromBody] Item item
- *DELETE* */api/Items/DeleteItem/{id}* [Authorize]
    - [FromURL] int id
- *PUT* */api/Items/ModifyItem/{id}* [Authorize]
    - [FromQuery] int id
    - [FromBody] Item item


## User endpoints:

*eliminato*
- *GET* */api/Users/AllUsers* [Authorize]

- *POST* */api/Users/Login*
    - [FromBody] User loginUserData
- *POST* */api/Users/Register*
    - [FromBody] User user
- *DELETE* */api/Users/DeleteUser* [Authorize(Policy = "AdminOnly")]
    - [FromQuery] string username
- *PUT* */api/Users/{targetUsername}* [Authorize]
    - [FromURL] string targetUsername
    - [FromBody] user newUser
- *GET* */api/Users/image/{username}* [Authorize]
    - [FromURL] string username
- *POST* */api/Users/image/{username}* [Authorize]
    - [FromURL] string username
    - [FromBody] IFormFile file


## Category endpoints:

- *GET* */api/Category/AllCategories* [Authorize]
- *POST* */api/Category/AddCategory* [Authorize(Policy = "AdminOnly")]
    - [FromBody] string nome
- *PUT* */api/Category/UpdateCategory/{id* [Authorize(Policy = "AdminOnly")]
    - [FromURL] int id
    - [FromBody] Category category
- *DELETE* */api/Category/DeleteCategory/{id}* [Authorize(Policy = "AdminOnly")]
    - [FromURL] int id


## Images endpoints:

- *GET* */api/Images/AllImagesInfo* [Authorize] 
- *GET* */api/Images/ImageName/{itemImageName}* [Authorize]
    - [FromURL] string itemImageName
- *GET* */api/Images/IdImage/{idImage}* [Authorize]
    - [FromURL] int idImage
- *GET* */api/Images/ItemsCount/{qty}* [Authorize] 
    - [FromURL] int qty;
- *POST* */api/Images/CreateImage* [Authorize]
    - [FromQuery] string? itemName
    - [FromBody] IFormFile file
- *PUT* */api/Images/UpdateImage/{id}* [Authorize]
    - [FromURL] int id
    - [FromQuery] string? itemName
    - [FromBody] IFormFile file
- *PUT* */api/Images/RenameImage/{id}* [Authorize]
    - [FromURL] int id
    - [FromQuery] string? itemName
- *DELETE* */api/Images/DeleteImage/{id}* [Authorize(Policy = "AdminOnly")]
    - [FromURL] int id