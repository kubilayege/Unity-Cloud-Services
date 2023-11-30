# Unity-Cloud-Services

Items were delivered to users from Unity Cloud COntent Delivery System.

Transactions had metadata for the icons.

Addressables were used to apply user side shop items.

User logging in was handled with Unity Authentication.

User balances were kept and updated with Unity Economy service.

When user wanted to make a transaction with their balance we called our cloud code module.

Cloud Module checked player balance to make sure the user had enough balance to accuire requested item. 

Cloud Module communicated with a database on Amazon where we kept the transaction items and retrieved said items through async operations.

The items were removed from database since a user made the purchase and user balance were updated.

The item was added to the user inventory with the metadata of real purchase item data.

User inventory is retrieved from Economy service and the metadata is shown to the user for easy access.

## 

Whole process is very secure and relatively fast. It was extremely fun and teaching to be able to work on this project. 
